# AI1 — Sửa điều khiển tay V2 — 20/10/2026

## Nguyên nhân và thay đổi

- Ảnh người dùng: model nhận `Right: stop 0.99`, nhưng code cũ chỉ dùng Left
  để di chuyển và bắt buộc C. Chỉ có tay phải sẽ không hiệu chỉnh/di chuyển.
  V2 mặc định Auto, khóa bàn tay xòe/chỉ ngón trỏ đầu tiên làm tay di chuyển;
  tay còn lại dùng chiêu. Nếu có cả hai lúc chọn thì ưu tiên Left. H đổi vai.
- Code cũ mặc định tắt bộ nhận Unity bằng F8 nhưng không có nút rõ ràng.
  HUD V2 thêm TAY [F8] và dòng trạng thái socket/camera/gameplay.
- Probe UDP thật tái hiện ACK `focused=false, blocked=true, reason=focus`
  dù bộ nhận đã bật và player tồn tại. Khi người dùng bật TAY, V2 bật xử lý
  nền và nhận lệnh mới khi camera đang được chọn. Tắt TAY đóng socket, xóa
  input và trả runInBackground về trạng thái ban đầu. Đổi focus vẫn xóa input
  cũ, bỏ cast ở lô đầu; không xếp hàng chiêu qua focus/pause/chuyển map.
- Xòe tay giữ yên 0,6 giây tự đặt tâm; C vẫn đặt lại tâm. Vùng chết 0,08.
- Thêm ngón trỏ chỉ bốn hướng, ba ngón còn lại gập. Đây là quy tắc hình học
  landmark, không phải bốn nhãn mới đã train. Hợp đồng MLP/NPZ không đổi và
  không cần train lại. Tay mất/khép lại sẽ dừng, không tự đổi vai sang tay cast.
- Camera V2 có mũi tên hướng, vai MOVE/CAST, trạng thái arm và ACK từ Unity.
  Chỉ nhận ACK đúng session/endpoint/version, hết hiệu lực sau 1,5 giây.
- Space trong cửa sổ camera chủ động arm một chiêu khi khó tạo tư thế none;
  mất tay/unknown vẫn không tự arm. Mana/cooldown/slot/modal do Unity quyết định.
- Notebook vẫn là nguồn Python duy nhất; adapter CLI hỗ trợ Auto/Left/Right,
  both/point/palm. Log train và kết quả mô hình được giữ nguyên.

## Kiểm tra

- Python 13/13 test đạt: đặc trưng, dữ liệu, gate cũ; bốn hướng ngón trỏ,
  tự chọn/hiệu chỉnh tay phải không cần Left, mất tay không đổi vai, đổi H,
  palm không bị nhầm index, trạng thái mất kết nối/focus/pause.
- nbformat.validate đạt, 23 ô sau bổ sung mục 8.1, output train còn nguyên. MediaPipe VIDEO trên
  frame đen đạt, không mở webcam của người dùng.
- Smoke toàn bộ vòng lặp OpenCV với frame/landmark fixture đạt, xem overlay
  `Tools/HandGestures/reports/controller_v2_overlay.png`. Đây là frame giả
  để kiểm tra vẽ UI/tài nguyên, không phải minh chứng nhận diện webcam.
- Probe Python → UDP → receiver → PlayerMovement trên Map01 đạt bốn hướng.
  Đưa player tạm tới vùng không có collider ở (1000,1000), phát lệnh từ fixture
  ngón trỏ khoảng 0,35 giây mỗi hướng rồi trả lại vị trí ban đầu. Đây là kiểm
  tra transport/physics, không phải nhận diện webcam thật:

  | Hướng | ΔX | ΔY |
  |---|---:|---:|
  | Lên | 0 | +1,056 |
  | Phải | +1,056 | 0 |
  | Xuống | 0 | −1,056 |
  | Trái | −0,996 | 0 |

  ACK có playerReady=true, blocked=false. Kết quả đầy đủ:
  `Tools/HandGestures/reports/unity-gesture-v2-directions.json`.
- Đã gọi sự kiện click của nút TAY trên HUD Map01: bật → tắt → bật đúng,
  socket đóng/mở và runInBackground=true khi bật. Đã xem screenshot
  `Assets/Screenshots/GestureControlV2_Map01_HUD.png`, không chồng nút launcher.
