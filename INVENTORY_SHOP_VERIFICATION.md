# Hành trang và lò rèn — 05/09/2026

## Đã triển khai

- Hành trang ba cột có viền phân khu, trạng thái đang dùng, bộ lọc và thanh cuộn.
- Lò rèn có thẻ đồ với icon/chỉ số tách cột, giá/ưu đãi, sức mạnh chênh lệch,
  trạng thái sở hữu/đang dùng/thiếu vàng và thanh cuộn catalog.
- Khung thiết kế 1920×1080 thu vừa canvas theo tỉ lệ, không cắt nút ở 4:3 hoặc 16:10.
- Chỉ số và điểm sức mạnh tính cả cường hóa; chỉ số âm hiển thị đỏ.
- Hành trang/lò rèn dùng GameplayState để tạm dừng và khóa combat/skill/interaction.
- Esc không mở Pause cùng frame đóng modal; khôi phục trạng thái movement trước khi mở.
- Không thu tiền mua khi thiếu GameProgressManager; không đổi ưu đãi trả phí khi sở hữu hết.
- Giữ nguyên catalog, item id, schema save, scene và builder. Không cần rebuild content
  vì tất cả thay đổi giao diện này được dựng bởi runtime code hiện có.

## Xác minh

- EditMode: **44/44 đạt**, lần cuối job `c2412d057619498689e56d3b8c7c85b6`.
- PlayMode: **50/50 đạt**, lần cuối job `ac6beb304d2346afb860f032ea7bb102`.
- Validator: `ValidateProject(true)` trả `IsValid=true`, không có lỗi.
- Không có lỗi biên dịch C# được phát hiện sau refresh.
- Test mới kiểm tra mua giảm giá, không thu tiền lần hai, equip/unequip, modal
  độc quyền, Esc/Pause, disable cleanup và bảo toàn trạng thái movement.
- Test mới dùng save test riêng và khôi phục trạng thái manager dùng chung.
  Một lỗi cleanup trong fixture mới đã được sửa trước lần chạy 50/50 cuối cùng.
- Render runtime bằng camera của fixture, đã xem ảnh tại 1920×1080, 1280×800,
  1024×768 trong `Assets/Screenshots/HanhTrang_*_Verified.png` và
  `Assets/Screenshots/CuaHang_*_Verified.png`.
- Báo cáo PlayMode gốc: `Logs/InventoryShop_FullPlayMode.xml`.
- Bản sao code trước sửa: `Backups/InventoryShop_20260905/`.

## Giới hạn còn lại

- Chưa kiểm tra thủ công thao tác bàn phím/chuột xuyên suốt một lượt chơi thực tế;
  fixture gọi nút/UI API và render trong Unity Test Runner.
- Khung 4:3 giữ đủ nội dung nhưng chữ nhỏ hơn; chưa có tùy chọn cỡ chữ accessibility.
- Chưa thiết kế lại Storage, Settings, Tutorial hoặc toàn bộ HUD. U5 vẫn đang làm.
- Builder không thay đổi nên không chạy lại hai lần; validator kiểm tra content hiện có.
- Console có cảnh báo kết nối MCP khi tải lại domain và importer TMP fallback
  trong quá trình chạy lần đầu; đây không phải lỗi biên dịch giao diện.
