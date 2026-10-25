# HUD chiến đấu — U5 (22/10/2026)

## Vấn đề đã thấy trong ảnh trước sửa

Ảnh né đòn Map01 1080p có tên vùng/cấp xuống hàng sát đếm quái, chỉ có thanh
màu cho HP/mana, nhãn Gold/Level còn tiếng Anh. Bốn ô kỹ năng 280×280, trải
ngang gần 1.180 đơn vị canvas, chiếm nhiều vùng chơi nhưng chỉ có phím Q/E/R/F.
Thiếu tên, số hồi chiêu, trạng thái thiếu mana và chỉ dẫn cho ô trống.

## Thay đổi

`Assets/Scripts/UI/GameplayHUD.cs` tiếp tục dựng UI runtime như trước, không
sửa scene/prefab/catalog/builder hay dữ liệu save/balance. Bản trước sửa:
`Logs/HudReviewBackup/GameplayHUD.cs`.

- Khung tài nguyên tối viền đồng: tên vùng tách cấp đề nghị và số quái đang có;
  số máu/mana hiện tại/tối đa trên thanh, cấp/XP, vàng/Tinh hoa. Dữ liệu lấy
  từ MapData và event PlayerHealth/PlayerStats/GameProgressManager hiện có.
- Bốn ô kỹ năng ở giữa cạnh dưới, cả thanh 596×180 thay cho khoảng 1.180×280;
  mỗi ô có icon, Q/E/R/F, tên tối đa hai dòng và trạng thái chữ.
- Trạng thái ưu tiên: Ô trống → đã gục → đang hồi → thiếu mana → sẵn sàng.
  Ô trống nhắc K để trang bị, cooldown có số giây cùng overlay radial.
  Trạng thái có chữ để không chỉ phụ thuộc màu. Không gắn input/button mới.
- Khung boss giữa cạnh trên có tên tiếng Việt và số máu; ẩn cả khung ở map
  không có boss. Lên cấp/thưởng/tinh anh đổi nhãn UI sang tiếng Việt.
- Tách gợi ý điều khiển thành ba dòng có nền tối; đặt thẻ né cùng lề trái.
  Root hiển thị đứng đầu sibling, không tạo Canvas override nên modal vẫn
  vẽ trên HUD. Graphics chỉ hiển thị không nhận raycast, không ăn click đi.
- Hủy HUD dọn root và gỡ các event đã đăng ký. Launcher và minimap vẫn do
  controller hiện có quản lý; không thay cửa sổ quản lý trang bị/kỹ năng.

## Kiểm thử tập trung

Bốn `CombatHudFlowTests` dùng scene thật và snapshot/restore key
`TopDownGame.Tests.Progress.v1` cùng manager; không dùng DeleteAll/save thật.

1. Event mất/hồi HP, tiêu/hồi mana, vàng/Tinh hoa và lên cấp cập nhật số/bar.
2. Tháo skill, thiếu mana, hồi chiêu/scaled pause, sẵn sàng và chết; thẻ không
   bị hủy/tạo lại giữa các thay đổi.
3. Raycast trên khung tài nguyên, skill và hint không nhận HUD; mở hành trang
   vẫn khóa input và focus vào modal, đóng trả quyền chơi.
4. Render sáu map × ba tỷ lệ; bounds, không chồng panel/minimap/dodge/boss,
   không cắt chữ, cỡ chữ phần HUD mới tối thiểu 12 pixel tại các kích thước
   kiểm tra. Kiểm tra tên cả chín skill và thêm ảnh loadout hai/ba, ô trống,
   máu thấp/thiếu mana và hồi chiêu.

Lượt đầu 2/4 đạt (`Logs/CombatHUD_Focused_Initial.json`): tên ChainLightning
bị cắt, đã tăng vùng tên từ 44 lên 56 để đủ hai dòng. Fixture thiếu mana gặp
cooldown từ auto-cast đầu scene; đã đợi cast/cooldown hiện có kết thúc trước
khi kiểm tra trạng thái sẵn sàng/thiếu mana, không bypass gate runtime.
Sau sửa **4/4 đạt**, 0 failed/skipped, 18,54 giây
(`Logs/CombatHUD_Focused_Final.json`, job `c1fadcb88be24928ba0d8c82ac7ee9f0`).

## Ảnh đã xem

- Map01 thiếu mana/ô trống 1024×768 và boss 1080p ở lượt đầu.
- Source cuối: loadout hai Map01 ở 4:3 đủ hai tên dài trên hai dòng; RiftMap01
  1280×800 và boss ở 4:3, thanh kỹ năng/khung boss không đè minimap hay tài nguyên.
- Tổng 30 ảnh `Assets/Screenshots/HUDChienDau_*_Verified.png`: 18 ảnh sáu map,
  sáu ảnh thiếu mana/hồi chiêu và sáu ảnh loadout hai/ba ở ba tỷ lệ.

## Gate source cuối

- **78/78 EditMode** đạt, 0 failed/skipped, 12,60 giây
  (`Logs/CombatHUD_EditMode_Final.json`).
- **134/134 PlayMode** đạt, 0 failed/skipped, 283,46 giây; gồm bốn bài HUD
  và toàn bộ regression combat/input/dodge/menu/progression/tutorial/travel
  (`Logs/CombatHUD_PlayMode_Final.json`, job `2885efdc56ae4861bf9504bcc1c6750c`).
- Validator đạt 0 lỗi (`Logs/CombatHUD_Validator_Final.json`). Không thay
  builder/content nên đợt này không cần dựng lại scene.
- Native Console **0 error, 4 warning**; Editor ở MainMenu, không Play/compile
  (`Logs/CombatHUD_EditorFinal.json`). Music_Exploration, Ambient_Forest,
  Ambient_Cave còn thiếu clip; AudioMissingClipProbe là probe chủ đích. Cave
  warning xuất hiện vì bộ render đi đủ sáu map. Console reader còn trả log
  thiếu AudioListener ở scene test và cleanup; không có exception UI mới.
- `git diff --check` cho mã nguồn/test/docs đạt, chỉ có thông báo CRLF/LF.
  Không thay save schema, HP/mana thử nghiệm, mức giá hoặc chỉ số combat.

## Giới hạn còn lại

Cỡ chữ launcher/gesture-status/minimap cũ và accessibility người chơi mới vẫn
cần nghiệm thu riêng. Chưa thêm chỉnh tỉ lệ HUD theo người dùng, rebinding hoặc
screen-reader. Giá trị HP/mana vẫn là cấu hình thử nghiệm 50.000/25.000; UI
mới không xác nhận balance. U3/âm thanh theo vùng/Windows player vẫn còn mở.