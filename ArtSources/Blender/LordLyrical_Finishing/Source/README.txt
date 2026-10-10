AUTHORING SOURCE NOTES

The saved .blend files and current Unity C# scripts are the editable source of truth.

build_props.py and character_distance.py are Blender authoring scripts. finish_audio.py creates the added non-vocal accent audio and procedural detail normals. They use paths from this workstation; adapt those paths before rebuilding elsewhere.

integrate.py, finish_runtime.py and review_fixes.py are historical one-time migration scripts. Their text substitutions have already been applied. Do not rerun these against the finished project. Later C# refinements are present in the saved Unity project and mirrored Unity folders.

verify_desktop.py requires the local authority and launches/stops its own Windows player. It performs two complete sessions and resets that test room. Do not use it during an audience session.

finalize_report.py, package_sources.py and verify_artifacts.py package and audit this workstation's finished deliverables. Their source paths and acceptance notes are specific to this delivery.
