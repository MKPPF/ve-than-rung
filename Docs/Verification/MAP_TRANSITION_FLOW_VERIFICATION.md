# Cửa chuyển vùng — U5 (22/10/2026)

## Phạm vi và tiêu chí

Rà soát sáu cửa trong chuỗi Map01 → Map02 → Map03 → BossMap01 → CaveMap01
→ RiftMap01 → Lobby. Giữ thiết kế săn quái: cửa mở ngay, không yêu cầu dọn map.
Tiêu chí: tên/cấp đề nghị thống nhất với MapData, callback không hợp lệ không
lưu hay mở vùng, chuyển một lần, hủy fade không kẹt điều khiển, tiến trình đi
qua cả chuỗi được giữ, lời nhắc đọc được ở ba tỷ lệ màn hình.

## Thay đổi

- Builder gắn DestinationMap cho năm cửa đến vùng chiến đấu; cửa về Lobby dùng
  tên Trạm nghỉ Tán Rừng. Biển cổng hiển thị hai dòng tên đích/cấp đề nghị hoặc
  nghỉ ngơi và chuẩn bị. Không chỉnh scene YAML bằng tay.
- MapDoor dùng cùng metadata cho lời nhắc E / Nhấp đúp, tên đích, cấp đề nghị
  và thông báo tự lưu. Validator phát hiện metadata thiếu/sai id và cửa Lobby
  trỏ nhầm MapData chiến đấu. Builder báo lỗi rõ nếu thiếu MapData đích.
- Cửa chỉ nhận nhân vật đang hoạt động, sống, ở trong bán kính tương tác của
  collider đang bật; chặn khi cửa khóa/disable, metadata sai, scene không có,
  modal khác đang mở, game đang pause hoặc đã nhận một chuyến đi. Vị trí dùng
  Rigidbody2D để khớp vật lý khi transform đang nội suy.
- SceneTransition.TryLoadScene kiểm tra trước khi chụp tiến trình và trả kết
  quả chấp nhận. Bỏ UnlockRegion trước fade; MapManager.SetSafeRespawn hiện có
  mở vùng khi thực sự đến nơi. Hủy chuyến không mở vùng đích trước thời điểm đó.
- FadeTransition giữ quyền modal/pause trong 0,25 giây bằng thời gian unscaled;
  không đánh, di chuyển hay mở thêm bảng trong lúc chuyển. Disable/destroy dừng
  coroutine, xóa độ mờ và trả input/timeScale/loading. Nạp cảnh Single và vào
  Play mới reset cờ loading; đường không có fade cũng được kiểm tra.
- LobbyPortal chuyển quyền input sang fade sau nghi thức; nếu fade bị ngắt,
  vòng truyền tống hủy trạng thái departure và trả camera/điều khiển. Hiệu ứng
  và thời gian nghi thức mặc định không đổi.
- GameplayHUD thêm nền tối viền đồng cho lời nhắc tương tác, hiện theo nội dung;
  toàn bộ graphic của nền không nhận raycast. Giữ đối tượng Prompt tại đường
  dẫn cũ trong CombatHUDRoot và giữ controller sở hữu callback như trước.

Backup trước sửa: `Logs/MapTransitionBackup/`.

## Kiểm thử

Sáu MapTransitionFlowTests mới:

1. Null/disable/collider tắt/cửa khóa/xa/modal/metadata thiếu/scene sai/nhân vật
   đã gục: không save, load, mở vùng hoặc kẹt loading.
2. Fade chặn gameplay và Pause/hành trang; callback lặp không ghi lại. Ngắt
   fade không mở vùng, không load muộn, có thể thử lại và chỉ đến Map02 một lần.
3. Đi đủ sáu cửa khi các map có quái vẫn còn quái (Map03 hiện không có seed).
   Giữ vàng tăng mỗi chặng, cấp/XP, kỹ năng/slot, vũ khí, đồ trong kho và tinh
   hoa; mana được chụp đúng lúc chấp nhận; đích mới mở sau khi đến, đúng spawn.
4. Không có fade vẫn chuyển Map02 và dọn loading.
5. Nghi thức Lobby bàn giao sang fade; tắt fade trả trạng thái vòng/camera/input.
6. Tên/cấp của sáu biển đúng dữ liệu; lời nhắc hiện/ẩn có nền, không raycast,
   không tràn, trong canvas và chữ ít nhất 14 pixel ở 1920×1080, 1280×800,
   1024×768. Lưu 18 ảnh ChuyenVung_*_Verified.png.

