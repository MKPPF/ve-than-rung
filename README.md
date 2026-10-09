# Vệ Thần Rừng — game hành động 2D

Kho bài tập nhóm (private): https://github.com/MKPPF/ve-than-rung
Người tải phải đăng nhập GitHub bằng tài khoản đã được mời và đã chấp nhận lời
mời. Chỉ gửi đường link là chưa đủ; người chưa có quyền có thể thấy trang 404.
Để cùng phát triển: nhận lời mời, clone mã nguồn, làm trên nhánh riêng và gửi
Pull Request theo CONTRIBUTING.md. Bản Windows chỉ dùng chơi thử, không sửa dự án.

Game top-down bằng Unity: khám phá vùng săn, chiến đấu, học kỹ năng, thu thập
và cường hóa trang bị. Đây là bản demo đồ án đang phát triển, không phải release
đã cân bằng hoàn chỉnh.

## Tải về để tiếp tục phát triển

Nên **clone bằng Git**, không dùng ZIP, để giữ lịch sử và gửi thay đổi lại nhóm:

```powershell
git clone https://github.com/MKPPF/ve-than-rung.git
cd ve-than-rung
```

Cài Unity **6000.5.6f1**, thêm thư mục vừa clone vào Unity Hub và chờ import xong.
Mở **Top Down Game → Open Main Menu**, Play để chạy; chọn **Validate Project**
để kiểm tra nội dung. Mã nguồn C# ở `Assets/Scripts`, công cụ sinh content ở
`Assets/Editor`. Tạo nhánh riêng trước khi sửa; xem [CONTRIBUTING.md](CONTRIBUTING.md)
cho quy trình cập nhật, commit và Pull Request. Không cần có bản EXE hay GitHub
Release để tiếp tục phát triển.

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

Nếu dùng Git để cập nhật bản đã clone, lưu/commit thay đổi của mình trước, rồi
chạy `git pull --ff-only` trên nhánh `main`. Không chép đè thư mục dự án đang sửa
bằng ZIP mới; ZIP không giữ quy trình cập nhật và lịch sử Git.

Kho hiện không dùng Git LFS: file ảnh thật được lưu trong Git, không phải pointer.
Không chép `Library`, `Temp` hoặc `UserSettings` từ máy người khác. Lần import đầu
có thể mất nhiều phút; chờ Unity hoàn tất biên dịch.

Chọn **Top Down Game → Open Main Menu**, rồi nhấn Play. Các scene/prefab đã được
đưa vào kho, không cần dựng lại chỉ để chơi. Không cần chạy MCP/AI để chơi hoặc
build game; các package hỗ trợ Editor hiện vẫn giữ theo cấu hình dự án gốc.

### Nếu máy khác chưa mở được

- Trang GitHub báo 404 hoặc clone báo `Repository not found`: kiểm tra tài khoản
  và lời mời vào kho private; không chia sẻ mật khẩu hay token.
- Unity báo thiếu Editor: cài đúng phiên bản trong `ProjectVersion.txt`, không
  chọn nâng/hạ phiên bản dự án chỉ để bỏ qua thông báo.
- Package Manager báo không tìm thấy Git: cài Git, đóng rồi mở lại Unity Hub và
  Unity để cập nhật PATH. Lần đầu cần truy cập GitHub và kho package Unity.
- Unity mở scene trống: dùng **Top Down Game → Open Main Menu**.
- Có lỗi đỏ: đợi import hoàn tất, mở Console, gửi lỗi đầu tiên cùng phiên bản
  Unity cho nhóm. Không xóa `.meta` hoặc chạy builder chỉ để che lỗi import.
- Git báo `dubious ownership`: thường gặp khi chép cả `.git` từ tài khoản khác.
  Ưu tiên clone bằng chính tài khoản Windows của mình vào thư mục mới; không
  tắt kiểm tra quyền sở hữu cho tất cả thư mục bằng `safe.directory=*`.

Kết quả và giới hạn kiểm tra bản tải mới: [DOWNLOAD_READINESS.md](DOWNLOAD_READINESS.md).

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
