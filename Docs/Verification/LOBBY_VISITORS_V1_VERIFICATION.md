# Lobby visitors + central plaza supplies V1

Three decorative travelers with green/rust/purple cloak materials, authored four-point
plaza routes, acceleration/braking, staggered rests, and yielding when the player is
ahead within 0.85 units. Kinematic interpolated Rigidbody2D MovePosition; no player tag,
input, health, combat, colliders, inventory or save components. Separate animator owns
sprite facing and distance-based eight-frame walking; short stationary grace avoids
idle flashes between physics updates. Pauses use normal scaled gameplay time.

Uses existing player animation artwork as traveler variants, not three unique newly
drawn character identities. Cloth recolor shader targets blue regions and retains skin.
No dialogue, quests or trading added. Two existing painted supply/lantern sprites add
decoration off the main compass and station access, with small physical footprints.

Source: builder partial TopDownGameBuilder.LobbyVisitors.cs, LobbyVisitor.cs,
LobbyVisitorAnimator.cs, LobbyVisitorCloth.shader. Main builder calls the factory.
Backup: Backups/LobbyVisitorsV1. Original background painting is unchanged.

Verification pending: two builds, validator, full test suites, route clearance and
visual inspection at two time samples. No performance or finished-quality claims yet.

Two full builds and validator passed (`VISITORS_BUILD_PASS_1` / `_2`). Focused Lobby
PlayMode passed, job `1f5b701cb14442ceb783133eedee0d4e`; inspected first plaza screenshot.
Adjusted vertical resting to retain facing, then full EditMode 73/73 passed,
job `2f115ecbfccc4ac7882011150ed47dc1`. Full final PlayMode 69/69 passed,
job `31875d208d36493db2eb9e556fc5b7ef` (198.07 seconds).
Inspected runtime images `Assets/Screenshots/Lobby_VisitorsV1_30_Verified.png` and
`Lobby_VisitorsV1_300_Verified.png`, plus intermediate movie frame. 165 frames at
15 fps encoded to `Captures/LobbyVisitorsV1_Motion.mp4` (11 seconds, 960×540).
These are real Unity runtime captures under fixed test timing, not an FPS benchmark.
Route clearance >0.25 against solid scenery over the sample, bounds, velocity cap,
pause, displacement and station access passed. Player-yield behavior is implemented
but not separately automated with a controlled blocking player. Close view uses camera
size 3.4 for inspection; normal gameplay camera stays unchanged. Complete scoped change.
