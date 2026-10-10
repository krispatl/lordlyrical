# Lord Lyrical / JERU

Full editable Unity project for the music-led PICO mixed-reality concert. This publication snapshots **Production 43 / Android 1.0.43**, including the current dragon, JERU, ten participant avatars, Orc combat, effects, soundtrack, authored cues, networking authority and Windows show launcher. Existing local Git history is retained.

## Get the project

Install Git with **Git LFS**. Clone the repository, then run `git lfs install` and `git lfs pull` in it. GitHub's automatic source ZIP does not reliably include the actual large assets; use a Git/LFS checkout. Add the repository root in Unity Hub using **Unity 6000.3.9f1** with Android Build Support (SDK/NDK/OpenJDK). Allow Unity to restore the pinned packages from `Packages/manifest.json` and `packages-lock.json`.

Open `Assets/LordLyrical/Scenes/Experience.unity` for the networked show. `Assets/LordLyrical/Scenes/OfflineDemo.unity` is the standalone demo. All asset `.meta` files, project settings and source art are included. Original Blender models/rigs and historical authoring scripts are in `ArtSources/Blender/`. Historical MRGraffiti assets remain for reference; active Chroma Wall development is in the separate [Chroma repository](https://github.com/krispatl/chromawall).

## Windows launcher

Install **Python 3.11** with the Windows `py` launcher, or install **uv**. Double-click `Launch_Lord_Lyrical.vbs`. On first use it runs `Setup_Launcher.ps1`, creates a project-local Python environment and installs `styly-netsync-server==0.17.4`. This first setup needs internet; show operation uses the local venue network. The browser launcher is at `http://127.0.0.1:8811`; the authority/operator is at `http://127.0.0.1:8810`.

Install the networked APK on every participating PICO, open it from the headset library, and connect headsets and PC to the same reachable LAN. Align each headset to the shared floor/reference location using the in-app instructions. Start enables only when all connected participants are awake, aligned and reporting fresh status. Adaptive mode supports 1–10 participants; FullCast requires ten. Remote app opening from the launcher additionally requires USB debugging or paired wireless debugging; wireless concert participation itself does not require USB.

Allow the venue server through Windows Firewall on the active network profile. Network services use TCP 5555/5556/5557/8800 and discovery UDP 49999 (TCP fallback also supported). The operator endpoints on 8810/8811 are local to the PC. Guest network isolation can block headset discovery despite matching Wi-Fi names.

## Build and verify

Close this project in Unity before running a batch build. `Build_PICO.bat` / `Tools/Launch.ps1 -Mode BuildPico` invokes `LordLyrical.Editor.ShowBuilder.BuildPico`, producing `Builds/LordLyrical-PICO.apk`. Standalone uses `OfflineDemoBuilder.Build`, producing `Builds/LordLyrical-OfflineDemo-PICO.apk`. These variants have separate Android packages. Do not run historical asset-generation builders casually; some reference original workstation output paths.

For authority tests: `.venv\Scripts\python.exe -m pip install -r Tools/requirements-dev.txt`, then `.venv\Scripts\python.exe -m pytest Tools/test_authority.py`. Unity verification and additional network checks are included in the editor scripts and `Tools/`. Existing evidence records are historical, not proof of acceptance on a new headset or venue.

## Timing and delivery status

The pre-song opening is 17 seconds. The title dissolves, a three-second pause precedes JERU's arrival, and the soundtrack starts on the existing portal cue. The cooperation window is song seconds 92–98, finishing strike at 98 and timed defeat by 101. MR return completes at 118.2; JERU fades by 124; the farewell title appears at 124.25 and dissolves by 129. The original music and authored cue timing are preserved.

Build 43 networked and standalone APKs passed their recorded build/automated checks and were installed on PICO. Physical multiplayer choreography and worn-headset visual/frame-time acceptance remain separate from simulated transport checks. See `CURRENT_PRODUCTION_BUILD.txt`; older root reports describe earlier versions.

Unity caches, Python environments, machine-local logs, signing secrets and generated build directories are excluded. Large source files use Git LFS. Third-party art, packages, music and likenesses retain their original ownership and licenses; publication does not grant additional redistribution rights.

The current installable APKs remain in the original local delivery workspace. GitHub APK release publication is pending explicit approval; this repository contains the complete editable source and build instructions.
