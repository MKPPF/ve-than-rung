# Map02 — Color/readability V5

## Scope

User asked to fix the washed-out yellow-grey appearance identified in the previous visual review.

- No bitmap edits or regeneration: original `Map02_AncientWoodlandV4.png` is unchanged. Color is a reversible sprite-material shader applied only to Map02's painted Floor.
- `PaintedForestColor.shader` uses soft color masks to cool olive/yellow foliage, deepen mid-shadows, reduce yellow in soil and slightly enhance local detail. Cyan water is protected. Handles the project's linear color space explicitly.
- Detail enhancement is bounded; source remains 1536×1024. This is not an upscale or replacement for higher-resolution artwork.
- No full-screen post-processing: UI, actors, loot, fire and water effects retain their existing color. Cooler/deeper foliage separates warm actor details from the backdrop.
- Forest danger markers use a more opaque red-orange inner stroke and dark outer stroke. Their original centerlines, radius/sector, duration and damage are unchanged. This improves visibility on light soil without relying only on hue.
- Builder creates/updates `Assets/Settings/Map02ColorV5.mat`. The previously pending V4 entrance widening is included when the existing builder is run; no new topology edits in this color pass.
- Backups: `Backups/Map02_ColorV5/` (touched source/docs/tests, scenes, settings). No real save changed/reset.

## Files

- `Assets/Editor/TopDownGameBuilder.Map02.cs`: floor material wiring and idempotent material creation.
- `Assets/Shaders/PaintedForestColor.shader`: selective sprite color grade.
- `Assets/Scripts/Enemies/Base/ForestAttackVisual.cs`: double-stroke warnings.
- `Assets/Editor/Map02Verification.cs`: safe two-pass build and same-camera before/after capture with temporary material clone, never saving comparison edits.
- EditMode material/scene assertions and PlayMode warning geometry/contrast assertion.

## Verification

- Two complete builder passes and validator passed in the current Editor (`Logs/Editor.log`: `MAP02_COLOR_V5_BUILD_PASS_1/2`, validation passed). The first scheduled delay callback did not execute across a recompile; direct invocation ran successfully despite the bridge request timing out while Unity continued.
- EditMode **70/70 passed**, job `1bf48f45522a4668915b8e6bc9ed949e`. Includes color-material isolation, shader support, all enemy-clearance paths from entrance and existing project regression tests. This also verifies the previously pending V4 entrance widening after generation.
- Visually compared both lake and camp views at camera size 5.2, 1920×1080. Foliage is greener/deeper, paths less yellow, pond remains turquoise, actor/loot colors unchanged. Inspected overview too via generated output; gameplay view is the primary color reference.
- Full PlayMode **69/69 passed**, no failures/skips, job `e614ebedd9c14d73999b2cf6ce81b18a` (189.3 s). Includes new warning double-stroke geometry test, all Map02 navigation/combat tests, pause/death/reset and existing gameplay regressions. The old synthetic-click test passes in this interactive Editor run; no click-input code/test was modified in this pass.
- Inspected actual runtime combat screenshot, copied to `Assets/Screenshots/Map02_ColorV5_CanhBao_Verified.png`. Red-orange warning and dark outer edge are clearly visible on pale earth. Capture runs the existing controlled two-enemy encounter with test save, not full loadout balance.

Saved images: `Assets/Screenshots/Map02_ColorV5_HoThac_Before.png`, `Map02_ColorV5_HoThac_After.png`, equivalent `TraiRung` pair and overview. Identical scene/camera; before has grade strength 0, after strength 1. No image retouching.

## Known baseline issues

V4 batch synthetic click test was failing independently of Map02. Report final full-suite results separately from the new color/Map02 checks. Unity/editor external service warnings are not proof of a game runtime error.
