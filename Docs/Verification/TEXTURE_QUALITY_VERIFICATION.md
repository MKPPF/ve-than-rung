# Scenery texture quality — source-resolution limitation

## Implemented

- Builder imports painted Lobby and Environment art using Bilinear instead of Point. Existing character/prop filtering remains unchanged.
- Full-map Map01 painting uses Clamp and a 4096 import cap, matching Lobby capacity. Tiled ground keeps Repeat and its existing size cap.
- No source image was enlarged, replaced, or moved. No camera zoom, collision, layout, input or save change.
- Added regression tests for source pixel retention, filtering, compression, import capacity and tiled wrapping.
- Captured matching static scene views at 1920x1080 with normal camera sizes: TextureQuality_Lobby_Before/After.png and TextureQuality_Map01_Before/After.png in Assets/Screenshots.

## Honest limitation

Current originals remain Lobby 1619x971 and Map01 1484x1060. A built-in image generation attempt requested 3600x2160 but returned only 1619x971, verified from PNG header. The candidate also redrew some detail, so it was not integrated. It is only a preview in the generation output folder. Smoother sampling reduces visible texel blocks but does not restore missing resolution and may soften edges. True high-resolution source replacement remains unfinished.

The imagegen skill permits an API/CLI fallback only with explicit user confirmation; it requires OPENAI_API_KEY. No API fallback was run and no key was inspected.

## Candidate provenance

Built-in image generation skill, output:
C:/Users/HUNG/.codex/generated_images/01a0703d-6e2b-7823-bbf9-0c22fed10729/exec-391f548d-3121-45bb-ac04-235031a272cf.png

Prompt:
Use case precise-object-edit. Production Unity RPG texture resolution enhancement. Output actual 3600 x 2160 pixels, NOT a 1.5 megapixel preview. Upscale and reconstruct fine detail of this exact source painting for close-up gameplay. Preserve entire composition, geometry, proportions and precise positions of every building, path edge, shoreline, trees, compass and teleport dais. No new objects, no relocated edges, no new people. Retain only original blacksmith. Keep original palette/lighting and painterly style. Sharply resolve roof tiles, individual cobbles, leaves, water ripples and petals with natural coherent detail, no grain/noise, no artificial sharpening halos. This is a fidelity restoration, not a redesign. Same 5:3 aspect ratio, no crop.

## Verification

- Two builder completions confirmed in Logs/Editor.log (22538 and 24400 at verification).
- EditMode 62/62 passed, job 693556b734b34282a38e40b28a584089.
- PlayMode 58/58 passed, job 455cfe963fc64297b8bfb7d4b506892e.
- Compared matching Before/After Map01 captures at normal 5.2 orthographic size and Full HD; texel edges less blocky, fine ground detail still resolution-limited.
- No performance/FPS benchmark performed. No new high-resolution texture integrated; pixel dimensions unchanged.
- This is a partial display-quality improvement, not a completed 4K upgrade.
