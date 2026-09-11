# Trạm nghỉ Tán Rừng — V3

Original painted background generated with the built-in ImageGen tool. Inspired by readable service clusters and welcoming woodland towns; no existing game's artwork is shipped.

References: https://moonlighterthegame.com/ and https://www.stardewvalley.net/page/2/

## Selected assets

- WoodlandWaystationV3.png: original environment without people.
- WoodlandWaystationV3_WithSmith.png: final selected background with static blacksmith at x45.6%, y38.1%. The merchant interaction remains a separate Unity object. Roofs and scenery are not separate sprites; playable plaza is bounded by authored colliders.
- No transparent character sprite was shipped: generation returned opaque backgrounds. The static character was integrated by a subsequent ImageGen edit.

## Environment prompt

Use case: stylized-concept. Asset type: PRODUCTION GAMEPLAY ENVIRONMENT BACKPLATE for a top-down 2D fantasy action RPG Unity lobby, opaque wide 2560x1536 image. An original beautiful woodland adventurer waystation, cohesive high-end hand-painted 2D game background with crisp stylized shapes, readable small details, NOT photoreal and NOT a UI mockup. Orthographic high overhead three-quarter RPG view, no horizon, no vanishing point, vertical walls short and roofs visible. Designed as a playable map. Image coordinate layout IMPORTANT: broad winding pale warm cobblestone lane from (22%,50%) spawn courtyard to (79%,50%) eastern magical gateway courtyard. All terrain in central band y=42%-67%, x=17%-84% must remain clear walkable paving and grass with no buildings or large props. Main open-front blacksmith workshop centered at x50%, y23%-38%, slate teal roof, copper chimney, glowing orange forge interior and anvil counter just inside, front step ending at y40%; leave empty paving at (50%,43%) for a runtime NPC. Cozy timber supply store building at x66%, y79%, slate-blue roof and crates beside it, upper porch edge at y66%; leave empty ground at (66%,60%) for a runtime storage chest. A round worn rune-stone gateway platform centered (79%,50%), empty center, two low mossy pillars at its far edge, no magical portal itself (added at runtime). A small tranquil spring with stone banks on far left x9%,y39%-66%; it does NOT cross the main road. Curving mossy low stone walls, tall dense forest trees, ferns and flower patches framing all four outer edges as clear non-walkable boundary. At top left a small timber lodge roof mostly hidden by trees. At lower left a little campsite outside the walkway with packed sacks and logs. Broad center plaza has faint inlaid compass-star paving at x39%,y53%, no fountain or raised obstacle in center. Warm lanterns only at building corners, soft afternoon sunlight, deep cool teal forest shadows, warm ochre stones, restrained amber highlights and pale turquoise eastern rune detail. Asymmetrical pleasing environmental storytelling. Room for UI at extreme image edges, but no written labels or floating markers. NO people, NO player, NO creatures, NO text, NO letters, NO UI, NO icons, NO watermark, no copied franchise assets. Every structure stays out of the clear main walkway. High resolution fully finished original game map art.

## Final edit

Preserve the full map; add one small copper-bearded blacksmith in teal tunic and leather apron in front of the forge at x45.6%, y38.1%, height roughly 7% of image; use ForgeKeeperV2 portrait as identity reference. No other changes, no UI or text.

## Runtime

Builder imports at max 4096, uncompressed, sprite; world footprint 26×15.6 units. Camera clamp, 18 boundary segments, ambient glows/smoke/fireflies, station signs and compact hub HUD are authored in code.
