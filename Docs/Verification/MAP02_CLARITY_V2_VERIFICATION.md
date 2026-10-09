# Map02 clarity V2

Only Map02 background presentation changes. Approved 6144×4096 restored bitmap,
world layout, camera, combat, fish and saves remain unchanged. Lobby is unaffected.

PaintedForestColor adds a five-sample screen-aware luminance clarity pass, strength
0.65 authored by builder. Radius is one screen pixel clamped to 1–2 texture texels.
Correction is capped at 2.5% times strength, suppressed in flat areas/strong edges,
and clamped to local luminance extrema. Existing color grade is retained; `_Detail`
remains zero. This improves edge contrast modestly, not actual source resolution or
missing painted detail; unlimited sharp zoom is not claimed. Shader adds four texture
samples, no new textures/objects. FPS impact is not benchmarked.

Backups: `Backups/Map02ClarityV2/`. Tests assert authored clarity and unchanged scene
layout; paired 1080p/2160p pond and camp captures provided by Map02ClarityVerification.
Capture restores user's scene setup; no save or Play toggle.

Verification in progress: compile, two full builds, validator, full tests and final
visual check. Initial pond capture extended beyond art because the diagnostic camera
was not constrained in EditMode; corrected the capture position, not gameplay camera.

Completed: compile, two full builds (`MAP02_CLARITY_BUILD_PASS_1` / `_2`) and validator.
73/73 EditMode passed, job `c287afd686c5449fbd642138d15f8c27`.
Paired captures created at 1920×1080 and 3840×2160 for Pond/Camp under
`Assets/Screenshots/Map02_ClarityV2_*`. Inspected camp before/after: modest finer contrast,
not a dramatic resolution upgrade.
Full PlayMode 69/69 passed, job `566180d4bc4d408fba8783d577b70e25`, 191.44 seconds.
Initial job `d23a4f48de8242889184f4c8a5389776` never started tests because the Editor
was in/entering Play; did not force Stop. Verified non-playing state before retry.
Inspected runtime pond capture `Assets/Screenshots/Map02_ClarityV2_Runtime_Verified.png`.
Complete for this bounded clarity adjustment, not for unlimited close-zoom fidelity.
