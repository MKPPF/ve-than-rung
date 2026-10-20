# AI1 — Kiểm tra pipeline cử chỉ tay (20/10/2026)

## Thay đổi

- Nguồn Python: `Tools/HandGestures/HandGestureControl.ipynb`, 22 ô, chín mục.
  Code đầy đủ cho đặc trưng, tải HaGRID, thu webcam, train MLP, đánh giá, live input.
- Adapter terminal đọc cùng ô định nghĩa; không có bản code ML Python trùng.
- Bridge Unity mặc định tắt, bật F8, localhost UDP 5057. Input đi qua
  PlayerMovement/SkillManager hiện có; không thay builder, scene, prefab hay save.
- Tay trái: MLP phải nhận ra tư thế xòe rồi vị trí cổ tay điều khiển bốn hướng.
  Tay phải: nắm/V/ngón cái/xòe gọi Q/E/R/F; nghỉ giữa hai lần dùng chiêu.

## Đã xác minh

- Python unit tests: **7/7 đạt**. Có kiểm tra đặc trưng dịch/tỉ lệ/phản chiếu,
  khóa lặp chiêu, mất dấu không tự rearm, di chuyển/vùng chết, duplicate và
  cùng người xuất hiện ở nhiều split.
- `nbformat.validate`: cấu trúc notebook hợp lệ.
- MediaPipe: nạp model chính thức bằng bytes để hỗ trợ đường dẫn tiếng Việt,
  chạy suy luận VIDEO với frame đen thành công, không mở webcam trong bước này.
  MediaPipe phát thông báo XNNPACK/feedback/projection của thư viện, không exception.
- `pip check`: không có dependency hỏng. Phiên bản thực tế lưu ở
  `Tools/HandGestures/requirements-lock.txt`.
- Unity **77/77 EditMode** và **78/78 PlayMode** đạt. Các test mới xác minh
  protocol/timestamp/NaN, timeout khi pause, input đi qua PlayerMovement,
  ưu tiên WASD, modal khóa input, gọi chiêu trừ mana một lần và cooldown.
- `Top Down Game > Validate Project Automated`: đạt.
- Console sau test: không có error; có cảnh báo thiếu audio listener trong
  scene test tạm. Warning API FindFirstObjectByType mới đã sửa sang FindAnyObjectByType.
- Kết quả Unity JSON: `Tools/HandGestures/reports/unity-editmode.json` và
  `unity-playmode.json`. Thử batch lúc đầu không chạy vì Editor đang mở;
  các kết quả đạt bên trên là chạy Test Runner qua Editor đang mở.
- `git diff --check` cho code/tài liệu đã sửa: đạt. ProjectSettings có thay đổi
  icon Android và whitespace từ trước phiên này; không đưa vào phần AI1.

## Dữ liệu/model

Nguồn thật: [HaGRID chính chủ](https://github.com/hukenovs/hagrid), tọa độ 2D
được tác giả tự gán bằng MediaPipe, giữ split theo `user_id`. Import ban đầu
có thể quét giới hạn prefix để giảm thời gian tải; không khẳng định lấy mẫu
đều trên toàn bộ HaGRID. Có thể đặt `scan_multiplier=0` để quét đầy đủ.
Mẫu webcam cá nhân chưa được thu tự động.

Notebook đã chạy các ô định nghĩa, kiểm tra dữ liệu, train, đánh giá và ô live
đang tắt; không có output lỗi. Ô pip được bỏ qua vì môi trường đã cài và lock.

| Tập | Mẫu | Người riêng biệt | Accuracy | Macro F1 |
|---|---:|---:|---:|---:|
| Train | 9.858 | 7.031 | 99,51% | 99,50% |
| Validation | 2.231 | 397 | 99,42% | 99,25% |
| Test | 2.500 | 699 | 99,12% | 99,12% |

Tổng **14.589 mẫu**, năm nhãn. Mỗi lớp train tối đa 2.000, validation/test
500; `none` train 1.858 và validation 231 do số landmark hợp lệ ít hơn quota.
Trainer kiểm tra không có người/samples trùng giữa các split; seed train 42.
MLP chạy 43 epoch, xuất checkpoint epoch 18 theo validation loss. Toàn bộ xác
suất suy luận bằng trọng số NPZ được đối chiếu classifier gốc trên cả ba tập.

- Model: `Tools/HandGestures/models/gesture_mlp.npz` (55.090 byte).
- CSV: `Tools/HandGestures/data/hagrid.csv`.
- SHA256 CSV: `a9fcd5bb89c6701fed5ef8e8fbd02a82847ee47446cb7724e104ab71d5852f71`.
- Chỉ số/biểu đồ: `Tools/HandGestures/reports/latest/metrics.json`,
  `history.csv`, `training.png`; đã xem trực quan biểu đồ.
- Notebook lưu sẵn log train, bảng kết quả và biểu đồ ngay trong output các ô.
- Lớp train/fist của snapshot này lấy reservoir trên toàn member ở lần tải
  đầu, được tiếp tục từ file partial; các lớp sau dùng bounded prefix như
  cấu hình hiện tại. Manifest ghi nhận khác biệt này. Khi tải lại từ đầu,
  tập con có thể khác; để tái lập kết quả train, dùng CSV có SHA256 bên trên.

**Đây là kết quả phân loại landmark tĩnh của tập con HaGRID, không phải tỷ lệ
điều khiển thành công trong game hay accuracy trên webcam cá nhân.**

## Còn cần nghiệm thu thực tế

- Chưa thu dữ liệu từ webcam của người dùng, chưa đo tỷ lệ nhận đúng/nhầm khi
  hai tay cùng hoạt động, độ trễ điều khiển hay số chiêu kích hoạt nhầm/phút.
- Chưa xác nhận điều khiển camera ngoài đời trong gameplay với người dùng;
  test tự động không thay thế bước này. Chưa có screenshot gameplay xác nhận AI1.
- Cần hiệu chỉnh C, thử đủ bốn hướng/va chạm/bốn slot, giữ dấu, mất tay,
  tắt Python, pause/inventory, đổi focus và scene trên người chưa train.
- MLP tự train từ đặc trưng; MediaPipe lấy landmark là pretrained. Nếu giảng
  viên yêu cầu train từ pixel ảnh, cần đổi phương án theo rubric.
- Giữ roadmap AI1 ở `[-]` cho đến khi hoàn tất nghiệm thu webcam/gameplay.
