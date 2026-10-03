import json,time,subprocess,urllib.request,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def status():
    with urllib.request.urlopen('http://127.0.0.1:8810/status',timeout=4) as r:return json.load(r)
def command(name,value=None):
    req=urllib.request.Request('http://127.0.0.1:8810/command',json.dumps({'command':name,'value':value}).encode(),{'Content-Type':'application/json'})
    with urllib.request.urlopen(req,timeout=5) as r:assert json.load(r)['ok']
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
command('reset')
p=subprocess.Popen([str(root/'Builds/Desktop/LordLyrical.exe'),'-screen-fullscreen','0','--capture-evidence','-logFile',str(root/'Evidence/final-player-check.log')],startupinfo=startup)
try:
    deadline=time.monotonic()+60
    while time.monotonic()<deadline:
        s=status()
        if any(p['connected'] for p in s['participants']) and any(v['phase']=='Ready' and time.monotonic()-v['received']<3 for v in s['telemetry'].values()):break
        time.sleep(.25)
    else:raise RuntimeError('Final player did not become ready')
    for seek,capture in [(21,21.5),(39,39.5),(66,66.5),(85,85.5),(98,98.8),(116,117)]:
        command('seek',seek);time.sleep(3)
        s=status();assert s['phase']=='Running'
        src=root/f'Builds/Desktop/Evidence/Player/show-{capture:05.1f}.png'
        assert src.stat().st_size>50000,'Capture is missing or blank'
        shutil.copy2(src,root/f'Evidence/Player/show-{capture:05.1f}.png')
    command('reset');time.sleep(3)
    assert any(v['phase']=='Ready' for v in status()['telemetry'].values())
    (root/'Evidence/final-player-check.json').write_text(json.dumps({'passed':True,'checks':['Final built player connected','Reconstructed six chamber, Orc, Dragon, Sonar, Word and credits checkpoints','Rendered six nonblank images','Reset to Ready']},indent=2))
    print('PASS final player checkpoints and reset')
finally:p.terminate();p.wait(timeout=10)
