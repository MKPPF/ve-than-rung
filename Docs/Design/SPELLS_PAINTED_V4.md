# Painted skills V4

User approved SwordSpin V3 and requested the remaining skills plus icons in the same painted style.

## Assets and integration

- Seven six-frame VFX sheets: Fireball, IceDash, Lightning (shared by strike/chain), Healing, Shield, Meteor, Void. Distinct stages: flight loops first 3 frames; fire impacts last 3; ward holds middle frames; other effects play full lifespan.
- Nine-icon atlas maps directly to stable SkillId order. Icons are NOT reused as world VFX.
- Six new magic casting actor poses, selected for forward, overhead or defensive release; chroma-key magenta material reused from approved sword sample. No new four-direction set claimed.
- Profiles, sprite rect assets and materials authored by TopDownGameBuilder. No damage, mana, cooldown, range or save schema values changed.
- PaintedSpell additive material rejects dark colored backing and feathers frame borders. Source art has black/dark backgrounds, not alpha PNGs. SwordSpin's approved art/material/timeline unchanged.
- Lightning chain link paints between confirmed target positions, while original target selection/damage is unchanged. Ground warnings/gathering retain V2 geometry for readability.

## Imagegen provenance

Final files:
- Assets/Art/Generated/SkillIcons/SkillIconsPaintedV4.png
- Assets/Art/Generated/SkillIcons/FireballPaintedV4.png
- Assets/Art/Generated/SkillIcons/IceDashPaintedV4.png
- Assets/Art/Generated/SkillIcons/LightningPaintedV4.png
- Assets/Art/Generated/SkillIcons/HealingPaintedV4.png
- Assets/Art/Generated/SkillIcons/ShieldPaintedV4.png
- Assets/Art/Generated/SkillIcons/MeteorPaintedV4.png
- Assets/Art/Generated/SkillIcons/VoidPaintedV4.png
- Assets/Art/Generated/Characters/PlayerMagicCastV4.png

## Verification

- Two full builds completed (Logs/Editor.log markers 32830 and 35319 at check).
- EditMode 64/64 passed, job 3ac35adea55f4a548fae22b3fe50394d. Profile tests verify painted frames, actor frames, all nine icon references and supported shaders; project validator included.
- PlayMode 62/62 passed, job 3cbba40dcfe74871a333f7e8a6c3ffdc. All nine spells cast using real player prefab; showcase includes three high-HP Slime targets with AI disabled for controlled review. Existing gameplay/save tests retained.
- Reviewed runtime screenshots: Fireball, Shield, ChainLightning, MeteorShower. No black rectangle or magenta background around actor; chain hits connect real targets; meteor has painted burning rock.
- Captures/SpellsPaintedV4_Showcase.mp4: 18 seconds, 30fps, 720x480, 540 rendered frames with Vietnamese captions. Silent video; this turn does not add audio for eight spells. Sword audio remains from V3.
- New caster poses are shared among the eight spells with forward/overhead/defensive release selection and horizontal mirroring. No complete 4-direction cast animation or new gameplay balance claimed.

