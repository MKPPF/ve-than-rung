# Map02 V4 — Tàn Tích Suối Ngọc

Update after Color V5: the pending entrance-width builder change below has now
been generated twice and validated. Strengthened navigation checks pass; full
interactive Editor runs pass 70/70 EditMode and 69/69 PlayMode. See
`Docs/Verification/MAP02_COLOR_V5_VERIFICATION.md`. The pending notes below describe the earlier
V4 handoff and are retained as history.

## User feedback and scope

The user rejected V3 as too simple and explicitly requested a new map plus new enemy skills and movement. V4 replaces V3's repeated rock/tree maze with a complete painted forest ruin, camp, pond/waterfall and bridge. No claims that the rejected V3 met the user's visual expectations.

- Builder remains source of truth. The new partial `Assets/Editor/TopDownGameBuilder.Map02.cs` owns painting placement, traced paths, filled terrain collision and enemy anchors.
- Painting is 36×24 world units, source 1536×1024. It is not native 4K and can look softer than sprites at close camera zoom. No artificial upscale.
- Camera is constrained to the painting. Filled blocked regions prevent enemies spawning inside groves/water, not merely crossing a rim collider.
- Eight Goblins/four archers, target population 16, region level 8–20 retained. Spawn/exit/chest/item anchors move to the painted layout; map id and Map03 destination remain stable.
- Invalid arrivals from the retired layout recover to the entrance, preserving inventory, levels, equipment and progress.
- Existing reusable campfire effects + new fixed-count pond ripples and waterfall spray use scaled gameplay time. They do not create damage/collision.

## New tactics

- Goblin: **Vòng Sườn Truy Kích / Ảnh Bộ Song Trảm**. Offset flank approach, short last-seen pursuit, home leash, physics dash from mid-range, two separately telegraphed directional slashes, then retreat. No transform teleport. Combo cooldown 5.5 s, dash windup 0.48 s, second slash warning 0.38 s.
- Archer: **Đổi Chốt Né Bắn / Vũ Tiễn Phong Tỏa**. Selects terrain-valid firing positions with line of sight instead of orbiting continuously. Diagonal evasive backstep when approached (4 s cooldown). Arrow rain locks the player's position, warns for 0.95 s, and hits once inside radius 1.45 for 1.35× base damage; escaping the warning dodges it. Cooldown 7.5 s. Regular aimed/fan shots remain as filler attacks.
- Both controllers own their whole pattern; they no longer invoke the old generic signature simultaneously.
- ForestNavigation builds one clearance-aware terrain grid, ForestPathAgent caches each enemy's route corner; no path search on every frame.
- Existing EnemyCharacterAnimator owns facing/poses and six-frame sheets. This iteration changes AI/skills, not new four-direction enemy sprite sheets.

## Art provenance

Built-in imagegen skill/tool, one new full-map image. Saved to `Assets/Art/Generated/Environment/Map02_AncientWoodlandV4.png`. Existing V3 texture and original generation output retained. No external asset/license dependency added.

Final generation prompt:

