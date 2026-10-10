# Chroma Wall MR — experimental V2

Updated 2026-10-07. JERU / Lord Lyrical remains the primary project. Chroma Wall is a separate scene and Android application, intended for experimentation before any future integration.

## Run on PICO

Open **Chroma Wall MR**. Wake both controllers. The right controller holds the can initially; this app does not require hand tracking.

- Pull the can-hand trigger to spray. Pressure, distance, angle, cap, movement and dwell affect the result.
- Press **X on the left controller** to open/close the compact 32 cm radial palette attached above that controller. It follows the controller and faces you for readability. Point with the can and pull its trigger to select.
- The centre buttons switch the outer wheel between **Colour**, **Caps**, **Finish** and **Tools**. The active colour, cap and finish are outlined. Tracking loss hides the wheel and blocks painting until it is closed and the spray trigger is released.
- Choose among 16 colours, Skinny / Standard / Fat caps, and Matte / Chrome / Holo finishes. The Fat cap has 2.04 times the original footprint close up, widening smoothly to 3.36 times at 90 cm. Skinny and Standard are unchanged.
- The can and nozzle tilt upward 30 degrees from controller forward; the ray starts at the can nozzle above the controller. This is the current interpretation of the requested 11 o'clock grip.
- Sustained wet paint produces thicker downward runs with animated wet tips. Drips continue after releasing the trigger and while starting a new stroke; they settle after five seconds. Undo restores the affected wall and its running drips.
- **Undo** restores the last painting action, up to six snapshots. **Clear** requires a second confirmation within four seconds.
- **Swap Hand** transfers the can to the left controller. The other controller then uses its A button for the palette.
- **Sound** toggles spray, shake and selection audio.

The app reads PICO's saved wall anchors when available. If none are available, open the palette and choose **Map Wall**:

1. Line up the virtual nozzle with the intended lower-left canvas corner on the real wall; squeeze the can controller's grip.
2. Mark the lower-right corner the same way.
3. Mark a point above the first corner to define canvas height.

The three points define a bounded vertical canvas. Paint is deposited only when the nozzle ray hits an accepted canvas. There is no free-space drawing. **Scan** opens the native room-capture flow when supported; manual mapping remains available without room data.

Canvases are session-only. Quitting discards the painting. Clear erases pigment but retains the currently mapped canvases.

## Implementation

- Unity 6000.3.9f1, existing URP 17.3 and PICO OpenXR 1.4 infrastructure; no package upgrades.
- Isolated runtime namespace and assets under `Assets/MRGraffiti`.
- Scene: `Assets/MRGraffiti/Scenes/MRGraffiti_Main.unity`.
- Android package: `com.fifthyang.chromawall`; app name: Chroma Wall MR.
- Four wall canvases maximum, each 1024 × 1024 pigment and finish/wetness buffers. Six bounded undo snapshots. Twelve active drips maximum. Aerosol pool: 128 particles.
- Localized GPU quad deposition, premultiplied pigment accumulation, oblique elliptical footprints and varying stochastic overspray. Paint drying uses deposition timestamps. Sustained oversaturation creates gravity-aligned runs.
- Chrome and holographic finishes use view-dependent shading; they do not capture reflections of the physical room.
- Native wall anchors are relocated during the session. Manually aligned walls live in XR tracking space; native relocalization accuracy and physical adhesion need wearer testing.
- Controller-only build configuration is temporary. The build restores JERU's package identity, version and PICO hand-tracking configuration afterward. JERU scenes and prefabs are not changed by this builder.
- Authored Blender can source: `ArtSources/MRGraffiti/ChromaWallCan.blend`; generation script alongside it. The FBX includes rolled rims, valve cup, three nozzle caps, colour band, wrapped front/back lettering and material slots.

Build with `GraffitiBuilder.Build` in Unity batch mode. GPU paint, UV orientation, coverage accumulation, undo, clear, cap/range/angle response, bounded drips and shader checks run before the Android build. Validation renders use synthetic walls, not headset footage. Logs include `GRAFFITI_READY` and periodic `GRAFFITI_STATS`.

## Acceptance still needed

V2 passes editor GPU and interaction checks and is intended for physical testing. Check actual passthrough, controller grip/nozzle alignment, automatic or manual wall alignment, paint stability while walking away/back, cap feel, undo/clear, and sustained worn-headset frame rate. Build success and editor renders alone do not establish this physical acceptance.

## JERU status

Build 36 contains the two authorized JERU changes: non-occluding room transitions and local avatar-coloured fist shields that merge when the fists meet. Guard damage logic is unchanged. The new shield surfaces are local visuals; no shield networking protocol was added. Build 35 remains a rollback. Chroma Wall does not replace either JERU package or its desktop launcher.

## V2 record — 2026-10-07

Controller-attached radial UI replaces the large floating panel. Fat cap uses distance-based taper/flare; aerosol spread follows the same footprint model. Drip tips share one bounded mesh and shader, with no new package dependencies. Automated checks include all radial colours and caps, finish/tool navigation, two-press clear, controller movement and hand swap, tracking loss/recovery, cap footprint pixels, downward drip movement after release and during another stroke, undo restoration and settling. Physical ergonomics and worn-headset performance remain wearer acceptance items. Android version 0.2.0 / code 2; V1 retained as rollback.