- Unity EditMode 77/77 đạt, không bỏ qua, trên bản sửa cuối.
  Job `4fb6d8a504434738a47123a0db74474e`; kết quả lưu tại
  `Tools/HandGestures/reports/unity-gesture-v2-editmode.json`.
- Unity PlayMode 83/83 đạt, không bỏ qua, trên bản sửa cuối. Bao gồm test UDP
  loopback, ACK, nhận khi chạy nền, pause, timeout và phục hồi runInBackground.
  Job `989bf29ee29f497492c9be94c44b1f58`; kết quả lưu tại
  `Tools/HandGestures/reports/unity-gesture-v2-playmode.json`.
- Validate Project đạt. Sau test, Console không có error/warning; Unity đã
  dừng Play, không compile và đang ở `Assets/Scenes/MainMenu.unity`.
- Snapshot save thật trước probe đã được phục hồi sau Stop. Các suite Unity
  dùng key test riêng.

## Cách dùng

1. Esc trong camera cũ hoặc Interrupt ô đang chạy; reload notebook từ ổ đĩa.
2. Unity Play → vào game → bật TAY [F8] dưới Tự nhặt. Camera có thể giữ focus.
3. Chạy ô 8.1 nạp model, rồi 8.2 mở camera V2. Không cần chạy lại các mục 1–7.
   Hoặc terminal dự án: `Run.ps1 -Action play -MoveHand Auto -MovementStyle both`.
4. Đưa tay muốn di chuyển lên trước; chỉ ngón trỏ bốn hướng hoặc xòe yên 0,6
   giây rồi dịch tay. H đổi vai. Tay còn lại dùng nắm Q / V E / ngón cái R / xòe F.
5. Chờ UNITY READY hoặc UNITY READY (background); NO UNITY REPLY nghĩa là
   kiểm tra Play và nút TAY. Tắt F8 khi nghỉ; pause/hành trang vẫn chặn lệnh.

## Còn cần nghiệm thu

### Sửa kích hoạt chiêu V2.1 theo ảnh người dùng

- Ảnh có UNITY READY, `Right: stop 1.00` nhưng gate đang đòi `none`/Space,
  `casts=1`. Code cũ không phát dấu đầu tiên, mọi dấu mới đều cần model nhận
  được `none`; lệnh phát trước ACK còn có thể bị Unity bỏ trong bước tạo session.
- Notebook V2.1 giữ dấu ổn định 0,35 giây là gửi dấu đầu/đổi dấu, không cần
  `none`. Chỉ ngón trỏ bằng tay cast 0,2 giây để dùng lại cùng dấu. Giữ dấu
  không spam, mất tay không lặp lại dấu vừa gửi. Gate chờ ACK mới/không block
  trước khi đếm thời gian giữ dấu. Có last sent/sent=N và log Q/E/R/F.
- Python 16/16 đạt, gồm vòng lặp camera giả lập + dự đoán giả lập + ACK giả lập:
  xòe đầu tiên gửi F, đổi nắm gửi Q, mất tay không lặp Q, index release cho
  phép gửi lại Q; không có ACK thì không phát chiêu. Đây là kiểm thử hồi quy
  điều phối, không đo nhận diện webcam hoặc xác nhận cooldown/mana thực tế.
- Chỉ đổi Python/notebook và tài liệu; không đổi Unity C#, scene hoặc save,
  không dừng phiên game đang mở. Các số Unity phía trên là kết quả lần trước.

Cập nhật notebook theo yêu cầu chạy trực tiếp: ô 8.1 nạp thành công model thật
từ namespace sạch (42 → 128 → 64 → 5, 14.085 tham số), in đường dẫn, nhãn và
SHA256. Ô 8.2 được kiểm tra bằng controller thay thế để xác nhận nhận cùng object
trọng số, Auto/both; không mở webcam. Toàn bộ code Python (ngoại trừ magic `%pip`)
compile đạt, nbformat đạt, 13/13 test Python đạt và 6 output ô train được giữ nguyên.

- Chưa đo accuracy/latency của webcam người dùng hoặc đánh giá hình ngón trỏ
  ngoài đời. Fixture hình học không xác nhận độ bền với che khuất/góc/ánh sáng.
- Chưa có phiên dùng đồng thời hai bàn tay của người dùng để xác nhận cảm giác
  điều khiển và tỷ lệ dùng chiêu đúng. Giữ AI1 ở [-] cho tới khi đạt thực tế.
- Không thay builder/content/scene/prefab; không cần dựng lại nội dung.
