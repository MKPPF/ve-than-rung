# Sảnh Tán Rừng V6

## Changes

- Restored left-click path movement, default auto combat and double-click interaction in the builder. Experimental click shooting is disabled; its isolated implementation is retained but not enabled in the game.
- Background V6 preserves buildings, central compass, pond, routes and teleport dais. Adds five painted residents, perimeter flowers, baskets, pots and eave bunting. Residents are static artwork, not autonomous NPCs.
- Ten runtime fish swim within the authored pond ellipse, jump on staggered cycles and show a landing ripple. Runtime geometry is reused; pause stops animation.
- All teleport light is planar, sorting orders -12 through -10. Removed the vertical light and upward spark path.
- Entering the dais opens a six-region destination picker after 1.15 seconds. Only unlocked regions plus Map01 can be selected. Cancel/E/re-entry, scene-change cleanup and modal input blocking are supported.
- Original V3 artwork is retained unchanged.

## Art

Built-in image generation skill used; final asset:
`Assets/Art/Generated/Lobby/WoodlandWaystationV6_Living.png`.

Prompt:
Use case: precise-object-edit. Asset type: Unity top-down 2D fantasy RPG lobby background. Input image is the EDIT TARGET. Preserve exact canvas aspect ratio, camera, lighting, all building positions and sizes, entire water/pond/waterfall boundary at left, the central stone compass, the right teleport circle location and shape, original smith, and open navigation paths. Enhance this exact woodland rest station with professionally hand-painted scenic life, respecting scale: add a small seated angler with fishing rod on outside lower-left bank of pond (not in water); two small travelers resting beside the existing bottom-left camp, flower baskets and a flower seller along the outside bottom-right low wall, a small gardener in the upper flower border, hanging bunting only under roof eaves, rich but restrained wildflowers and clay pots in perimeter grass. Keep the middle stone plaza and all routes to buildings, bottom exit and teleport completely clear. New adult people approximately the existing smith height, not giant. Add detail only at perimeter, don't add buildings, don't replace scene, no standing portal arch, NO glowing columns or overlays over portal, no text, UI, watermark. Output landscape same framing, high resolution crisp painted game environment.

## Verification

- Two completed builder markers in Logs/Editor.log (8966 and 11155 at time of check).
- EditMode first run: 58/58 passed (61400a114dd24d5fa0f5b6ba17ee7430), includes validator and restored click movement content.
- Final PlayMode: 58/58 passed (ee73dbc7c768426cb777712ca44cba76), including pond boundaries/jumps, station routes, ground-light sorting, picker cancel/reopen and real Map01 arrival.
- The retained experimental shooting test needed a real input-frame delay after UI layout; corrected the test, not enabled shooting in the game.
- Removed the separate MapGuidanceVisuals exit column for Lobby only; combat-map signals unchanged.
- Visually reviewed Screenshots/SanhChoV3_ToanCanh_Verified.png, TruyenTongTran_Choice_Verified.png and TruyenTongTran_Charging_Verified.png. Existing screenshot names retained by capture suite; images now show V6.
- Yellow circular marker near the portal is unrelated to the new planar rune effect.
