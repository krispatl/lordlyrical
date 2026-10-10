from pathlib import Path
import json,re,hashlib,shutil,datetime
base=Path(r'C:\Users\kris\Documents\Codex\2026-10-03\bui');out=base/'outputs/LordLyrical_Finishing';project=base.parent/'als/outputs/LordLyrical'
desktop=json.loads((out/'Desktop_Playback_Validation.json').read_text());assert desktop['passed'] and len(desktop['cycles'])==2
network=json.loads((out/'Network_Ten_Client_Validation.json').read_text());assert network['passed']
text=(out/'Desktop_Playback.log').read_text(errors='replace');full=text[text.rindex('LL CUE vortex expected='):]
cues=re.findall(r'LL CUE (\S+) expected=([\d.]+) actual=([\d.]+) errorMs=([\d.-]+)',full)
expected=json.loads((project/'Assets/LordLyrical/Resources/Cues.json').read_text())['cues']
assert [c[0] for c in cues]==[c['id'] for c in expected], 'Built-player cue order or count mismatch'
errors=re.findall(r'^.*(?:NullReferenceException|MissingReferenceException|InvalidOperationException|^LL: ).*$',text,re.M)
assert not errors,errors
cue_report={'passed':True,'cycle':'second, uninterrupted 121-second run','cueCount':len(cues),'exactOrderAndOnceEach':True,'maxSchedulingErrorMs':max(abs(float(c[3])) for c in cues),'note':'CPU cue scheduling relative to the master timeline; not acoustic or headset-to-headset latency.','cues':[{'id':c[0],'expected':float(c[1]),'actual':float(c[2]),'schedulingErrorMs':float(c[3])} for c in cues]}
(out/'Desktop_Cue_Validation.json').write_text(json.dumps(cue_report,indent=2))
player=out/'Player_Checkpoints';player.mkdir(exist_ok=True)
for p in (project/'Builds/Desktop/Evidence/Player').glob('*.png'):
 assert p.stat().st_mtime>datetime.datetime(2026,10,3,21,28).timestamp(), 'Stale built-player capture '+p.name
 shutil.copy2(p,player/p.name)
lines=['LORD LYRICAL - FINAL INTEGRATION VERIFICATION','3 October 2026','',
'ASSETS IMPLEMENTED AND CONNECTED',
'Gothic chamber and animatable glyph surfaces; preserved cybernetic dragon; Jeru body/finger/facial rig; existing Meshy Orc rig; ten participant avatars and first-person hands; authored tuning-fork club and vortex; role effects, dragon reactions/dissolve, Word/lock/finale/title; quiet spatial accent audio and cue haptics.',
'Ten participant roles have 60 starter clips total. Jeru has 66 facial shapes and three mesh LODs, including facial-shape relay. Body and hand LODs are present on participants. This is an integrated authored game-art pass, not a certified photoreal scan or final venue acceptance.',
'', 'RECORDED VOICE',
'User requested slots for later recordings. Four slots are present and silent: Jeru counter-call at 34 s; Seeker deep-sonar at 83 s; Jeru identify-discordant at 87 s; Jeru amplitude-rise at 92 s. No new spoken performance was generated. Each slot supports a clip; Jeru slots also support timed facial curves.',
'', 'EXECUTED CHECKS',
'PASS full Unity show: 26 checkpoints, reverse seek, reset, finite transforms, unchanged dragon scale, material dissolve, eleven-letter title, four empty voice slots; peak 35 pooled ribbons.',
'PASS all six Jeru distance facial meshes: 66 retained shapes, matching binding and world bounds; all ten hand LOD groups and first-person visibility.',
'PASS twelve authored Orcs, hand-mounted weapon sockets, three mesh LODs, timed appearance/defeat and repeat-seek vertex error below 0.000001 m.',
'PASS nine existing Unity core check groups and eleven Python authority tests.',
'PASS ten simulated clients over the real local NetSync transport: two complete 121-second runs, disconnect at 78 s/rejoin at 85 s, stable slots/epoch, completion and reset. This was not ten physical headsets.',
'PASS built Windows player: two complete runs, one forced process disconnect/restart, recovery and reset to Ready. Resolution 1280x720; one desktop client.']
for c in desktop['cycles']:
 lines.append(f"  Run {c['cycle']}: mean {c['fps_mean']:.2f} FPS; minimum telemetry sample {c['fps_min']:.2f} FPS; max reported music-clock drift {c['drift_abs_max_ms']:.3f} ms; maximum reported memory {c['memory_max_mb']:.2f} MB.")
