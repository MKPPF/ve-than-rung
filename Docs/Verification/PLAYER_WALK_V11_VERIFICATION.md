# Bước đi V11 — 05/09/2026

## Đính chính V11.1 — tư thế đứng

Người dùng phát hiện pose passing vẫn nhấc chân khi đứng yên. Phần mô tả giữ
pose nghỉ ở bản V11 bên dưới đã được thay thế: chuyển tiếp 0,1 giây nay phải
kết thúc bằng Idle_00 trung tính. Giữ nguyên đồng bộ LateUpdate và tăng/giảm tốc.
Chưa có idle riêng bốn hướng nên hình đứng không giữ đúng hướng quay lưng.
Test đã đổi để bắt buộc sprite idle sau khi dừng ở cả bốn hướng; không còn
coi một sprite WalkUp là kết quả đúng khi đứng yên.

V11.1 xác minh: 54/54 PlayMode (da84051d320540c9bf07a34fff69625d),
56/56 EditMode (a288e713ff794e9b9cd826bcce6eec11). Ảnh:
Assets/Screenshots/PlayerWalkV11_1_NeutralStop.png.
Video V11 cũ bên dưới không phản ánh thay đổi idle của V11.1.

## Nguyên nhân đã xác nhận

- 8 pose × 3 đơn vị/giây × 0,4 chu kỳ/đơn vị = 9,6 pose/giây.
  Ghi hình 60 fps trước sửa cho thấy một pose đi thường giữ 6–7 frame.
- Animator cũ kết hợp MoveDirection (ý định) với vận tốc physics, nên chuyển idle
  trước khi chuyển động nội suy của thân kết thúc. Hình idle chung quay về mặt trước.
- Physics cũ đổi vận tốc ngay từ 0 tới 3 và ngược lại.
- Interpolation đã bật sẵn, vì vậy chỉ bật lại interpolation không giải quyết vấn đề.

## Đã sửa

- Builder bật smoothLocomotion cho PlayerMovement và PlayerCharacterAnimator.
- Animator mẫu mới lấy quãng đường transform hiển thị trong LateUpdate, giữ pha
  liên tục theo quãng đường; không chèn idle vì một tick intent bằng 0.
- Một cửa sổ 0,1 giây đưa chân về pose passing gần nhất (2/8 hoặc 6/8);
  giữ hướng lên/xuống/trái/phải lúc dừng. Đây là pose nghỉ tái sử dụng,
  chưa phải sprite idle riêng bốn hướng.
- Tăng tốc 60 và giảm tốc 90 đơn vị/giây²: 3 đơn vị/giây đạt trong khoảng 0,05s;
  vẫn dùng Rigidbody2D.MovePosition và một trục mỗi bước.
- Giữ tốc độ qua góc click-path; đích chuột dừng chính xác không thêm quãng phanh.
- Khóa modal/skill/death reset tốc độ tăng dần; không đọc input gameplay khi pause.
- Không chỉnh tốc độ game, không đổi bốn hướng thành tám hướng, không đổi skill,
  không tạo sprite trung gian hoặc crossfade hai sprite.
- Flag mặc định false cho đối tượng tạo động/legacy; builder bật true trên prefab
  và scene chính thức để các contract cũ không bị thay đổi ngầm.

## Xác minh

- Builder chạy hoàn tất hai lần, marker MVP built trong Logs/Editor.log.
- 56/56 EditMode: job 3930cc255de54133ab792c5b87abe19a.
- 54/54 PlayMode: job 04564be5fb054e30b8acadc8782acc3d.
- Validator IsValid=true, errors=[].
- Bài mới kiểm tra không flash idle khi đi lên, giữ hướng sau dừng, pha nghỉ đứng yên,
  bước tăng tốc đầu nhỏ hơn tốc độ tối đa, phanh trong hai tick và không vượt đích chuột.
- Test dùng test save riêng. Scene chưa lưu chỉ có camera đã sao lưu tới
  Backups/PlayerWalk_ContinuityV11/UnsavedScene.unity trước khi builder thay scene.

## Video và giới hạn

- Captures/WalkContinuity_Comparison.mp4: trước trái, sau phải; 5 giây, 60 fps.
- Captures/WalkContinuity_Before.mp4 và WalkContinuity_After.mp4: bản riêng.
- Hai thư mục frame tương ứng có trace.csv để đối chiếu sprite và vị trí.
- Assets/Screenshots/PlayerWalkV11_BeforeStop.png và PlayerWalkV11_AfterStop.png.
- Video camera bám nhân vật trên nền phẳng nhằm xem thay đổi tư thế, không dùng
  để kết luận chất lượng scrolling nền hoặc frame pacing khi máy chạy thực tế.
- Bộ sprite vẫn 8 pose nên nhịp đổi hình khi đi đều vẫn ~9,6 pose/giây.
  Không tuyên bố đã tạo animation 60 pose/giây. Nếu phần chân đi đều vẫn chưa
  ưng ý cần bộ pose trung gian tốt hoặc rig 2D; không nên tiếp tục che bằng blur.
- Chưa đánh giá trực tiếp bàn phím người dùng hay frame-time profiler ở combat đông.

Tham khảo cơ chế interpolation Unity:
https://docs.unity.cn/6000.1/Documentation/ScriptReference/Rigidbody2D-interpolation.html
