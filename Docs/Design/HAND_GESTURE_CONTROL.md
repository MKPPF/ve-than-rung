# AI1 — Điều khiển di chuyển và kỹ năng bằng cử chỉ tay

## Phạm vi và kiến trúc

**Nguồn code Python chính:** `Tools/HandGestures/HandGestureControl.ipynb`.
Notebook chia chín mục với các ô cấu hình và code đầy đủ để xem/sửa/chạy trực tiếp.
Chọn kernel `.venv/Scripts/python.exe` trong VS Code/Jupyter. `run_pipeline.py`
chỉ là adapter đọc các ô định nghĩa cho terminal và kiểm thử; không nhân đôi
code ML. Các file C# giữ dạng `.cs` vì Unity cần biên dịch chúng.

Đồ án dùng webcam thường. MediaPipe Hand Landmarker có sẵn lấy 21 điểm tay;
MLP do dự án tự huấn luyện nhận diện `none`, `fist`, `peace`, `like`, `stop`.
MLP nhận 42 đặc trưng XY, qua hai lớp 128/64 ReLU, xuất xác suất năm lớp.
Đầu ra của model được dùng thực sự trong quyết định dùng chiêu và bật di chuyển.

```text
HaGRID / webcam → đặc trưng XY → train MLP → gesture_mlp.npz
Webcam → MediaPipe → cùng đặc trưng XY → MLP → bộ lọc theo thời gian
       → UDP 127.0.0.1:5057 → Unity → PlayerMovement / SkillManager
```

Phần phân loại cử chỉ được train từ trọng số ngẫu nhiên. Phần phát hiện điểm
bàn tay sử dụng model đã train sẵn. Di chuyển kết hợp model nhận diện tư thế
xòe tay với vị trí cổ tay làm cần điều khiển ảo; hướng không phải một model
được train riêng. V2 thêm ngón trỏ chỉ bốn hướng dựa trên hình học landmark;
đây cũng không phải bốn nhãn mới đã train. Cần trình bày đúng sự phân chia này với giảng viên. Nếu môn
yêu cầu tự train toàn bộ từ pixel ảnh, pipeline này cần được điều chỉnh.

## Dữ liệu có sẵn và nguồn

