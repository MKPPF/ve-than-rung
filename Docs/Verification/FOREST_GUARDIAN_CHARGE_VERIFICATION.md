# Vệ Thần Rừng — điểm đáp đòn lao và địa hình

Ngày 23/10/2026, Unity 6000.5.6f1. Tiếp tục mục tiêu hoàn thiện các map sẵn có,
sau đợt hủy đòn/vòng đời đạn. Không coi đợt này là nghiệm thu toàn bộ boss.

## Bằng chứng trước sửa

- Bản trước thay đổi trong `Logs/BossChargeBackup/`.
- Job `32a6de739e8b4674b412c346fe2b2ee9`: hai bài đều thất bại;
  `Logs/BossCharge_Reproduction.json`.
  - Player đứng bên hông đường lao thực sự mất máu, nhưng không nằm trong bất
    kỳ vùng cảnh báo nào. Line cũ dài 4,5, rộng 0,72 trong khi impact tròn có
    bán kính AttackRange=2,3 và tâm đọc từ transform khi kết thúc impulse.
  - Dời boss 4 đơn vị trong windup làm vụ nổ xuất hiện ở đất không được báo;
    HP player 50.000 → 49.798. Bài này đặt hai prefab ở vùng trống của scene
    để tách khỏi tường arena đẩy player đi; không dùng nó để chứng minh địa hình.

## Thay đổi

- Chốt điểm đáp trước windup từ vị trí Rigidbody. Plan tính lại quãng đường
  nominal của impulse cũ theo mass, impulseDamping và fixedDeltaTime, rồi cast
  toàn collider để dừng trước mặt chắn đầu tiên. Prefab boss mass=1; với bước
  0,02 giây, impulse=10 và damping=20, quãng đường trống khoảng 1,96 đơn vị.
- Vẽ vòng cam đúng tâm/radius vụ nổ; dải mảnh, mờ chỉ hướng đến điểm đáp.
  Owner giữ cả hai marker đến khi đáp hoặc hủy, kể cả khi frame trễ hoặc pause.
  Lease VFX tái sử dụng xóa trạng thái giữ vòng báo.
- Khi pool đầy, ưu tiên thu hồi lease không được giữ (impact) trước vòng báo
  điểm đáp còn chờ; giữ hard cap hiện có là 96 effect.
- EnemyMovement sở hữu chuyển động mới: Rigidbody2D.MovePosition trong
  FixedUpdate, SmoothStep ở đầu/cuối, giữ đường thẳng đã báo. Đi bộ, đuổi và
  slide bình thường tiếp tục dùng solver cũ. Không sửa flip/visual ngoài animator.
- Boss bị dời quá 0,12 đơn vị trong windup sẽ hủy đòn. Nếu xuất hiện vật cản
  mới, bị hất lùi hoặc bị dời giữa active, boss dừng charge, không dời vụ nổ
  sang đất chưa báo. Knockback nhận từ player vẫn được áp dụng.
- Chỉ nổ khi charge hoàn tất và Rigidbody tới trong 0,08 đơn vị quanh điểm đã
  chốt; damage/hất lùi lấy tâm đã báo. Giữ windup 0,42, active 0,28 và recovery
  0,25 giây; không đổi damage multiplier, radius, HP, armor, level hay loot.
- Không đổi builder, prefab, map, asset content hoặc save schema trong đợt này.

## Kiểm thử và giới hạn

`ForestGuardianFlowTests` dùng BossMap01, prefab thật và snapshot/restore test
prefs như trước. Các bài logic hất lệch/vật cản chủ động dùng khu trống (1000,
1000) và tường mỏng tạo trong test; bài hình ảnh và charge dừng dùng arena thật.
Không lấy tường synthetic làm bằng chứng đã đi hết các góc arena thật.

Các bài mới kiểm tra: side hit được báo, displacement windup không dời nổ,
đáp đúng vòng qua từng bước vật lý/pause/reuse VFX, tường có trước, tường xuất
hiện sau vòng báo, knockback active, chết/disable active và reenable, render
windup/landing ở ba tỷ lệ. Giữ toàn bộ bài boss cũ.

Lượt tập trung đầu `0af0190d050d418dac29de262062e300` có 6 lỗi, lưu
`Logs/BossCharge_Focused_First.{json,xml}`: các staging teleport dùng Transform
nội suy không đồng nhất với Rigidbody, và bài projectile còn dùng Transform
để đặt điểm contact. Đã dùng Rigidbody cho quyết định charge và đồng bộ hai
tọa độ trong fixture; không bỏ assertion di chuyển/ngắt/hit. Lượt kế tiếp
`a80d5d41f4a3491b8c179ed0556b7d2c` đạt 19/19, 0 skipped:
`Logs/BossCharge_Focused_Second.json`.

