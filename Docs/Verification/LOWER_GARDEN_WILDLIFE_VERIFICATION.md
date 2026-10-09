# Lower Lobby garden wildlife

## Scope and implementation

User requested additional differently colored butterflies and birds around the lower
Lobby flower beds. Added `LobbyGardenWildlife` through `TopDownGameBuilder` on the
unscaled scenery root. Six blue/purple/pink butterflies and three small birds are
decorative; no colliders, rewards, save fields or gameplay scripts. Existing six gold
butterflies, ten fish and restored painting are unchanged.

Butterflies circle three flower beds with independently phased wing folding. Birds
alternate ten seconds perched with a five-second curved flight between three perches;
breast/head/eye/beak/tail/feather shapes and ground shadows distinguish them from fish.
Scaled gameplay time freezes all movement during pause. Objects and owned meshes are
created once and cleaned on destruction; no per-frame object creation. Presentation
is original code-native art extending existing wildlife, no new external art dependency.

Backup: `Backups/LowerGardenWildlife/` stores pre-build source and generated content.
Staged `.cs.txt` is historical; runtime source under Assets is authoritative after import.

## Verification

Compiled and completed two full builds (`LOWER_GARDEN_BUILD_PASS_1` / `_2`), automated
validator passed. Full EditMode 73/73 passed, job `7bd197cfa5a54ec5ac600e188c259d7e`.
Full PlayMode 69/69 passed, job `d2d36a5e76a541818762d020ad2d1d50` (192.68 seconds).
Perched/flying screenshots were visually inspected before the next Map02 request.
The garden wildlife is integrated; no save/gameplay changes. The documentation finish
was interrupted by the new request, not the implementation/tests.
