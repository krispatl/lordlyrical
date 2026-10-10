"""Capture authority telemetry and PICO runtime GPU metrics during the same worn-headset review.
Read-only: does not launch, seek, start, align, or change power settings.
"""
from pathlib import Path
import argparse, subprocess, sys, datetime, json
parser=argparse.ArgumentParser()
parser.add_argument('--adb',required=True)
parser.add_argument('--serial',required=True)
parser.add_argument('--version',type=int,default=22)
parser.add_argument('--slot',type=int,default=0)
parser.add_argument('--wait-seconds',type=int,default=600)
parser.add_argument('--output',type=Path,required=True)
args=parser.parse_args()
args.output.mkdir(parents=True,exist_ok=True)
stamp=datetime.datetime.now().strftime('%Y%m%d_%H%M%S_%f')
pid=subprocess.run([args.adb,'-s',args.serial,'shell','pidof','com.fifthyang.lordlyrical'],capture_output=True,text=True,check=True).stdout.strip()
if not pid.isdigit():raise RuntimeError('Expected exactly one running Lord Lyrical app process')
logfile=args.output/('Pico_Runtime_'+stamp+'.log')
flags=getattr(subprocess,'CREATE_NO_WINDOW',0)
with logfile.open('w',encoding='utf-8') as log:
    native=subprocess.Popen([args.adb,'-s',args.serial,'logcat','-T','1','--pid='+pid,'-v','threadtime','PxrMetric:I','Unity:I','*:S'],stdout=log,stderr=subprocess.STDOUT,creationflags=flags)
    try:
        result=subprocess.run([sys.executable,str(Path(__file__).with_name('capture_headset_review.py')),'--version',str(args.version),'--slot',str(args.slot),'--wait-seconds',str(args.wait_seconds),'--output',str(args.output)],creationflags=flags)
    finally:
        native.terminate()
        try:native.wait(timeout=5)
        except subprocess.TimeoutExpired:native.kill();native.wait()
metadata={'candidate':args.version,'appPid':int(pid),'serial':args.serial,'nativeLog':str(logfile),'authorityRecorderExitCode':result.returncode,'scope':'Metrics sampled during live recorder lifetime. No completeness or successful wearer test is implied by this file.'}
logfile.with_suffix('.json').write_text(json.dumps(metadata,indent=2))
print(json.dumps(metadata),flush=True)
raise SystemExit(result.returncode)
