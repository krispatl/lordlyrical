"""Venue authority: NetSync 0.17.4 RPC clock, stable slots and atomic show state.
The operator HTTP UI is loopback-only. All headset traffic uses NetSync.
"""
from __future__ import annotations
import argparse
import json
import math
import queue
import threading
import time
import uuid
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from styly_netsync.client import net_sync_manager

KEYS = ['D', 'C', 'E', 'G', 'F', 'B', 'C#', 'F#', 'Bb', 'A']
DURATION = 121.0

class Registry:
    def __init__(self):
        self.slots = [None] * 10
        self.seen = {}
        self.clients = {}

    def join(self, device, client, now):
        if not device or len(device) > 256:
            raise ValueError('Invalid device identity')
        if device in self.slots:
            slot = self.slots.index(device)
        elif None in self.slots:
            slot = self.slots.index(None)
            self.slots[slot] = device
        else:
            return -1
        self.seen[device] = now
        self.clients[device] = client
        return slot

    def records(self, now):
        return [dict(slot=i, deviceId=d or '', clientNo=self.clients.get(d, 0),
                     key=KEYS[i], connected=bool(d and now-self.seen.get(d, -999) < 6))
                for i, d in enumerate(self.slots)]

    def clear_absent(self, now):
        for i, d in enumerate(self.slots):
            if d and now-self.seen.get(d, -999) >= 6:
                self.slots[i] = None

class ShowAuthority:
    def __init__(self, manager, mode='Adaptive', countdown=5.0):
        self.net = manager
        self.registry = Registry()
        self.boot = str(uuid.uuid4())
        self.revision = 0
        self.run = 0
        self.phase = 'Ready'
        self.mode = mode
        self.countdown = countdown
        self.epoch = 0.0
        self.commands = queue.Queue()
        self.telemetry = {}
        self.last_publish = -999.0
        self.last_roster = ''
        self.error = ''

    def snapshot(self):
        return dict(bootId=self.boot, revision=self.revision, runId=self.run,
                    phase=self.phase, epoch=self.epoch, duration=DURATION, mode=self.mode,
                    authorityClientNo=self.net.client_no or 0, protocol=8, sdk='0.17.4')

    def publish(self):
        self.revision += 1
        value=json.dumps(self.snapshot(), separators=(',', ':'))
        self.net.set_global_variable('ll.show', value)
        self.net.rpc('ll.state', [value])
        self.last_publish = time.monotonic()

    def on_rpc(self, sender, function, args):
        now=time.monotonic()
        if function == 'll.clock' and len(args) == 1:
            self.net.rpc('ll.clock.reply', [args[0], repr(now), repr(time.monotonic()), self.boot], [sender])
        elif function == 'll.join':
            # Identity comes from the server mapping, never from an arbitrary name.
            device=self.net.get_device_id_from_client_no(sender)
            if device:
                slot=self.registry.join(device, sender, now)
                self.net.set_client_variable(sender, 'll.slot', str(slot))
                self.net.rpc('ll.assignment', [str(slot), self.boot], [sender])
                if slot < 0:
                    self.net.rpc('ll.error', ['All ten slots reserved; reset and release absent devices.'], [sender])
        elif function == 'll.telemetry' and len(args)==1:
            try:
                item=json.loads(args[0])
                self.telemetry[sender] = dict(item, received=now)
            except (ValueError, TypeError):
                pass

    def command(self, command, value=None):
        now=time.monotonic()
        count=sum(r['connected'] for r in self.registry.records(now))
        if command in ('start','force_start'):
            if self.phase not in ('Ready','Complete'):
                raise ValueError('Reset before starting an active show')
            if self.mode=='FullCast' and count!=10:
                raise ValueError('Full Cast requires ten connected physical participants')
            if count<1:
                raise ValueError('At least one participant is required')
            self.run+=1
            self.epoch=now+self.countdown
            self.phase='Countdown'
        elif command=='reset':
            self.phase='Ready'
            self.epoch=0.0
            self.run+=1
        elif command=='release_absent':
            if self.phase not in ('Ready','Complete'):
                raise ValueError('Cannot release slots during a show')
            self.registry.clear_absent(now)
        elif command=='mode':
            if self.phase not in ('Ready','Complete') or value not in ('Adaptive','FullCast'):
                raise ValueError('Mode can change only while Ready or Complete')
            self.mode=value
        elif command=='seek':
            t=float(value)
            if not math.isfinite(t) or not 0<=t<=DURATION:
                raise ValueError('Seek must be between 0 and 121')
            self.run+=1
            self.epoch=now-t
            self.phase='Running' if t<DURATION else 'Complete'
        else:
            raise ValueError('Unknown command')
        self.error=''
        self.publish()

    def tick(self):
        now=time.monotonic()
        self.net.dispatch_pending_events()
        while not self.commands.empty():
            command,value,done=self.commands.get_nowait()
            try:
                self.command(command,value)
                done.put({'ok':True})
            except (ValueError,TypeError) as exc:
                self.error=str(exc)
                done.put({'ok':False,'error':str(exc)})
        if self.phase=='Countdown' and now>=self.epoch:
            self.phase='Running'
            self.publish()
        if self.phase=='Running' and now-self.epoch>=DURATION:
            self.phase='Complete'
            self.publish()
        roster=json.dumps({'participants':self.registry.records(now)},separators=(',',':'))
        if roster!=self.last_roster or now-self.last_publish>2:
            self.net.set_global_variable('ll.roster',roster)
            self.last_roster=roster
        if now-self.last_publish>2:
            self.publish()

    def status(self):
        now=time.monotonic()
        return dict(self.snapshot(), connected=self.net.is_ready, room=self.net.room_id,
                    time=max(0,min(DURATION,now-self.epoch)) if self.epoch else 0,
                    countdown=max(0,self.epoch-now), participants=self.registry.records(now),
                    telemetry=self.telemetry, error=self.error)

