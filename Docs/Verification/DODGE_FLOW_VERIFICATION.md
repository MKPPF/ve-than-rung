# Né đòn — G1 (22/10/2026)

## Phạm vi

Thêm né đòn không tiêu mana, nối từ input → kiểm tra trạng thái → di chuyển
vật lý → miễn sát thương → hồi chiêu/HUD. Không thay cấu hình HP/mana thử nghiệm,
giá trang bị, chỉ số quái hoặc schema save. Builder tiếp tục là nguồn sinh
prefab, HUD và reference cho Lobby cùng sáu map chiến đấu.

| Thông số | Giá trị ban đầu |
|---|---|
| Phím | Space; nút dưới (South) gamepad |
| Hướng | Tám hướng theo input; click path theo hướng đang đi; đứng yên dùng hướng nhìn cuối |
| Thời gian | 0,28 giây |
| Tốc độ | 7,5 đơn vị/giây, khoảng 2,1 đơn vị khi không va chạm |
| Miễn sát thương trực tiếp | 0,18 giây đầu; không xóa/chặn độc đã có |
| Hồi chiêu | 1 giây từ lúc bắt đầu; không hưởng chỉ số hồi kỹ năng |
| Đồng hồ | Scaled time: pause giữ nguyên vị trí, hồi chiêu và cửa sổ miễn đòn |

## Thiết kế luồng

- `PlayerDodge` nhận input trước combat, chặn khi đang đánh/cast, đang khóa
  di chuyển, pause/modal, nhập văn bản, chết hoặc controller tắt. Nút South
  thuộc UI Submit nếu đang focus nút HUD; không đồng thời né rồi mở bảng.
- `PlayerMovement.FixedUpdate` dùng `MoveKinematically`/bộ cast-and-slide đang
  có. Không teleport, không đặt velocity để bỏ qua solver, không tắt collider.
  Né hủy đường click đang theo; có thể chọn điểm đi mới ngay sau khi kết thúc.
- `PlayerHealth` có cửa sổ miễn sát thương riêng; kết thúc né không xóa miễn
  sát thương sau trúng đòn. Trúng đòn ở phần cuối dễ tổn thương sẽ ngắt né.
- Đánh thường/tự đánh/bắn thử nghiệm và cast kỹ năng không khởi động trong né.
  Nhấn né cùng frame với lệnh đánh/cast ưu tiên né, không mất mana kỹ năng.
- Animator sở hữu sprite, hướng, màu và pose; dùng frame hướng hiện có với
  tư thế hạ thấp/nghiêng nhẹ, không có bộ hình lộn vòng mới. Không script khác
  tự set flipX/transform của visual. Disable/chết/hết né trả pose về vị trí gốc.
- HUD nhỏ có trạng thái Sẵn sàng/Đang né/Hồi và thanh hồi; không phải nút input.
  Builder gắn reference; validator kiểm tra prefab, binding và HUD từng scene.
- Bước 2 tutorial và bảng hướng dẫn giải thích phím, thời gian miễn đòn và
  giới hạn không né giữa đòn đánh/cast. Sửa luôn câu hướng dẫn lò rèn cũ để
  thống nhất mua tự trang bị, I dùng đổi/tháo.

## Triển khai và kiểm chứng

- Mã đã compile; bổ sung reference `Unity.InputSystem` cho EditMode assembly
  để kiểm tra asset binding thật.
- Hai lượt builder đều có thông báo hoàn tất, validator sau lượt hai đạt 0 lỗi.
  Request dựng bị timeout ở lớp transport 25 giây; đã kiểm tra trạng thái Editor
  và thông báo hoàn tất trước khi chạy lượt kế tiếp, không khởi động lại do timeout.
- `DodgeAuthoringTests`: prefab nối movement/health/animator/input và hai binding.
- Tám `DodgeFlowTests`: tám hướng/hướng nhìn/không teleport, tường mỏng/trượt,
  cửa sổ miễn đòn và độc/post-hit immunity, pause/disable/chết/pose, bàn phím
  cùng frame/giữ Space/click sau né, gamepad/UI Submit, gate attack/cast/modal/
  nhập chữ, ảnh ba trạng thái ở ba tỷ lệ trên Lobby và Map01.
- Lượt tập trung source cuối đạt **14/14 PlayMode**, 0 failed/skipped, gồm tám
  bài né và sáu bài tutorial (35,22 giây; `Logs/Dodge_Focused_Final.json`).
