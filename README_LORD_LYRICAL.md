# Lord Lyrical: The Resonant Realm

A 121-second network-controlled XR music experience for up to ten participants. This is a new, isolated Unity project assembled from the supplied Drive materials. Daniel's original project source was not supplied or located; this does not replace his project.

## Open and run

1. Open this folder in **Unity 6000.3.9f1**, with Android Build Support, SDK, NDK and OpenJDK installed.
2. Run `Start_NetSync_Server.bat`. It uses uv to install the exact **styly-netsync-server 0.17.4** into a local `.venv` and starts both NetSync and the venue authority. The operator opens at **http://127.0.0.1:8810**.
3. Open `Assets/LordLyrical/Scenes/Experience.unity` and press Play, run `Builds/Desktop/LordLyrical.exe` for desktop review, or launch the installed PICO APK.
4. Wait for connected participants in the operator. Choose Adaptive or FullCast, then **Arm & start countdown**. A five-second countdown precedes the shared start.
5. After Complete, select **Reset show**. Device reservations survive reset. **Release absent slots** frees disconnected devices between sessions.

The audience sees no operator UI. The localhost operator is available only on the server computer. The developer scrubber deliberately changes the epoch for every client; use it during rehearsal. Desktop navigation uses WASD and Q/E. Choreography never moves the XR origin.

## Versions and networking

- Unity package `com.styly.styly-netsync@0.17.4`; Python server **0.17.4**; protocol **8**.
- URP **17.3.0**, Unity OpenXR, PICO OpenXR SDK pinned to `release_1.4.0` (its package reports **1.4.1**), Input System.
- PICO OpenXR loader, PICO Support, OpenXR Extensions, PICO 4 / Neo 3 / PICO 4 Ultra controller profiles, display refresh extension.
- Android ARM64 / IL2CPP / OpenGLES3 / linear color / single-pass instanced stereo / Internet permission.
- TCP **5555** control, **5556** broadcast, **5557** pose uplink; **8800** NetSync REST; UDP **49999** discovery. The stable upstream default is 9999; this project and launch scripts explicitly agree on 49999.
- Operator port **8810** stays loopback-only. Allow the NetSync ports on the venue private LAN; do not expose the venue services to the public Internet. Put all headsets and server on the same LAN and disable client isolation on the exhibition access point.

The `Experience.asset` configuration lives in `Assets/LordLyrical/ScriptableObjects`. Set `serverOverride` to the venue server's fixed LAN address when discovery is unsuitable, then run **Lord Lyrical > Build Show Scene** to bake networking settings into the scene before building. An empty address selects discovery. Room defaults to `lord-lyrical`. The launcher reads room, initial mode and countdown from this asset when starting a new authority; restart the venue services after changing these defaults. Use `Tools/show_authority.py --help` for direct authority arguments. The operator can change mode between sessions.

## Clock and recovery

The authority publishes one atomic state document containing boot ID, revision, run ID, phase and monotonic start epoch. A four-timestamp exchange estimates its clock from each client's DSP clock. The track starts through `AudioSource.PlayScheduled`; sample position follows small bounded pitch corrections and a 180 ms hard recovery threshold. Visual state is evaluated from audio time. This measures software clock/sample alignment, not acoustic output latency between headsets.

Each server device ID keeps one of ten unique Root Key slots until released. A temporary reconnect recovers that slot and seeks directly to the current epoch. Late join reconstructs persistent environment, characters and VFX; historical impact bursts/haptics are skipped. Authority restart creates a new boot ID and safely returns clients to Ready; reservations are in memory and must be reallocated after that restart.

Application suspension or an audio-device change clears the DSP clock estimate and reacquires three clock samples before reconstructing playback. Physical headset sleep/wake remains a device acceptance test.

FullCast requires ten connected assigned clients. Adaptive permits one to ten, with deterministic spectral role stand-ins. The eleventh figure, Lord Lyrical, is never allocated a participant slot. Actual remote poses have separate physical-presence avatars; artistic role choreography is independent.

