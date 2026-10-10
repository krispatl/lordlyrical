"""Read-only capture of a wearer-operated Lord Lyrical headset review.

Start this tool, align the real headset, then start the show from the operator.
Only GET /status is used. Each capture writes new files; existing evidence is retained.
Telemetry cannot confirm the real-room camera image, visual quality, or comfort.
"""
from __future__ import annotations
import argparse
import json
import math
from pathlib import Path
import statistics
import time
import urllib.error
import urllib.request

WINDOWS = {
    "VR show": (9, 121),
    "Main action (baseline comparison)": (9, 111),
    "Orc combat": (39, 51),
    "Dragon and JERU walk": (69, 91),
    "Crescendo": (91, 105),
    "Finale": (111, 121),
}

def percentile(values, fraction):
    ordered = sorted(values)
    if not ordered:
        return None
    pos = (len(ordered) - 1) * fraction
    lo = math.floor(pos)
    hi = math.ceil(pos)
    return ordered[lo] + (ordered[hi] - ordered[lo]) * (pos - lo)

def analyze(rows, version=16, reason="imported recording"):
    # Each row must be one distinct telemetry message, not a repeated HTTP response.
    unique = []
    seen = set()
    duplicates = 0
    for row in rows:
        t = row.get("telemetry", {})
        key = (row.get("bootId"), row.get("runId"), row.get("clientNo"),
               t.get("received"))
        if t.get("received") is None or key in seen:
            duplicates += 1
            continue
        seen.add(key)
        unique.append(row)
    data = [r["telemetry"] for r in unique]
    def window(a, b):
        return [t for t in data if t.get("phase") in ("Running", "Ending", "Complete")
                and a <= t.get("time", -1) < b]
    def stat(a, b):
        frames = window(a, b)
        fps = [t["fps"] for t in frames if isinstance(t.get("fps"), (int, float))
               and math.isfinite(t["fps"]) and t["fps"] > 0]
        def measurements(key, positive=False):
            values = [t[key] for t in frames if isinstance(t.get(key), (int, float))
                      and math.isfinite(t[key]) and (t[key] > 0 if positive else t[key] >= 0)]
            return {"samples": len(values), "minimum": min(values) if values else None,
                    "median": statistics.median(values) if values else None,
                    "maximum": max(values) if values else None}
        quality = [t["vfxQuality"] for t in frames if isinstance(t.get("vfxQuality"), int)
                   and 0 <= t["vfxQuality"] <= 2]
        return {
            "samples": len(fps), "minimum": min(fps) if fps else None,
            "p10": percentile(fps, .1), "median": statistics.median(fps) if fps else None,
            "maximum": max(fps) if fps else None,
            "samplesBelow70": sum(v < 70 for v in fps),
            "maxWorldRenderMs": max((t.get("worldRenderMs", 0) for t in frames), default=None),
            "vfxQualitySampleCounts": {str(q): quality.count(q) for q in sorted(set(quality))},
            "unknownVfxQualitySamples": len(frames) - len(quality),
            "measurements": {key: measurements(key, key.endswith("Ms")) for key in
                             ("worldRenderMs", "gpuFrameMs", "mainThreadMs", "renderThreadMs",
                              "drawCalls", "triangles", "activeParticles", "memoryMb")},
        }
    valid_times = [t["time"] for t in data
                   if t.get("phase") in ("Running", "Ending", "Complete")]
    gaps = [b["sampleAt"] - a["sampleAt"] for a, b in zip(unique, unique[1:])
            if "sampleAt" in a and "sampleAt" in b]
    identities = {(r.get("bootId"), r.get("runId"), r.get("clientNo")) for r in unique}
    identity_known = bool(unique) and all(r.get("bootId") and r.get("runId") is not None
                                        for r in unique)
    version_ok = bool(data) and all(t.get("animationVersion") == version for t in data)
    opening = [t for t in data if t.get("passthroughActive") and t.get("roomOpacity") == 1]
    vr = window(10, 111)
    checks = {
        "expectedBuildOnly": version_ok,
        "singleKnownRunAndClient": len(identities) == 1 if identity_known else None,
        "recordedCountdown": any(r.get("phase") == "Countdown" and r.get("countdown", 0) >= 15
                                 for r in unique),
        "songBeginningAndEndingCaptured": bool(valid_times) and min(valid_times) < 2.5 and max(valid_times) >= 120,
        "noTelemetryGapOverFourSeconds": bool(gaps) and max(gaps) <= 4,
        "nativePassthroughDuringGathering": any("gathering" in t.get("openingStage", "") for t in opening),
        "nativeArrivalParticles": any("JERU arrival" in t.get("openingStage", "")
                                       and t.get("arrivalParticles", 0) > 0 for t in opening),
        "portalWithSong": any("Portal" in t.get("openingStage", "") and 0 < t.get("time", 0) < 8
                              and t.get("passthroughActive") for t in data),
        "nativeProjectionRegistered": bool(data) and all(t.get("projectionLayerReady") is True
                                      and t.get("compositionProvider") == "OpenXRLayerProvider" for t in data),
        "transparentOpeningCamera": bool(opening) and all(t.get("cameraBackgroundAlpha") == 0
                                     and t.get("cameraHdr") is False
                                     and t.get("cameraPostProcessing") is False for t in opening),
        "passthroughOffInVR": bool(vr) and all(t.get("passthroughActive") is False
                                               and t.get("roomOpacity") == 0 for t in vr),
        "sharedMarkAligned": bool(data) and all(t.get("roomAligned") is True for t in data),
        "singleDragon": bool(data) and all(t.get("dragonOwners") == 1 for t in data),
        "noReportedPoseConflict": bool(data) and all(t.get("animationConflicts") == 0 for t in data),
        "noReportedApplicationError": bool(data) and all(t.get("error") == "" for t in data),
    }
    coverage_keys = ("expectedBuildOnly", "singleKnownRunAndClient", "recordedCountdown",
                     "songBeginningAndEndingCaptured", "noTelemetryGapOverFourSeconds")
    return {
        "expectedVersion": version, "observedVersions": sorted({t.get("animationVersion") for t in data if isinstance(t.get("animationVersion"), int)}), "stopReason": reason,
        "participantScope": "One selected headset slot; this does not validate all ten participants.",
        "uniqueTelemetrySamples": len(unique), "duplicateOrUntimedRowsIgnored": duplicates,
        "checks": checks,
        "completeRecordingCoverage": all(checks[k] is True for k in coverage_keys),
        "longestObservedSampleGapSeconds": max(gaps) if gaps else None,
        "fpsBySequence": {name: stat(*bounds) for name, bounds in WINDOWS.items()},
        "motionPeakDegreesPerTelemetryWindow": {
            key: max((t.get(key, 0) for t in data), default=None)
            for key in ("jeruLegMotion", "jeruArmMotion", "orcWalkMotion",
                        "orcAttackMotion", "dragonWingMotion", "dragonTailMotion")
        },
        "performanceComparability": "Compare the same view, effects quality, firmware, build type and headset thermal state. Main/render thread markers include waits. Negative or missing counters are unavailable, not zero. Older recordings have unknown effects quality.",
        "visualPassthroughConfirmed": None, "wearerComfortConfirmed": None,
        "fullShowPerformanceAccepted": None, "overallAccepted": False,
        "scope": "Native telemetry samples, normally every two seconds. Not individual frame timings. Missing a short phase is inconclusive. Actual room visibility, animation quality, comfort, and performance acceptance require wearer review.",
    }

