# Map01 V8 — local-scale terrain

User rejected both the original noisy forest and the large yellow V7 painting.

## Implementation

- Removed EmeraldSanctuaryBackdrop from builder output for Map01 only; original image file retained for recovery.
- New Map01_MossSoilV8 tile at six world units per repetition. World-space shader coordinates avoid restarts at individual floor rectangles. A rotated secondary sample reduces repeated patterns.
- Builder generates a small blurred walkability data mask for a soft transition between cooler forest ground and lighter combat lanes.
- Low mossy rocks with occasional larger trees mark physical boundaries. Additional shaded groves are placed outside the walkable shape with a clearance margin.
- Preserved topology/colliders, enemies, nests, drops, chest, Map02 exit, input and save logic. Lobby unchanged.

## Art provenance

Built-in imagegen skill used for one ground tile, not a full-map painting.
Final asset: Assets/Art/Generated/Environment/Map01_MossSoilV8.png

Prompt:
One seamless square game terrain tile, overhead stylized painted sage green moss and grey brown soil, tiny slate pebbles, understated even diffuse lighting, no focal point, no trees buildings or characters. Soft broad variation, crisp small detail, seamless all edges, not yellow, no noise. For 6x6 world units in top-down RPG.

## Verification

- Final builder ran twice after soft blend changes (Logs/Editor.log completion lines 5698 and 7800 at verification).
- EditMode 62/62 passed (daf0abbefdda4f4593ac51e1dc364fcc), including validator, route topology and new tiled-ground assertions.
- PlayMode 58/58 passed (823013071ab14a41a40cecefbb9c3c1c), including scene travel, movement, combat and lobby regressions.
- Inspected Map01_V8_GameplayScale.png at 1920x1080, normal camera size, and Map01_V8_Verified.png overview. Ground is no longer full-map magnified and rigid tint edges were softened. Repeated modular assets remain visible in overview; this is not a bespoke hand-painted whole scene.
- New tile output 1254x1254, about 209 source pixels/world unit at 6-unit repetition versus about 30 previously. This is local texture density improvement, not 4K source resolution or a measured FPS result.
