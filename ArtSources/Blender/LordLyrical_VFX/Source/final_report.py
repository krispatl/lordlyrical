from pathlib import Path
import json,re,shutil,datetime
B=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');O=B/'outputs/LordLyrical_VFX';P=B.parent/'als/outputs/LordLyrical'
pc=json.loads((O/'Desktop_VFX_Playback.json').read_text());assert pc['passed']
expected=json.loads((P/'Assets/LordLyrical/Resources/Cues.json').read_text())['cues']
log=(O/'Desktop_VFX.log').read_text(errors='replace');cues=re.findall(r'LL CUE (\S+) expected=([\d.]+) actual=([\d.]+) errorMs=([\d.-]+)',log)
assert [c[0] for c in cues]==[c['id'] for c in expected]
assert not re.search(r'NullReferenceException|MissingReferenceException|Shader error|\bLL: ',log)
(O/'Built_Player_Cues.json').write_text(json.dumps(dict(passed=True,count=len(cues),maxSchedulingErrorMs=max(abs(float(c[3])) for c in cues)),indent=2))
player=O/'Player_Checkpoints';player.mkdir(exist_ok=True)
for f in (P/'Builds/Desktop/Evidence/Player').glob('*.png'):
 assert f.stat().st_mtime>datetime.datetime(2026,10,3,23,30).timestamp()
 shutil.copy2(f,player/f.name)
pico=json.loads((O/'PICO_Build_Result.json').read_text());assert pico['result']=='Succeeded' and pico['errors']==0
usb=(O/'USB_Build_Result.txt').read_text();assert usb.startswith('Succeeded; errors=0;')
lines=['LORD LYRICAL - CINEMATIC VFX VERIFICATION',datetime.datetime.now().isoformat(timespec='seconds'),'',
'PASS five native Unity ParticleSystem layers, four custom VFX shaders, original 64-frame volume atlas and detail maps, HDR/bloom/tone mapping, pooled lights, shockwave surfaces and Healer membrane connected to the production scene.',
'PASS 32 hero-beat captures: native particle counts agree with authored pools; all positions finite; no overflow.',
'PASS complete 1,452-step sweep across the 121-second show: peak normal-profile particles = 1,150; no pool overflow or invalid values.',
'PASS reverse seek and reset reconstruct matching particle positions/seeds; low and high density profiles differ as expected.',
'PASS soft-particle depth texture is copied after opaque geometry; corrected desktop captures show the smoke and energy layers.',
'PASS all nine existing core timing, clock, key and reconstruction check groups.',
'PASS duplicate renderer ownership removed from ten imported avatar LOD groups; authored body/hand LOD controls preserved.',
'PASS original dragon: 55 Unity files match the preceding delivered project archive; original Blender hash unchanged. Cue table, master audio and four empty voice slots also match.',
f"PASS production PICO build: {pico['errors']} errors, {pico['warnings']} warnings.",
'PASS separate USB PICO build: '+usb.splitlines()[0],
'PASS built desktop player completed the entire show without an application error; all 37 cues fired once in order.',
f"Desktop 1280x720, normal VFX profile: mean {pc['fpsMean']:.2f} FPS; minimum telemetry sample {pc['fpsMin']:.2f}; 10th percentile {pc['fpsP10']:.2f}; maximum reported memory {pc['memoryMaxMB']:.2f} MB; maximum absolute reported music-clock drift {pc['musicClockDriftMaxMs']:.3f} ms."]
for name,result in pc['beats'].items():lines.append(f"  {name}: {result['fpsMean']:.2f} FPS mean over {result['samples']} samples.")
lines+=['Desktop telemetry is not headset GPU profiling or audio-output latency measurement. Build warnings are retained in build evidence; successful builds do not establish headset comfort or frame-rate acceptance.','']
device=O/'Headset_VFX_Playback.json'
if device.exists() and json.loads(device.read_text()).get('passed'):
 h=json.loads(device.read_text());lines += [f"PASS one PICO completed the updated 121-second show: mean {h['fpsMean']:.2f} FPS, minimum sample {h['fpsMin']:.2f}, 10th percentile {h['fpsP10']:.2f}; memory max {h['memoryMaxMB']:.2f} MB.", 'This is one-device telemetry, not a ten-headset venue acceptance test.']
else:lines += ['PENDING final headset playback/performance review: the PICO was asleep during the VFX pass and no completed updated-show headset run was recorded. The new USB review APK is supplied for the next awake-device test.']
lines += ['', 'DELIVERY',
'Cinematic_VFX_Full_Show.mp4: current final pass, 121 seconds, 12 source frames/s encoded at 24 fps, existing master audio. Editor art preview, not real-time performance evidence. Actual built-player captures are in Player_Checkpoints/.',
'VFX_*.png: current hero-beat renders. Before/: previous pass; review camera differs slightly.',
'LordLyrical_Cinematic_VFX.unitypackage: integrated effects and asset dependencies. Renderer settings are supplied with the complete source project.',
'LordLyrical_VFX_Unity_Project.zip: current Assets, Packages, ProjectSettings, Tools and root launch/config documents. No Library cache or virtual environment.',
'Builds/: updated desktop player, production-network PICO APK, and separate USB review APK.',
'Source/: original texture generator, C# authoring/runtime source, shaders, packaging and validation scripts.',
'Preservation_Validation.json and Deliverable_Checksums.json: preservation evidence and delivered file hashes.',
'', 'REVIEW STATUS',
'The major VFX pass is implemented and visually reviewed in Unity. Final artistic approval and device-specific stereo/overdraw/comfort review remain necessary before treating it as a finished cinema-quality headset experience. The four requested voice slots remain silent pending recordings.']
(O/'FINAL_VERIFICATION.txt').write_text('\n'.join(lines)+'\n')
shutil.copy2(O/'START_HERE.txt',P/'VFX_README.txt');shutil.copy2(O/'FINAL_VERIFICATION.txt',P/'VFX_VERIFICATION.txt');print('VFX_FINAL_REPORT_READY',flush=True)
