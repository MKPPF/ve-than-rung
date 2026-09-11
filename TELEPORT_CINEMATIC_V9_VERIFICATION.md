# Truyền tống trận điện ảnh V9

## Phạm vi

- Vòng ngoài khép kín, cùng sáng với vòng trong, có hai vệt sáng chạy riêng.
- Chọn vùng → đóng bảng → khóa input (không pause) → zoom out 18% và dựng
  tia sáng/hạt sáng/vòng sáng bay lên → sau 3 giây mới chuyển map.
- CameraFollow2D vẫn là chủ sở hữu vị trí/zoom, vẫn kẹp viewport trong tranh.
- LobbyPortal sở hữu đồng hồ và lệnh load duy nhất. TeleportDepartureVisual
  chỉ trình bày; shader dùng đồng hồ từ portal, không dùng thời gian độc lập.
- Pause/modal khác dừng tiến độ. Tắt portal hoặc nhân vật không còn hợp lệ
  sẽ hủy, trả camera và quyền điều khiển. Không lưu trước khi hiệu ứng xong.
- Không thay save schema, số vùng mở khóa, thời gian mở bảng hay combat.
- Backup: Backups/TeleportCinematicV9/.

## File chính

- Assets/Editor/TopDownGameBuilder.cs
- Assets/Scripts/Maps/LobbyPortal.cs
- Assets/Scripts/Maps/TeleportCircleVisual.cs
- Assets/Scripts/Maps/TeleportDepartureVisual.cs
- Assets/Scripts/Maps/LobbyStationSign.cs (ẩn nhãn trong lúc trình diễn)
- Assets/Scripts/Core/CameraFollow2D.cs
- Assets/Shaders/TeleportDeparture.shader
- Test: LobbySceneryTests, ZZLobbySceneryPlayModeTests, ZZZPortalTransitionTests.

## Xác minh

- Hai lượt builder hoàn tất: marker MVP built trong Logs/Editor.log tại
  dòng 11834 và 13918 của phiên này.
- Validator đạt, marker automated validation passed tại dòng 14087.
- EditMode 64/64 Passed: Logs/TeleportCinematicV9_EditMode.xml,
  job e0bc1171ac4c4e23aa035f9d44fbdc83.
- Hai test thực tế Lobby/truyền tống đạt 2/2, job
  641fbb9866794c8093de802a970463e7: zoom >10%, input khóa nhưng timeScale=1,
  có shader tia sáng, đủ thời gian trình diễn, pause, hủy bằng disable trả camera,
  click lặp không tải hai lần, chuyển thật sang Map01 đúng một lần.
- PlayMode bản cuối: 62/62 Passed, Logs/TeleportCinematicV9_PlayMode.xml,
  job 7a589c9a1a3349f7ab9103235c60e990. Đã chạy lại các kiểm tra Lobby và
  chuyển Map01 thật trong lượt đầy đủ này.
- Ảnh đã xem: Assets/Screenshots/TruyenTongTranV9_LuiMay_TiaSang_Verified.png
  (camera thật, 1920x1080) và TruyenTongTranV9_CanCanh_TiaSang_Verified.png
  (camera cận bổ sung). Vòng ngoài đầy đủ, tia sáng hiện rõ, chữ trạm đã ẩn,
  nhân vật vẫn đọc được. Ảnh chụp tại khoảng 65% tiến độ, không phải ảnh ghép.
- Console không có lỗi C#/shader/runtime mới; connector xếp thông báo lưu XML
  vào bộ lọc error. Warning obsolete cũ ngoài phạm vi vẫn còn.
- Giới hạn: zoom 18% có thể giảm ở màn hình rất rộng do giới hạn tranh;
  luôn ưu tiên không lộ nền trống. Toàn bộ vòng ngoài là lớp phép thuật đầy đủ,
  có thể sáng chồng lên phần chân cột trong tranh nền phẳng.
- Không thêm âm thanh mới trong đợt này.
