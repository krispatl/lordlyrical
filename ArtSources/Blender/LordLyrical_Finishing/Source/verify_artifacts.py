from pathlib import Path
import json,tarfile,zipfile,hashlib
base=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');out=base/'outputs';report={}
for name in ['LordLyrical_Complete_Art.unitypackage','LordLyrical_Ten_Avatars.unitypackage']:
 with tarfile.open(out/name,'r:gz') as archive:
  paths=[archive.extractfile(m).read().decode('utf-8-sig') for m in archive if m.name.endswith('/pathname')]
  assert len(paths)==len(set(paths)), 'Duplicate asset paths'
  if 'Complete' in name:
   for root in ['LordLyricalFinishing_Unity','LordLyricalChamber_Unity','LordLyricalJeru_Unity','LordLyricalAvatars_Unity','LordLyricalOrc_Unity','LordLyricalDragon_Unity','LordLyrical']:
    assert any(p.startswith('Assets/'+root+'/') for p in paths),root
   assert 'Assets/LordLyricalFinishing_Unity/Voice/ShowVoiceSlots.asset' in paths
  else:
   assert sum(p.endswith('_Distance_Hands.fbx') for p in paths)==10
  report[name]={'passed':True,'assetPaths':len(paths),'bytes':(out/name).stat().st_size}
for path in [out/'LordLyrical_Finishing/LordLyrical_Complete_Unity_Project.zip',out/'LordLyrical_Finishing/Builds/LordLyrical-Desktop.zip']:
 with zipfile.ZipFile(path) as archive:
  assert archive.testzip() is None
  names=archive.namelist();assert len(names)==len(set(names))
  if 'Unity_Project' in path.name:
   assert 'LordLyrical/ProjectSettings/ProjectVersion.txt' in names
   assert 'LordLyrical/Assets/LordLyrical/Scenes/Experience.unity' in names
   assert 'LordLyrical/Tools/show_authority.py' in names
  else:
   assert 'LordLyrical-Desktop/LordLyrical.exe' in names
   assert 'LordLyrical-Desktop/UnityPlayer.dll' in names
   assert not any('/Evidence/' in p for p in names)
  report[path.name]={'passed':True,'files':len(names),'bytes':path.stat().st_size}
(out/'LordLyrical_Finishing/Package_Validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2),flush=True)
