from pathlib import Path
import json,hashlib,zipfile,datetime
B=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');P=B.parent/'als/outputs/LordLyrical';O=B/'outputs/LordLyrical_VFX'
changed=[];count=0
with zipfile.ZipFile(B/'outputs/LordLyrical_Finishing/LordLyrical_Complete_Unity_Project.zip') as previous:
 for name in previous.namelist():
  if name.startswith('LordLyrical/Assets/LordLyricalDragon_Unity/') and not name.endswith('/'):
   count+=1;current=P/name.removeprefix('LordLyrical/')
   if not current.exists() or hashlib.sha256(previous.read(name)).digest()!=hashlib.sha256(current.read_bytes()).digest():changed.append(name)
assert not changed,changed
blender=B/'outputs/LordLyrical_CyberDragon.blend';blendhash=hashlib.file_digest(blender.open('rb'),'sha256').hexdigest().upper();assert blendhash=='F8553501EB371B157C9902FDD8236C2CB461619072D3258823924E9E4FAC9B83'
preserved=[]
with zipfile.ZipFile(B/'outputs/LordLyrical_Finishing/LordLyrical_Complete_Unity_Project.zip') as old:
 for path in ['Assets/LordLyrical/Resources/Cues.json','Assets/LordLyrical/Audio/LordLyrical.mp3','Assets/LordLyricalFinishing_Unity/Voice/ShowVoiceSlots.asset']:
  assert hashlib.sha256(old.read('LordLyrical/'+path)).digest()==hashlib.sha256((P/path).read_bytes()).digest(),path
  preserved.append(path)
report=dict(checkedAt=datetime.datetime.now().isoformat(),baseline='Delivered 21:33 Unity project archive, before the VFX request',passed=True,originalDragonFilesCompared=count,changedOriginalDragonFiles=changed,originalDragonBlendSHA256=blendhash,unchangedTimelineAudioAndVoiceSlots=preserved)
(O/'Preservation_Validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
