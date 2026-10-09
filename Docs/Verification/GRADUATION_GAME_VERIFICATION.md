# Kiểm tra game để trình diễn đồ án — 10/09/2026

Chỉ đánh giá sản phẩm game, không phải báo cáo Word. Không có rubric của trường,
nên chưa thể xác nhận điều kiện chấm điểm chính thức.

## Nhận định về phạm vi

Đủ nền tảng cho đồ án game hành động 2D: di chuyển và chiến đấu, AI quái nhiều
hành vi, boss, 6 vùng săn ngoài Lobby, 9 kỹ năng, 40 trang bị/5 slot, cửa hàng,
kho, tăng cấp/tiềm năng/cường hóa, HUD/bản đồ nhỏ, lưu và tiếp tục. Scene và asset
do builder sinh; không cần triển khai toàn bộ backlog hang băng để có demo cơ bản.

Game hiện là vòng săn quái có hồi sinh, không phải chiến dịch yêu cầu dọn sạch
map. Không mô tả các mục quest, achievement, dodge, boss băng chưa làm như tính năng
đã có. Chưa khẳng định hiệu năng vì chưa đo FPS/GC trên máy trình diễn.

## Bổ sung trong đợt này

- Xác nhận Chơi mới trước khi thay thế save; Hủy/Esc không sửa tiến trình.
- Hướng dẫn tiếng Việt mở từ menu chính và Pause. Giải thích rõ E ưu tiên tương
  tác và mục tiêu săn quái; chưa phải tutorial nhiều bước tự bật lần đầu.
- Việt hóa tên/nút menu chính, Pause và màn thua.
- Pause/hướng dẫn/màn thua dùng GameplayState: đóng help vẫn giữ Pause;
  Esc không mở Pause chồng lên màn thua.
- Bỏ tự ghi 2.000.000 vàng lúc khởi động; không trừ vàng/save hiện có. Chơi mới
  giữ 2.500 vàng. Menu đầu tiên không tự tạo save giả để bật Tiếp tục.
- Test mới cô lập save, kiểm tra hủy, nạp lại vàng, modal, bố cục/overflow và chụp
  ảnh 1920×1080, 1280×800, 1024×768. Sửa dọn camera/scene của fixture mới và trạng
  thái/quái thử của fixture PersistentHunt để không nhiễu các test tiếp theo.

Không đổi builder, scene, prefab hoặc thông số combat: không cần tái dựng content.
UI ghép ở runtime theo mẫu sẵn có của equipment/Pause, vẫn hoạt động sau rebuild.
Backup mã trước thay đổi: `Backups/GraduationReadiness/`.

## Kiểm tra

- 6/6 test MenuReadinessTests đạt (job 74c7d59299704839bd41c82fda570be2).
- Đã xem ảnh hướng dẫn 4:3, xác nhận 16:10, menu 1080p; tiêu đề menu được rút gọn
  sau khi thấy bản tiếng Anh tràn chiều rộng panel. Ảnh cuối được tạo khi chạy lại.
- Toàn bộ EditMode trước tinh chỉnh cuối: 73/73 đạt (8f7a6b0d8e264bab918dd527e5ea6b09).
- Lượt PlayMode đầy đủ đầu phát hiện camera/điểm hồi sinh/actor test rò sang bài
  sau. Không coi lượt đó là pass. Kết quả cuối cập nhật bên dưới sau khi chạy lại.

## Kết quả cuối

Kết quả cuối sau sửa cleanup: **75/75 PlayMode đạt**, 202,30 giây,
job `8945ffd465994b9fa2a3be6be13fbad4`; **73/73 EditMode đạt**, 13,28 giây,
job `cd1d1886eab141b99f710d92b67e89c4`. Validator báo
`Top Down Game automated validation passed.` Ảnh menu cuối 4:3 đã được xem,
tiêu đề tiếng Việt vừa panel. Sáu test mới nằm trong tổng 75 PlayMode.

## Phần chưa đủ để gọi là release hoàn chỉnh

1. Player builder vẫn bật `experimentalVitals`: 50.000 HP, 25.000 mana, hồi mana
   thử nghiệm. Cần chốt cấu hình chơi thật và balance bằng số liệu trước khi
   báo cáo độ khó; không tự giảm HP của save cũ trong đợt sửa menu.
2. Chưa có tutorial lần đầu, cài đặt độ phân giải/toàn màn hình hoàn chỉnh và
   màn tổng kết chiến dịch. Help hiện có có thể xem lại bằng chuột/bàn phím.
3. Chưa playthrough thủ công toàn bộ map/loadout đến cuối trong phiên này.
4. Còn warning API Unity cũ trong ClickShotTests; log môi trường có lỗi chứng chỉ
   mạng/licensing của dịch vụ Editor. Không đánh đồng chúng với exception gameplay.
5. Build Settings còn product/company mặc định. Cần tên sản phẩm/tác giả thật
   trước khi điền thông tin sở hữu; không tự bịa tên trường/tác giả.

## Bản Windows

Đã build Development Windows64 thành công: `Builds/GraduationDemo/VeThanRung.exe`,
job `build-218378b6df`, 239,11 giây, 475,09 MB theo build report, 0 lỗi / 186 cảnh
báo. Log có nhiều shader warning từ package inference (ConvGeneric). Không gọi
đây là release không cảnh báo. Giữ nguyên company/product để tránh đổi vùng save.

Đã khởi chạy và hệ thống trả về cửa sổ `My project` của VeThanRung.exe. Chưa xác
nhận giao diện bản standalone bằng ảnh: cửa sổ bị thu nhỏ và công cụ báo có thao
tác người dùng khi cố hiển thị; dừng điều khiển để không giành quyền thao tác.
Các ảnh menu/hướng dẫn đã verify ở trên là runtime trong Editor, không phải ảnh
standalone. Cần kiểm tra trực tiếp bản Windows trước buổi báo cáo. Khi mang sang
máy khác, sao chép cả thư mục GraduationDemo, không chỉ file exe.

## Kịch bản demo ngắn

Menu/hướng dẫn → Tiếp tục về Lobby → mua và trang bị/cất đồ → truyền tống Map01
→ di chuyển, đánh/skill/nhặt loot → nâng tiềm năng/kỹ năng → Pause và xem hướng
dẫn → lưu về menu → Tiếp tục chứng minh tiến trình được giữ. Dùng save riêng
khi thử Chơi mới; không xác nhận ghi đè save đang dùng để báo cáo.
