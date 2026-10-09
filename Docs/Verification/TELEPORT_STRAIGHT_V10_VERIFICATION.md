# Straight teleport column V10

User requested vertical parallel light shafts instead of converging rays.
TeleportDeparture.shader now keeps each ray's horizontal center equal to its anchor,
independent of height/time. Width was already fixed. Brightness shimmer, height reveal,
ground ring, rising collars, color and duration remain unchanged.
Ascending spark angle/radius are now constant per particle; only height/opacity animate.
This removes the inward spiral as well as the beam taper.

No builder/scene content or save/portal logic changes; existing shader/component references
consume the changes directly. No full rebuild needed. Backup: Backups/TeleportStraightV10.

Verification pending: full EditMode/PlayMode, validator, console and visual capture.
Added runtime assertion that ascending sparks keep their x coordinates across frames.

Completed during Wildlife Detail V2 follow-up: validator, full 73/73 EditMode and
69/69 PlayMode passed (job 7d2a7cabbf7a46ccb44d17fbe40ff7b5), including the spark
assertion and actual portal transition. Inspected TeleportStraightV10_Verified.png;
parallel beams maintained. Original interrupted full test run was not a pass.
