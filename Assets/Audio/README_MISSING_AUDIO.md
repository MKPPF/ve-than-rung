# Licensed audio assets still required

The project now contains pooled audio playback, Master/Music/Ambient/SFX/UI buses,
distance attenuation, concurrency limits, pitch/volume variation, saved volume
settings, and music/ambient crossfades. No third-party sound was imported because
the project does not currently contain audio with a verified licence.

Create `AudioCue` assets and assign licensed loop-safe clips for:

- forest wind, birds, insects, leaves, river/pond and cave ambience;
- grass, dirt, stone and shallow-water footsteps;
- sword swing, hit, player hurt, level-up, gold and equipment pickup;
- Q, E, R and F skill casts;
- enemy notice, attack, hit and death;
- distinct elite and regional-boss cues;
- one exploration music loop per region.

For seamless ambience, use loop-ready WAV/OGG files without silence at the seam.
The generated `GameAudioMixer` already contains Master, Music, Ambient, SFX and
UI groups; cue buses are routed automatically when licensed clips are assigned.

## Current generated sounds and verification (22 October 2026)

`SwordSwingV3.wav` and `SwordContactV3.wav` are synthesized locally by
`TopDownGameBuilder.CreateSwordSound` for the sword combo. They are not a regional
music/ambient library. The Resources music/ambient cues still contain no clips.

The runtime now preserves the same loop across repeated requests, arbitrates
rapid transitions, applies live bus volume together with fade gain, and activates
expanded pooled voices up to the 32-source cap. Empty cues warn once; mixed arrays
skip null clips. All six synthetic-clip regression tests passed in the 105/105
PlayMode run (`Logs/Onboarding_PlayMode.json`). This does not certify audio content
or the full U1 roadmap item.