- Toàn bộ **78/78 EditMode** đạt, 0 failed/skipped (15,77 giây;
  `Logs/Dodge_EditMode_Final.json`). Validator cuối đạt 0 lỗi
  (`Logs/Dodge_Validator_Final.json`). Kết quả full PlayMode ghi ở mục dưới.

Lượt tập trung đầu 14 bài có hai lỗi (`Logs/Dodge_Focused_Initial.json`):

- Hành trang tạm disable movement khi pause, làm PlayerDodge hiểu nhầm cần hủy
  né. Đã giữ né khi component movement tắt bởi pause chung; disable thực/chết
  vẫn hủy. Bài pause/disable/pose sau sửa đã đạt.
- Ảnh Map01 bắt đầu giữa một auto-cast đã được nhận ở những frame đầu scene.
  Diagnostic xác nhận cast/khóa movement đang hoạt động, nên né từ chối là đúng.
  Bài render được đổi để đợi timeline đó hoàn tất trước khi thử né; giữ nguyên
  gate runtime. Log diagnostic: `Logs/Dodge_ModalHud_Diagnostic.json`.

## Kết quả source cuối

- **130/130 PlayMode** đạt, 0 failed/skipped, 267,63 giây;
  `Logs/Dodge_PlayMode_Final.json`, job `4870ebaa619f4eed93c82d36b97849eb`.
  Bao gồm tám bài né mới và toàn bộ regression menu/tutorial/kho/lò rèn/
  hành trang/cường hóa/kỹ năng/tiềm năng/di chuyển/chuyển vùng.
- Native Console: **0 error, 3 warning**; MainMenu, không Play/compile
  (`Logs/Dodge_EditorFinal.json`). Cảnh báo là Music_Exploration,
  Ambient_Forest thiếu clip và AudioMissingClipProbe chủ đích của test.
  Console reader còn trả log thiếu AudioListener ở scene test và cleanup;
  không có exception runtime mới.
- Có **18 ảnh né đòn**: Lobby/Map01 × Sẵn sàng/Đang né/Hồi chiêu ×
  1920×1080, 1280×800, 1024×768. Tên `Assets/Screenshots/NeDon_*_Verified.png`.
  Đã xem Map01 đang né 4:3, Map01 sẵn sàng 1080p, Lobby hồi 1280×800,
  Lobby đang né 4:3 và tutorial bước 2 ở 4:3. Thẻ né không chồng lên UI cũ.
  Sáu bước tutorial được render lại thành 18 ảnh với chỉ dẫn né ở bước 2.
- `git diff --check` cho mã nguồn/test/docs/input asset đạt; chỉ có thông báo
  chuẩn hóa CRLF/LF. Không sửa tay whitespace trong YAML do Unity sinh.
- Test dùng key `TopDownGame.Tests.Progress.v1`, có snapshot/cleanup, không
  dùng DeleteAll hoặc ghi save thật. Không thay schema hay thông số balance.

## Đánh giá giao diện sau bổ sung

Né có nhãn phím, trạng thái và thanh hồi riêng; tutorial giải thích cửa sổ
miễn đòn và giới hạn hành động. Phản hồi chưa có âm thanh riêng hoặc U3.

Ảnh Map01 1080p cho thấy HUD chiến đấu cũ còn vấn đề: tên vùng/cấp bị xuống
hàng sát dòng đếm quái; nhóm tài nguyên chỉ có thanh màu, thiếu số HP/mana;
bốn thẻ kỹ năng chiếm nhiều diện tích dưới màn hình. Ở Lobby 4:3, các launcher
và dòng trợ giúp cũng khá nhỏ. Đây là ưu tiên U5 tiếp theo: thu gọn thanh skill,
tăng phân cấp thông tin, hiển thị HP/mana và cooldown bằng chữ, kiểm tra khả
năng đọc ở cả ba tỷ lệ. Chưa coi ảnh không tràn của thẻ né là nghiệm thu toàn HUD.

## Giới hạn nghiệm thu

Cần người chơi đánh giá khoảng né và độ rõ tư thế trong giao tranh thật. Chưa
có phản hồi camera shake/hitstop U3 hoặc âm thanh né riêng. Không coi G1/UI là
bằng chứng T2 (balance) hay T3 (Windows release) hoàn tất.
