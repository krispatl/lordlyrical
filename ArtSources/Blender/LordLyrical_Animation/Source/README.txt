EDITABLE ORC MOTION WORKSHOP

Open Orc_Motion_Workshop.blend in Blender 5.2 or compatible newer Blender.
The original Sonic_Orc_Rig drives the original LOD meshes and resonance glyphs.
All eleven Orc_Motion_* actions are retained with fake users. The file opens on a walking pose. Select the rig, choose an action in the Action Editor and set the timeline to that action's range before playback. Actions are authored at 30 FPS.
The original Meshy source and lower LOD meshes are hidden in the preview. The packed texture and two original materials are retained.
FBX/ contains animation-only exports that were retargeted to this skeleton. The game uses converted Generic Unity animation curves; the existing Sonic Orc character is not replaced by the animation provider's mesh.

OPTIONAL REBAKE HELPER
retarget_orc.py defines bake(label, filename, first_source_frame, last_source_frame, loop=False). Run it from Blender's text editor with this workshop open. It does not automatically bake or overwrite the saved workshop.
To rebake, place the original animation FBXs from your owned Fantasy Horde - Orc package in Source/Rebake/vendor-orc next to this .blend. This helper expects the original file names and FBX take ranges below. New exports go to Source/FBX_Rebaked; diagnostic samples go to Source/Rebake.
The source skeleton is transformed from the provider's bind pose using anatomical child-bone directions. The original bent-arm target skeleton is retained.

Walk: FH_Orc@walk_inplace.FBX, 385, 415
Run: FH_Orc@run_inplace.FBX, 420, 445
CombatIdle: FH_Orc@idle fight.FBX, 775, 820
Slam: FH_Orc@attack axe .FBX, 870, 890
Sweep: FH_Orc@attack sword.FBX, 910, 930
Double: FH_Orc@attack double.FBX, 930, 960
Thrust: FH_Orc@attack spear.FBX, 1025, 1045
Burst: FH_Orc@cast 1.FBX, 1060, 1090
Block: FH_Orc@blocking.FBX, 870, 890
Stagger: FH_Orc@hit.FBX, 554, 575
Defeat: FH_Orc@die soft.FBX, 668, 740

Example after running the helper: bake('Walk_Revised', 'FH_Orc@walk_inplace.FBX', 385, 415, True)
Use a new label for revisions. Save a new workshop file after editing so that the delivered reference remains available.
