# Lobby + Map02 — restored scenery V1

## Scope

User approved the Real-ESRGAN general-model previews and requested full replacement.
Replace the entire paintings for Lobby and Map02, not other maps or UI/characters.
Do not integrate the rejected native geometric art studies.

## Implementation

- Lobby: `Assets/Art/Generated/Lobby/WoodlandWaystation_RestoredV1.png`, 6476×3884.
- Map02: `Assets/Art/Generated/Environment/Map02_AncientWoodland_RestoredV1.png`, 6144×4096.
- Both use the approved general `realesrgan-x4plus` outputs, not the anime variant.
- Builder owns references and texture import: cap 8192, bilinear, Clamp, no mipmaps,
  no compression, no NPOT scaling, no CPU-readable copy, no Standalone downscale override.
- LobbyWaterFlow receives the same restored Sprite; its UV mapping remains normalized
  against the same 26×15.6 world bounds, so no old blurry painting overlays the new pond.
- Map02 retains V5 selective color grade; extra shader sharpening is disabled because
  the restored source already resolves edges. The approved raw preview did not show V5.
- World geometry, camera zoom, stations, blockers, paths, fish habitats, combat and save
  schema are unchanged. Original paintings remain at their old paths.

## Backup

`Backups/SceneryRestoredV1/`: touched builder/test source and pre-build scenes, prefabs,
ScriptableObjects and settings. New versioned artwork never overwrites the originals.

## Memory and limitations

This is AI super-resolution, not a new hand-painted native 6K master. Some small details
are inferred/smoothed. Static source dimensions are tested, not just max import settings.
Unity inspection reports RGB24, full dimensions, approximately 72 MiB of raw pixel
data per painting (driver allocation may differ), without mipmaps/CPU copies. They live in separate scenes.
No claims about FPS or mobile support without profiling on the target device.

## Verification status

Two complete builder passes succeeded (`RESTORED_SCENERY_BUILD_PASS_1` and `_2` in
`Logs/Editor.log`), followed by automated validation passed. All 73/73 EditMode tests
passed, job `f5ce52023b4b400db30f003dd4b5480c` (17.04 seconds).
Full PlayMode 69/69 passed, job `3ea4fe66697e44b089018c78798733ca` (186.48 seconds).
The first run found an over-broad new assertion that included non-water meshes under
Environment; narrowed it to FlowingStream/FallingWater, then reran the FULL suite.
The latest Lobby test verifies both water materials share the restored floor texture,
visible water motion, pause/resume, 10 fish containment, all three station routes and
portal behavior. Map02 tests verify navigation, combat, 20 fish and produce screenshots.

Visually inspected native 1920×1080 runtime screenshots:
- `Assets/Screenshots/Lobby_RestoredV1_Ho_Verified.png`
- `Assets/Screenshots/Map02_RestoredV1_Ho_Verified.png`
- River capture: `Assets/Screenshots/Map02_RestoredV1_Song_Verified.png`.

Replacement complete for Lobby and Map02. Other maps intentionally unchanged. Original
paintings and pre-build backups retained; no player save reset or rejected study import.
