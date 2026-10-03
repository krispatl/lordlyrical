# Verification report

Executed on 3 October 2026, Windows, Unity 6000.3.9f1, NVIDIA GeForce RTX 5060. Results below distinguish real execution from configuration and untested hardware behavior.

## Passed

| Test | Evidence / result |
|---|---|
| Authority unit tests | 11 passed: unique slots, reservations/rejoin, FullCast gating including force-start, reset, invalid seeks, release guards, cue ordering and all thirty VFX IDs |
| Unity core tests | Nine groups passed: Root Keys, 72 Hz cue simulation, continuous choreography retiming, late join at 85 s, pure state reconstruction, 59–61 s wall window, Dragon/title state, clock math and DSP reset, state JSON and duplicate IDs |
| Ten-client wire integration | Exact NetSync 0.17.4 / protocol 8. Ten identities and slots, same epoch, two actual 121 s runs, disconnect at 78 s/rejoin at 85 s, preserved slot/epoch, two resets |
| Official simulator | `styly-netsync-simulator --clients 10 --room official-load-test --transform-send-rate 20`: ten clients started, zero failed |
| Unity end-to-end playback | Two real-time 121 s sessions through the actual music, DSP scheduling, scene controllers, cue scheduler, operator and reset |
| Live cue audit | All 37 cues fired once, in order, in each full Unity run. Largest frame-dispatch delay 21.0 / 21.3 ms |
| Real remote avatars in Unity | Nine remote NetSync avatar objects plus local client; live head/left/right hand poses received; keyed physical-presence component attached |
| FullCast live gate | Operator start succeeded with one Unity client and nine show-aware simulated participants. Unit tests reject fewer than ten |
| Error check | No Console errors during the two full playback runs; the later local-avatar readiness race was fixed and its populated-scene retest returned no Console errors |
| Visual review | Thirteen authored time checkpoints rendered through URP. Error shader and mirrored text were fixed. The experimental reduced scan did not pass art review and is disabled by default |
| Delivered launcher | Created its project-local Python environment, installed NetSync 0.17.4, started services, stopped them and restarted successfully. The authority reported Ready, connected, the configured room and protocol 8 |

## Measured Editor performance

| Metric | Run 1 | Run 2 |
|---|---:|---:|
| Mean FPS while Running/Ending | 71.96 | 71.94 |
| Minimum one-second FPS sample | 71.57 | 71.55 |
| Largest absolute software audio/server drift | 5.95 ms | 2.88 ms |
| Unity allocated memory range | 979.27–983.97 MB | 980.02–984.67 MB |

The target was capped at 72 FPS. These are desktop Editor measurements with one rendering client and spectral stand-ins, **not ten physical rendering headsets**. Memory includes Editor overhead. Two runs do not establish an all-day soak guarantee. No thermal, battery-drain, acoustic latency or headset GPU measurement was possible.

The separate ten-client wire test collected 2,441 clock round trips: mean 36.04 ms, maximum 140 ms on this workstation. Clock round-trip measurements are not audio drift measurements.

## Measured built desktop player

Two full 121-second sessions passed, including termination at 78 seconds and
relaunch at 85 seconds during the first run. The device identity and Root Key slot
were retained; playback reconstructed at the current shared time. Both resets
returned to Ready. Each frame was explicitly rendered through URP at 1280×720.

| Metric | Run 1 | Run 2 |
|---|---:|---:|
| Mean FPS | 71.84 | 71.86 |
| Minimum one-second FPS sample | 69.05 | 69.56 |
| Largest absolute software audio/server drift (ms) | 18.28 | 6.75 |
| Unity allocated memory (MB) | 101.70–102.83 | 102.29–103.63 |

Image encoding was active at six checkpoints. These are capped desktop GPU
measurements, not stereo PICO measurements. All 37 cues fired once and in order
in the uninterrupted second player run; maximum frame dispatch delay was
20.4 ms. The log restarts with the player process,
so the first run's complete cue log is not retained; reconnect telemetry is.

No application exceptions or telemetry errors were observed. Two missing-script
messages remain in the Windows startup log for null objects; the earlier Meta
feature serialization errors were removed. This does not verify Android startup.
The final Dragon visibility correction was subsequently checked by automated
21/63/101-second assertions, regenerated sequence frames and built-player
six reconstructed player checkpoints. Full-run measurements precede that visual
visibility correction; the timing and networking code is unchanged.

## Build and device status

Final Android build **Succeeded**, with 0 errors and 15 build warnings. The signed development APK is 96,709,251 bytes (92.23 MiB), ARM64 only, with IL2CPP and OpenXR native libraries. Manifest and signature verification passed; SHA-256 is in `Evidence/apk-inspection.json`. Build-report totalSize includes intermediate/debug output and is not the APK file size. Initial validation failures (composition-layer feature and null PICO platform App ID field) were corrected in the builder. The empty platform App ID is intentional for this LAN sideload build; PICO Platform services are not used.

ADB detected **no connected physical PICO device**. Two offline emulator transports appeared on the later inventory; neither was used or modified. Physical installation, OpenXR startup, controller/haptic behavior, audio output, refresh-rate acceptance, venue alignment and sustained performance remain unverified.

## Limits and observations

- The source-chamber surfel derivative is retained for experimentation but disabled; it did not preserve enough visual detail at the tested reduction. The reviewed procedural chamber is the default.
- Running the upstream simulator in another room at the same time as the show-aware avatar test produced unexpected remote pose samples. After stopping it, all nine expected trajectories were received. The cause was not established. Run simulator tools only during rehearsal with audience clients disconnected; this project validates one venue room.
- A real Wi-Fi outage, headset sleep/wake cycle, server-computer reboot, shared spatial calibration and long-duration device soak need physical venue testing. Simulated disconnect/rejoin and state reconstruction passed.
- Source characters and effects remain procedural art substitutions. Functioning timing/animation is not final creative approval. See the replacement list.
- Initial screen captures from a hidden Windows player were black and were rejected as render evidence. The corrected verification path explicitly renders every frame through URP into a 1280×720 buffer; it does not depend on Windows presentation of the hidden window.

Machine-readable evidence is in `Evidence/network-load-test.json`, `unity-core-tests.txt`, `unity-live-sessions.json`, `live-cue-audit.json`, `unity-remote-avatars.txt` and `fullcast-live.json`. Rendered frames and the Meshy generation screenshot are also retained there.

Additional evidence: `desktop-live-sessions.json`, `desktop-cue-audit.json`, `final-player-check.json`, `apk-inspection.json`, `launcher-check.json` and `Evidence/Player`.
