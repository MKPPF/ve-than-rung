# Sảnh sống động V4 — 05/09/2026

## Nội dung

- Lớp nước refraction nhẹ lấy chính màu tranh nền, giới hạn theo dải lòng suối.
- Thác chảy, 14 bọt trôi, 12 giọt nước chân thác.
- Hai nguồn lửa tại lò rèn/lửa trại, 28 tàn lửa và khói trại, ánh sáng rung nhẹ.
- Bốn cá nhỏ bơi trong ellipse ao, có đuôi quẫy; cá trang trí không thể bắt.
- Sáu bướm bay/đập cánh quanh ba cụm hoa, bảy lá rơi nhẹ ở mép vườn.
- Giữ khói ống khói, đom đóm, ánh cổng và đèn từ V3.
- Không chỉnh tranh gốc, collider, vị trí tương tác, combat, kinh tế hoặc save.

## Triển khai

- TopDownGameBuilder sở hữu mọi anchor và reference.
- LobbyWaterFlow + LobbyWaterFlow.shader: hai mesh ribbon và hai material runtime,
  fade mép, sampling texture gốc; giảm cường độ sau khi xem render lần đầu.
- LobbyFireEffects: sprite ngọn lửa procedural antialias và các sprite tái sử dụng.
- LobbyWildlife: hình học cánh/đuôi/lá đơn giản cùng sprite hình elip cho cá.
- Không sinh/hủy object mỗi frame; số renderer cố định. Không collider, không
  âm thanh mới. Hiệu ứng dùng Time.deltaTime và đóng băng khi pause.
- Mesh/material/texture riêng được hủy khi component bị hủy, không hủy material
  SpriteRenderer dùng chung. Disable/enable không tạo trùng.

## Xác minh

- Hai lượt builder bản có cá/bướm đã hoàn tất (marker MVP built tại dòng 22408
  và 26318 trong Logs/Editor.log ở thời điểm kiểm tra).
- EditMode cuối: 56/56 đạt, job 0189b5a26e7142feaa27d1a34ce304d2.
- Validator cuối: IsValid=true, errors=[].
- Báo cáo: Logs/LobbyLivingV4_EditMode.xml và Logs/LobbyLivingV4_PlayMode.xml.
- PlayMode cuối: 54/54 đạt, job 9a5e9c4a80cb483086193086d20e4a67.
- Test thật tải Lobby, kiểm tra shader sẵn sàng, nước đổi pixel giữa hai thời điểm,
  pause đứng yên, cá không vượt vùng bơi, số object ổn định và cleanup mesh/material.
- Kiểm tra đường đi, cửa hàng, kho và cổng vẫn giữ nguyên trong smoke test.
- Video 5 giây ở 30 fps: Captures/LobbyLivingV4_Complete.mp4.
  Trên trái: suối/cá; trên phải: bướm; dưới trái: lò rèn; dưới phải: lửa trại.
- Ảnh cận và 600 frame render trong Captures/LobbyLivingV4/.
- Backup trước thay đổi: Backups/LobbyLivingV4/.

## Giới hạn

- Cá/bướm/lá là hình procedural nhỏ, không phải sprite vẽ tay nhiều frame.
- Nước là overlay trên tranh, không phải mô phỏng chất lỏng hay thác 3D.
- Không có tiếng suối/chim/lửa mới; không đổi hệ âm thanh ngoài yêu cầu.
- Test ghi hình timestep cố định để kiểm tra chuyển động, không phải benchmark
  FPS/GC trên máy người chơi. Cần playtest trực tiếp nếu muốn điều chỉnh mật độ.
- Một số lượt builder bị ngắt khi Editor vào Play Mode; chỉ tính lượt có log
  "MVP built" là hoàn tất.
