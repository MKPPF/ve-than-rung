# Map02 V3 — Rừng Cổ Lá Đồng

## Scope — 08/09/2026

- Source of truth: `Assets/Editor/TopDownGameBuilder.cs`. No hand-edited scene content.
- New six-world-unit repeating leaf/soil ground, soft world-space walkability blend, larger combat clearings, low mossy boundary stones/logs, large trees outside walkable areas.
- Existing entrance (-17,8), chest (13,9), item (8,3), exit (16,-7), Map03 transition and pond landmark retained.
- Eight authored Goblins + four archers retained; two archer anchors moved away from clutter. Population target 16 and level 8–20 unchanged.
- Map02 opts out of radial respawn-anchor expansion; respawns use its 12 validated anchors plus the existing small jitter, camera, player-distance and collider checks.
- Forest melee now checks locked direction, range and solid scenery. Forest detection and release check line of sight; triggers and character layers do not occlude sight.
- Archer death during windup cancels release. Disabling a forest controller stops its pending local actions.
- Goblin signature slashes also respect their visible directional sectors; forest signature attacks check scenery on continuation.
- Windups: slash 0.14 → 0.30 s; leaf dash 0.24 → 0.48 s; quick shot 0.22 → 0.40 s; volley 0.32 → 0.65 s. Slash recovery 0.12 → 0.24 s. No HP/damage/reward/level changes.
- Backups: `Backups/Map02_RuinsV3/` contains original builder/runtime/tests/docs, scenes, prefabs, data and settings before generation. Existing player save was not reset or migrated.

## Art provenance

Built-in image generation tool via imagegen skill; one new ground texture, existing prop and enemy artwork reused without image edits.

Final asset: `Assets/Art/Generated/Environment/Map02_LeafSoilV3.png` (1254×1254).

Final prompt:

> Use case: stylized-concept. Asset type: seamless square terrain texture for a top-down 2D fantasy action RPG, six by six world units per tile. Create a beautiful hand-painted overhead forest-floor ground texture, muted grey tan earth, soft desaturated sage moss patches, a sparse scattering of small warm copper fallen leaves and tiny weathered slate fragments. Broad soft organic color variation with refined small details. Surface fills all four edges, seamless matching edges, even diffuse lighting with no baked shadows. Subdued contrast so enemy silhouettes remain clearly readable. No trees, bushes, grass tufts, buildings, paths, characters, large stones, focal point, border, text or watermark. Not pixel art, not yellow, not noisy. This is a small repeating ground tile, NOT an entire landscape illustration.

## Verification

- First two builder passes + validator passed: `Logs/Map02V3_Builder.log`.
- Final two passes after respawn-anchor change + validator passed: `Logs/Map02V3_FinalBuilder.log` (pass markers at lines 2065/3660, validation at 3792).
- EditMode: 70/70 passed, zero failures/skips (`Logs/Map02V3_EditMode.xml`). Includes generated enemy clearance/separation, own terrain material/mask, directional sectors and scenery occlusion.
- PlayMode final full run: 64/65 passed, 1 failed, zero skipped (`Logs/Map02V3_PlayModeFinal.xml`). All three new Map02 runtime tests passed. The original run passed 63/65 because the new route test initially tried walking into the solid chest centre; corrected to its interaction approach, without altering chest collision or gameplay.
- Focused rerun: all three Map02 tests and two projectile tests passed (`Logs/Map02V3_Focused.xml`); `ClickShotTests.LeftClickShootsButUIClickDoesNot` still fails in the selected batch suite before Map02 loads. Diagnostic: second synthetic mouse press reports `pressed=False`, input unblocked, timeScale=1, nextShot=0. Explicit release update did not fix it. Temporary diagnostics/test edits were reverted; player input/runtime unchanged. This issue remains open, and the entire suite is NOT claimed green.
- Inspected `Assets/Screenshots/Map02_V3_ToanCanh.png` and `Map02_V3_BaiGiaoChien.png` at 1920×1080. Local-scale ground detail and clear actor silhouettes; modular repetition is still visible in the overview.
- Additional still: `Assets/Screenshots/Map02_V3_TanTich.png`.
- Runtime walking/capture test passed for six waypoints including chest approach and exit, plus targeted dead-archer and dodge-behind-Goblin regressions. Test scenes use the existing isolated test save.
- Visually inspected `Assets/Screenshots/Map02_V3_Runtime_Verified.png` (1920×1080): real scene, player, enemies, health bars and HUD. Enemies/population are paused separately during the navigation/capture test to isolate collision; the image is not evidence of a full live fight or loadout balance. The test save's existing 2,000,000 gold grant and level 1 appear in HUD, not the user's progress. Normal gameplay zoom remains 5.6.

## Limits

- This is a modular map rework, not new enemy animation sheets or a bespoke full-map painting.
- Existing level/variant/reward balance is preserved; no end-to-end balance claim for every equipment loadout.
- Existing obsolete API warnings in unrelated ClickShot tests are not changed by this task.
- Batch PlayMode logs include existing empty-test-scene audio-listener warnings. No C# compilation errors or shader failures were observed. No claim of a warning-free full Console.
- Full builder regenerates all generated scenes/prefabs per project instructions; non-Map02 content definitions are unchanged.
