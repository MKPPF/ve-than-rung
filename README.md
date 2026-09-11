# Vệ Thần Rừng — game hành động 2D

Game top-down bằng Unity: khám phá vùng săn, chiến đấu, học kỹ năng, thu thập
và cường hóa trang bị. Đây là bản demo đồ án đang phát triển, không phải release
đã cân bằng hoàn chỉnh.

## Chỉ muốn chơi, không cài Unity

Sau khi người quản lý kho đăng bản phát hành, mở mục **Releases** của kho và tải
**VeThanRung-Windows-x64.zip**. Không chọn `Source code.zip` nếu chỉ muốn chơi.

1. Giải nén toàn bộ ZIP vào một thư mục.
2. Mở `VeThanRung/VeThanRung.exe`.
3. Giữ `VeThanRung_Data`, `UnityPlayer.dll`, `MonoBleedingEdge` và các file còn lại
   cạnh file EXE. Không chạy ngay trong cửa sổ xem ZIP, không chỉ chép riêng EXE.

Yêu cầu: Windows 64-bit và card đồ họa hỗ trợ DirectX 11. Chưa đo cấu hình RAM/GPU
tối thiểu. Game chơi đơn; không cần tài khoản GitHub, Unity hoặc máy chủ game.
Nếu Windows hỏi quyền truy cập mạng, không cần cấp quyền cho việc chơi offline.
Không tắt phần mềm bảo mật để chạy game.

## Tải mã nguồn và mở Unity

- Unity **6000.5.6f1** (xem `ProjectSettings/ProjectVersion.txt`). Cài qua Unity Hub,
  kèm Windows Build Support nếu muốn xuất bản Windows.
- Cài Git và đảm bảo Git có trong PATH: Unity dùng Git để tải dependency Unity MCP.
- Có kết nối mạng ở lần mở đầu để Unity khôi phục các package trong
  `Packages/manifest.json` và `Packages/packages-lock.json`.

Trên trang kho, chọn **Code → Download ZIP**, giải nén rồi vào Unity Hub → Add
project from disk và chọn thư mục có `Assets`, `Packages`, `ProjectSettings`.
Hoặc chọn **Code → HTTPS**, sao chép URL và chạy `git clone` với URL đó.

Kho hiện không dùng Git LFS: file ảnh thật được lưu trong Git, không phải pointer.
Không chép `Library`, `Temp` hoặc `UserSettings` từ máy người khác. Lần import đầu
có thể mất nhiều phút; chờ Unity hoàn tất biên dịch.

Chọn **Top Down Game → Open Main Menu**, rồi nhấn Play. Các scene/prefab đã được
đưa vào kho, không cần dựng lại chỉ để chơi. Không cần chạy MCP/AI để chơi hoặc
build game; các package hỗ trợ Editor hiện vẫn giữ theo cấu hình dự án gốc.

## Điều khiển

| Thao tác | Phím |
|---|---|
| Di chuyển | WASD, phím mũi tên hoặc chuột trái |
| Đánh thường | Chuột phải hoặc J |
| Kỹ năng | Q / E / R / F |
| Tương tác | E gần đối tượng; ưu tiên tương tác thay kỹ năng E |
| Hành trang / tiềm năng / kỹ năng | I / P / K |
| Đóng bảng, tạm dừng | Esc |

Mở Hướng dẫn trong menu chính hoặc Pause. Tiếp tục đưa người chơi về Lobby và
khôi phục tiến trình; Chơi mới thay thế save sau khi xác nhận. Save nằm trong
PlayerPrefs trên từng máy, không nằm trong kho Git. Không chia sẻ save cá nhân.

## Dựng nội dung và kiểm thử

`Assets/Editor/TopDownGameBuilder.cs` và các partial của nó là nguồn sinh nội dung.
Không chỉnh tay scene rồi kỳ vọng thay đổi được giữ khi chạy builder.

1. Lưu thay đổi và dừng Play Mode.
2. Nếu sửa builder, chạy **Top Down Game → Build Complete MVP** hai lần.
3. Chạy **Top Down Game → Validate Project**.
4. Mở **Window → General → Test Runner** để chạy EditMode và PlayMode.

Test dùng save key `TopDownGame.Tests.*`; không dùng `PlayerPrefs.DeleteAll()`.
Ảnh kiểm tra được tạo cục bộ trong `Assets/Screenshots`, không đưa vào Git.
Chạy `Prepare Vietnamese Font` nếu build guard báo thiếu glyph tiếng Việt.

## Xuất và đóng gói Windows

Trong Build Profiles chọn Windows x64, sử dụng đủ các scene đã có trong Build
Settings (MainMenu đầu tiên), rồi Build. Không chuyển sang Android/WebGL nếu chưa
kiểm tra tương thích. Sau khi build, chạy PowerShell từ thư mục dự án:

```powershell
./Tools/Package-Windows.ps1 -BuildDirectory ./Builds/GraduationDemo_FontFixed
```

Script tạo `Releases/VeThanRung-Windows-x64.zip` cùng checksum SHA-256, kiểm tra các
file thiết yếu và không đưa log, ký hiệu debug hoặc thư mục DoNotShip vào ZIP.
Chỉ package một bản đã build xong và chơi thử. Script không build hoặc tự đăng GitHub.
Đăng ZIP và checksum lên **Releases**, không commit các file build vào Git.

## Giới hạn và quyền sử dụng

- Nhân vật vẫn dùng cấu hình thử nghiệm 50.000 HP / 25.000 mana; chưa coi là bản
  cân bằng chính thức. Một số phần của roadmap còn chưa triển khai.
- Windows build có cảnh báo từ package/shader; chưa cam kết hết warning hoặc
  chạy tốt trên mọi máy. Nên chơi thử trên máy trình diễn.
- Thư mục ảnh tham chiếu, backup, cache và công cụ upscale cục bộ không cần để chạy
  và không được chia sẻ trong Git.
- Giữ các thông báo bản quyền đi kèm TextMesh Pro/Liberation Sans và
  `Assets/ThirdParty`. Tài nguyên game gồm hình ảnh AI và nội dung tự tạo;
  cần rà soát quyền phân phối trước khi công khai hoặc dùng thương mại.
- Chưa chỉ định giấy phép mã nguồn mở cho toàn dự án. Công khai kho không tự động
  cấp quyền tái phân phối mọi tài nguyên. Xem `THIRD_PARTY_NOTICES.md`.

Hướng dẫn kiến trúc: `AGENTS.md` và `Assets/Scripts/README.md`.
