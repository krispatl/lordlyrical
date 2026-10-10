"""Local operator launcher. No external services or internet dependency."""
from pathlib import Path
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json, subprocess, urllib.request, urllib.error, threading, time, webbrowser, secrets, sys
ROOT=Path(__file__).resolve().parent.parent
PORT=8811
TOKEN=secrets.token_urlsafe(32)
LOCK=threading.Lock()
ADB=Path('C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe')
def request(path, data=None):
    req=urllib.request.Request('http://127.0.0.1:8810'+path,data=None if data is None else json.dumps(data).encode(),headers={'Content-Type':'application/json'})
    try:
        with urllib.request.urlopen(req,timeout=5) as r:return json.load(r)
    except urllib.error.HTTPError as e:return json.load(e)
def start_server():
    try:
        s=request('/status')
        if s.get('sdk')=='0.17.4' and s.get('connected'):return
    except (OSError,ValueError):pass
    r=subprocess.run(['powershell.exe','-NoProfile','-ExecutionPolicy','Bypass','-File',str(ROOT/'Tools/Launch.ps1'),'-Mode','Server','-NoBrowser'],cwd=ROOT,capture_output=True,text=True,timeout=60,creationflags=subprocess.CREATE_NO_WINDOW)
    if r.returncode:raise RuntimeError('Server could not start. '+(r.stderr or r.stdout)[-900:])
def launch_headsets():
    if not ADB.exists():return 'Open Lord Lyrical in the headset library. Remote app launch is unavailable on this PC.'
    r=subprocess.run([str(ADB),'devices'],capture_output=True,text=True,timeout=10,creationflags=subprocess.CREATE_NO_WINDOW)
    devices=[l.split()[0] for l in r.stdout.splitlines()[1:] if len(l.split())>=2 and l.split()[1]=='device']
    launched=0
    for device in devices:
        p=subprocess.run([str(ADB),'-s',device,'shell','pm','path','com.fifthyang.lordlyrical'],capture_output=True,text=True,timeout=10,creationflags=subprocess.CREATE_NO_WINDOW)
        if not p.stdout.startswith('package:'):continue
        p=subprocess.run([str(ADB),'-s',device,'shell','am','start','-n','com.fifthyang.lordlyrical/com.unity3d.player.UnityPlayerGameActivity'],capture_output=True,text=True,timeout=15,creationflags=subprocess.CREATE_NO_WINDOW)
        if p.returncode==0 and 'Error' not in p.stdout+p.stderr:launched+=1
    return f'Opened the app on {launched} connected headset(s).' if launched else 'Open Lord Lyrical in the PICO library. Wireless show control works once it connects. Remote app opening requires USB or paired wireless debugging.'
def checked_status():
    s=request('/status');now=time.monotonic()
    for t in s.get('telemetry',{}).values():t['age']=max(0,now-t.get('received',0))
    return s
def ready(s):
    people=[p for p in s.get('participants',[]) if p.get('connected')]
    if not s.get('connected') or not people:return False
    for p in people:
        t=s.get('telemetry',{}).get(str(p['clientNo']),{})
        if not p.get('aligned') or t.get('age',999)>6 or not t.get('applicationFocused'):return False
    return s.get('mode')!='FullCast' or len(people)==10
def command(name):
    if name=='prepare':start_server();return {'ok':True,'message':launch_headsets()}
    if name not in ('start','restart','stop'):raise ValueError('Unknown action')
    s=checked_status()
    if name in ('start','restart'):
        if not ready(s):return {'ok':False,'error':'Wake and align every connected headset before starting. Wait for fresh headset status.'}
        if name=='restart':
            r=request('/command',{'command':'reset'})
            if not r.get('ok'):return r
        return request('/command',{'command':'start'})
    return request('/command',{'command':'reset'})
class Handler(BaseHTTPRequestHandler):
    def log_message(self,*args):pass
    def send(self,body,mime='application/json',status=200):
        self.send_response(status);self.send_header('Content-Type',mime);self.send_header('Cache-Control','no-store');self.end_headers();self.wfile.write(body)
    def do_GET(self):
        if self.path=='/health':return self.send(b'{"launcher":"LordLyrical27"}')
        if self.path=='/status':
            try:return self.send(json.dumps(checked_status()).encode())
            except Exception:return self.send(b'{"connected":false,"participants":[],"telemetry":{},"phase":"Server offline"}')
        if self.path in ('/','/index.html'):
            return self.send((ROOT/'Tools/launcher.html').read_text(encoding='utf-8').replace('__TOKEN__',TOKEN).encode(),'text/html; charset=utf-8')
        self.send(b'{}',status=404)
    def do_POST(self):
        if self.path!='/command' or self.headers.get('X-Launcher-Token')!=TOKEN:return self.send(b'{}',status=403)
        if self.headers.get('Origin') not in (f'http://127.0.0.1:{PORT}',f'http://localhost:{PORT}'):return self.send(b'{}',status=403)
        if not LOCK.acquire(blocking=False):return self.send(b'{"ok":false,"error":"Another action is still running."}',status=409)
        try:
            length=int(self.headers.get('Content-Length','0'))
            if not 0<length<512:raise ValueError('Invalid request')
            result=command(json.loads(self.rfile.read(length))['command'])
            self.send(json.dumps(result).encode())
        except Exception as e:self.send(json.dumps({'ok':False,'error':str(e)}).encode())
        finally:LOCK.release()
def main():
    try:
        with urllib.request.urlopen(f'http://127.0.0.1:{PORT}/health',timeout=1) as r:
            if json.load(r).get('launcher')=='LordLyrical27':
                webbrowser.open(f'http://127.0.0.1:{PORT}');return
    except (OSError,ValueError):pass
    server=ThreadingHTTPServer(('127.0.0.1',PORT),Handler)
    webbrowser.open(f'http://127.0.0.1:{PORT}')
    def boot():
        try:start_server()
        except Exception as e:(ROOT/'Logs/launcher-error.txt').write_text(str(e))
    threading.Thread(target=boot,daemon=True).start()
    server.serve_forever()
if __name__=='__main__':main()
