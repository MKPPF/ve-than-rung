# Đền Kỹ Năng — population và luồng Map03

Ngày 23/10/2026, Unity 6000.5.6f1. Tiếp tục U5: khôi phục nhịp săn quái trước
boss trên nội dung do builder sinh; chưa xác nhận toàn bộ game hoàn tất.

## Bằng chứng ban đầu

- `BuildCompleteMvpAutomated` truyền primary=null/count=0/secondary=null cho
  Map03, dù MapData đặt target 18. EnemySpawnManager lấy template từ quái trong
  scene nên không thể tạo pool khi không có seed.
- Cùng builder, đường dựng lại combat đã dùng Archer ×8 và Mage ×4 cho Map03.
  Đợt này thống nhất đường dựng đầy đủ với roster đó, không thêm loại quái mới.
- Test authoring mới thất bại trên scene cũ: expected 12, actual 0. Bằng chứng
  `Logs/Map03Population_Reproduction.json`, job `88fe474d31c440adbec502e73b75a5d8`.
- Bản mã/scene trước sửa: `Logs/Map03PopulationBackup/`.

## Nội dung và runtime

- Builder tạo 12 seed: 8 Cung Thủ và 4 Pháp Sư; giữ cấp vùng 18–35, mục tiêu
  18 con, rương chọn kỹ năng, cửa đến BossMap01 và spawn người chơi như cũ.
- Dời Cung Thủ trùng tâm rương từ (-5,8) xuống (-5,6.5). Playtest phát hiện
  Pháp Sư tại (5,-7) sát mép tường; dời về tâm hành lang (5,-8).
- Map03 tắt mở rộng anchor hướng tâm như Map02; dùng 12 vị trí seed và 9 vị trí
  hồi sinh đặt sẵn trong hành lang. Không phụ thuộc việc quái rời vị trí ban đầu
  mới có chỗ trống để đạt target. Không thêm asset hoặc chỉnh scene bằng tay.
- EnemySpawnManager nhận `additionalSpawnPoints` qua builder và thêm vào danh
  sách anchor bằng cơ chế loại trùng hiện có. Mặc định rỗng cho map khác. Vẫn dùng
  chung pool, guard ngoài camera, khoảng cách người chơi ≥7, overlap và timer hiện có.
  Pool Map03 prewarm 26 đối tượng (target 18 + reserve 8), không tạo mới mỗi lần chết.
- Validator cho vùng săn không phải boss yêu cầu spawner hoạt động và ít nhất một
  seed hoạt động có EnemyData/EnemyHealth hợp lệ. Báo đúng scene/seed khi thiếu;
  không ép phải đặt sẵn đủ target vì pool có trách nhiệm bổ sung.
- Việt hóa tên hiển thị quái gốc trong builder, giữ nguyên id; các nhóm Rift vốn
  có tên riêng vẫn giữ. Profile hiển thị biến thể Nhanh Nhẹn/Bọc Giáp/Hút Máu/
  Bộc Phá/Tinh Anh; nhãn máu dùng Cấp thay Lv., mở rộng chiều ngang và không wrap.
  Di vật có tên Di Vật Rừng/mô tả tiếng Việt. Không đổi chỉ số hay phần thưởng.
- Bỏ ngoại lệ Map03 trong bài kiểm tra đi toàn chuỗi: cửa hunting phải mở ngay cả
  khi Map03 vẫn còn quái, như những vùng khác. Không thêm điều kiện dọn sạch map.
- Lượt kiểm thử nhãn phát hiện pool clone cả thanh máu runtime của seed rồi
  EnemyHealthBar.Awake tạo thêm một bản. BuildBar nay nhận lại canvas/slider/label
  đã clone; OnEnable cập nhật theo profile của chính quái mới. Kiểm tra cả 26
  đối tượng chỉ có một thanh máu và nhãn đúng sau khi reserve xuất hiện.

## Kiểm tra mới

Ba EditMode test:

1. Scene Map03 có đúng roster seed, spawner không mở rộng hướng tâm, target 18,
   rương kỹ năng và cửa boss đúng.
2. Cố ý không có/tắt seed, thiếu data/tắt health, thiếu/tắt spawner trong preview
   scene: validator đều báo hunting population; không lưu thay đổi vào scene thật.

3. Tên quái gốc và mọi biến thể dùng tiếng Việt, id giữ nguyên.

Bốn PlayMode test dùng scene thật và key `TopDownGame.Tests.*`:

1. Kiểm tra 21 anchor đủ khoảng trống vật lý và tìm được route từ spawn. Population
   đạt 18, pool 26, profile cấp 18–35, đếm quái manager khớp và cửa vẫn mở.
2. Hạ quái, drop đúng một lần, vàng/XP chỉ nhận khi Collect và không nhận lặp;
   reserve bổ sung vào cùng pool, sinh ngoài camera/đủ xa người chơi, giữ anchor
   và không đụng terrain. Capture giữ vàng hiện tại.
