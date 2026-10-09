# Trạm nghỉ Tán Rừng — bản bàn giao 05/09/2026

## Đã thay đổi

- Builder thay toàn bộ nền cỏ/đường đá/props Lobby cũ bằng tranh môi trường riêng.
- Sân đá, suối, lò rèn, nhà kho, trại và cổng cổ cùng phong cách rừng fantasy.
- Thợ rèn tĩnh tích hợp trong tranh; điểm tương tác Unity riêng tại (-1.15, 1.85).
- Kho đồ tại (5.85, 1.65), cổng tại (8.55, -0.35), xuất phát giữ (-5.5, 0).
- Tranh 26×15,6 đơn vị, 18 collider biên theo bờ suối, nhà và tường.
- Camera Lobby có giới hạn và tự điều chỉnh ở màn hình rộng; combat camera giữ nguyên cấu hình.
- Ánh sáng lồng đèn, khói ống khói, đom đóm và ánh cổng chuyển động nhẹ, không tạo collider.
- Biển tên trạm có độ sáng theo khoảng cách, gợi ý chỉ hiện khi đến gần.
- LobbyHUD thay HUD chiến đấu tại sảnh: bỏ các ô skill lớn, giữ vàng/cấp/tinh hoa,
  minimap, các nút mở menu và gợi ý tương tác.
- Mọi scene/prefab vẫn sinh từ TopDownGameBuilder; không chỉnh scene thủ công.

## Xác minh cuối

- Builder bản cuối hoàn thành hai lượt (log Editor.log, các dòng hoàn tất 17306 và 19457
  tại thời điểm kiểm tra; Logs/LobbyV3_Build1.done cũng ghi nhận lượt được lên lịch).
- EditMode: 49/49 đạt, job d9157689cdf34da1b2bf90da01c477c3.
- PlayMode: 51/51 đạt, job cd6b331d77064c9fab4e09751923cd1a.
- ValidateProject(true): IsValid=true, errors=[].
- Bài kiểm tra thực tế tải Lobby, đi bằng PlayerMovement tới lò rèn, kho, cổng,
  mở/đóng cửa hàng và kho, kiểm tra destination Map01, thử va chạm mặt tiền lò rèn.
- Kiểm tra viewport 16:9, 16:10, 4:3, ultrawide nằm trong tranh; chụp ảnh bằng camera runtime.
- Không bấm chuyển cổng thật trong fixture; chỉ kiểm tra component, khả năng tương tác,
  destination và scene graph validator.
- Test dùng khóa TopDownGame.Tests.*, không dùng DeleteAll hoặc đổi save thật.

## Ảnh và tài liệu

- Assets/Screenshots/SanhChoV3_ToanCanh_Verified.png
- Assets/Screenshots/SanhChoV3_GameplayHUD_Verified.png
- Assets/Screenshots/SanhChoV3_Spawn_Verified.png
- Assets/Screenshots/SanhChoV3_WeaponMerchant_Verified.png
- Assets/Screenshots/SanhChoV3_StorageChest_Verified.png
- Assets/Screenshots/SanhChoV3_BattlePortal_Verified.png
- Có thêm ảnh 1024×768, 1920×1200, 2560×1080.
- Logs/LobbyV3_EditMode.xml và Logs/LobbyV3_PlayMode.xml.
- Prompt ImageGen và hướng mỹ thuật: Assets/Art/Generated/Lobby/ART_DIRECTION.md.
- Backup trước thay đổi: Backups/Lobby_WoodlandV3/.

## Giới hạn

- Đây là cảnh vẽ sẵn kết hợp collider, đối tượng tương tác và hiệu ứng runtime,
  không phải tilemap chia từng mái nhà/cây. Không có nội thất đi vào được.
- Thợ rèn đứng yên; thử tạo sprite alpha không thành công nên không ship ảnh nền giả trong suốt.
- Suối/thác vẽ tĩnh, chỉ hiệu ứng ánh sáng/khói/đom đóm chuyển động.
- Chưa kiểm thử thủ công bằng chuột/phím xuyên suốt một lượt chơi; đã có smoke test thực tế bằng API Unity.
- Unity MCP có lần timeout do reload/đổi cổng; kết quả cuối lấy từ lượt test thành công.
- Builder phát cảnh báo Sprite Tiling/Full Rect từ nền map cũ ngoài Lobby; Lobby dùng SpriteDrawMode.Simple.
