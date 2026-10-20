# Nút tự đánh và tự nhặt — 20/10/2026

## Thay đổi

- `PlayerAutomationUI` tạo hai nút theo mẫu FantasyEquipmentUI, dưới nhóm
  launcher phía phải HUD tại Lobby và các map chiến đấu. Có chữ BẬT/TẮT và
  màu trạng thái; phím T đồng bộ với nút tự đánh.
- `PlayerAutomationState` giữ lựa chọn qua chuyển scene trong phiên chơi.
  Reset khi bắt đầu phiên mới; không thay schema hay ghi vào save thật.
- Tắt tự đánh chặn auto combat và hủy đánh thường tự động đang trong thời
  gian chuẩn bị. Chiêu đã bắt đầu cast vẫn hoàn thành; đánh/skill/cử chỉ thủ
  công đi qua pipeline hiện có.
- Tắt tự nhặt chặn hút/trigger/nhận thưởng theo khoảng cách. EnemyLootPickup
  và ItemPickup hỗ trợ IInteractable: E/nhấp đúp khi đứng gần để nhặt thủ công.
- Bật lại tự nhặt có hiệu lực với đồ đang ở gần hoặc đã nằm trong trigger.
  Modal/pause/người chơi chết chặn nhận đồ và thao tác nút.
- Không thay builder, scene, prefab hay content; runtime HUD tự tạo nút cho
  cả scene hiện có và scene do builder tạo sau này.

## Kiểm tra

- Unity biên dịch thành công; Console cuối phiên có 0 error / 0 warning.
- Project validator đạt, không có lỗi. Không thay builder nên không cần dựng
  lại content.
- **77/77 EditMode**, **82/82 PlayMode** đạt, không skip. PlayMode bao gồm bốn
  test mới trong `Assets/Tests/PlayMode/PlayerAutomationTests.cs`:
  - Hai nút độc lập, chặn thao tác khi modal, không tạo UI trùng và player mới
    nhận lại lựa chọn trong phiên.
  - Tắt nhặt chặn cả trigger và khoảng cách; thao tác thủ công nhận vàng đúng
    một lần và bị chặn khi pause/modal.
  - Tắt nhặt ngừng hút; bật lại tự hút/nhặt đồ đang ở gần.
  - ItemPickup chặn trigger khi tắt, nhận qua tương tác thủ công và nhận lại
    qua OnTriggerStay khi bật.
- Job PlayMode: `36499e3c50fb4adba4c91beeee8213f9` (205,15 giây).
  Job EditMode: `5714d215e7664e1fa110ca3bf4285116` (11,67 giây).
  JSON kết quả lưu tạm ở `Temp/AutomationPlayModeResults.json` và
  `Temp/AutomationEditModeResults.json` (không track Git).
- Playtest bằng bridge trên scene thật: tắt hai chế độ ở Map01; chuyển runtime
  về Lobby xác nhận cả hai vẫn tắt; gọi sự kiện click của hai nút xác nhận cả
  hai bật lại. Đã xem ảnh HUD ở độ phân giải 3840×2160, nút nằm dưới nhóm launcher
  và không đè lên nhau:
  - `Assets/Screenshots/AutomationControls_Map01_Off.png`
  - `Assets/Screenshots/AutomationControls_Lobby_On.png`
- Phiên playtest có snapshot save thật trước khi chạy và đã phục hồi snapshot
  sau khi Stop. Editor đã được trả về MainMenu, không chạy Play Mode.

## Phạm vi

- Hai lựa chọn thuộc phiên chơi, không phải cài đặt lưu qua khởi động lại app.
- Tắt tự nhặt áp dụng cả gói vàng/XP/trang bị rơi từ quái và ItemPickup.
  Rương thưởng vẫn dùng tương tác riêng như trước.
- Kiểm tra tự động cô lập key `TopDownGame.Tests.Progress.v1` và phục hồi dữ
  liệu test/in-memory sau mỗi test, không dùng PlayerPrefs.DeleteAll().
- Test nút dùng Unity UI callback; chưa xác nhận bằng thao tác chuột vật lý
  hoặc phiên webcam của người dùng. Chưa kiểm tra bố cục nút mới ở màn 4:3.
