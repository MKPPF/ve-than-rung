# HUD tiện ích và navigation — U5 (22/10/2026)

## Phạm vi và nhận xét UI

Launcher cũ ở Lobby bị thu xuống 76%, trong khi các nút chiến đấu có chiều cao
và chiều rộng khác nhau. Nhãn minimap 17 và tài nguyên Lobby 17 bị nhỏ khi
CanvasScaler co về 4:3. Các nút tự động tắt navigation và thông báo cử chỉ IMGUI
có thể trùng vùng hướng dẫn dưới màn hình.

Đợt này thống nhất nhóm điều khiển HUD và độ dễ đọc ở ba kích thước đã kiểm tra.
Không đổi content, builder, schema save, giá đồ hoặc thông số combat. Builder
không bị sửa trong đợt này nên không cần dựng lại scene.

## Thay đổi

- `HudUtilityLayout`, được ensure cùng PlayerAutomationUI, bố trí bốn launcher
  300×54, text 24, bước dọc 64 và cùng vị trí ở Lobby/combat. Scale mỗi khung là
  max(1, 0,7 / canvas.scaleFactor), giữ chữ nút tối thiểu 16,8 pixel vật lý ở các
  kích thước hỗ trợ. Các launcher vẫn ở canvas root, không thay callback của
  Inventory/Potential/Skill/Enhancement. Bộ đếm điểm, số skill và tinh hoa vẫn
  do controller cập nhật. Nhãn N đổi thành CƯỜNG HÓA để khớp bảng thực sự mở.
- Tab hoặc View vào/rời nhóm nút; mũi tên/WASD/D-pad/stick và Enter/A dùng action
  Navigate/Submit sẵn có. Link explicit đi dọc các nút đang hoạt động và có thể
  tương tác. Modal dùng CanvasGroup scope hiện có; layout không mở khóa nền.
- Layout đọc UI action trước world controls. `ConsumeNavigationInput` có hiệu
  lực đúng frame để Navigate không đồng thời di chuyển và Submit không đồng
  thời né. Không thêm pause hoặc giữ input owner; rời HUD trả điều khiển bình
  thường. Click nền dùng deselectOnBackgroundClick hiện có của UI module.
- Tự đánh/tự nhặt/cử chỉ có nhãn BẬT/TẮT bằng chữ và màu; CanToggle vẫn từ chối
  modal, timeScale=0 và người chơi đã chết. Trạng thái cử chỉ có nền tối, tự vừa
  số dòng. Thêm hướng dẫn Tab/View, điều hướng và xác nhận ngay dưới nhóm nút.
- GestureInputReceiver đăng ký nơi hiển thị trạng thái HUD. Khi HUD tồn tại,
  thông báo IMGUI dự phòng không đè lên prompt/skill strip. MainMenu/scene thiếu
  HUD vẫn có fallback. Tắt cử chỉ có câu ngắn; bật chưa có packet hướng dẫn chạy
  mục 8 notebook. Không đổi giao thức UDP, quyền bật camera hoặc logic model.
- Thẻ Lobby tăng chữ, tách hướng dẫn di chuyển/tương tác hai dòng. Thẻ né dùng
  font 22 và chiều cao 78; vị trí ControlHints được tính lại để không chồng khung.
- Minimap giữ render texture/camera/marker thật, tăng header hai dòng và thêm
  chú giải Bạn/Lối đi/Rương/Địch với sprite/màu tương ứng. Lobby hiện TRẠM NGHỈ
  TÁN RỪNG. Toàn bộ graphics minimap và các khung thông tin tiếp tục không nhận click.

## Kiểm chứng tập trung

Bốn `HudUtilityFlowTests`, dùng progress/preferences test và phục hồi trạng thái:

1. Click/Submit mở đủ bốn bảng thật, khóa controls nền, đóng trả focus; số điểm
   và tinh hoa cập nhật không tràn label. Tab chọn nút đầu, mũi tên và S chuyển
   focus không dịch player; D-pad chuyển focus; A bật tự nhặt không gây dodge.
   View rời nhóm và phím mũi tên lại di chuyển bình thường, không kẹt input.
2. Tự đánh/tự nhặt giữ độc lập và từ chối callback trong modal; hint cử chỉ tắt
   được đồng bộ từ receiver.
3. Bật cử chỉ với cổng localhost test tạm thời, chưa gửi packet: hiện BẬT và
   hướng dẫn chờ camera. HUD đăng ký thay overlay cũ; tắt/khôi phục cổng sau test.
4. Render Lobby, Map01, BossMap01, RiftMap01 tại 1920×1080, 1280×800, 1024×768:
   label không tràn; font trong các khung mới ≥14,5 pixel vật lý; khung ở trong
   canvas và không chồng nhau hoặc combat HUD. Minimap không nhận pointer raycast.

Tập trung cuối: **4/4 đạt**, 7,01 giây, job `af66f630d26f422fb40a3ffca524669e`;
`Logs/HudUtility_Focused_Final.json`.

Ảnh: 15 file `Assets/Screenshots/HUDTienIch_*_Verified.png`, gồm bốn scene và
Lobby bật cử chỉ × ba tỷ lệ. Đã xem trực tiếp Lobby/Rift 4:3, boss 1080p và Lobby
chờ camera 4:3. Ảnh render từ scene thật; kiểm thử input chạy trên device mô phỏng.

## Regression cuối

| Gate | Kết quả bản cuối | Bằng chứng |
| --- | --- | --- |
| EditMode | 78/78 đạt, 16,15 giây | `Logs/HudUtility_EditMode_Final.json`, job `a3ab7303f93c46cf8667c9651f684dec` |
| Validator | IsValid=true, 0 lỗi | `Logs/HudUtility_Validator_Final.json` |
| PlayMode | 145/145 đạt, 0 failed/skipped, 313,06 giây | `Logs/HudUtility_PlayMode_Final.json`, job `3cf22e3093e8467ab066e03c5c97e930` |
| Console/Editor | 0 lỗi, 4 warning; không Play/compile; MainMenu; timeScale=1 | `Logs/HudUtility_EditorFinal.json` |

Warning native gồm ba cue chưa có clip (Music_Exploration, Ambient_Forest,
Ambient_Cave) và AudioMissingClipProbe chủ động của test. Console reader còn
trả thông báo no-audio-listener ở scene probe và log cleanup Test Runner.
Không có warning mới về HUD hoặc cổng cử chỉ. Scoped `git diff --check` đạt;
Git chỉ nhắc chuyển CRLF sang LF ở file cường hóa đã có từ trước.

Không sửa source sau lượt full green. Test dùng key `TopDownGame.Tests.*`,
không dùng DeleteAll; temporary port test và trạng thái automation được phục hồi.

## Giới hạn còn mở

- Giữ U5 ở `[-]`: chưa nghiệm thu với người chơi mới, thiết bị tay cầm vật lý,
  màn hình Windows/restart và cử chỉ từ webcam thật.
- Đây là pass bố cục/luồng HUD ở ba kích thước nêu trên; không khẳng định mọi
  cỡ cửa sổ nhỏ hơn đều đạt, không phải balance pass cho cấu hình HP/mana thử nghiệm.
- Các hạng mục audio còn thiếu clip và release progression vẫn nằm trong roadmap.
