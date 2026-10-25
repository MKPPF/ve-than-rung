# Khôi phục tiến trình không đọc được — U5

Ngày 22/10/2026, Unity 6000.5.6f1. Đợt này xử lý một lỗi mất dữ liệu trong luồng
MainMenu → Tiếp tục/Chơi mới. U5 và mục tiêu hoàn thiện game vẫn đang làm.

## Tái hiện trước sửa

Hai PlayMode test mới đều thất bại trên mã cũ: `LoadProgress` xóa key primary khi
save rỗng hoặc JSON bị cắt giữa chừng. Dữ liệu không còn để người chơi quyết định
khôi phục hay giữ lại. Bằng chứng: `Logs/SaveRecovery_Reproduction.json`, job
`8ec780c05426474eb5b44121256235df` (2/2 thất bại theo kỳ vọng tái hiện).
Bản mã trước sửa tại `Logs/SaveRecoveryBackup/`.

## Hành vi đã triển khai

- `ProgressSnapshot` đọc v1 vào đối tượng độc lập, kiểm tra các field lõi,
  tài nguyên/progression hợp lệ, normalize collection và migrate tiềm năng/tutorial.
  JSON thiếu field lõi hoặc bị lỗi không thể áp dụng một phần lên manager đang sống.
  Vẫn chấp nhận HP thưởng cao hơn HP gốc và checkpoint 0 HP của save hiện có.
- Giữ key/schema v1 và các tên field campaign. Bổ sung hai bản ghi cùng prefix:
  `.Backup` giữ lần lưu hợp lệ khác gần nhất; `.Unreadable` giữ nguyên chuỗi primary
  lỗi của sự cố gần nhất đã được người chơi giải quyết. Chỉ record cuối cùng được giữ.
  Test dùng hoàn toàn prefix `TopDownGame.Tests.*`.
- Lần lưu đầu khởi tạo dự phòng; lần lưu giống hệt không đẩy mất revision trước.
  Primary hợp lệ sửa được backup hỏng. Primary lỗi hoặc mất nhưng còn backup sẽ
  chặn SaveProgress/CapturePlayer cho đến khi người chơi quyết định.
- Khi phát hiện lỗi ở menu, hiện giải thích tiếng Việt và xem trước cấp/vàng của
  backup đã kiểm tra. Mặc định focus ĐỂ SAU — GIỮ NGUYÊN; đóng/Esc không ghi dữ liệu.
  Có thể mở lại bằng KIỂM TRA TIẾN TRÌNH. Không có backup hợp lệ thì không có nút
  khôi phục. Không tự bắt đầu campaign hoặc tự phục hồi.
- Khôi phục kiểm tra lại cả primary và revision backup lúc xác nhận. Backup đã đổi
  phải xem trước/xác nhận lại; primary mới hợp lệ được ưu tiên. Thành công ở lại menu,
  hiện thông báo đã khôi phục rồi người chơi chọn Tiếp tục về Lobby.
- Chơi mới vẫn cần xác nhận khi có primary hoặc backup. Hủy giữ nguyên cả hai;
  xác nhận lưu raw primary lỗi riêng, reset cả primary/backup về campaign cấp 1,
  2.500 vàng và hướng dẫn lần đầu. Bản dự phòng campaign cũ không quay lại sau reset.
- Menu chặn callback nền/lặp/disable; MenuDialog gắn callback với panel sở hữu,
  callback cũ không thể đóng hoặc xác nhận hộp thoại mới. Giữ focus/modal hiện có.
  Dòng phụ/hướng dẫn menu tăng lên 20 để đủ ít nhất 14 pixel trong ba tỷ lệ đã thử.

Không sửa builder/content/scene trong đợt save này. Hai lượt dựng của đợt cửa chuyển
vùng trước đó vẫn là content đang dùng; không chạy lại builder cho thay đổi runtime.

## Kiểm tra

Sáu EditMode mới: dữ liệu thiếu/lỗi, số/entry không hợp lệ, roundtrip độc lập,
v1 nguyên thủy, migration/null collection lặp lại và HP thưởng/checkpoint đã gục.

Tám PlayMode mới:

1. Primary rỗng giữ nguyên; không autosave/Continue và không nút khôi phục khi thiếu backup.
2. JSON bị cắt/không liên quan/thiếu core/số âm không sửa live manager hay backup.
3. Xoay backup khác revision, giữ qua save lặp, sửa backup hỏng, restart với primary mất
   vẫn chờ xác nhận; API restore giữ backup và không tạo archive giả.
4. Navigation chỉ trong modal, Esc trả focus, modal khác chặn xác nhận, callback nền/lặp
   và callback panel cũ không khôi phục/chơi mới; Hủy giữ cả hai record.
