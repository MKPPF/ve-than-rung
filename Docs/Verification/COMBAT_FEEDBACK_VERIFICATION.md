# Phản hồi combat — U3 và tích hợp U2 (22/10/2026)

## Phạm vi

Thêm rung camera nhẹ và hitstop khi trúng đòn, có hai lựa chọn tắt độc lập ở
Cài đặt từ MainMenu/Pause. Đây là hành vi runtime, không thay content/builder,
scene/prefab/catalog, chỉ số damage/HP/mana, giá đồ hoặc schema tiến trình.
Bản sao source trước sửa ở `Logs/FeedbackReviewBackup/`.

## Thiết kế

### Đồng hồ và quyền pause

- `GameplayState` vẫn là nơi duy nhất ghi timeScale trong gameplay. Thêm
  request hitstop, tick hết hạn và cancel; `CombatFeedback` là driver persistent
  bootstrap bằng code, giống mẫu audio manager hiện có. Không cần reference
  scene hoặc sửa scene bằng tay.
- Đòn player xác nhận gây damage lên enemy trong khung hình yêu cầu 35 ms;
  chí mạng 55 ms. Nhiều mục tiêu cùng frame nâng request theo max, không cộng.
  Mỗi đợt tối đa **65 ms** kể từ request đầu, rồi có khoảng phục hồi **120 ms**
  tính sau giới hạn đợt; không cho multi-hit giữ game đứng liên tục.
- Timer hết hitstop dùng unscaled time. Trong hitstop timeScale=0: di chuyển,
  cooldown và WaitForSeconds đứng cùng nhau; UI/fade unscaled tiếp tục.
  Mốc 65 ms là giới hạn timer; thời điểm frame phục hồi phụ thuộc FPS.
- Mở modal có pause hủy hitstop. Hết hitstop/cancel/tắt setting không thể resume
  sau lưng PauseOwners khác. Tắt/hủy driver hủy freeze; driver không hoạt động
  thì từ chối request mới. Chuyển scene xóa đợt và thời gian recovery.
- Runtime dùng GameplayState cho pause. Việc tự ghi timeScale ngoài service này
  không phải contract gameplay được hỗ trợ; test pause nên dùng cùng API sản phẩm.

### Camera

- `CameraFollow2D` giữ `unshakenPosition` riêng; smoothing không lấy vị trí đã
  rung làm đầu vào. Sample rung không dùng Random toàn cục để tránh đổi RNG loot.
- Offset có envelope giảm dần, biên độ tối đa mặc định **0,16 đơn vị**, mỗi đợt
  tối đa **0,3 giây**. Nhiều impact dùng max amplitude; không cộng độ lệch.
  Cả vị trí gốc và vị trí rung được kẹp bounds nên không lộ ngoài tranh map.
- Hết rung, pause, setting tắt, disable camera, mất target hoặc cinematic trả
  offset về zero. Chỉ main CameraFollow nhận request, minimap không rung.

### Nguồn impact

- `EnemyHealth` gửi phản hồi sau khi xác nhận trúng. `CombatFeedback.EnemyHit`
  nhận nguồn có PlayerCombat, còn trong viewport, không phải periodic damage.
  Chí mạng/đòn knockback nặng rung nhỏ; mọi đòn trực tiếp hợp lệ có hitstop.
- `PlayerHealth` gửi rung sau direct damage thực sự >0. Né/i-frame từ chối hit
  trước hook; TakeStatusDamage không phát feedback. Incoming hit không hitstop.
- `DamageInfo.IsPeriodic` là tham số tùy chọn cuối, mặc định false, chỉ dùng để
  loại feedback cho tick. Không thay công thức damage hoặc tự triển khai status.
- Gắn world impact vào dậm/lao boss, thiên thạch và sét AoE khi impact xảy ra,
  không rung trong cảnh báo. Khoảng cách đến camera làm giảm biên độ; vẫn rung
  nhẹ khi đòn rơi gần nhưng không có mục tiêu, không đổi hitbox/telegraph.

### Cài đặt

- `FeedbackPreferences` lưu `TopDownGame.Feedback.CameraShake` và `.Hitstop`,
  độc lập save tiến trình; mặc định bật, load trước gameplay, Chơi mới không reset.
- Cài đặt có **RUNG CAMERA** và **DỪNG NHẸ KHI TRÚNG**, lưu ngay, cùng focus scope
  của modal. Tắt rung xóa offset ngay; tắt hitstop cancel giữ pause hiện có.
  Callback từ nút của cửa sổ đã đóng không được đổi setting.
- Test tự đổi sang `TopDownGame.Tests.Feedback.*`, snapshot/restore key và
  manager test. Không dùng PlayerPrefs.DeleteAll hoặc ghi save/preference thật.

