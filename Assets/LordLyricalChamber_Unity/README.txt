LORD LYRICAL — CHAMBER OF THE ROOT

Built 3 October 2026 for the existing LordLyrical Unity 6000.3.9f1 / URP project.

The authored chamber is connected through Experience.asset > chamberModel.
Open Assets/LordLyrical/Scenes/Experience.unity to run the complete show.
Open Assets/LordLyricalChamber_Unity/Scenes/ChamberShowcase.unity to inspect the chamber independently.
Select Chamber of the Root and scrub ChamberPresentation > Preview Time in the Inspector.
Preview In Editor must be enabled in the showcase. The live show drives time from the existing cue clock.

DELIVERED
32 m wide x 40 m deep hall, about 17 m high; Gothic vaults, twelve clustered column groups,
side galleries, portals, obsidian floor, Frequency Well, sixteen authored frequency glyphs,
silver inlays, canopy ribs, three independent upper-column fracture pieces, floor/wall/column colliders.
97 editable architecture meshes in the FBX / Blender source. Unity static material batches reduce the
runtime environment to 30 mesh renderers and 128,526 imported triangles; the dragon is additional.

GLYPH MATERIALS
Shader: LordLyrical/Chamber Surface. All surfaces are opaque, with recessed normal-map detail.
CH_GlyphMasks.png is a 4 x 4 atlas: red = engraved line, green = spatial reveal order, blue = spectral sector.
CH_GlyphNormal.png supplies the engraved surface relief. Colour textures and data masks are imported
with appropriate colour spaces; atlas textures are clamped and mipmapped.

Material properties:
_GlyphColor       Energy colour
_GlyphIntensity   Brightness, including fully unlit carvings
_GlyphReveal      0–1 spatial activation threshold
_PulseSpeed       Pulse frequency multiplier
_ShowClock        Authored show time in seconds
_ArtBloom         0–1 multicolour final art state
_Cracks           0–1 floor fracture state
_NormalStrength   Perceived carving depth

ChamberPresentation drives these through MaterialPropertyBlocks by default. Its glyph colour,
intensity multiplier and pulse speed are exposed for art direction. To animate materials independently,
disable Drive Materials From Show; the component releases its property overrides. Then animate the
material properties using a Unity animation, Timeline integration, or your own material controller.

SHOW STATES
0–21: original Frequency Vortex remains active.
21: dormant chamber, with the dragon represented by the overhead architecture.
26: floor fissures; 52–55: repair.
59–61: wall activation and first dragon silhouette.
63–69: added Chamber_Unfold animation and opening canopy ribs; three columns tilt/separate at their joints.
69–98: preserved dragon clips evaluated at the show time; existing VFX use its head/chest positions.
98–101: dragon contracts and descends into the Well; the room shifts to full-spectrum artwork.
111 onward: wall light recedes while the existing show title is displayed.
These are deterministic scripted states, including reconstruction after a seek. There is no runtime
destruction simulation. The six-second unfold clip is new; the original five clips are untouched.

DRAGON PRESERVATION
The chamber references Assets/LordLyricalDragon_Unity/Prefabs/LordLyrical_CyberDragon.prefab.
It does not overwrite the original meshes, armature, materials, animation controller or five clips.
Runtime material copies briefly dim the concealed reveal and are destroyed when the presentation ends.
The Blender chamber contains an inspection copy of the existing dragon; the original dragon .blend remains separate.
Dragon_Preservation_Verification.json records the SHA-256 comparison of all 55 original Unity asset files
and the original Blender file. All matched after integration.

TOOLS
Lord Lyrical > Chamber > Build and connect chamber
Lord Lyrical > Chamber > Validate and render chamber
Lord Lyrical > Chamber > Verify complete show integration
The editor tooling targets the existing LordLyrical game scripts and the supplied preserved dragon package.
The delivery package also includes the updated ExperienceConfiguration.cs, ResonantWorld.cs,
Experience.asset and MobileURP.asset needed to activate the room in this LordLyrical project.
For another Unity project, import the meshes, textures, materials, prefab, animation and Scripts folder,
and omit the game-specific Editor folder. URP is required.

VALIDATION
Nine chamber states rendered in Unity; full-show preview tested in non-chronological order, including
the vortex boundary and repeat seeks; existing core cue/clock/key tests passed. No shader or C# errors
remained in the final check. The new environment has not been profiled on a PICO headset, and the
existing APK has not been rebuilt or deployed with this chamber.

SOURCE AND LICENSE
Architecture and glyph designs are authored for this project using the supplied chamber reference.
Stone texture: Medieval Blocks 06 by Rob Tuytel, Poly Haven, CC0.
https://polyhaven.com/a/medieval_blocks_06
The supplied chamber scan and original dragon are retained; the chamber is an authored interpretation,
not a reconstruction of all geometry in the scan.
