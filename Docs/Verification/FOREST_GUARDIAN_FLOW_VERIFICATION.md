# Vệ Thần Rừng — báo đòn, hủy hành động và vòng đời đạn

Ngày 23/10/2026, Unity 6000.5.6f1. Tiếp tục hoàn thiện chiến đấu trên các map
hiện có sau đợt Map03. Đây là một đợt sửa runtime, chưa nghiệm thu toàn bộ boss,
cảnh vật, cân bằng hoặc mục tiêu chung.

## Bằng chứng trước sửa

- Bốn bài scene thật đều thất bại trên mã cũ, job
  `9e24999c7a2f4a75ba3d8d84670b4e83`, `Logs/ForestGuardian_Reproduction.json`:
  - Hạ boss giữa windup Slam: HP người chơi vẫn giảm 50.000 → 49.799,332.
  - Tắt component giữa windup: cùng đòn cũ vẫn gây sát thương.
  - Radial ở giây 0,4 chưa phóng đạn nhưng animation đã ở Recovery.
  - Đẩy boss trong windup: người chơi đứng trong vòng báo cũ không bị trúng,
    vì tâm sát thương được đọc lại từ transform lúc impact.
- Mã boss chỉ đổi State/ghi tiến trình lúc chết, không dừng coroutine. Các hàm
  đòn thường không kiểm tra Health sau yield. PlaySkill chia tổng thời lượng
  thành tỷ lệ nên mốc active không khớp các WaitForSeconds riêng của đòn.
- Lượt tập trung sau sửa đầu bắt thêm MissingReferenceException ở
  EnemyProjectile.OnTriggerEnter2D (trước sửa dòng 56), job
  `679946dba777427eb50e3a17eb4951c7`. Pool DontDestroyOnLoad mang đạn sang scene
  sau; source bị hủy nhưng toán tử ?. không xử lý null đặc biệt của Unity.
  Kết quả: `Logs/ForestGuardian_Focused_First.json` và
  `Logs/ForestGuardian_Focused_First_Console.json`.
- Bản boss trước sửa: `Logs/BossReadabilityBackup/ForestGuardianBoss.cs`.
- Kiểm tra thêm hướng hất lùi thất bại trên bản đã khóa vùng Slam: sau khi boss
  bị dời sang phải, player đứng bên phải tâm báo lại bị đẩy sang trái (x=10,617;
  yêu cầu >11,048). Job `b108f9bb3edc4662a800c8304a4794fb`, bằng chứng
  `Logs/ForestGuardian_Knockback_Reproduction.json`. Nguyên nhân: hướng vẫn lấy
  transform hiện tại của caster thay vì tâm impact.

## Thay đổi

- Boss đăng ký Died từ Awake, hủy coroutine/hành động signature/quán tính/
  tư thế đang chuẩn bị/vòng báo khi chết, disable hoặc reset. OnDestroy gỡ event.
  State Dead không bị phần còn lại của đòn cũ ghi đè. Reenable có thể bắt đầu
  đòn mới với một windup mới.
- Slam giữ tâm vòng báo ban đầu; radial giữ gốc phóng đạn đã báo. Bắt đầu đòn
  dừng vận tốc cũ, không trôi từ lần đuổi trước. Không khóa knockback người chơi.
  Hướng hất lùi của đòn tròn được tính từ tâm impact, kể cả khi caster đã bị dời.
- PlaySkillPhased nhận đúng windup có sẵn: Slam 0,65; radial 0,48; charge 0,42;
  summon 0,7; đổi phase 1 giây. Active pose 0,1 và recovery 0,25 giây; charge
  giữ active 0,28. Sau impact không đuổi ngay giữa tư thế hồi phục. Charge xóa
  quán tính cuối active, không tiếp tục trôi trong recovery.
- Không đổi HP, damage multiplier, armor, level, phần thưởng, attack cooldown,
  số đạn hoặc catalog/content. Có thêm khoảng hồi phục chuyển động như trên;
  chưa xem đó là một lượt cân bằng độ khó.
- EnemyCombatVfx có owner tùy chọn cho vòng báo boss/signature Ancient Quake.
  CancelWarnings chỉ dừng cảnh báo đang chờ của đúng owner. Mỗi lần lấy từ pool
  xóa owner cũ; Update xử lý cả owner bị hủy. Burst đã phóng giữ lifetime riêng.
- Telegraph chỉ pulse opacity, giữ nguyên đường biên từ đầu windup thay vì co
  90% rồi nở ra. Áp dụng cho hiệu ứng Telegraph dùng EnemyCombatVfx; không sửa
  hệ LineRenderer riêng của Goblin/Archer trong rừng.
- Ancient Quake kiểm tra lại trước nhịp echo; boss chết giữa nhịp đầu/echo không
  sinh thêm echo/hazard chưa phóng. Hazard/đạn đã phóng trong cùng scene tiếp tục
  lifetime của chúng, có guard Unity null khi caster đã bị hủy.
