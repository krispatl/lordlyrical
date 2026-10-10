LORD LYRICAL / JERU - FIRST LIKENESS REVIEW BUILD

Open Scenes/JeruCharacterReview.unity, or place Prefabs/LordLyrical_Jeru.prefab in a URP scene.
The character has a valid Humanoid avatar, 69 deform bones, fully articulated fingers, 51 facial expression shapes, 15 speech visemes and six animation clips. Jaw/eye bones retain their original animation outside Humanoid facial retargeting. Blender authoring includes four IK limbs and mantle controls.

JeruPerformance.SampleShow(time) samples the experience from absolute narrative time.
SampleClip(index,seconds) selects Idle, Conduct, Raise The Word, Counter Call, Turn To Group or Face Calibration (indices 0-5).
SetExpression(name,weight) accepts facial or viseme names and a normalized weight of 0-1. Apply expression overrides after SampleShow/Clip.
SetSpectrum(colour,strength) controls the separate cymatic emission material.
The hat and glasses can be disabled as child meshes. The scalp underneath is an approximation, since every supplied photograph includes a hat.

The preview controller provides all six clips as Animator states. Facial mesh names retain Jeru_Face_51Expressions for source continuity but contain 66 channels after the speech visemes were added.

The open LordLyrical project is connected through Experience.asset.lordModel and its LordLyricalController adapter. This portable package supplies the character assets and runtime component; other projects must call the component from their own game logic. Editor/Connect to Lord Lyrical Show only assigns a matching existing configuration; it does not rewrite controller source.

14 skinned renderers, approximately 82,683 triangles. Skin detail comes from a four-view photograph fitting with a 4K projected texture. This is not a scan or final photoreal art pass. Lighting cleanup, eye/cornea and beard refinement, cloth polish, extreme-expression correctives, LODs and PICO profiling remain. Dedicated walking animation and audio-driven lip-sync are not included.

Sources: user-provided Jeru reference photographs; KeenTools FaceBuilder 2026.3.1; MPFB 2.0.17 and MakeHuman CC0 system assets (body, eyes, teeth, tongue, fitted hat/underlayer/shoes). Custom mantle, eyewear, rig additions and animations were authored for this project.
https://keentools.io/products/facebuilder-for-blender
https://static.makehumancommunity.org/about/license.html
https://static.makehumancommunity.org/assets/assetpacks/makehuman_system_assets.html