5. Lượt chơi Map01 thật có XP/vàng, Shield ở slot F, vũ khí/cường hóa, kho, tinh hoa,
   boss flag và vùng mở. Restore qua UI → tạo lại manager → Tiếp tục đúng một lần về
   Lobby, giữ trạng thái campaign trước đó và không mở lại tutorial đã hoàn tất.
6. Backup không dùng được → để sau/Hủy giữ raw → xác nhận Chơi mới reset campaign
   và backup, giữ archive nguyên văn, mở tutorial.
7. Đổi backup sau preview phải xác nhận lại; primary mới hợp lệ không bị ghi đè bởi
   nút khôi phục cũ.
8. Năm trạng thái UI ở 1920×1080, 1280×800 và 1024×768: không tràn chữ, chữ ≥14 pixel,
   nút trong canvas; lưu 15 ảnh `KhoiPhucSave_*_Verified.png`.

Fixture chụp/khôi phục đủ ba key, trạng thái manager và tự động hóa. Callback test
bổ sung dọn hai auxiliary key cho các fixture cũ, kể cả coroutine thất bại; không
chạm key save người chơi hoặc dùng DeleteAll.

Lượt tập trung đầu phát hiện dòng Eyebrow, lượt sau phát hiện Controls nhỏ hơn
14 pixel; đã tăng cả hai và chạy lại. Nhóm cuối **12/12 đạt**, 13,15 giây,
`Logs/SaveRecovery_Focused_Final.json`, job `33029edb569e45d4af2b83e8021f9aab`.
Có thêm bốn test hồi quy Chơi mới/lần đầu/vàng cũ/tutorial v1 trong nhóm này.

Toàn bộ EditMode **86/86 đạt**, 16,02 giây; `Logs/SaveRecovery_EditMode_Final.json`,
job `50e421a1a39144e38fd2202216b4e935`.

## Gate cuối

| Kiểm tra | Kết quả | Bằng chứng |
| --- | --- | --- |
| Nhóm tập trung | 12/12 đạt, 0 failed/skipped | `Logs/SaveRecovery_Focused_Final.json` |
| EditMode toàn bộ | 86/86 đạt, 16,02 giây | `Logs/SaveRecovery_EditMode_Final.json` |
| PlayMode toàn bộ | **165/165 đạt**, 0 failed/skipped, 340,65 giây | `Logs/SaveRecovery_PlayMode_Final.json`, job `784f3ef5e2cf472ba5ee7459a3339a48` |
| Validator | IsValid=true, 0 lỗi | `Logs/SaveRecovery_Validator_Final.json` |
| Native Console/Editor | 0 lỗi, 4 warning; MainMenu, không Play/compile, timeScale=1 | `Logs/SaveRecovery_EditorFinal.json` |
| Giao diện | 15 ảnh, đủ năm trạng thái × ba tỷ lệ | `Assets/Screenshots/KhoiPhucSave_*_Verified.png` |
| Whitespace | Scoped git diff --check đạt | Các file runtime/test/tài liệu trong đợt này |

Đã xem năm trạng thái: CoDuPhong, DeSau, XacNhanChoiMoi, KhongDuPhong ở 4:3 và
DaKhoiPhuc ở 1080p; đủ nội dung, nút trong khung và dấu tiếng Việt hiển thị đúng.
Các ảnh được tạo lại trong lượt PlayMode toàn bộ cuối. Không sửa runtime/test
sau lượt full green; chỉ bổ sung báo cáo, mục lục và ghi chú roadmap.

Bốn warning native là Music_Exploration, Ambient_Forest, Ambient_Cave chưa có
clip và AudioMissingClipProbe chủ động của test. Console reader còn trả thông
báo audio listener ở scene probe/cleanup và ghi kết quả Test Runner; không có
lỗi C#/scene/save/reference mới. Xem `Logs/SaveRecovery_Console_Final.json`.

## Giới hạn

- Backup nằm trong cùng kho PlayerPrefs; đây không phải bản sao ngoài máy hay
  transaction chống mất điện. Chưa mô phỏng disk-full, registry hỏng hoặc crash
  trong PlayerPrefs.Save; chưa nghiệm thu Windows player sau restart thật.
- Chỉ khôi phục dữ liệu parse/validate được. Không suy ra tiến trình bị mất, không
  sửa tự động JSON lỗi, không cam kết dữ liệu lỗi được giữ riêng có thể đọc lại.
- Save v1 hợp lệ có thể chưa có backup cho đến lần ghi tiếp theo. Validate không
  nhận biết mọi thay đổi có chủ đích thành một JSON hợp lệ.
- Chưa cân bằng HP/mana thử nghiệm hoặc thay soundtrack. Map03 thiếu seed quái trong
  builder, ba cue nhạc/ambient thiếu và nghiệm thu accessibility/người chơi/webcam
  vẫn còn mở. Không đánh dấu U5 hoặc toàn bộ game hoàn tất.