## Sequence and editing

`Resources/Cues.json` contains the authored cue IDs, times, durations, role masks and VFX IDs 1–30. Main sections are vortex (0–21), chamber/Orcs (21–59), wall pulse (59–61), Dragon reveal (63–69), breath and response (69–90), Discordant/charge (90–98), The Word (98–101), evolving cymatic artwork and credits (101–121).

Encounter controllers and `ShowFrame.At` reconstruct the visual state at any show time. Edit a cue's `time` to retime it; keep `authoredTime` as its original choreography anchor. `CueTimeline` interpolates the continuous poses and transitions between those anchors while leaving the music clock unchanged. Preserve cue order and the 0/121-second endpoints. Cue effect durations remain expressed in actual seconds. Source audio is 121.733 seconds; the authored show fades the final second and ends at exactly 121 seconds. The untouched source file is retained.

## Builds and tests

Run **Lord Lyrical > Build PICO APK** in Unity or close Unity and run `Build_PICO.bat`. Output: `Builds/LordLyrical-PICO.apk`. Unity also has **Build Desktop Test Player**. Use ADB `install -r` and launch package `com.fifthyang.lordlyrical` for device deployment.

The delivered APK is a signed development build, version 1.0, minimum Android API 29 and target API 36. Its manifest includes Internet and the storage/images permissions used by NetSync's device-ID provider. Grant that provider's permission request on first headset launch so it can resolve a persistent device identity. Final store signing and store submission are outside this LAN sideload delivery.

- `Start_10_Client_Simulation.bat` runs the official simulator in a test room. Run it only during rehearsal with audience clients disconnected; see the pose observation in TEST_REPORT.md. It tests NetSync poses/identity, not show-specific role RPCs.
- `Tools/network_load_test.py` exercises ten show-aware clients, reconnection and two full sessions against the running server.
- `python -m pytest Tools/test_authority.py` runs authority/cue-table tests (install pytest in the environment).
- Unity menu **Lord Lyrical > Tests > Verify Core** checks nine groups covering cue ordering, exactly-once dispatch, retiming, key uniqueness, clock math/reset, serialization and late-join reconstruction.
- **Render Sequence Frames** captures thirteen checkpoints into `Evidence/Frames`.
- `Tools/verify_player_checkpoints.py` reconstructs and captures six checkpoints in the built desktop player, then checks reset.
- `Tools/verify_unity_sessions.py` drives two full real-time sessions against a running Unity client and writes telemetry into `Evidence`. Add `--player Builds/Desktop/LordLyrical.exe` to launch the built player and also test process loss at 78 seconds and recovery at 85 seconds. This test renders every frame explicitly into a 1280×720 URP buffer, including while its window is hidden, and captures image evidence. Run from the project folder with its `.venv/Scripts/python.exe` after starting the server.

See `TEST_REPORT.md` for actual results and `ASSET_REPLACEMENT_LIST.md` for production art substitutions. **No physical PICO headset was connected for validation.** APK build success alone does not establish headset performance, tracking, audio output or venue alignment.

## Troubleshooting

No participants: check server/authority processes, matching room, LAN isolation and ports; inspect `Logs` and Unity Console. Protocol mismatch: reinstall both sides at 0.17.4. All slots reserved: reset then release absent slots. Authority unavailable: restart the authority between sessions; this returns clients to Ready. No audio: verify the configuration's track and the device's output volume. No XR: check the Android loader and PICO features, then inspect logcat. Low frame rate: use the procedural chamber fallback and reduce quality after testing on the actual headset. Shared room registration still requires physical venue calibration and a walk-through before admitting an audience.

## Technical references

[STYLY NetSync develop](https://github.com/styly-dev/STYLY-NetSync/tree/develop), [PICO OpenXR SDK](https://github.com/Pico-Developer/PICO-Unity-OpenXR-SDK), [SPZ format](https://github.com/nianticlabs/spz). Source art/audio remain the supplied project's materials; package licenses remain with their respective dependencies.

