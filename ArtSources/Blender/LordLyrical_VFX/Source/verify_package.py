from pathlib import Path
import tarfile,json,hashlib
B=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');O=B/'outputs/LordLyrical_VFX';P=B.parent/'als/outputs/LordLyrical'
with tarfile.open(O/'LordLyrical_Cinematic_VFX.unitypackage','r:gz') as archive:
 members={m.name:m for m in archive.getmembers()};paths={}
 for name,m in members.items():
  if name.endswith('/pathname'):paths[name.rsplit('/',1)[0]]=archive.extractfile(m).read().decode('utf-8-sig')
 assert len(paths.values())==len(set(paths.values()))
 assert 'Assets/LordLyrical/ScriptableObjects/MobileRenderer.asset' in paths.values()
 for expected in ['Assets/LordLyricalVFX_Unity/Scripts/CinematicParticleDirector.cs','Assets/LordLyricalVFX_Unity/Scripts/CinematicLook.cs','Assets/LordLyricalVFX_Unity/Textures/Volumetric_Plume_8x8.png','Assets/LordLyricalVFX_Unity/Profiles/Cinematic_Resonance.asset','Assets/LordLyricalFinishing_Unity/Prefabs/ShowArtPresentation.prefab','Assets/LordLyrical/ScriptableObjects/MobileURP.asset']:
  assert expected in paths.values(),expected
 checked=0
 for id,path in paths.items():
  entry=members.get(id+'/asset')
  if entry and (P/path).is_file():
   assert hashlib.sha256(archive.extractfile(entry).read()).digest()==hashlib.sha256((P/path).read_bytes()).digest(),path
   checked+=1
 report=dict(passed=True,assetPaths=len(paths),assetFilesMatchingCurrentProject=checked,bytes=(O/'LordLyrical_Cinematic_VFX.unitypackage').stat().st_size)
(O/'Unity_Package_Validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
