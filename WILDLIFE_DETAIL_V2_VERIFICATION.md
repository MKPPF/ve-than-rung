# Fish and bird detail V2

Scope: refine existing fish in Lobby/Map02 and three Lobby birds. Original code-native
sprite-layer drawing, not newly AI-painted bitmap sheets. No count/habitat/route/save
or gameplay changes; butterflies remain unchanged. Builder fields and references unchanged.

FishDetailArt adds spine/belly shading, gills, paired eyes, pectoral fins and rays,
color patches, restrained scale glints and mouth. Details fit existing bounding lengths.
Lobby uses cached SortingGroups to preserve intra-fish depth when jumping; marking alpha
follows body fade. Original tail animation remains. Shared sprites, no new per-frame
allocations from drawing; extra sprite renderers incur overhead, FPS not benchmarked.

Birds have a raised smaller head, short beak, brow/eye highlight, back feathers, longer
separated flight feathers and legs/toes. Separate folded-wing art is used while perched;
feet hide and flight wings show while flying. Original flight path/timing retained.

Backup: Backups/WildlifeDetailV2. Staged ArtReview drafts are superseded by runtime source.
No builder changes: no content rebuild needed. Runtime-owned roots/meshes cleaned as before.

Verification: validator passed. Focused Lobby+Map02 8/8 passed,
job 91032ad8b0554cc484430f3fdfbdd8c3. Inspected fish and perched bird screenshots;
then corrected raised perched wing to folded wing. Final full EditMode 73/73 passed
(6c8d1a3f152347fe9038defc5d6bf56a), full PlayMode 69/69 passed
(7d2a7cabbf7a46ccb44d17fbe40ff7b5, 196.71s). Final perched/flying images inspected.
Dedicated copies: Assets/Screenshots/WildlifeDetailV2_Fish.png,
WildlifeDetailV2_BirdsPerched.png, WildlifeDetailV2_BirdsFlying.png.
Console error query returned only Test Runner result-saving notice. Complete scoped
detail revision. Art remains stylized and layered, not photorealistic painted sprites.
Assertions cover ten detailed Lobby fish with paired fins/gills/pupils, eighteen bird
toes, feet hidden during flight, pause, stable counts including inactive folded parts,
cleanup and existing Map02 fish containment.
