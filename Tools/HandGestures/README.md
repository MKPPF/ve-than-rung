# Nhận diện cử chỉ tay cho Unity

Mã thu dữ liệu, huấn luyện MLP từ khởi tạo ngẫu nhiên, đánh giá và chạy webcam.
Tài liệu đầy đủ: [Docs/Design/HAND_GESTURE_CONTROL.md](../../Docs/Design/HAND_GESTURE_CONTROL.md).

**Mở [HandGestureControl.ipynb](HandGestureControl.ipynb) để kiểm soát toàn bộ
code trong từng ô.** Chọn kernel `.venv/Scripts/python.exe`, chạy theo các mục
1–9. Đổi các cấu hình ngay trong notebook. Ô thu dữ liệu mặc định tắt;
ô 8.2 mở camera ngay khi chạy. Để test với model đã có, chỉ chạy 8.1 → 8.2.
Notebook là nguồn code Python duy nhất của pipeline. `run_pipeline.py` chỉ
đọc các ô định nghĩa để cung cấp lệnh terminal/test, không chứa bản code ML thứ hai.

Chạy trong PowerShell tại thư mục dự án:

```powershell
& .\Tools\HandGestures\Run.ps1 -Action setup
& .\Tools\HandGestures\Run.ps1 -Action prepare
& .\Tools\HandGestures\Run.ps1 -Action train
& .\Tools\HandGestures\Run.ps1 -Action play
```

Nếu đã có `data/hagrid.csv`, bỏ bước `prepare`. Nếu đã có model thì có thể chạy
`play` ngay. Đọc báo cáo kiểm tra để biết model đã được kiểm thử đến đâu.

Thu thêm dữ liệu của bạn, rồi chạy lại `train`:

```powershell
& .\Tools\HandGestures\Run.ps1 -Action collect -Subject p01 -Hand Right
```

Trong cửa sổ thu: **1 nghỉ, 2 nắm tay, 3 chữ V, 4 ngón cái, 5 xòe tay**;
**Space** bắt đầu/dừng ghi, **Esc** thoát. Đổi nhãn sẽ tự dừng ghi để bạn đổi
tư thế. Mỗi người dùng ID riêng; giữ ID đó qua các buổi. Thu cả hai tay bằng
`-Hand Left` và `-Hand Right`. Không có tay sẽ không được ghi thành mẫu nghỉ.

## Chơi bằng hai tay — V2

1. Unity: Play, vào game, bật nút **TAY [F8]** dưới **TỰ NHẶT**, click Game View.
2. Notebook: dừng camera cũ bằng Esc/Interrupt, reload file từ ổ đĩa rồi chạy
   **8.1 — Nạp model** → **8.2 — Chạy model trên camera**. Ô 8.1 tự đọc lại
   các hàm trong notebook, in file NPZ, kiến trúc, nhãn và SHA256; chạy được
   ngay sau Restart Kernel. Ô 8.2 dùng đúng trọng số vừa nạp. Không cần train
   lại model. Camera mới có tiêu đề **V2**.
3. Mặc định `MOVE_HAND = "Auto"`: đưa tay muốn dùng để di chuyển lên trước.
   Tay đó được khóa vai di chuyển; tay còn lại dùng chiêu. **H** trong camera
   đổi vai; có thể chọn cố định `"Left"`/`"Right"` trong notebook.
4. Di chuyển bằng **chỉ ngón trỏ lên/xuống/trái/phải**, gập ba ngón còn lại;
   hoặc **xòe tay giữ yên 0,6 giây** để tự đặt tâm rồi dịch tay bốn hướng.
   **C** đặt lại tâm; đưa về tâm/khép tay/mất tay thì dừng.
5. Tay dùng chiêu: **nắm = Q, V = E, ngón cái = R, xòe = F**. V2.1 giữ dấu
   ổn định **0,35 giây** là gửi chiêu, kể cả dấu đầu tiên. Đổi dấu để dùng
   chiêu khác. Muốn lặp cùng chiêu: **chỉ một ngón trỏ bằng tay dùng chiêu
   0,2 giây**, rồi tạo lại dấu. `none` hoặc Space cũng cho phép lặp lại.
   Giữ nguyên dấu không spam; mất tay không cho phép lặp cùng chiêu.
   Camera hiện `last sent`/`sent=N`, notebook in từng lệnh Q/E/R/F đã gửi;
   Unity vẫn kiểm tra cooldown và mana.
6. Camera phải báo **UNITY READY**. `NO UNITY REPLY`: kiểm tra game đang Play
   và nút TAY đã bật. `click Unity Game View`: click game. `unpause`: đóng bảng
   đang mở. Trên HUD cũng có trạng thái kết nối, không chỉ F8 ẩn.

Khi TAY bật, game nhận cả lệnh mới khi camera đang được chọn (hiện
`UNITY READY (background)`). Tắt TAY/F8 để ngừng điều khiển; mở pause/hành trang
vẫn khóa thao tác. Chuyển focus không xếp hàng chiêu cũ để tung sau đó.

Nhãn Left/Right là nhãn camera sau phản chiếu; Auto/H giúp tránh chọn nhầm.
Hướng ngón trỏ dùng hình học landmark, không phải bốn lớp mới được train.
MLP tự train vẫn nhận năm tư thế; xòe tay làm cần điều khiển và dùng chiêu vẫn
dùng đầu ra MLP. Hướng chiêu theo hướng di chuyển gần nhất, ban đầu hướng phải.

Terminal tương đương (đọc code trực tiếp từ notebook):

```powershell
& .\Tools\HandGestures\Run.ps1 -Action play -MoveHand Auto -MovementStyle both
```

Model nằm ở `models/gesture_mlp.npz`, dữ liệu ở `data/`, biểu đồ và chỉ số ở
`reports/latest/`. Những thư mục này và môi trường `.venv` được bỏ qua bởi Git.
Hàm `prepare_data` trong notebook tải **tọa độ đã gán nhãn** từ HaGRID, không tải ảnh; đây là dữ liệu
công khai thật, không phải dữ liệu giả. Model MediaPipe lấy điểm bàn tay là model
đã train sẵn của Google, còn MLP phân loại năm cử chỉ được train bằng mã này.
