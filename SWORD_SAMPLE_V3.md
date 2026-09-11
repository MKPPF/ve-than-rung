# Kiếm Luân V3 — sample approval build

## Scope

Only SwordSpin is rebuilt as the sample requested after reference review. Other eight skills retain V2. Not a claim that all skills are finished or that the design matches a commercial title's production quality.

## Architecture and art

- SwordComboProfile: six actor poses, six painted slash frames, two materials, swing/contact cues. Builder creates profile and sprite rectangle assets from source sheets; no destructive slicing.
- Animator owns six newly generated arm/leg poses and returns to original material/idle after the combo. Leftward uses mirrored poses; there are no new up/down sheets.
- SwordComboVisual receives each damage beat, displays one painted sweep with a fading tail, requests actor pose and sound. Previous geometric rotating ring not used for this skill.
- Gameplay damage remains radial over 1.2s with original radius, damage and six-hit cap. Whole combo now owns the cast lock so other casts/auto attacks cannot overwrite its poses. Pause cannot tick damage. Death/disable cleans up.
- Audio is builder-synthesized whoosh/contact PCM played by GameAudioManager using SFX volume; not professionally recorded foley.

## Asset provenance

Built-in imagegen skill used for actor poses and painted slash. First actor output had a painted checkerboard rather than alpha and was rejected. A second pass replaced background with magenta; SwordActorMatte shader keys that color at render time. Source file is RGB, not genuine transparent PNG. Slash uses black-background additive compositing.

Final source paths:
- Assets/Art/Generated/Characters/PlayerSwordComboV3.png
- Assets/Art/Generated/SkillIcons/SwordSlashPaintedV3.png

Actor prompt:
Use case identity-preserve. Image 1 is CHARACTER IDENTITY and PIXEL ART STYLE reference, not edit target. Create production sprite animation sheet for this exact blue hooded young sword hero, same brown hair blue eyes gold-trim blue tunic brown gloves boots belt tan trousers silver single sword. Transparent background with actual alpha, no checkerboard painting. Exactly SIX equal cells in a rigid 3 columns x 2 rows grid, no borders text or labels. Each cell square with full body centered at same foot baseline 84% cell height, consistent character scale head-to-foot 60% of cell height, ample clear space for sword. Camera three-quarter top-down RPG, facing right, same identity across all frames. Reading order left-to-right top row then bottom: 1 knees braced torso twisted backswing sword drawn behind left shoulder; 2 hips rotate forward sword raised diagonally beginning cut; 3 committed sweeping horizontal slash toward right with extended sword hand, weight over forward foot; 4 low follow-through sword across body, cloak follows momentum; 5 reverse backswing sword low-right elbow bent; 6 upward reverse cut torso returns with sword extended upper-right. Distinct anatomical arm/leg poses, clean silhouette, feet remain on shared ground baseline, not character rotating as a rigid sticker. No energy effects no shadows no extra weapons no other characters, no scenery. Crisp high-quality illustrated pixel sprites not blurry. 1536x1024 sheet.

Matte correction:
Use case background-extraction. Input is EDIT TARGET. Preserve exact six hero poses, exact pixels of character, sword, clothes, their positions, scale and 3x2 layout. Replace ALL checkerboard/white background with perfectly FLAT SOLID MAGENTA #FF00FF for runtime chroma key. Absolutely no magenta on subject. No shadows, no gradients, no checkerboard, no white border/halo. Do not redraw or relocate any character. Keep canvas 1536x1024. All background pixels must be solid magenta.

Slash prompt:
Use case stylized-concept. Game VFX sprite sheet for an original 2D fantasy sword attack. EXACT 3 columns x 2 rows grid of SIX equal square cells, no dividers labels text. All background solid pure BLACK (#000000), for additive rendering, no transparency checkerboard. Each cell centered same pivot: a ONE-SIDED sweeping silver-white crescent brush slash with pale gold rim, sharp leading blade, flowing tapering brushstroke tail, subtle sparks, irregular organic hand-painted edges, NOT a complete circle, NOT three geometric arc segments, not a UI icon. Frame order: 1 narrow opening stroke top right at 2 o'clock; 2 extended bright quarter arc right side; 3 strongest half-crescent sweep on right and bottom; 4 thin follow-through crescent lower right; 5 broken trailing wisps bottom-right; 6 just tiny scattered golden fading sparks lower right. Each effect within 80 percent cell with black padding all sides. Clear empty center for hero. Consistent size, palette and center, natural 6 frame lifetime of ONE slash, sharp impactful 2D animation. No character, sword object, landscape, red, green, blue or text.

## Verification

- Two completed builds: Logs/Editor.log lines 24235 and 26700 at check.
- EditMode 64/64 passed (9b91562f64814127b05b6333a8220906), includes validator, six frame references and supported matte/additive shaders and cue clips.
- PlayMode 62/62 passed (69dde93dafb645beb9b80d456551ff5a).
- Final sample recapture 1/1 passed (3720f0769a2b433cb0434c4770334101), real Slime prefabs with test-only 5000 HP runtime profiles; AI/movement disabled for a controlled demonstration. Validated HP decreases, pause prevents damage, material restores and effect cleans up.
- Video Captures/SwordComboV3_RealTargets.mp4: 5 seconds, 30fps, 960x640. Audio mixed from the same generated cue clips at logged in-game feedback timestamps, not loopback audio capture. PCM peak 12231/32767; no clipping. Audio/video encoded successfully.
- Viewed contact and earlier strike frames: no visible magenta/checkerboard; actual arm/leg poses differ, painted arc fades. Earlier gold circles were EnemyLootPickup from low-HP targets; final high-HP sample avoids death drops.
- User visual approval still required before expanding this style to the remaining eight skills. Six-frame mirrored animation remains a sample, not a full four-direction action set.
