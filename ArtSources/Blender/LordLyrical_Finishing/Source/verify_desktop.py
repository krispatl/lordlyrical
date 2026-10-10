import json,time,urllib.request,subprocess,statistics
from pathlib import Path
BASE=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');PROJECT=BASE.parent/'als/outputs/LordLyrical';OUT=BASE/'outputs/LordLyrical_Finishing';URL='http://127.0.0.1:8810';process=None
def status():
 with urllib.request.urlopen(URL+'/status',timeout=5) as r:return json.load(r)
def command(name,value=None):
 request=urllib.request.Request(URL+'/command',json.dumps(dict(command=name,value=value)).encode(),{'Content-Type':'application/json'})
 with urllib.request.urlopen(request,timeout=5) as r:assert json.load(r)['ok']
def launch():
 startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
 return subprocess.Popen([str(PROJECT/'Builds/Desktop/LordLyrical.exe'),'-screen-fullscreen','0','-screen-width','1280','-screen-height','720','--capture-evidence','-logFile',str(OUT/'Desktop_Playback.log')],startupinfo=startup)
report=dict(test='Final integrated art; built desktop player, 1280x720, explicit URP rendering; not headset performance',cycles=[])
try:
 command('reset');command('mode','Adaptive');process=launch();deadline=time.monotonic()+60;identity=None
 while time.monotonic()<deadline:
  s=status()
  for p in s['participants']:
   v=s['telemetry'].get(str(p['clientNo']),{})
   if p['connected'] and v.get('battery',0)<0 and v.get('phase')=='Ready':identity=p['deviceId'];break
  if identity:break
  time.sleep(.5)
 assert identity,'Desktop did not become ready'
 slot=next(p['slot'] for p in s['participants'] if p['deviceId']==identity)
 for cycle in range(2):
  command('start');samples=[];seen=-1;start=time.monotonic();stopped=False;rejoined=False
  while time.monotonic()-start<145:
   s=status();p=next(p for p in s['participants'] if p['deviceId']==identity);v=s['telemetry'].get(str(p['clientNo']),{})
   if cycle==0 and s['time']>=78 and not stopped:process.terminate();process.wait(timeout=10);stopped=True;print('Desktop disconnected at 78s',flush=True)
   if cycle==0 and s['time']>=85 and not rejoined:process=launch();rejoined=True;print('Desktop relaunched at 85s',flush=True)
   if p['connected'] and v.get('received',-1)!=seen:seen=v.get('received',-1);samples.append(v)
   if s['phase']=='Complete' and v.get('phase')=='Complete':break
   time.sleep(.5)
  else:raise RuntimeError('Show failed to complete')
  assert p['slot']==slot and p['connected'];assert all(not v.get('error') for v in samples),samples
  if cycle==0:assert any(85<v['time']<94 and v['phase']=='Running' for v in samples),'Late join reconstruction missing'
  active=[v for v in samples if v['phase'] in ['Running','Ending']];assert len(active)>25
  result=dict(cycle=cycle+1,runId=s['runId'],samples=samples,fps_mean=statistics.mean(v['fps'] for v in active),fps_min=min(v['fps'] for v in active),drift_abs_max_ms=max(abs(v['driftMs']) for v in active),memory_max_mb=max(v['memoryMb'] for v in active));report['cycles'].append(result)
  print(json.dumps({k:v for k,v in result.items() if k!='samples'}),flush=True);command('reset');time.sleep(3)
  s=status();p=next(p for p in s['participants'] if p['deviceId']==identity);v=s['telemetry'][str(p['clientNo'])];assert v['phase']=='Ready' and v['time']==0
 report['passed']=True
except Exception as e:report['passed']=False;report['error']=repr(e);raise
finally:
 if process and process.poll() is None:process.terminate();process.wait(timeout=10)
 (OUT/'Desktop_Playback_Validation.json').write_text(json.dumps(report,indent=2));print('Desktop verification saved',flush=True)
