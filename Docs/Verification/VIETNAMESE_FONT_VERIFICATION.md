# Vietnamese font / missing menu text — 10 September 2026

User screenshot showed missing entire labels and gaps in Vietnamese subtitle.
Editor.log confirmed IndexOutOfRangeException in TMP_MaterialManager.GetFallbackMaterial
(atlasIndex) → TextMeshProUGUI.SetArraySizes. The original primary LiberationSans
atlas was static with incomplete Vietnamese coverage; its dynamic fallback had
clear-on-build enabled. Previous Editor screenshots did not prove post-build safety.

Fix lives in TopDownGameBuilder.Font.cs and is called by BuildCompleteMvpAutomated:
rebake the existing primary font GUID with ASCII, all Vietnamese upper/lowercase
letters, prior glyphs and common UI symbols. Single 4096 atlas, 357 characters,
static mode; clear-on-build disabled. Existing material and texture references
are retained. No OS font dependency, no font download or save/schema change.
Build guard rejects missing Vietnamese primary glyphs. Test verifies glyph coverage,
nonempty glyph rectangles, material texture identity and clear-on-build setting.

Two full builder passes completed (Editor.log lines 51427 and 53848 at verification
time), then automated validator passed. Full EditMode 74/74 passed, job
03748aa18b1a47ffb73fb88e626aeb31. Menu screenshot from current runtime was inspected:
Assets/Screenshots/MenuDoAn_1024x768_Verified.png, title and every label present.
Further PlayMode/build/post-build verification recorded below when finished.

Full PlayMode: 75/75 passed, job ae62aa91842a47a59138fafe97b2c3fd (207.86s).
Windows build succeeded, job build-4279a4aa5f, 0 errors / 185 package/shader warnings,
output Builds/GraduationDemo_FontFixed/VeThanRung.exe. Post-build MenuReadinessTests
6/6 passed, job 8317b1414ccc4d0ba4e9fa1bd60df364; this explicitly retests after the
build cleanup stage that exposed the original failure.

Opened the actual new Windows executable and visually inspected its main menu:
VỆ THẦN RỪNG, PHIÊU LƯU HÀNH ĐỘNG 2D, TIẾP TỤC, CHƠI MỚI, THOÁT GAME,
HƯỚNG DẪN and footer all present. Windows firewall permission dialog then occluded
the right part; did not interact with security prompt. Standalone Help clicking
is not verified; Editor post-build Help/render tests passed. A logged startup
probe showed no TMP exception; unrelated Unity network certificate warning remains.

Backup: Backups/VietnameseFontFix. The dynamic fallback remains available for symbols
outside the baked Vietnamese set, but Vietnamese rendering no longer depends on it.