3. Đi bộ bằng click route qua hành lang từ spawn tới rương; chọn một kỹ năng không
   nhận lặp; đi tiếp tới cửa khi quái vẫn sống. Chuyển BossMap01 giữ vàng/cấp/XP/
   skill và mở vùng mới sau khi đến, giải phóng modal/fade.
4. Chụp cảnh săn quái, lựa chọn kỹ năng và cửa boss ở 1920×1080, 1280×800, 1024×768;
   kiểm tra chữ không tràn/nút trong canvas. Giữ gate HUD hiện có ≥12 pixel;
   bảng thưởng ≥14 pixel. Không thay typography HUD trong đợt content này.

Lượt tập trung đầu đã bắt anchor sát tường. Probe tiếp theo duyệt đủ các anchor
trong một lượt và xác nhận chỉ (5,-7) không đủ clearance; kết quả tại
`Logs/Map03Population_AnchorProbe.json`. Fixture render ban đầu dùng nhầm gate
14 pixel của hộp thoại cho toàn HUD; đã đối chiếu và dùng gate 12 của
CombatHudFlowTests cho HUD, giữ 14 cho lựa chọn kỹ năng.
Gate cỡ chữ bỏ qua chuỗi rỗng/whitespace; vẫn kiểm tra overflow và mọi chữ có nội dung.

## Kết quả cuối

| Hạng mục | Kết quả | Bằng chứng trong Logs/ |
|---|---|---|
| Builder cùng nguồn content cuối | Hai lượt thành công, UTC 22/10 17:27:06 và 17:31:31 (23/10 giờ máy) | `Map03Population_Release_Builder1_Complete.txt`, `Map03Population_Release_Builder2_Complete.txt` |
| Tái hiện nhãn nhân đôi | 1/6 thất bại trước sửa; Single tìm thấy nhiều EnemyLevelLabel | `Map03Population_DuplicateLabel_Reproduction.json` |
| Tập trung sau sửa | 6/6 đạt, 36,69 giây | `Map03Population_Focused_Release.json` |
| EditMode toàn bộ | 89/89 đạt, 13,81 giây | `Map03Population_EditMode_Release.json` |
| PlayMode toàn bộ | 169/169 đạt, 371,64 giây | `Map03Population_PlayMode_Release.json` |
| Validator sau hai build và cuối kiểm thử | Hợp lệ, 0 lỗi | `Map03Population_Validator_Release.json`, `Map03Population_FinalEditor_Release.json` |
| Native Console / Editor cuối | 0 lỗi, 4 warning; MainMenu, không Play/compile, timeScale=1 | `Map03Population_FinalEditor_Release.json`, `Map03Population_Console_Release.json` |

Sửa tái sử dụng thanh máu sau hai lượt builder chỉ thay runtime và test; nguồn
builder/content giữ nguyên. Các job cuối: tập trung `2be1cf391dfc4679bd344d083b3177a7`,
EditMode `e1a1b6d5f5004173944e26ea7535c29b`, PlayMode `f4e6168e1f5b4ddeb5b441e69cff0164`.

Đã xem chín ảnh của cùng nguồn runtime ở ba tỷ lệ: nhãn quái không còn chồng
hai bản, bảng chọn kỹ năng giữ đủ nội dung/nút, prompt cửa ghi đúng vùng/cấp và
lưu tự động. PlayMode toàn bộ tạo lại đủ chín ảnh tại `Assets/Screenshots/`, mẫu
`Map03SanQuai_{GiaoTranh|RuongKyNang|CuaBoss}_{1920x1080|1280x800|1024x768}_Verified.png`.
Các giới hạn camera của ảnh cửa được ghi bên dưới; chưa nghiệm thu phần đó.

Bốn warning native là ba cue thiếu clip Music_Exploration/Ambient_Forest/
Ambient_Cave và AudioMissingClipProbe cố ý trong test. Console reader còn trả
thông báo không có AudioListener trong scene dọn test và IPostBuildCleanup;
không dùng số dòng reader thay cho số đếm native.

## Giới hạn

- Ảnh là render từ scene thật với AI đóng băng và camera đặt riêng để kiểm tra
  HUD/modal/prompt; không phải toàn bộ một trận chiến tương tác. Ở ảnh cửa, cổng
  và một phần avatar nằm dưới launcher bên phải. Chưa dùng các ảnh này để nghiệm
  thu camera theo người chơi, giới hạn rìa map hoặc toàn bộ cảnh vật Map03.
- Đợt này khôi phục population/luồng săn–thưởng–đi tiếp, không cân bằng sát thương,
  tốc độ tăng cấp hoặc cấu hình thử nghiệm 50.000 HP / 25.000 mana.
- Không thay thuật toán spawn của các map khác. Các giới hạn target/cap/patrol ở
  những vùng khác chưa được đợt này đánh giá lại như một lượt cân bằng tổng thể.
- Nhạc/ambient thiếu clip, Windows restart thật và nghiệm thu accessibility bằng
  người chơi mới/tay cầm/webcam thật vẫn còn mở. U5 và mục tiêu chung giữ đang làm.