- [HaGRID chính chủ](https://github.com/hukenovs/hagrid): annotation có nhãn,
  `user_id` và `hand_landmarks`; có thể dùng tọa độ thay vì tải ảnh dung lượng lớn.
- [Script tải của tác giả](https://github.com/hukenovs/hagrid/blob/master/download.py).
- [Giấy phép riêng của HaGRID](https://github.com/hukenovs/hagrid/blob/master/license/en_us.pdf):
  biến thể CC BY-SA 4.0; lưu bản gốc ở `Tools/HandGestures/licenses/HAGRID_LICENSE.pdf`.
  Giữ attribution và kiểm tra điều khoản khi phân phối dữ liệu/model dẫn xuất.
- [Google Hand Landmarker](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker).

Hàm `prepare_data` trong notebook dùng HTTP Range đọc ZIP annotation, chỉ đọc các member thuộc năm
nhãn cần thiết, reservoir sampling seed 42 trong phần đầu mỗi member (mặc định
quét tối đa gấp hai số mẫu cần). Đặt `scan_multiplier=0` trong notebook để
quét toàn bộ member và lấy mẫu reservoir trên toàn file. Cách mặc định có thể
bị thiên lệch theo thứ tự annotation, phù hợp bộ khởi đầu để bổ sung webcam.
Không tải toàn bộ ZIP 719 MB hay
bộ ảnh hàng trăm GB. Mỗi lớp tối đa 2.000 mẫu train, 500 validation và 500 test;
số thực tế có thể ít hơn nếu thiếu landmark hợp lệ. Lưu URL, ETag, SHA256,
số mẫu bị bỏ và lượng tải trong `data/hagrid.manifest.json`. File `.partial.csv`
giữ các lớp đã hoàn tất khi tải gián đoạn; chạy lại sẽ tiếp tục từ các lớp đó.

Nhãn dựa trên từng bàn tay trong annotation, không lấy nhãn của cả ảnh để gán
cho bàn tay thứ hai. Giữ split chính thức; trainer từ chối nếu một `user_id`
xuất hiện ở nhiều split hoặc sample trùng. Landmark tự gán có thể sai; kết quả
trên dữ liệu này không thay thế kiểm thử webcam thực tế.

## Cài đặt và train

Windows, Python 3.11–3.13, webcam; không bắt buộc GPU.
Từ thư mục gốc dự án:

```powershell
& .\Tools\HandGestures\Run.ps1 -Action setup
& .\Tools\HandGestures\Run.ps1 -Action prepare
& .\Tools\HandGestures\Run.ps1 -Action train
```

Nếu PowerShell chặn chạy script, gọi Python trực tiếp thay vì thay execution policy:

```powershell
python -m venv .\Tools\HandGestures\.venv
& .\Tools\HandGestures\.venv\Scripts\python.exe -m pip install -r .\Tools\HandGestures\requirements.txt
& .\Tools\HandGestures\.venv\Scripts\python.exe .\Tools\HandGestures\run_pipeline.py prepare
& .\Tools\HandGestures\.venv\Scripts\python.exe .\Tools\HandGestures\run_pipeline.py train
```

Trainer chuẩn hóa bằng mean/scale của **train**, cân bằng lớp bằng lấy mẫu lại
trong train, chọn epoch tốt nhất bằng **validation loss**, chỉ dùng test để
báo cáo cuối. Không dùng test để chọn model hoặc ngưỡng. Xuất trọng số số học
NPZ không pickle; kiểm tra xác suất suy luận tương đương classifier gốc trước
khi công bố báo cáo. Đầu ra: `metrics.json`, `history.csv`, `training.png`.

## Thu thêm dữ liệu webcam

```powershell
& .\Tools\HandGestures\Run.ps1 -Action collect -Subject p01 -Hand Right
& .\Tools\HandGestures\Run.ps1 -Action collect -Subject p01 -Hand Left
```

1. Bắt đầu với 5–10 người nếu có thể, mỗi người có ID riêng; giữ ID qua các buổi.
2. Trong cửa sổ camera, phím 1–5 chọn `none/fist/peace/like/stop`.
3. Space ghi/dừng, đổi nhãn tự dừng, Esc lưu và thoát. Ghi khoảng 30–60 giây
   mỗi nhãn mỗi người, chia nhiều lần; mặc định 6 mẫu/giây.
4. Đổi khoảng cách, góc tay, nền và ánh sáng. Thu tư thế xòe ở cả các vùng
   trái/phải/trên/dưới vì tư thế này quyết định di chuyển.
5. `none` là tay thả lỏng/tư thế nghỉ vẫn thấy trong camera. Thu thêm các tư thế
   ngoài bốn dấu, tránh vô tình chứa chính dấu cần dùng. Không coi mất tay là
   một mẫu landmark. Dùng cùng quy ước giữa các người.
6. Không quay người đang làm cử chỉ khác rồi gán nhãn bằng phím chưa đổi.

Chỉ lưu đặc trưng và ID, không ghi ảnh/video cá nhân. Camera chỉ mở khi chạy
ô thu dữ liệu hoặc ô điều khiển trong notebook một cách chủ động.

Thu người đánh giá riêng bằng Python trực tiếp:

```powershell
& .\Tools\HandGestures\.venv\Scripts\python.exe .\Tools\HandGestures\run_pipeline.py collect --subject p09 --split val --hand Right
& .\Tools\HandGestures\.venv\Scripts\python.exe .\Tools\HandGestures\run_pipeline.py collect --subject p10 --split test --hand Right
```

Không đổi split của cùng người. Sau thu, lệnh sau tự trộn HaGRID + webcam:

```powershell
& .\Tools\HandGestures\Run.ps1 -Action train
```

## Điều khiển game

```powershell
& .\Tools\HandGestures\Run.ps1 -Action play
```

- Lần đầu tự tải model landmark chính thức từ Google; đây không phải model MLP.
- Unity: Play → vào game → bật nút **TAY [F8]** dưới Tự nhặt → click Game View.
  F8 vẫn bật/tắt như trước. HUD có trạng thái tắt/chờ camera/mất focus/menu/kết nối.
- Camera V2 mặc định `MOVE_HAND = "Auto"`: khóa bàn tay xòe/chỉ ngón trỏ đầu
  tiên làm tay di chuyển; tay đối diện dùng chiêu. Nếu thấy hai tay cùng lúc,
  ưu tiên nhãn Left. H trong camera đổi vai. Mất tay không chuyển vai tự động.
- Di chuyển: chỉ **một ngón trỏ** lên/xuống/trái/phải với ba ngón khác gập;
  hoặc xòe tay yên 0,6 giây để tự đặt tâm rồi dịch tay khỏi vùng chết.
  C trong cửa sổ camera đặt tâm lại khi đang xòe tay di chuyển. Nắm/mất tay
  dừng; trở về tâm dừng cần điều khiển. Ngón trỏ không cần hiệu chỉnh tâm.
- Tay đối diện: nắm → ô Q, chữ V → ô E, ngón cái → ô R, xòe → ô F.
- V2.1: dấu đầu tiên hoặc đổi sang dấu khác, giữ ổn định 0,35 giây sẽ phát
  một lệnh. Giữ nguyên dấu không lặp. Muốn lặp cùng chiêu: chỉ một ngón trỏ
  bằng tay cast 0,2 giây, hoặc `none`/Space, rồi tạo lại dấu. Ngón trỏ là
  release hình học, không cần model nhận đúng `none`. Mất dấu/không chắc chắn
  không cho phép lặp cùng dấu vừa phát; dấu khác vẫn cần ổn định đủ thời gian.
- Chỉ bắt đầu đếm thời gian giữ dấu khi đã có ACK Unity mới và không bị block;
  tránh gửi dấu đầu trước kết nối rồi mắc kẹt ở trạng thái đã dùng chiêu.
  Camera V2.1 có `last sent`/`sent=N`, notebook in từng Q/E/R/F đã gửi. Đây là
  lệnh gửi, không đảm bảo đã tung chiêu khi còn cooldown/thiếu mana.
  Đổi focus xóa input cũ và bỏ cast ở lô đầu; các lệnh mới sau đó được nhận
  nền khi người dùng đã bật TAY. Không xếp hàng chiêu.
- Chọn tay bằng `MOVE_HAND = "Auto"/"Left"/"Right"`, cách di chuyển bằng
  `MOVEMENT_STYLE = "both"/"point"/"palm"` trong notebook. Run.ps1 hỗ trợ
  `-MoveHand Auto -MovementStyle both`; đổi camera bằng `-Camera 1`.
- Unity trả ACK về cổng UDP tạm của camera mỗi 0,15 giây khi nhận gói hợp lệ.
  Camera chỉ hiện `UNITY READY` khi ACK đúng session, không bị block và còn
  mới trong 1,5 giây. Gói V1 cũ vẫn nhận được nhưng camera cũ không có trạng thái ACK.
  Chưa có ACK: kiểm tra Play/TAY F8/Game View; focus/menu/player thiếu có thông báo riêng.
- Sau cập nhật, dừng camera cũ, reload notebook và chạy **8.1 — Nạp model**,
  rồi **8.2 — Chạy model trên camera**. Ô 8.1 tự đọc lại hàm từ notebook,
  nạp NPZ, in kiến trúc/nhãn/SHA256; chạy được sau Restart Kernel. Ô 8.2 mở
  camera ngay khi chạy và dùng chính trọng số đó. Không cần train lại NPZ
  vì hợp đồng 42 đặc trưng không đổi.
- Hướng chiêu là hướng di chuyển gần nhất, ban đầu hướng phải. Slot trống,
  thiếu mana, đang cast, đang cooldown, chết hoặc modal thì lệnh bị bỏ.
- WASD ưu tiên hơn tay khi có cả hai. Tự dùng skill của auto combat được ngắt
  khi tín hiệu tay còn sống; đánh thường tự động vẫn theo hệ thống cũ.
- Khi TAY được bật, receiver bật runInBackground và nhận lệnh dù camera đang
  được chọn; HUD/camera báo trạng thái nền. Tắt TAY trả lại runInBackground ban
  đầu và đóng socket. Cấu hình nội bộ allowBackgroundControl=false giữ hành vi
  yêu cầu focus nếu cần; ACK khi đó báo focus. Pause/modal luôn chặn input.
- Python phải tiếp tục chạy khi bạn chuyển về cửa sổ game. Esc ở cửa sổ camera
  dừng controller; Esc trong Unity vẫn mở Pause như cũ.

Không sửa scene/prefab/builder: receiver tự bootstrap lúc runtime, mặc định tắt,
chỉ mở socket loopback khi F8 hoặc tham số `--hand-gestures`. State có timeout
0,3 giây theo realtime. Gói sai version/NaN/quá cũ bị bỏ; sequence chống xử lý
lặp, castId chống lặp chiêu. Đổi scene/pause/mất focus không xếp hàng chiêu cho
lúc quay lại. Input đi qua physics/animation và pipeline mana/cooldown hiện có.
Đây là tính năng desktop; chưa hỗ trợ WebGL hay thiết bị di động.

## Kiểm tra và nghiệm thu

```powershell
& .\Tools\HandGestures\Run.ps1 -Action test
```

Chạy Unity EditMode và PlayMode suite, `Top Down Game > Validate Project`.
Builder không đổi nên không cần dựng lại content để gắn receiver.

Nghiệm thu thủ công: di chuyển bốn hướng/dừng, va chạm tường, bốn slot, mana/
cooldown, giữ dấu không spam, mất tay, tắt Python, pause, inventory, đổi scene,
đổi focus và WASD. Đo độ trễ, số lệnh nhầm/phút và tỷ lệ thực hiện thành công
trên người chưa tham gia train. Chụp ảnh theo quy ước vào `Assets/Screenshots/`.
Không đánh dấu hoàn thành gameplay chỉ từ accuracy của tập ảnh tĩnh.
