# Truyền tống trận Lobby — 06/09/2026

## Phạm vi

- Giữ nền WoodlandWaystationV3_WithSmith.png; bản vườn bàn ghế V5 không được dùng.
- Loại bỏ SpriteRenderer cổng ForestPortal khỏi BattlePortal của Lobby,
  không chỉ ẩn sprite. Giữ asset ForestPortal vì combat map vẫn dùng.
- Vòng đá vẽ trong nền làm truyền tống trận. Thêm nét rune quay, ánh mặt đất,
  hạt sáng bay lên khi nạp; không có khung cổng đứng.
- Đứng ở tâm vòng khoảng 1,15 giây tự chuyển Map01 qua SceneTransition/FadeTransition.
- Phím E hoặc nhấp đúp bên ngoài không bỏ qua điều kiện bước vào trận.
- Bước ra hủy charge; pause/modal dừng tiến độ; nhân vật chết không truyền tống.
- Dùng vị trí Rigidbody2D để xác định ra/vào, không dùng vị trí render trễ nội suy.
- Cập nhật label: TRUYỀN TỐNG TRẬN / BƯỚC VÀO VÒNG ĐÁ.
- Giữ các hiệu ứng cá/bướm/nước/lửa hiện có, không đổi gameplay các map chiến đấu.

## Xác minh

- Builder hoàn tất hai lần, marker MVP built ở dòng 31360 và 33470 của
  Logs/Editor.log tại thời điểm thực hiện.
- Test riêng charge/cancel/pause/travel đạt, job a3c77dba144f4515a62824253d2c54f1.
- Toàn bộ PlayMode: 55/55 Passed, báo cáo gốc Logs/TeleportCircle_PlayMode.xml.
- EditMode cuối (06/09): 56/56 Passed, Logs/TeleportCircle_EditMode.xml.
  Bao gồm ProjectValidatorPassesForGeneratedBaseline và kiểm tra Lobby không
  có SpriteRenderer cổng đứng, có TeleportCircleVisual và đúng nền V3.
- Test tự chuyển thật sang Map01 và đếm đúng một scene-loaded event.
- Collider trận không chặn đường; bài scenery vẫn kiểm tra tới cả ba trạm.
- Backup: Backups/TeleportCircleV5/ và Backups/LobbyLowerGardenV5/.
- Bản art V5 đã sinh được giữ lại như phương án không dùng, không áp dụng vào game.

## Giới hạn

- Không thêm âm thanh truyền tống mới.
- Ảnh charge của scenery fixture đặt tiến độ 65% để chụp trước lúc chuyển map;
  hành vi chuyển thật được kiểm thử riêng.
