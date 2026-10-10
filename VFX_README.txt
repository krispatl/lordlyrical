LORD LYRICAL - CINEMATIC VFX PASS
3 October 2026

The existing Unity project has been updated in place:
C:\Users\kris\Documents\Codex\2026-10-03\als\outputs\LordLyrical
Scene: Assets/LordLyrical/Scenes/Experience.unity
Unity 6000.3.9f1 / URP 17.3.0 / PICO OpenXR / STYLY NetSync 0.17.4.

NEW VISUAL LAYERS
- Arrival: flowing cyan/violet plasma curtains, helical light, airborne energy and velocity-aligned motes.
- Impacts: anticipation charge, short corona, radiating wavefront, incandescent sparks, physical mesh fragments, rolling dust and light spill.
- Sonar: spatial scanning points and expanding wave surfaces, maintaining the role's colour and timing.
- Healer: a translucent hexagonal membrane, rim response, contact ripple and restrained gold lattice.
- Dragon: a turbulent blue plasma beam, bright core, impact corona and scattered sparks; charging and roar fronts.
- Shatter, amplitude, dread, cacophony and reconstruction each have layered treatments matched to the authored cues.
- Chord barrage and anti-frequency: separate turbulent streams and fracture bursts.
- Finale: ten coloured energy streams, convergence wave, released-frequency particles flowing into the Well, and settling motes.
- HDR bloom, ACES tone mapping and pooled local light spill. No camera shake or forced head motion was added.

NATIVE PARTICLE SYSTEMS
Five reusable Unity ParticleSystem layers: sparks, volumetric plumes, turbulent energy, impact corona and mesh debris. Total allocated capacity: 3,400 particles. Normal-profile peak in the complete 1,452-step validation sweep: 1,150 particles. Twenty-eight wavefront surfaces, one membrane, one plasma curtain mesh and three pooled lights support them.

Particles are evaluated from the absolute shared show time, then submitted to the native particle renderers. This preserves reset, arbitrary seeking and reconnect behavior. Particle pools and arrays are reused; no GameObjects are created per impact.

Original procedural textures include a 64-frame volume atlas (1024 square; 128-pixel tiles), four-channel multiscale flow noise, spark core and corona. Soft depth intersections and viewer-distance fade are implemented in the particle shader. Android textures use ASTC 6x6.

EDIT AND TUNE
Assets/LordLyricalVFX_Unity contains Scripts, Shaders, Textures, Materials and Profiles. The CinematicParticleDirector component is attached to the existing ShowArtPresentation prefab. Material references and quality are exposed there; cue-level timing, trajectory and counts are in CinematicParticleDirector.cs.

ExperienceConfiguration.vfxQuality: 0 = reduced density, 1 = normal, 2 = high density. Core cue timing and role colours stay the same. Cinematic_Resonance.asset controls bloom, tone mapping and colour adjustments. MobileURP enables HDR, depth texture and vertex additional lights. MobileRenderer copies depth after opaque objects so the transparent particle passes can use it. Backups of the changed rendering settings and presentation code are in Backup/.

REVIEW AND VALIDATION
Cinematic_VFX_Full_Show.mp4 is a full 121-second editor-rendered preview, with 12 source frames per second encoded at 24 fps and the existing master track. It is not a headset performance recording.
VFX_*.png contains 32 hero-beat stills. Before/ preserves the preceding pass for comparison; the review camera differs slightly.
Unity_VFX_Validation.txt contains shader, native-particle count, deterministic reverse-seek/reset, full sequence pool/finite-value checks, quality density and core timing results.
FINAL_VERIFICATION.txt records actual player builds and performance results. Use that report for headset acceptance status.

PRESERVATION
The original dragon model, rig and source files are retained. New lighting and encounter effects use the existing runtime material copies. Four voice slots remain empty for later recordings. The master audio and 121-second cue schedule are unchanged.
Ten redundant imported avatar LOD groups were cleared so each renderer belongs to its intended authored body or hand LOD group.

DELIVERY
LordLyrical_Cinematic_VFX.unitypackage contains the integrated effects and their asset dependencies. Import into the existing project or a Unity project with the package dependencies above. For a complete configured source project, use LordLyrical_VFX_Unity_Project.zip.
Builds/ contains the new Windows player and PICO builds when confirmed in FINAL_VERIFICATION.txt. The USB review app is separate from the existing production app and requires the local venue server plus ADB reverse TCP ports 5555, 5556, 5557 and 8800.

Unity menus: Lord Lyrical / VFX / Build Cinematic Effects; Validate and Render Hero Beats; Render Motion Reel; Export Cinematic VFX Package. Do not run the older Build Show Scene command; it recreates the authoring scene/configuration.

SOURCE NOTES
Source/make_textures.py reproduces the original procedural maps. The C# and shader source is also saved in the Unity project and mirrored Unity asset folders. Scripts use workstation-specific paths; adapt paths for another machine. Source/verify_playback.py starts/resets the local test room and must not run during an audience session.

TECHNICAL REFERENCES
Unity native particle submission: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ParticleSystem.SetParticles.html
Unity particle vertex streams: https://docs.unity3d.com/6000.0/Documentation/Manual/PartSysVertexStreams.html

Visual quality still needs artistic and headset review. Standalone VR frame time, stereo clarity and overdraw are acceptance criteria; the cinematic preview alone does not establish headset performance.
