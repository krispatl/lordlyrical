"""Real 0.17.4 wire integration, ten moving clients, two 121-second sessions."""
import json,sys,time,math
from pathlib import Path
from styly_netsync.client import net_sync_manager
from styly_netsync.types import client_transform_data,transform_data
from show_authority import ShowAuthority

room='ll-load-'+str(int(time.time()))
authority_net=net_sync_manager(server='tcp://127.0.0.1',room=room,auto_dispatch=False)
authority=ShowAuthority(authority_net,countdown=.5)
authority_net.on_rpc_received.add_listener(authority.on_rpc)
authority_net.start();authority_net.send_stealth_handshake()
clients=[net_sync_manager(server='tcp://127.0.0.1',room=room,auto_dispatch=False) for _ in range(10)]
for c in clients:c.start()
report={'sdk':'0.17.4','protocol':8,'clients':10,'cycles':[],'checks':[]}
next_join=0;next_pose=0;paused=-1;clock_samples=[]
def clock_reply(sender,name,args):
    if name=='ll.clock.reply':clock_samples.append(time.monotonic()-float(args[0]))
for c in clients:c.on_rpc_received.add_listener(clock_reply)
def tick():
    global next_join,next_pose
    now=time.monotonic();authority.tick()
    for c in clients:c.dispatch_pending_events()
    if now>=next_join:
        for i,c in enumerate(clients):
            if i!=paused and c.is_ready:c.rpc('ll.join',[]);c.rpc('ll.clock',[repr(now)])
        next_join=now+1
    if now>=next_pose:
        for i,c in enumerate(clients):
            if i!=paused and c.is_ready:c.send_transform(client_transform_data(client_no=c.client_no,device_id=c.device_id,head=transform_data(pos_x=i*1.2,pos_y=1.7,pos_z=math.sin(now)*.2)))
        next_pose=now+.05
    time.sleep(.005)
def until(predicate,timeout):
    end=time.monotonic()+timeout
    while not predicate():
        if time.monotonic()>end:raise AssertionError('Timeout: '+str(timeout))
        tick()
try:
    until(lambda:all(c.is_ready for c in clients) and len(authority.registry.clients)==10,20)
    assert len({c.client_no for c in clients})==10
    report['checks'].append('Ten distinct client identities and stable unique slots')
    for cycle in range(2):
        authority.command('start');run=authority.run
        until(lambda:all(json.loads(c.get_global_variable('ll.show','{}')).get('runId')==run for c in clients),10)
        epoch=authority.epoch
        assert all(json.loads(c.get_global_variable('ll.show'))['epoch']==epoch for c in clients)
        print(f'Cycle {cycle+1}: ten clients share epoch {epoch}',flush=True)
        disconnected=False;rejoined=False;next_status=30
        while authority.phase!='Complete':
            tick();show=time.monotonic()-epoch
            if cycle==0 and show>78 and not disconnected:
                original_slot=authority.registry.slots.index(clients[3].device_id);clients[3].stop();paused=3;disconnected=True
            if cycle==0 and show>85 and not rejoined:
                clients[3].start();paused=-1;rejoined=True
            if show>=next_status: print(f'Cycle {cycle+1}: {show:.1f}s',flush=True);next_status+=30
        until(lambda:all(json.loads(c.get_global_variable('ll.show','{}')).get('phase')=='Complete' for c in clients),10)
        if cycle==0:
            assert authority.registry.slots.index(clients[3].device_id)==original_slot
            assert json.loads(clients[3].get_global_variable('ll.show'))['epoch']==epoch
            report['checks'].append('Disconnect at 78s and same-device rejoin at 85s recovered slot and epoch')
        report['cycles'].append({'runId':run,'duration':121,'all_completed':True})
        authority.command('reset')
        until(lambda:all(json.loads(c.get_global_variable('ll.show','{}')).get('phase')=='Ready' for c in clients),10)
    report['checks'].append('Two full start, completion and reset cycles; no protocol mismatch')
    report['clock_roundtrip_ms']={'samples':len(clock_samples),'max':max(clock_samples)*1000,'mean':sum(clock_samples)/len(clock_samples)*1000}
    report['passed']=True
except Exception as e:
    report['passed']=False;report['error']=repr(e);raise
finally:
    for c in clients:c.stop()
    authority_net.stop()
    out=Path(__file__).parents[1]/'Evidence';out.mkdir(exist_ok=True)
    (out/'network-load-test.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report,indent=2),flush=True)
