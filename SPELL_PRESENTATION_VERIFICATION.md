# Spell presentation refresh

## Implementation

- SkillManager captures aim/target once, spends original mana/starts original cooldown, presents a 0.12s windup, releases exactly once, then recovers (total 0.42s, IceDash 0.25s). It blocks overlapping casts and regular attacks during the sequence. Death/disable before release cancels without refund. Pause/modal freezes gathering.
- Animator times the existing three casting poses to windup, release, recovery and uses actual target direction rather than mouse position for auto casts. No new directional sprite sheets or skeletal rig: facing art still has existing left/right limitations.
- SpellPresentation supplies small converging sparks, expanding ground ring/fragments, following ward, falling meteor and sprite afterimages with finite lifetimes. Uses existing code-native sprites, not new generated art.
- Fireball no longer uses inventory icon as its projectile: warm shell, hot core and tapered trail.
- IceDash gains spaced afterimages. Shield follows caster for its existing duration; Healing uses a brief green ward. Lightning and chain hits gain local impacts; meteors descend during the existing warning, with softer ground warnings. Void pulse contracts inward to match pulling behavior.
- SwordSpin visual scale now preserves the authored radius instead of being overwritten each animation frame. Damage radius and hit cap unchanged.
- No SkillData balance values, save schema, builder, map or input bindings changed. Runtime timing now has a deliberate windup, so responsiveness differs by 0.12s.

## Verification

- Initial PlayMode 61/61 passed (23f1852eca87429f81bafa13d1faad3d).
- Added tests for windup, target lock, overlap prevention, modal freeze, disable cancellation and VFX cleanup. Showcase test casts all 9 skills using the real player prefab and isolated test save.
- Reviewed screenshots Spell_Fireball, Spell_Shield, Spell_SwordSpin, Spell_MeteorShower in Assets/Screenshots. Adjusted meteor trail continuity/warning opacity and fireball trail after initial review.
- Final EditMode 62/62 passed (d2709217097848cfbe0a6d3c8e2d62d7), including project validator.
- Final PlayMode 61/61 passed (81136de2515247fca7a765ba0adbde98).
- During reruns, retained click-shoot input test failed when another canvas/input update interfered. Isolated/restored other raycasters and explicitly processed the test mouse frame before sampling PlayerCombat. No game input bindings changed.
- Final meteor screenshot reviewed: contiguous tapered fiery trails, translucent ground warning below character; sword spin, shield and fireball screenshots reviewed as well.
- Builder unchanged, so full content rebuild not required. No FPS benchmark or new character animation frames claimed.
