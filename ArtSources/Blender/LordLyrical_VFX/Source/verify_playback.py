import time,json,urllib.request,subprocess,statistics,argparse
from pathlib import Path
BASE=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');P=BASE.parent/'als/outputs/LordLyrical';OUT=BASE/'outputs/LordLyrical_VFX'
parser=argparse.ArgumentParser();parser.add_argument('--device',choices=['desktop','headset'],required=True);args=parser.parse_args()
def status():
 with urllib.request.urlopen('http://127.0.0.1:8810/status',timeout=4) as r:return json.load(r)
def command(c,v=None):
 req=urllib.request.Request('http://127.0.0.1:8810/command',json.dumps({'command':c,'value':v}).encode(),{'Content-Type':'application/json'})
 with urllib.request.urlopen(req,timeout=4) as r:assert json.load(r)['ok']
def participant(s):
 for p in s['participants']:
  v=s['telemetry'].get(str(p['clientNo']),{})
  if p['connected'] and (v.get('battery',-1)>=0)==(args.device=='headset') and v.get('phase')=='Ready':return p
 return None
process=None;report={'device':args.device,'purpose':'Cinematic VFX integrated show at normal quality','passed':False,'samples':[]}
try:
 command('reset');command('mode','Adaptive')
 if args.device=='desktop':
  startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
  process=subprocess.Popen([str(P/'Builds/Desktop/LordLyrical.exe'),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','--capture-evidence','-logFile',str(OUT/'Desktop_VFX.log')],startupinfo=startup)
 deadline=time.monotonic()+50;actor=None
 while time.monotonic()<deadline:
  s=status();actor=participant(s)
  if actor:break
  time.sleep(.5)
 assert actor,'No ready '+args.device+' client'
 identity=actor['deviceId'];report['participant']=actor;command('start');deadline=time.monotonic()+150;seen=None
 while time.monotonic()<deadline:
  s=status();p=next(x for x in s['participants'] if x['deviceId']==identity);v=s['telemetry'].get(str(p['clientNo']),{})
  if v.get('received')!=seen and p['connected']:report['samples'].append(v);seen=v.get('received')
  if s['phase']=='Complete' and v.get('phase')=='Complete':break
  time.sleep(.5)
 else:raise RuntimeError('Playback did not complete')
 active=[v for v in report['samples'] if v['phase'] in ['Running','Ending']];assert len(active)>40
 assert all(not v.get('error') for v in active)
 fps=[v['fps'] for v in active]
 report.update(passed=True,fpsMean=statistics.mean(fps),fpsMin=min(fps),fpsP10=sorted(fps)[int(len(fps)*.10)],musicClockDriftMaxMs=max(abs(v['driftMs']) for v in active),memoryMaxMB=max(v['memoryMb'] for v in active),runId=s['runId'])
 report['beats']={name:dict(fpsMean=statistics.mean(v['fps'] for v in active if start<=v['time']<end),samples=sum(start<=v['time']<end for v in active)) for name,start,end in [('Vortex',1,21),('Orcs',26,54),('DragonBeam',69,81),('Finale',96,104)]}
 print(json.dumps({k:v for k,v in report.items() if k!='samples'},indent=2),flush=True)
except Exception as e:report['error']=repr(e);raise
finally:
 if process and process.poll() is None:process.terminate();process.wait(timeout=10)
 (OUT/(args.device.title()+'_VFX_Playback.json')).write_text(json.dumps(report,indent=2))