Hai MapTransitionAuthoringTests mới kiểm tra wiring/biển của sáu scene và ba
trường hợp metadata sai bằng preview scene, không ghi asset. Regression tập
trung gồm hai TravelFlowTests và ZZZPortalTransitionTests hiện có.

Lượt đầu phát hiện vị trí guard theo transform chưa theo Rigidbody nội suy;
đã sửa sang vị trí vật lý. Fixture chụp ảnh được sửa để tìm Canvas cha và đợi
cập nhật tương tác; khi render qua camera phải giữ sortingOrder UI = 30000.
Không coi ảnh có world sprite che UI do fixture render sai là kết quả nghiệm thu.

## Kết quả cuối

- Builder hoàn tất hai lượt trên cùng source cuối: dấu SUCCESS trong
  `Logs/MapTransition_Builder1_Complete.txt` (15:57:26 UTC) và
  `Logs/MapTransition_Builder2_Complete.txt` (15:59:50 UTC). Lượt lỗi null ban
  đầu không tính vào hai lượt thành công. Các sửa runtime/fixture sau đó không
  thay đổi builder hoặc dữ liệu scene.
- Tập trung **9/9 đạt**, 23,86 giây; `Logs/MapTransition_Focused_Final.json`,
  job `5e25ef66fcc14291ac0f5f6bc9b42c75`.
- EditMode cuối **80/80 đạt**, 13,01 giây; `Logs/MapTransition_EditMode_Final.json`,
  job `ef83700010ae43e2952f212cf675dcc3`.
- 18 ảnh `Assets/Screenshots/ChuyenVung_{Map01|Map02|Map03|BossMap01|CaveMap01|RiftMap01}_*_Verified.png`.
  Đã xem ảnh cuối CaveMap01 4:3 (đích có tên dài) và RiftMap01 1080p (về sảnh):
  chữ đủ, nền prompt không bị world sprite che, thứ tự UI đúng.

| Gate cuối | Kết quả | Bằng chứng |
| --- | --- | --- |
| PlayMode toàn bộ | **157/157 đạt**, 0 failed/skipped, 331,55 giây | `Logs/MapTransition_PlayMode_Final.json`, job `3b49b94927a640b8989c6db7828ab090` |
| Validator | IsValid=true, 0 lỗi | `Logs/MapTransition_Validator_Final.json` |
| Console/Editor | 0 lỗi, 4 warning; MainMenu, không Play/compile, timeScale=1 | `Logs/MapTransition_EditorFinal.json` |
| Diff | Scoped git diff --check đạt | Source runtime/editor và tài liệu trong phạm vi đợt này |

Bốn warning native gồm ba cue Music_Exploration/Ambient_Forest/Ambient_Cave
chưa có clip và AudioMissingClipProbe chủ động của test. Console reader còn
trả log audio listener ở scene probe và cleanup của Test Runner. Không có
lỗi mới về scene load, save hoặc reference. Không sửa runtime/test sau lượt
full green; chỉ cập nhật báo cáo, mục lục và ghi chú roadmap.

## Giới hạn và việc còn mở

- Không thay schema/key save hoặc số balance. Cấu hình experimentalVitals vẫn
  hồi đầy HP khi ApplyToPlayer, mana còn cơ chế hồi tự nhiên. Bài này kiểm tra
  snapshot mana tại cửa; không tuyên bố đã cân bằng tài nguyên toàn game.
- Map03 trong Build Complete MVP hiện truyền primary=null, primaryCount=0 và
  secondary=null. EnemySpawnManager không có template để tạo population; đây
  là chênh lệch cần rà soát tiếp với TargetActiveEnemies của MapData. Không
  tự thay nội dung hay suy diễn rằng cửa phải khóa khi map trống.
- Save hỏng vẫn có đường xóa im lặng, clip nhạc/ambient còn thiếu. Nghiệm thu
  người chơi mới, tay cầm/webcam thật và Windows player/restart còn mở.
- Giữ U5 ở trạng thái đang làm; đợt này không xác nhận toàn bộ game hoàn tất.
