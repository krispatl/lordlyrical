"""Run two real-time sessions against a running Unity client and local authority."""
import argparse,json, time, urllib.request, statistics,subprocess
from pathlib import Path
URL='http://127.0.0.1:8810'
def status():
    with urllib.request.urlopen(URL+'/status',timeout=4) as r: return json.load(r)
def command(command,value=None):
    data=json.dumps(dict(command=command,value=value)).encode()
    with urllib.request.urlopen(urllib.request.Request(URL+'/command',data,{'Content-Type':'application/json'}),timeout=5) as r:
        result=json.load(r); assert result['ok'],result
parser=argparse.ArgumentParser();parser.add_argument('--player',type=Path);args=parser.parse_args();player=None
def launch_player():
    startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
    return subprocess.Popen([str(args.player.resolve()),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','--capture-evidence','-logFile',str(Path(__file__).resolve().parents[1]/'Evidence'/'desktop-player.log')],startupinfo=startup)
report={'kind':('Built desktop player, explicit URP render each frame at 1280x720' if args.player else 'Unity Editor')+' actual playback, desktop GPU; no physical XR headset','cycles':[]}
command('reset')
command('mode','Adaptive')
if args.player:
    command('release_absent');player=launch_player()
deadline=time.monotonic()+60
while time.monotonic()<deadline:
    s=status()
    if any(p['connected'] for p in s['participants']) and any(v.get('phase')=='Ready' for v in s['telemetry'].values()): break
    time.sleep(.5)
else: raise RuntimeError('Unity client not ready')
for cycle in range(2):
    original=[p for p in status()['participants'] if p['connected']][0]
    command('start'); samples=[]; began=time.monotonic(); last=-1;disconnected=False;rejoined=False
    while time.monotonic()-began<140:
        s=status()
        if args.player and cycle==0 and s['time']>=78 and not disconnected:
            player.terminate();player.wait(timeout=10);disconnected=True
        if args.player and cycle==0 and s['time']>=85 and not rejoined:
            player=launch_player();rejoined=True
        for p in s['participants']:
            if not p['connected']:continue
            v=s['telemetry'].get(str(p['clientNo']))
            if v and v.get('received')!=last:
                samples.append(v);last=v['received']
        if s['phase']=='Complete' and any(v['phase']=='Complete' for v in s['telemetry'].values()):break
        time.sleep(.5)
    else: raise RuntimeError('Show did not complete')
    assert all(not v.get('error') for v in samples),samples
    if args.player and cycle==0:
        recovered=next(p for p in s['participants'] if p['deviceId']==original['deviceId'])
        assert recovered['connected'] and recovered['slot']==original['slot'],'Player reconnect did not preserve identity/slot'
        assert any(85<v['time']<94 and v['phase']=='Running' for v in samples),'Player did not reconstruct at late-join time'
        report['reconnect']='Player process terminated at 78 s and relaunched at 85 s; device/slot retained and playback reconstructed'
    active=[v for v in samples if v['phase'] in ('Running','Ending')]
    assert len(active)>30,'Insufficient live telemetry'
    result={'cycle':cycle+1,'duration':s['duration'],'runId':s['runId'],'samples':samples,
            'fps_mean':statistics.mean(v['fps'] for v in active),'fps_min':min(v['fps'] for v in active),
            'drift_abs_max_ms':max(abs(v['driftMs']) for v in active),
            'memory_min_mb':min(v['memoryMb'] for v in active),'memory_max_mb':max(v['memoryMb'] for v in active)}
    report['cycles'].append(result);print(json.dumps({k:v for k,v in result.items() if k!='samples'}),flush=True)
    command('reset');time.sleep(3)
    assert any(v['phase']=='Ready' and v['time']==0 for v in status()['telemetry'].values()),'Reset failed'
report['passed']=True
out=Path(__file__).resolve().parents[1]/'Evidence'/('desktop-live-sessions.json' if args.player else 'unity-live-sessions.json');out.write_text(json.dumps(report,indent=2))
print('PASS two complete Unity playback and reset cycles',flush=True)
if player:player.terminate();player.wait(timeout=10)