def capture(args):
    args.output.mkdir(parents=True, exist_ok=True)
    stem = time.strftime("Headset_Review_%Y%m%d_%H%M%S") + "_" + str(time.time_ns() % 1000000)
    target = args.output / (stem + ".ndjson")
    rows, seen = [], {}
    locked_identity = None
    deadline = time.monotonic() + args.wait_seconds
    last_fresh = None
    reason = "wait expired before an observed show start"
    print("Waiting for a wearer-operated show. Use the existing operator to start.", flush=True)
    with target.open("x", encoding="utf-8") as out:
        while time.monotonic() < deadline:
            try:
                with urllib.request.urlopen(args.url.rstrip("/") + "/status", timeout=4) as response:
                    status = json.load(response)
            except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as error:
                if locked_identity and last_fresh is not None and time.monotonic() - last_fresh > 8:
                    reason = "authority unavailable during recording: " + str(error)
                    break
                time.sleep(.5)
                continue
            participant = next((p for p in status.get("participants", []) if p.get("slot") == args.slot), {})
            client = str(participant.get("clientNo", ""))
            tele = status.get("telemetry", {}).get(client, {})
            identity = (status.get("bootId"), status.get("runId"), client)
            if locked_identity and identity != locked_identity:
                reason = "run, authority, or participant changed; recording kept separate"
                break
            if participant.get("connected") and tele.get("animationVersion") == args.version:
                sequence = (status.get("bootId"), client)
                received = tele.get("received")
                fresh = received is not None and seen.get(sequence) != received
                seen[sequence] = received
                if fresh and not locked_identity and status.get("phase") in ("Countdown", "Running"):
                    locked_identity = identity
                    deadline = time.monotonic() + 160
                    last_fresh = time.monotonic()
                if fresh and locked_identity:
                    row = {"sampleAt": time.time(), "bootId": identity[0], "runId": identity[1],
                           "authorityTime": status.get("time"), "countdown": status.get("countdown"),
                           "phase": status.get("phase"), "clientNo": client,
                           "slot": args.slot, "telemetry": tele}
                    rows.append(row)
                    out.write(json.dumps(row) + "\n")
                    out.flush()
                    last_fresh = time.monotonic()
                    if tele.get("time", 0) >= 120 and tele.get("phase") in ("Ending", "Complete"):
                        reason = "recorded through the last second of the song"
                        break
            if locked_identity and last_fresh is not None and time.monotonic() - last_fresh > 8:
                reason = "headset stopped providing fresh telemetry"
                break
            time.sleep(.5)
        else:
            if locked_identity:
                reason = "recording deadline reached"
    report = analyze(rows, args.version, reason)
    summary = target.with_suffix(".json")
    summary.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(str(summary), flush=True)
    print(json.dumps({"samples": len(rows), "reason": reason,
                      "completeRecordingCoverage": report["completeRecordingCoverage"]}), flush=True)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--url", default="http://127.0.0.1:8810")
    parser.add_argument("--version", type=int, default=22)
    parser.add_argument("--slot", type=int, default=0, help="Zero-based participant slot")
    parser.add_argument("--wait-seconds", type=float, default=600)
    parser.add_argument("--output", type=Path, default=Path("Evidence/HeadsetReviews"))
    parser.add_argument("--analyse", type=Path, help="Analyse a saved recording instead of contacting the authority")
    args = parser.parse_args()
    if not 0 <= args.slot < 10 or args.wait_seconds < 0:
        parser.error("slot must be 0-9 and wait-seconds nonnegative")
    if args.analyse:
        rows = [json.loads(line) for line in args.analyse.read_text(encoding="utf-8").splitlines() if line.strip()]
        print(json.dumps(analyze(rows, args.version), indent=2))
    else:
        capture(args)

if __name__ == "__main__":
    main()
