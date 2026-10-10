from pathlib import Path
import zipfile,shutil,json,hashlib
base=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');project=base.parent/'als/outputs/LordLyrical';out=base/'outputs/LordLyrical_Finishing'
# Project source only: no machine caches, virtual environments, logs or signing credentials.
roots=['Assets','Packages','ProjectSettings','Tools']
files=[]
for folder in roots:
 files.extend(p for p in (project/folder).rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.suffix.lower() not in {'.pfx','.keystore','.jks'})
files.extend(p for p in project.iterdir() if p.is_file() and p.name!='FINISHING_README.txt' and p.suffix.lower() in {'.bat','.txt','.md','.json'})
with zipfile.ZipFile(out/'LordLyrical_Complete_Unity_Project.zip','w',zipfile.ZIP_DEFLATED,compresslevel=5) as archive:
 for p in files:archive.write(p,'LordLyrical/'+str(p.relative_to(project)))
 archive.write(out/'START_HERE.txt','LordLyrical/FINISHING_README.txt')
print('PROJECT_ARCHIVE_READY',len(files),(out/'LordLyrical_Complete_Unity_Project.zip').stat().st_size,flush=True)
desktop=project/'Builds/Desktop'
with zipfile.ZipFile(out/'Builds/LordLyrical-Desktop.zip','w',zipfile.ZIP_DEFLATED,compresslevel=5) as archive:
 for p in desktop.rglob('*'):
  if p.is_file() and 'Evidence' not in p.relative_to(desktop).parts and p.suffix.lower()!='.log' and not any('DoNotShip' in part for part in p.parts):archive.write(p,'LordLyrical-Desktop/'+str(p.relative_to(desktop)))
print('DESKTOP_ARCHIVE_READY',(out/'Builds/LordLyrical-Desktop.zip').stat().st_size,flush=True)