Built-in imagegen skill used. Project assets in Assets/Art/Generated/SkillIcons/*PaintedV4.png and Characters/PlayerMagicCastV4.png.

Prompts:
 
### paintCasterPrompt

Input image is exact character identity reference. Create NEW six-frame MAGIC CAST animation sprite sheet for same blue hood young hero, gold-trim blue tunic, brown hair/gloves/boots, tan pants. Background perfectly flat SOLID MAGENTA #FF00FF for chroma key, absolutely no checkerboard, shadow, glow or magenta on character. EXACT three columns x two rows equal cells on landscape3:2 canvas, six full-body poses at same scale and centered foot anchor, head-to-boot ~65% cell height, feet at 86% of each cell. Facing right in three-quarter top-down RPG style, crisp illustrated pixel art same identity as reference. Sword sheathed at waist, empty gloved hands visibly casting. Frames reading order: 1 braced knees both hands gather at chest; 2 windup hands draw backward torso coils; 3 one palm thrusts forward releasing magic toward right; 4 both hands raised above head summoning from sky; 5 hands spread at chest for defensive healing/ward; 6 arms relax returning to guard. Distinct arm/leg anatomy poses and shifting cloak, not rigid rotated sprite. No VFX, no letters or extra characters, keep every pose fully within own cell with padding.
 
### paintVoidPrompt

Production hand-painted fantasy RPG VOID PULL spell animation sprite sheet, exactly six equal cells in rigid 3 columns x 2 rows grid on pure BLACK background, no borders labels text or characters. One violet dimensional vortex with indigo-black empty center, silver-purple ragged energy ribbons spiral INWARD and tiny fragments get pulled in. Full effect contained in 75% cell with black margins, identical center across frames. Frames1-2 outer wisps gathering into wide open vortex, frame3 strongest layered asymmetric spiral around empty center, frame4 spiral contracts inward, frame5 smaller collapse knot, frame6 scattered fading violet filaments. Rich brush-painted organic magic matching premium painted sword VFX. Not clean concentric circles, not mathematical pattern, not solid purple disk. Empty center keeps character readable; violet/cool silver palette only. Landscape3:2.
 
### paintMeteorPrompt

Production fantasy game METEOR sprite animation atlas: SIX equal cells, 3 columns x 2 rows, pure BLACK background, no labels borders scenery. All isolated effects centered inside cell, identical registration and ample black margins. Frames1-3: a molten dark volcanic meteor rock with glowing orange fissures, bright white-gold leading tip at lower center, long irregular flaming tail streaming UPPER LEFT so travel down-right; flame contour subtly changes. Frames4-6: impact of that meteor, first explosive fiery crown and incandescent stone fragments, then smaller broken flame plume, finally scattered fading embers. Distinct painted rock core not just fireball. Dynamic fine hand-painted flame strokes, sharp debris, restrained smoke that fades to pure black, professional 2D game VFX matching painted sword slash. Nothing outside its cell; no ground disk, no circles, no geometric diagram. Landscape 3:2.
 
### paintShieldPrompt

Production hand-painted blue magical protection barrier sprite sheet, exactly SIX equal cells 3 columns x 2 rows regular grid on pure solid black. No text borders labels character or ground. Each centered effect fully contained in its cell with black margins. NOT a shield item icon: an upright oval ethereal ward surrounding empty black center where a character will stand, sapphire flowing filigree around edges, silver energy threads, wisps around base, very faint blue membrane not opaque fill. Frame1 forming bottom crescent; frame2 protective oval emerging; frame3 full luminous ward with elegant curved runic ornamental edges; frame4 similar full ward with wisps shifted for breathing; frame5 dissolving arcs; frame6 fading blue dust. Same scale center proportions across frames. Organic painterly crystalline-light aesthetic matching elaborate painted sword VFX; no rigid hexagon grids, no opaque solid shield, no straight geometric diagram. Landscape 3:2.
 
### paintHealingPrompt

Production fantasy RPG HEALING spell VFX animation atlas. Six frames EXACT 3 columns x 2 rows of equal cells, black background for additive blend, no text dividers frames or characters. Top-down three-quarter ground aura centered same anchor each cell, empty black central opening for hero. Painterly luminous emerald foliage, elegant swirling golden-green wisps, tiny leaves and light motes rise around perimeter. Frame1 a few awakening leaves and low light; frame2 curling leaf ribbons begin encircling empty center; frame3 richest healing spiral with leaves and light; frame4 ribbon lifts softly; frame5 leaves disperse; frame6 small fading green motes. No medical plus symbol, no rigid pentagram, no flat circle or UI icon. Organic hand-painted fine strokes, magical yet natural, white-gold highlights, distinct depth. At least 10% black padding in every cell, no cross-cell spill, landscape 3:2.
 
### paintLightningPrompt

Production 2D RPG painted LIGHTNING STRIKE animation sprite sheet, six sequential frames EXACT 3 columns x 2 rows equal square cells, black background for additive VFX. Each centered within its own cell and fully contained with 12% black padding; no dividers text labels terrain characters. Pure white lightning heart, electric blue-violet branching filaments. Frame1 wispy charged vertical filaments; frame2 growing forked bolt; frame3 powerful jagged bolt from upper cell to lower center with a small crown splash of electricity; frame4 branches spreading around bottom impact; frame5 broken flickering arcs; frame6 just short fading sparks. One strike lifecycle, detailed organic hand-painted energy with narrow brilliant core, feathery electric edges, elegant asymmetry, no thick cartoon zigzag icon, no concentric geometric circles. Keep consistent center and scale. Landscape 3:2.
 
### paintIcePrompt

Production 2D fantasy game hand-painted ICE DASH VFX sprite animation sheet, EXACT 3 columns x 2 rows equal cells, six frames read left-right top-bottom. Pure solid BLACK background for additive rendering, no grid lines borders text characters terrain. ONE centered low sweeping burst of sharp turquoise and silver ice shards, frost vapor trailing LEFT, motion toward RIGHT. Frame1 compact gathering crystalline shards; frame2 growing forward lance with curved frost ribbon; frame3 peak long crystalline sweep; frame4 breaking spray and sharp splinters; frame5 thinning frozen wisps; frame6 fading tiny snow crystals. Each effect centered same pivot, consistent scale, fits 75% of cell, clear padding on all sides. Crisp hand-painted brush texture, luminous white edges and translucent cyan facets, airy elegant curling vapor, NOT simple geometric stars or smooth blobs. Professional action RPG spell aesthetic matching finely painted sword slash. Landscape 3:2 sheet.
 
### paintFirePrompt

Production hand-painted game VFX sprite animation sheet. EXACT 3 columns x 2 rows equal square cells on pure black background for additive rendering. No borders labels text or scenery. Six sequential frames reading left to right top then bottom of ONE orange-gold fireball with hot pale yellow core, ragged curling flame tongues, trailing embers; travels RIGHT, bright head slightly right of cell center, tail left. Consistent center and size, all within 75% cell square, black padding. Frames 1-3 fireball flickering in flight, frames 4-6 expanding fiery impact then fading flame wisps and sparks. Ornate painterly crisp flame detail, strong fluid silhouette, not geometric circles or a blurred orange blob. Warm fire colors only. Animation meant for 2D fantasy action game like a painted silver sword slash in quality, clear transparent black negative space.
 
### allIconsPrompt

Production fantasy RPG skill icon atlas. EXACTLY 3 columns by 3 rows, nine equal square cells, edge-to-edge regular grid with no gutters, each icon fills its own square with dark charcoal background. Consistent premium hand-painted fantasy artwork, crisp strong silhouette readable at 64 pixels, restrained thin aged brass square frame inside each cell, luminous subject, no letters numbers text UI badges. Reading order row1: 1 flaming orange fireball pointing right, 2 icy blue dash silhouette with crystalline shards, 3 silver sword sweeping white-gold crescent. Row2: 4 white-blue lightning bolt striking earth, 5 glowing emerald leaf and healing light, 6 sapphire magical shield. Row3: 7 three linked electric lightning bolts, 8 blazing falling meteor rock, 9 violet swirling void vortex with dark center. Cohesive painted game art, detailed but not noisy, unique shape each, no extra icons. Square canvas.