Kiểm tra áp lực pool bổ sung tạo 120 Burst trong lúc hai marker đang chờ:
job `ac1099ba64804f9fba8d2f521213b992` thất bại trước sửa ưu tiên thu hồi,
`Logs/BossCharge_PoolPressure_Reproduction.json`. Ring/line bị đổi tên thành
impact ngay trong windup, dù lifetime giữ vòng báo chưa kết thúc. Đã sửa
Acquire ưu tiên lease không được giữ thay vì vòng chờ của boss.
Sau sửa, job `ef0800b535ba46869cfe6371dfe3a501` đạt 1/1, 0 skipped:
`Logs/BossCharge_PoolPressure_Release.json`.

Ảnh: `Assets/Screenshots/VeThanRung_Lao_{BaoDiemDap|DapDungVung}_<resolution>_Verified.png`,
1920×1080, 1280×800, 1024×768. Camera theo player; pose được đóng băng bằng
GameplayState để render HUD/marker. Không phải đánh giá nhịp chiến đấu bằng người.
Đã xem cả sáu ảnh. Ở 4:3, một phần biên phải của vòng báo nằm sau nhóm nút HUD;
ảnh chứng minh marker đúng điểm đáp và chữ đọc được, chưa chứng minh toàn bộ
vùng chiến đấu luôn thoáng khỏi HUD. Đây là việc cần xử lý tiếp khi bố trí vùng nhìn.

## Kết quả cuối trên nguồn đã sửa

| Cổng | Kết quả | Bằng chứng |
|---|---|---|
| PlayMode toàn bộ | 189/189, 0 failed/skipped, 409,558 giây | Job `90776e0ad17c4f0d8169499355e4a798`; `Logs/BossCharge_PlayMode_Release.{json,xml}` |
| EditMode toàn bộ | 89/89, 0 failed/skipped, 14,127 giây | Job `e53b071cec564601a0343c7e6921d751`; `Logs/BossCharge_EditMode_Release.{json,xml}` |
| Validator | IsValid=true, không lỗi validation | `Logs/BossCharge_Validator_Release.json` |
| Console gốc | 0 error, 4 warning | GetCountsByType trong log validator; `Logs/BossCharge_Console_Release.json` |

Lượt toàn phần bao gồm cả 20 bài boss (11 cũ và 9 mới), không chỉ bài charge.
Các smoke test trượt tường/đổi hướng của EnemyMovement và luồng combat khác
cũng đạt trên cùng nguồn. Không mở rộng test sau khi đã qua các cổng này.

Bốn warning vẫn là ba cue Music_Exploration/Ambient_Forest/Ambient_Cave thiếu
clip và AudioMissingClipProbe chủ đích của test. Native Console là số đếm
chuẩn; reader MCP còn trả một số dòng cleanup/audio listener từ quá trình test.
Editor kết thúc ở MainMenu, không Play/compile, timeScale=1. InputSettings gốc
còn sống, flags/chế độ focus được khôi phục, Keyboard/Mouse native bật lại.

File chính: `EnemyMovement.cs`, `ForestGuardianBoss.cs`, `EnemyCombatVfx.cs`,
`ForestGuardianFlowTests.cs`; sáu ảnh charge và tài liệu README/ROADMAP/báo cáo.
Không đổi builder/content, không chạy lại builder trong đợt runtime này.
Diff trong phạm vi thay đổi không có lỗi khoảng trắng. Mục tiêu chung và U5
tiếp tục mở cho các giới hạn bên dưới.

## Còn mở trong mục tiêu chung

- Summon ở sát địa hình, minion tích lũy và toàn luồng thắng/thưởng/ra cửa boss.
- Camera/cảnh vật ở rìa các map; cân bằng thử nghiệm 50.000 HP/25.000 mana.
- Đòn tròn vẫn theo overlap như hệ combat cũ, chưa thêm occlusion xuyên tường
  cho toàn bộ AoE. Bộ test ở đây chứng minh collider boss không xuyên tường và
  vụ nổ không đổi điểm đã báo khi charge bị cản.
- Ba clip nhạc/ambient thiếu và nghiệm thu trận đấu/người chơi/Windows.
