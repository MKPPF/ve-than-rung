# Editor startup recovery — 2026-09-06

- User screenshot shows an unsaved Untitled scene with Main Camera, not the
  project's saved Lobby/MainMenu. Existing generated scenes remain on disk.
- Catalog test cleanup can create an empty untitled scene. This is a plausible
  contributor; no restart trace proves it caused this specific screenshot.
- Added `Assets/Editor/TopDownEditorStartup.cs`: once per editor session,
  recover a clean, single temporary scene to MainMenu. Skip dirty scenes,
  saved scenes, multi-scene workspaces, batch mode and play mode.
- Added an isolated preview-scene regression test; it does not replace the
  user's open scene. Documented manual recovery for Untitled* in Scripts README.
- No builder, generated content or gameplay/save code changed.

## Verification

- Asset refresh compiled the new code before verification.
- EditMode: 57/57 passed, job `8344c82fc39e40be8007dae4c48fee86` (includes project validator).
- PlayMode: 55/55 passed, job `4cc41285c6c74285a1bb813e19b0beea`.
- Console error query: zero entries after tests.
- Final active scene confirmed: `Assets/Scenes/MainMenu.unity`.
- Did not close/relaunch the user's editor. Actual next-launch recovery remains
  a manual check; do not report the earlier 56-test run as testing this change.
- Dirty Untitled* requires the existing Open Main Menu save/discard prompt;
  the recovery intentionally never auto-discards unsaved changes.