- EnemyProjectile lưu Scene lúc Launch. Pool thu hồi đạn của scene vừa unload;
  OnDisable reset vận tốc, simulation, source và Scene để lần dùng sau sạch.
  Pool vẫn tồn tại/tái sử dụng, không mang đạn cũ vào map kế tiếp.
- Không đổi builder/asset/scene bằng tay; nguồn content là bản đã dựng hai lần
  trong đợt Map03. Các thay đổi này nằm ở runtime và kiểm thử.

## Phạm vi kiểm thử

`ForestGuardianFlowTests` dùng scene BossMap01 thật, chụp/khôi phục ba record
`TopDownGame.Tests.Progress.v1{,.Backup,.Unreadable}`, live manager và automation.

1. Chết giữa windup cả năm routine: không damage/đạn/summon mới, vòng báo dọn,
   State giữ Dead. DeathDelay kéo dài riêng trong test để phát hiện công việc
   tiếp tục khi xác vẫn còn; không dựa vào hủy GameObject che lỗi.
2. Disable giữa Slam, không hoàn thành đòn cũ; enable/đòn mới gây damage được.
3. Đẩy boss sau vòng báo: vùng cũ còn nguy hiểm, hất lùi ra xa tâm đã báo,
   vùng mới không thành hitbox ngầm.
4. Radial giữ Windup tới lúc phóng 10 đạn, giữ origin dù boss bị dời.
5. Pattern thật giữ nhịp hồi phục trước khi đuổi lại.
6. Charge khỏe thực sự lao rồi dừng, không trôi trong hồi phục.
7. Biên vòng báo giữ bán kính; VFX tái sử dụng không còn owner cũ.
8. Unload arena thu hồi đạn đã phóng và cùng pool dùng lại được ở scene sau.
9. Đạn/hazard đã phóng vẫn giải quyết được khi source bị hủy, không exception.
10. Chết đúng sau nhịp đầu Ancient Quake, không tạo hazard của nhịp còn chờ.
11. Render báo Slam và hạ boss/hủy đòn ở 1920×1080, 1280×800, 1024×768 bằng
    camera theo player; kiểm tra HUD ≥12 pixel, overflow và vị trí nút.

Ảnh `Assets/Screenshots/VeThanRung_{BaoDon|HaBossHuyDon}_<resolution>_Verified.png`.
Pose chụp được đóng băng bằng GameplayState; không phải nghiệm thu toàn trận
chiến bằng người chơi. Khi chuyển canvas sang camera để render, sorting được
đặt/khôi phục riêng như các fixture HUD hiện có, tránh cây world đè ảnh UI.

Lượt hồi quy sau sửa hướng hất lùi, job `7e9ea59ffb2e4f6187cede251caed458`,
bắt được bài Ancient Quake chưa nhận nhịp đầu trong thời gian chờ cố định 1,2
giây. Không tính lượt này là đạt; lưu `Logs/ForestGuardian_PlayMode_TimingFailure.json`.
Fixture cần chờ sự kiện nhịp đầu có deadline, sau đó mới chờ qua cửa sổ echo;
không bỏ assertion nhịp đầu để che việc đòn chưa xảy ra.
Đã đổi fixture sang wrapper `PerformRoutine` thật (giữ `IsPerforming` để AI
thường không chen vào signature), chờ nhịp đầu tối đa 4 giây thực rồi chờ thêm
0,3 giây gameplay. Không thay thời gian hay điều kiện sát thương trong runtime
Ancient Quake để xử lý lỗi kiểm thử này; nguyên nhân timing/AI chưa tách riêng.

Lượt kế tiếp `069f9329762e4e0ebdf99a587f55eafb` đã qua nhóm boss nhưng thất bại
12 bài dùng phím/focus. Probe trực tiếp ghi `Application.isFocused=false`,
`focusedWindow=null`, background `ResetAndDisableNonBackgroundDevices`, editor
input `PointersAndKeyboardsRespectGameViewFocus`. Package Input System cục bộ
quy định bàn phím/chuột bị chuyển khỏi player khi Game View mất focus.
Bằng chứng: `Logs/ForestGuardian_InputFocus_Diagnostics.json`,
`Logs/ForestGuardian_PlayMode_InputFocusFailure.{json,xml}`.

`PlayModeInputIsolation` được gọi từ callback khôi phục save sẵn có: mỗi bài dùng bản sao
InputSettings với IgnoreFocus/AllDeviceInputAlwaysGoesToGameView và tạm tắt
thiết bị native đang bật. TestFinished và RunFinished khôi phục settings gốc,
thiết bị và hủy bản sao; không sửa asset/cấu hình điều khiển của bản chơi.
Không xóa hoặc nới các assertion phím/modal để xử lý lỗi môi trường này.
Lượt tập trung `5d33eb6324734f12acaecef4bcf63d3f` không khởi tạo (0 bài) do
CS0579: Unity không cho đăng ký hai TestRunCallback trong một assembly. Đã bỏ
đăng ký thứ hai, ghép Begin/Restore vào callback có sẵn; không tính lượt này là
một kết quả kiểm thử gameplay.
Lượt `eed292c951a94299b6531570dd7f0fe8` qua hai bài né bằng phím rồi bị abort
vì InputManager tự DestroyImmediate settings có HideAndDontSave khi thay thế.
Helper ban đầu vì thế làm mất object cấu hình gốc. Đã đối chiếu setter trong
package, tạm giữ flags DontSave của object gốc khi hoán đổi và khôi phục flags
sau khi gắn lại. Không tính lượt bị abort là đạt; log lỗi tại
`Logs/ForestGuardian_InputIsolation_LifetimeErrors.json`. Trước khi dọn cờ bridge,
đã xác nhận TestRunnerApi.IsRunActive=false và Editor không Play.

