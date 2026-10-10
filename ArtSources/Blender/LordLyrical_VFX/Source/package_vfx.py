from pathlib import Path
import shutil,zipfile,json,hashlib,datetime
B=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');P=B.parent/'als/outputs/LordLyrical';O=B/'outputs/LordLyrical_VFX'
for name in ['LordLyricalVFX_Unity','LordLyricalFinishing_Unity','LordLyricalAvatars_Unity']:
 shutil.copytree(P/'Assets'/name,B/'outputs'/name,dirs_exist_ok=True)
shutil.copytree(B/'work/vfx',O/'Source',dirs_exist_ok=True,ignore=shutil.ignore_patterns('movie','*.log','__pycache__'))
(O/'Builds').mkdir(exist_ok=True)
for source,target in [('LordLyrical-PICO-USB-Review.apk','LordLyrical-VFX-PICO-USB.apk'),('LordLyrical-PICO.apk','LordLyrical-VFX-PICO.apk')]:
 p=P/'Builds'/source
 if p.stat().st_mtime>datetime.datetime(2026,10,3,23,30).timestamp():shutil.copy2(p,O/'Builds'/target)
with zipfile.ZipFile(O/'LordLyrical_VFX_Unity_Project.zip','w',zipfile.ZIP_DEFLATED,compresslevel=5) as archive:
 for root in ['Assets','Packages','ProjectSettings','Tools']:
  for p in (P/root).rglob('*'):
   if p.is_file() and '__pycache__' not in p.parts and p.suffix.lower() not in {'.pfx','.keystore','.jks'}:archive.write(p,'LordLyrical/'+str(p.relative_to(P)))
 for p in P.iterdir():
  if p.is_file() and p.suffix.lower() in {'.bat','.md','.txt','.json'}:archive.write(p,'LordLyrical/'+p.name)
 for name in ['START_HERE.txt','Unity_VFX_Validation.txt']:
  archive.write(O/name,'LordLyrical/VFX_DELIVERY/'+name)
with zipfile.ZipFile(O/'Builds/LordLyrical-VFX-Desktop.zip','w',zipfile.ZIP_DEFLATED,compresslevel=5) as archive:
 root=P/'Builds/Desktop'
 for p in root.rglob('*'):
  if p.is_file() and 'Evidence' not in p.relative_to(root).parts and p.suffix.lower()!='.log' and not any('DoNotShip' in x for x in p.parts):archive.write(p,'LordLyrical-Desktop/'+str(p.relative_to(root)))
checks=[]
for p in [O/'LordLyrical_VFX_Unity_Project.zip',*(O/'Builds').glob('*.zip')]:
 with zipfile.ZipFile(p) as archive:
  assert archive.testzip() is None;assert len(archive.namelist())==len(set(archive.namelist()));checks.append(dict(file=p.name,files=len(archive.namelist()),crcPassed=True))
(O/'Archive_Validation.json').write_text(json.dumps(checks,indent=2))
files=[*O.glob('*.zip'),*O.glob('*.unitypackage'),*O.glob('*.mp4'),*(O/'Builds').glob('*')]
(O/'Deliverable_Checksums.json').write_text(json.dumps([dict(file=str(p.relative_to(O)),bytes=p.stat().st_size,sha256=hashlib.file_digest(p.open('rb'),'sha256').hexdigest()) for p in files if p.is_file()],indent=2))
print('VFX_PACKAGES_VERIFIED',flush=True)
