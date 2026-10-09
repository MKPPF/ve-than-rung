# Garden pond and Map01 V7

Requested: remove decorative residents except blacksmith, expand pond, larger colorful jumping fish, redesign Map01 scenery. Left-click movement and the destination picker remain unchanged.

## Changes

- Lobby V7 garden painting removes all five V6 residents, retains original smith, replaces old camp with planting and expands turquoise pond downward/sideways.
- Runtime fish remain 10, with orange/gold/silver/blue/pink bodies at 1.2–1.52 scale (previously 0.58–0.78); eyes, jumping and ripples remain.
- Pond habitat center (-10.4,-1.9), radii (1.4,2.25); expanded water ribbon and adjusted left boundary. Forge is now the only fire anchor.
- Map01 painting adds golden stone paths, forest canopy, marsh islands and ancient ruin scenery. All original route graph, colliders, enemy locations, treasure, slime spawning and Map02 exit retained.
- Changes originate in TopDownGameBuilder; original artwork versions remain for recovery.

## Art provenance

Built-in image generation skill (no external API/CLI). Final files:
- Assets/Art/Generated/Lobby/WoodlandWaystationV7_GardenPond.png
- Assets/Art/Generated/Environment/Map01_EmeraldSanctuaryV7.png

Lobby prompt: precise edit of V6; keep original buildings, smith, compass and portal positions; remove angler, gardener, flower seller and both camp travelers; enlarge turquoise pond; replace people with vibrant flowers, reeds, pots and natural borders; no painted fish, UI or extra people.

Map prompt: use rendered beige walkability mask as layout; paint beige zones as clear gold stone/earth combat routes, dark green zones as emerald forest, turquoise marsh and ruins; no enemies, UI or new blocking structures on paths; maintain route topology and overhead view.

## Verification

- Two full builder completions: Logs/Editor.log markers 16624 and 18557.
- EditMode 59/59 passed (db81937173db410b9562f6fbe6b3d299), including full project validator, route graph and Map01 presentation content.
- PlayMode 58/58 passed (2983205b2d524abf8dc39ec2241d8a18), including 5 distinct fish colors, larger scale, pond containment, jumps, station paths and actual Map01 travel.
- Visually reviewed Assets/Screenshots/SanhChoV3_ToanCanh_Verified.png (now V7) and Assets/Screenshots/Map01_V7_Verified.png. Fish and blacksmith visible; all five V6 residents absent. Existing modular wall/tree props remain on Map01 to mark physical boundaries over the new painting.
- No save reset, combat balance change, or scene/prefab edits outside builder. Older art files retained for recovery.

## Full generation prompts

### Lobby

Use case precise-object-edit. Edit this exact Unity 2D top-down hand-painted woodland village map. Preserve canvas framing, all houses, original red-bearded blacksmith outside center forge, stone compass plaza, paths, and right teleport stone circle EXACTLY at current positions. REMOVE all five added people: angler and fishing rod at left pond, gardener above right of forge, flower seller lower-right, both seated camp travelers bottom-left. Only ONE person remains, original blacksmith. Replace removed people seamlessly with flowers/ferns/pots or ground. Make scenery more vivid and lush using colorful planted flower borders (coral, violet, white, gold), flowering shrubs and elegant mossy stone garden details. Enlarge the left pond to roughly twice its current water surface: extend it slightly right and downward into former angler area; broad turquoise oval water below existing unchanged waterfall, bounded by natural rock shore and iris/reed/lily planting. New pond may reach x 20 percent of canvas, bottom 64 percent of canvas; keep compass plaza and walkable paths clear. Water surface clear for runtime fish; do not paint fish or creatures. Keep waterfall exact location. No people other than original blacksmith, no added structures in walking paths, no glow columns, no text, no UI. Maintain original camera, painted detail and warm woodland lighting, visually rich professional game backdrop, not neon.

### Map01

Use case sketch-to-render. Asset: production 2D top-down RPG map ground/backdrop, 2800x2000 landscape (same 1.4 ratio as image). Input is EXACT WALKABILITY LAYOUT guide, not visual style. Preserve camera pure overhead and every beige clearing and connecting corridor silhouette and exact position. Beige = WALKABLE: paint luminous mossy earth, flat worn flagstones, subtle fallen leaves, open combat arenas, no obstacles or water. Dark green = BLOCKED: paint rich layered emerald forest canopy, mossy boulders, flowering understory, ferns, ancient ruins and turquoise marsh water. Match boundary precisely, never block beige pathways. Main scenic landmark: ruined sanctuary and pool inside upper-left green margin near but not inside circular beige clearing; sweeping clearings with warm sunlit ochre earth contrasted against cool turquoise-green woodland. Two enclosed dark green islands between paths can contain lush marsh pools and dense forest with reeds and broken ancient stone pillars. Elaborate trees and flowering accents toward outer perimeter. Bottom-left clearing is entrance, upper-right clearing exit: both stay empty walkable ground. Central beige arena spacious, readable, soft golden sunlight; delicate cyan bioluminescent mushrooms limited to dark border. Hand-painted premium fantasy adventure environment, clear large organic forms, intricate natural texture but NO visual noise. No people, no monsters, no UI, no labels, no portals, no chests, no drawn fish. Do not change layout, rotate, crop or add paths. Keep 1.4 aspect ratio and framing.