Sau sửa vòng đời, job `0704cceb9c484101a4cef247a1520366` đạt 13/13, 0 skipped,
gồm toàn bộ 12 bài input từng lỗi và Ancient Quake. Bằng chứng:
`Logs/ForestGuardian_InputIsolation_Focused.json`. Probe sau lượt xác nhận
settings còn sống, flags HideAndDontSave, hai chế độ focus ban đầu và hai
thiết bị Keyboard/Mouse native được bật lại:
`Logs/ForestGuardian_InputIsolation_Restored.json`.

## File chính

- Runtime: `ForestGuardianBoss`, `EnemyCharacterAnimator`, `EnemyCombatVfx`,
  `EnemySignatureAbility`, `EnemyProjectile`, `EnemyProjectilePool`, `EnemyHazardZone`.
- Test: `ForestGuardianFlowTests` (11 bài mới), `PlayModeInputIsolation` và
  `SaveRecoveryPreferenceIsolation` (môi trường input/cleanup dùng chung).
- Tài liệu: báo cáo này, `Docs/README.md`, `Docs/ROADMAP.md`,
  `Assets/Scripts/README.md`; sáu ảnh trạng thái boss trong `Assets/Screenshots/`.

## Kết quả cuối trên nguồn đã sửa

| Cổng kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| PlayMode toàn bộ | 180/180, 0 failed/skipped, 404,721 giây | Job `86061e3f5b174d22b1211faf2fffb28f`; `Logs/ForestGuardian_PlayMode_Release.{json,xml}` |
| EditMode toàn bộ | 89/89, 0 failed/skipped, 17,019 giây | Job `3bc61adaa4994175bee0eb8064d675a4`; `Logs/ForestGuardian_EditMode_Release.{json,xml}` |
| Input/Ancient Quake tập trung | 13/13, 0 failed/skipped | `Logs/ForestGuardian_InputIsolation_Focused.json` |
| Validator | IsValid=true, không lỗi validation | `Logs/ForestGuardian_Validator_Release.json` |
| Console gốc | 0 error, 4 warning | GetCountsByType trong cùng log validator; `Logs/ForestGuardian_Console_Release.json` |

Bốn warning gồm ba cue thiếu clip Music_Exploration/Ambient_Forest/Ambient_Cave
và AudioMissingClipProbe chủ đích của test. Reader MCP còn trả dòng cleanup/
audio listener từ quá trình test; số đếm nêu trên lấy trực tiếp từ Console gốc.
Không coi ba clip thiếu là đã giải quyết.

Editor kết thúc ở `Assets/Scenes/MainMenu.unity`, không Play/compile,
timeScale=1. InputSettings còn sống, flags HideAndDontSave, hai chế độ focus
gốc đã khôi phục; Keyboard/Mouse native đều bật. Không đổi builder trong đợt
này; content tiếp tục dùng bản Map03 đã dựng hai lần ở đợt trước.

Sáu ảnh đã được kiểm tra bằng mắt ở ba tỷ lệ; assertion render trong lượt cuối
cũng đạt. Đây là kiểm thử có dàn dựng trên scene thật, không thay cho nghiệm
thu cân bằng toàn trận bằng người chơi. Diff trong phạm vi sửa không có lỗi
khoảng trắng. Mục tiêu chung và U5 vẫn đang thực hiện.

## Còn cần hoàn thiện

Phần charge dưới đây là trạng thái ở cuối đợt này; đã được tiếp tục trong
`FOREST_GUARDIAN_CHARGE_VERIFICATION.md`. Kết quả mới nằm ở báo cáo kế tiếp.

- Charge vẫn dùng impulse cũ và báo line rộng 0,72 trước impact tròn bán kính
  AttackRange=2,3. Đợt này kiểm tra hủy/dừng chuyển động; chưa nghiệm thu độ khớp
  hình học giữa line và toàn vùng sát thương. Đây là ưu tiên đợt boss kế tiếp.
- Chưa đánh giá lại độ an toàn của toàn bộ vị trí summon khi boss ở sát tường,
  áp lực minion tích lũy, chiến thắng/ra cửa trong một trận cân bằng đầy đủ.
- Camera/cảnh vật ở rìa các map và cấu hình thử nghiệm 50.000 HP/25.000 mana
  vẫn cần xử lý trong mục tiêu map hiện có. Mục tiêu chung và U5 giữ đang làm.