## Kiểm chứng tập trung

Bảy `CombatFeedbackFlowTests` trên scene sinh sẵn:

1. 100 request cùng đợt và request liên tục: cap + có frame phục hồi.
2. Hitstop → hai modal pause → đóng từng modal, tắt setting; disable driver đang
   freeze và request khi driver tắt; đổi scene không kẹt timeScale.
3. Clock UI unscaled vẫn tăng, scaled cooldown và coroutine wait dừng rồi tiếp.
4. Direct hit player/enemy thực; không feedback cho periodic/non-player/offscreen,
   độc player, né thành công; tắt hai setting vẫn trừ HP bình thường.
5. Rung có smoothing không tích drift; clamp mép map, pause, setting tắt, disable,
   cinematic và mất target dọn offset.
6. Nút hai setting từ MainMenu/Pause, đóng callback, New Game/reload/persistence;
   render bật/tắt ở 1920×1080, 1280×800, 1024×768, text/bounds.
7. Impact MeteorShower thật và coroutine SlamRoutine của Forest Guardian: pause
   dài hơn windup không gây damage, resume hoàn tất đòn và rung.

Lượt đầu **7/7 đạt** (`Logs/Feedback_Focused_Initial.json`). Sau rà soát đã tăng
độ phủ driver đang active (đợi recovery rồi assert request được nhận), smoothing,
mép map thật, callback bảng đóng, periodic/non-player/offscreen và disable setting.
Lượt source cuối **7/7 đạt**, 0 failed/skipped, 11,16 giây
(`Logs/Feedback_Focused_Final.json`, job `1a30692030714786b5821a193b663d8d`).

Có 12 ảnh `Assets/Screenshots/PhanHoi_CaiDat_*_Verified.png`, MainMenu/Pause ×
bật/tắt × ba tỷ lệ. Đã xem Pause tắt ở 4:3 và MainMenu bật ở 1080p; nút đủ chữ,
không chồng nút đóng, bố cục hiển thị giữ chỗ cho xác nhận/hoàn tác cũ.

Domain reload chuyển endpoint Unity từ 6401 sang 6400. Request refresh/run ở
cổng cũ timeout; reflection tại cổng mới xác nhận CurrentJobId=null/Running=false
trước khi dispatch test. Không restart job sống vì timeout quan sát.

## Gate source cuối

- **78/78 EditMode** đạt, 0 failed/skipped, 15,72 giây
  (`Logs/Feedback_EditMode_Final.json`).
- **141/141 PlayMode** đạt, 0 failed/skipped, 295,76 giây; gồm bảy bài U3 và
  toàn bộ regression đã có, kể cả combo kiếm/manual pause, chín skill,
  cooldown/dodge, settings/menu, tutorial, Lobby và portal
  (`Logs/Feedback_PlayMode_Final.json`, job `d428f7dbc64d48f6a3cf34b024b4c30f`).
- Validator **0 lỗi** (`Logs/Feedback_Validator_Final.json`). Không thay builder,
  scene/prefab/catalog nên không cần hai lượt dựng lại trong đợt runtime này.
- Native Console **0 error, 4 warning**; MainMenu, không Play/compile,
  **timeScale=1** (`Logs/Feedback_EditorFinal.json`). Ba cue Music_Exploration,
  Ambient_Forest/Ambient_Cave còn thiếu clip và AudioMissingClipProbe chủ đích.
  Console reader còn trả log scene test thiếu AudioListener/cleanup; không có
  exception runtime hoặc cảnh báo C# mới từ U3.
- `git diff --check` mã nguồn/test/docs đạt; chỉ có thông báo CRLF/LF.
  Meta cho hai script mới và test đã được Unity tạo.
- Đã xem lại ảnh Pause tắt 4:3 trên source cuối với tiêu đề Âm thanh & Phản hồi;
  hai nút vừa khung, đủ chữ và cách nút đóng.

Ba tiêu chí U3 (không drift/tắt rung, cap multi-hit, pause/resume không sai
scale) đã có test runtime và source gate đạt; đánh dấu U3 hoàn thành kỹ thuật.
Đây không phải xác nhận release hoặc thay thế đánh giá cảm giác với người chơi.

## Giới hạn và việc tiếp theo

Cần đánh giá cảm giác rung/nhịp dừng với người chơi ở loadout và FPS thực tế,
nhất là skill nhiều hit. Hai tùy chọn giúp tắt hiệu ứng khi không thích chuyển
động/dừng nhịp. Chưa nghiệm thu Windows restart/đổi màn hình trên player, chưa
có clip audio vùng hoàn chỉnh và chưa cân bằng HP/mana thử nghiệm. Các bài test
không xác nhận toàn bộ U2/U5/T2/T3 hay toàn game đã hoàn tất.