> Create a finished high-end 2D top-down action RPG playable environment background painting, landscape 3:2 aspect ratio, native 3072x2048 pixels if possible. A lush ancient woodland ruin and goblin encampment, rich handcrafted game illustration comparable to a premium hand-painted fantasy game, NOT a diagram or tilemap. Camera straight overhead with subtle front faces of trees/buildings, no horizon. World spans 44 metres wide by 30 metres tall. Organic broad connected walking clearings and flowing paths, aged stone paving partly reclaimed by emerald grass, little wildflowers, fallen leaves. Compelling detailed landmarks: upper central ruined circular shrine with half-standing vine-draped arch and softly glowing turquoise rune; western lower clearing contains small goblin camp with three ragged ochre tents, wooden weapon rack, barrels and glowing campfire clustered along its edge leaving center open; right side has crystalline turquoise pond under a short waterfall with stone ruins and ferns around it; lower right ancient moss stone gateway beside a broad playable clearing. Dense layered varied tree canopies and rocky banks provide a natural enclosing perimeter, not small stones laid in lines. Narrow winding creek flows along the bottom edge with one old wooden bridge entering lower-left clearing, keep all main clearings connected by dry paths. General navigable layout: entrance at upper left 12% x / 23% y, west clearing at 25% x / 48% y, camp clearing at 22% x / 76% y, middle lower clearing at 42% x / 66% y, shrine courtyard at 44% x / 27% y, largest battle clearing at 64% x / 47% y, treasure alcove at 80% x / 23% y (leave empty for real chest), pond landmark at 87% x / 47% y, exit clearing at 82% x / 75% y. No hard cell outlines, no polka-dot stones, no repeated identical trees. Paths should have breathing room for player and enemies, with decor mostly in margins. Sunbeams and soft forest shadows, luminous water, tasteful amber campfire contrast against cool jade foliage, detailed readable materials, crisp confident painted edges not pixel art. No characters, monsters, player, treasure chests, HUD, text, labels, grid, border or watermark. This is the actual complete game environment art, not a concept sheet.

Final world scale chosen as 36×24 after inspecting the actual output; prompt dimensions are not treated as delivered image resolution or final gameplay geometry.

## Verification status

- Initial and final builder two-pass runs + validator passed: `Logs/Map02V4_Builder.log`, `Logs/Map02V4_FinalBuilder.log`.
- EditMode 70/70 passed: `Logs/Map02V4_EditMode.xml`.
- Full batch PlayMode: 66/68 passed (`Logs/Map02V4_PlayMode.xml`). Existing synthetic-click failure retained; the new route test reached the chest but its first approach was outside interaction radius. Corrected the test approach, not chest collision or interaction logic.
- Focused PlayMode in existing Editor: **6/6 passed**, job `2d3a6faa38d04c98a816b3b99358fb99`. Includes real routes through camp, shrine, chest interaction and exit; dead-archer cancellation, dodge-behind slash, rain warning/pause/escape/damage, death/reset, physical dash/evasion and controlled two-enemy combat.
- Recorded 96 frames at 12 fps, `Captures/Map02V4_Combat.mp4`, 8 seconds. This is a controlled encounter with two active enemies, other enemies/spawner held still, test-save player stats. Not a full progression balance test. Inspected frames 5 and 10 showing dash + marked rain/falling arrows.
- Later strengthened EditMode navigation assertion found entrance disconnected for the enemy clearance grid: 69/70, job `8d359f769c2d4ba2853e0ba263ae5a3e`. Widened the entrance elbow polygon in the builder; independent raster/flood check connects it to the main area (714 navigable sample cells versus isolated 12 before). **This last builder tweak is NOT yet regenerated/verified in Unity.**
- User opened Unity during batch testing, then was playing `Assets/Scenes/Lobby.unity`. Did not stop the user's Play session or overwrite it. Final builder two passes, strengthened EditMode check and final PlayMode rerun require Stop first. Current on-disk scene includes the new painting/tactics but predates the final entrance-width adjustment.
- Inspected gameplay-zoom and overview renders. Actual path/skill tests required before completion.
- Existing isolated test-save system used. No `PlayerPrefs.DeleteAll`, real save reset or schema migration.
- Backups: `Backups/Map02_PaintedV4/`, including pre-change source/tests/docs, scenes, prefabs, data and settings.

## Known limits

- V3 full suite had one existing synthetic-click test failure in batch. Keep that result distinct from Map02's new gameplay tests.
- Map source artwork is 1536×1024, not requested 3072×2048. Visual composition improves substantially, but this is not a source-resolution upgrade.
- No new sound recordings or new directional animation sheets. No full loadout balance claim.
- Final verification is pending the user's active Play session ending. Do not mark MAP2 complete yet. Final source change is in `TopDownGameBuilder.Map02.cs`; rebuild through `Map02Verification.BuildAndValidate`, then run EditMode + PlayMode. Inspect the collision-overlay capture (renderer enable/size fix is now in capture helper).
