# Map02 Living V6 — fish and quieter combat warnings

## Request

User supplied a screenshot showing overlapping thick red/black warning rings
and sectors over the player, called it ugly, and asked for fish in river and pond.

## Implementation

- Forest warnings: single thin amber/orange outline, sorting -5 (below enemies 5
  and player 10). Sector is an open 130-degree arc at the existing hit radius;
  removed centre spokes, closed triangle and thick black border. Rain keeps its
  complete circle. Opacity rises 0.38 → 0.72. Radius, time and damage unchanged.
- ForestFishSchool: 20 decorative swimmers, 8 pond/12 river, spread across 8
  inset builder habitats. Body, fins, tail, back glint and eyes use the existing
  project's procedural sprite/mesh approach; no new raster art or asset download.
- Swimmers follow smooth elliptical routes, face their motion, wag their tails,
  and have faint wakes. They cannot leave their assigned inset water habitat;
  conservative body-radius padding is included. No jumping across shore/bridge.
- Fixed population/geometry, no per-frame object creation, no colliders, loot,
  interaction or saved progress. Pauses with scaled time; hides on disable and
  cleans owned meshes/material/root on destroy.
- Existing painting, V5 color, player controls, enemy skills/stats preserved.
- Source of truth: TopDownGameBuilder.Map02.cs. Backup: Backups/Map02_LivingV6.

## Verification

- Initial and final habitat-tuning builds each ran twice; validator passed.
  Final markers in `Logs/Editor.log`: 24217 / 26050, validation 26220.
- Full EditMode 70/70 passed (`6e7af3027e87466cb94bcc271dc8780e`).
- Full PlayMode 69/69 passed (`5d9e1e0ac2c640748de1561f7734674b`). Updated
  tests assert eight habitat configs, 20 fish, no wildlife colliders, pause,
  containment during combat and single ground-level warning/no central spokes.
- Inspected initial pond/river and combat screenshots. Reduced the second pond
  ellipse and shifted three river habitats upstream after seeing overlap with
  painted exposed rocks. Final focused Map02 rerun **7/7 passed**
  (`161e2e0873044600ac2667f075205e07`) after habitat adjustments.
- Inspected final pond and river captures: fish sit in clear water instead of
  the exposed-rock positions seen in the first captures. Inspected runtime combat
  warning: thin amber circle below actors, no heavy black edge. Saved as
  `Assets/Screenshots/Map02_V6_CanhBao_Verified.png`.
- Console error query after tests returned only Test Runner result-save notice,
  no runtime exception or compiler error. No real save reset.
- Pond and river screenshots are produced by the real Map02 runtime route test:
  `Assets/Screenshots/Map02_V6_HoCa_Verified.png`, `Map02_V6_SongCa_Verified.png`.

## Limitations

Decorative procedural fish, not a fishing/catching mechanic or AI raster sprites.
Small inset river pools intentionally avoid bridge and exposed rocks rather than
allowing fish to traverse the entire painted stream without an accurate water mask.