lines += [f"PASS all {len(cues)} built-player cues fired once in order on the uninterrupted run; maximum scheduling error {cue_report['maxSchedulingErrorMs']:.1f} ms.",
'These are desktop telemetry and scheduling measurements, not headset GPU timings, audio-output latency or venue synchronization certification.',
'PASS production PICO Android build: zero build errors, 15 warnings. Actual APK 210,786,567 bytes.',
'PASS separate USB review Android build: zero build errors, 15 warnings. Actual APK 210,328,311 bytes. Installed as com.fifthyang.lordlyrical.usbreview; the existing production application was retained.',
'PENDING physical headset playback: connected PICO remained asleep. No running review-app process or headset playback/performance acceptance was recorded. Haptic strength, stereo legibility, controller alignment, and audience comfort need an awake-device/venue review.',
'', 'PRESERVATION',
'All 55 original dragon Unity asset files and the original Blender file are unchanged. Encounter effects use runtime material copies. Production app id/product name and the normal Experience scene were preserved during the separate USB build.',
'', 'DELIVERABLES',
'Complete_Show_Preview.mp4: 121 seconds, editor-rendered at 12 source frames/s and encoded at 24 fps with the existing master track. It is an art/timing preview, not a real-time headset recording.',
'Player_Checkpoints/: six captures from the actual final Windows player.',
'../LordLyrical_Complete_Art.unitypackage: integrated assets plus show code; requires the project dependencies listed in START_HERE.txt.',
'../LordLyrical_Ten_Avatars.unitypackage: refreshed standalone avatar content, including distance hands and their detail textures.',
'LordLyrical_Complete_Unity_Project.zip: Assets, Packages, ProjectSettings, Tools and launch/config documents. Unity restores packages on first open; Library and machine environments are excluded.',
'Builds/LordLyrical-Desktop.zip: Windows player and all required player data; run the venue authority from the project before launching.',
'Builds/LordLyrical-PICO.apk: production-network Android build.',
'Builds/LordLyrical-PICO-USB-Review.apk: separate localhost/USB review app. ADB reverse ports 5555, 5556, 5557 and 8800 are required for the current USB setup.',
'', 'REMAINING ART AND ACCEPTANCE',
'Provide final Jeru/Seeker recordings and author their final lip-sync curves. Review Jeru close-up likeness, skin/eyes/beard and facial correctives against the actual person; this pass does not claim lifelike identity capture. Participant costumes and acting remain stylized game art and need artistic approval. Test extreme retargeted poses and garment clearance if additional motions are introduced. Complete physical PICO and ten-headset venue profiling, stereo/contact/comfort review and haptic calibration.',
'No production deployment or store release was performed.']
(out/'FINAL_VERIFICATION.txt').write_text('\n'.join(lines)+'\n')
status='''CURRENT ASSET STATUS - 3 OCTOBER 2026

BUILT AND INTEGRATED
Chamber, animatable glyphs, Well and architecture-to-dragon reveal.
Original dragon, rig and source preserved; reaction/fracture/dissolve effects connected.
Jeru: body/fingers, facial shapes, mantle, six starter clips, conducting/calls and three LODs.
Ten roles: Warrior D, Foundation C, Blade E, Balanced G, Healer F, Shadow B, Seeker C#, Architect F#, Discordant Bb, Ancient A. Distinct silhouettes/materials, six clips each, Humanoid/finger rigs, body and hand LODs, ability cues and first-person hand isolation.
Existing Meshy Orc: body rig, six starter motions, three LODs, twelve actors and authored tuning-fork weapon.
Vortex corridor, role abilities, dragon encounter effects, Word/ten-key lock, surface cymatics and animated title.
Six original quiet synthetic spatial accents and cue-specific haptic patterns.
Four empty recording slots and optional Jeru facial-performance tracks.
Current Unity project, complete art package, avatar package, Windows player and two PICO APK variants.

STILL NEEDED
Final voice recordings and their lip-sync animation.
Art approval and further close-up likeness/costume/acting polish; the models remain authored game art, not certified photoreal scans.
Awake-headset and ten-physical-headset acceptance: performance, stereo readability, controller contacts, haptic strength and comfort.

See FINAL_VERIFICATION.txt for measured results and limits.
'''
(out/'ASSET_STATUS.txt').write_text(status)
shutil.copy2(out/'FINAL_VERIFICATION.txt',project/'FINISHING_VERIFICATION.txt')
shutil.copy2(out/'ASSET_STATUS.txt',project/'FINISHING_ASSET_STATUS.txt')
print('FINAL_REPORT_READY',len(cues),'cues',flush=True)
