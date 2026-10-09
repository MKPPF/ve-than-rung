# Spell V2 — rebuild

## Ownership

- SkillManager: input, slots, mana/cooldown acceptance.
- SkillCastController: captured target/direction, Ready/Windup/Release/Recovery, release exactly once, pause/death/disable cancellation.
- SkillData + existing SkillBase behaviours: gameplay balance and effects; existing skill IDs/save entries unchanged.
- SkillPresentationProfile: builder-authored element, shader, palette and phase timings. Nine persistent profiles linked from SkillData.
- SpellVfx: finite presentation instances, optional owner following, local animation clock and material disposal. No damage/colliders.
- ElementalSpell shader: analytic curled fire, crystal shards, tapered blade crescents, forked electricity, petal/cross nature pattern, hexagonal ward and inward void spiral. Separate ground gathering/target sigils.
- PlayerCharacterAnimator: sole owner of visible cast lean/squash and frame selection; no physical-body motion or new rig/sprite sheets claimed.

## Visual integration

Fireball main icon/soft-blob layer removed, replaced by procedural flame core; explosion uses new element effect. SwordSpin hides legacy slash when a profile exists and uses rotating three-crescent blade shape at its real radius. Heal/Shield replace previous rings with nature/hex ward. Meteor uses new fire visuals and sigils; void replaces legacy ring with spiral. Ice/Lightning/Chain retain gameplay but gain new element release/impact shapes.

## Verification

- Two successful full builds (Logs/Editor.log markers 18273 and 20470). Earlier failed enum serialization attempt corrected; shader reserved-word compilation error corrected and ShaderUtil then reported supported with no messages.
- EditMode 63/63 passed (d66f60f5c7544f478a95ef101e7a01cc), including profile references, timings, shader support and project validator.
- PlayMode 61/61 passed (2ffc3f96240f4793ba2a83c8dfb9c894).
- Final shader refinement rechecked via nine-skill showcase: 1/1 passed (3e4eb97c204b4bdf8786caed778a5d74), 540 captured frames.
- Captures/SpellV2_Showcase.mp4: 18 seconds / 30fps, real prefab on isolated review ground, Vietnamese skill captions. ChainLightning segment has no enemies and is explicitly labeled; it is not a demonstration of chain hits.
- Screenshots inspected for sword and shield; blade taper refined after first review. No new four-direction sprite sheets or skeletal rig claimed. Existing damage queries remain in skill behaviours, not a complete damage-system rewrite.
- No paid APIs or generated raster assets used in V2.
