LORD LYRICAL - CYBERNETIC DRAGON
Created 3 October 2026 through Blender MCP, Blender 5.2.

STATUS
This is a local procedural interpretation of the supplied blueprint, not an exact
replica. The head, armor shapes, wing construction and surface detailing remain
visibly different from the concept. The animations are initial authored cycles,
not finished cinematic animation. No image was uploaded to Hyper3D.

UNITY
Import LordLyrical_CyberDragon.unitypackage via Assets > Import Package > Custom
Package. Drag Prefabs/LordLyrical_CyberDragon into a scene. This package contains
a ready-made Standard/Built-in shader prefab with a Generic avatar, materials,
Animator controller, and three LODs sharing the same skeleton.

For a URP project, run Tools > Lord Lyrical > Build Dragon Prefab after import.
The setup command detects URP and creates URP/Lit materials. HDRP is not supported
by the supplied setup script. URP conversion code is included but the actual
validation project used Built-in/Standard. Use bloom in your game's volume/post
processing if you want a halo around the emissive blue energy.

If importing the folder rather than the unitypackage, keep its exact path:
Assets/LordLyricalDragon_Unity, then run the setup command above.

ANIMATION
Set the Animator integer parameter Motion:
  0 Idle (3 seconds, looping)
  1 Fly (1.333 seconds, looping)
  2 Glide (2 seconds, looping)
  3 Roar (2.5 seconds)
  4 Bite (1 second)
Change Motion back to 0 after a one-shot animation finishes. Fly and Glide include
an approximately 1.85 m lift in the pose; they do not translate the character
across the level. Root motion is disabled. Locomotion and gameplay/AI are outside
this asset package. There is no walk/run cycle yet.

SCALE AND PERFORMANCE
Metric design: approximately 14 m wingspan, 10 m long and 6.5 m high in bind pose.
LOD0: 146,310 triangles. LOD1: 65,838. LOD2: 29,258.
Four skinned renderers per LOD, at most four bone influences per vertex.
Profile on the game's target Android hardware; no device performance test was run.
The authored LOD screen-height thresholds are 0.55, 0.25, 0.04. A conservative
renderer bound avoids wing/tail culling during animation.

BLENDER
Open the adjacent LordLyrical_CyberDragon.blend. The original blueprint is packed
into the file in a hidden reference collection. The user's prior scene is retained.
Choose an LL_Dragon action in the Action Editor. All deform chains support FK.
For planted feet, set the rig's fore_IK_L, fore_IK_R, hind_IK_L or hind_IK_R custom
property to 1 and pose the corresponding CTRL foot target and knee pole. The
supplied clips use FK, so all IK blend values default to 0. The control bones are
excluded from the FBX deformation skeleton. Automatic FK/IK snapping is not supplied.

VALIDATION
Unity 6000.3.9f1 imported all FBXs, created a valid Generic avatar and prefab, and
sampled five poses per clip for finite, correctly scaled deformation. Blender
verified normalized weights, at most four influences, and finite posed vertices.
See adjacent Unity_Validation.txt and validation_report.json for evidence.
These checks do not certify exact visual matching or polished animation.
