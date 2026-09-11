# Potential and skill UI V2

Replaces production PotentialUpgradeUI and SkillEquipmentUI using the established
FantasyEquipmentUI dark/brass styling and EquipmentPanelLayout fitted 1920×1080 surface.
Existing builder already attaches these runtime UI controllers; builder/scene generation
is unchanged, so no full-content rebuild is needed for this UI-only change.

## Presentation and behavior

- Potential: four attribute cards, investment count, effective total stat, gain per
  point from PlayerStats constants. Existing immediate spending/save API retained.
- Skills: scrollable owned-skill list with existing illustrated icons, large selected
  icon, Vietnamese name/description, level, base mana/cooldown, upgrade cost and gold.
  All four Q/E/R/F slots have an icon and mouse selection, equip/remove buttons.
- P/K launchers match inventory; all existing keyboard actions retained; Esc closes.
- Full-canvas blocking backdrop, nested canvas sorting 2000, no scene effects drawn
  over the panel. Pause/input ownership through GameplayState; disable releases it.
- No new skills unlocked in production, no price/stat/schema changes, no imagegen art.
- Backup of overwritten source: `Backups/ProgressionUIV2/`.

## Verification

Compile and validator succeeded. Full EditMode 73/73 passed,
job `8dbf5686fb284b54884086aa3a32ae19`.
First focused Lobby PlayMode passed (`9e8c6e47833c4eb19a7b0ced489cff48`), then visual
inspection found scene effects drawn over camera-space test captures; nested canvas
sorting corrected. Full final PlayMode 69/69 passed, job
`797b795e5e1a4e7aa120f736378854f5` (187.21 seconds).
Reinspected final SkillsUIV2_1920x1080 and PotentialUIV2_1024x768 screenshots:
no scene ornaments over panel, icons visible in library/detail/occupied slots,
potential cards fit 4:3. Other generated screenshots cover 1280×720 and both panels.
Complete UI-only integration; no builder changes or manual scene edits.

Tests exercise mouse actions, modal exclusivity, pause, points/gold changes, potential
capture into isolated progress, empty-points, missing-gold/max-level, duplicate equip,
skill slot images, and disable cleanup. Screenshots at 1920×1080,1280×720,1024×768.
Test-only gold/skills do NOT affect real player progress. Full restart persistence and
keyboard automation are not newly exhaustively tested here; existing APIs are reused.