def serve_operator(authority, port):
    class Handler(BaseHTTPRequestHandler):
        def log_message(self,*args): pass
        def do_GET(self):
            if self.path=='/status':
                body=json.dumps(authority.status()).encode(); mime='application/json'
            else:
                body=Path(__file__).with_name('operator.html').read_bytes(); mime='text/html; charset=utf-8'
            self.send_response(200); self.send_header('Content-Type',mime)
            self.send_header('Cache-Control','no-store'); self.end_headers(); self.wfile.write(body)
        def do_POST(self):
            if self.path!='/command': self.send_error(404); return
            # Reject cross-origin browser commands even though the listener is local.
            origin=self.headers.get('Origin')
            if origin and origin not in (f'http://127.0.0.1:{port}',f'http://localhost:{port}'):
                self.send_error(403); return
            try:
                length=int(self.headers.get('Content-Length','0'))
                if not 0<length<4096: raise ValueError('Invalid length')
                data=json.loads(self.rfile.read(length))
                done=queue.Queue(); authority.commands.put((data['command'],data.get('value'),done))
                result=done.get(timeout=4)
            except (ValueError,KeyError,queue.Empty):
                result={'ok':False,'error':'Invalid command or authority unavailable'}
            self.send_response(200 if result['ok'] else 409)
            self.send_header('Content-Type','application/json'); self.end_headers()
            self.wfile.write(json.dumps(result).encode())
    httpd=ThreadingHTTPServer(('127.0.0.1',port),Handler)
    threading.Thread(target=httpd.serve_forever,daemon=True).start()
    return httpd

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--server',default='127.0.0.1')
    parser.add_argument('--room',default='lord-lyrical')
    parser.add_argument('--operator-port',type=int,default=8810)
    parser.add_argument('--mode',choices=['Adaptive','FullCast'],default='Adaptive')
    parser.add_argument('--countdown',type=float,default=5.0)
    args=parser.parse_args()
    if not math.isfinite(args.countdown) or not 1<=args.countdown<=60:
        parser.error('--countdown must be between 1 and 60 seconds')
    net=net_sync_manager(server='tcp://'+args.server,room=args.room,auto_dispatch=False)
    authority=ShowAuthority(net,args.mode,args.countdown)
    net.on_rpc_received.add_listener(authority.on_rpc)
    net.start(); net.send_stealth_handshake()
    httpd=serve_operator(authority,args.operator_port)
    print(f'Operator: http://127.0.0.1:{args.operator_port}',flush=True)
    try:
        while True:
            if net.is_ready: authority.tick()
            time.sleep(.005)
    except KeyboardInterrupt: pass
    finally: httpd.shutdown(); net.stop()

if __name__=='__main__': main()
