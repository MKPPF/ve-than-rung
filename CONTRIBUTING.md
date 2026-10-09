# Phát triển cùng nhóm

Kho private: https://github.com/MKPPF/ve-than-rung

1. Dùng tài khoản GitHub riêng, chấp nhận lời mời vào kho.
2. Cài Git, Unity Hub và Unity **6000.5.6f1**.
3. Clone: `git clone https://github.com/MKPPF/ve-than-rung.git`.
4. Unity Hub → Add project from disk → chọn thư mục có Assets, Packages,
   ProjectSettings. Chờ import rồi chọn Top Down Game → Open Main Menu.
   Không chép `Library`, `Temp`, `UserSettings` hay thư mục `.git` từ máy thành
   viên khác. Git phải nằm trong PATH để Unity tải package phụ thuộc GitHub.
5. Mỗi việc tạo nhánh riêng từ main mới nhất, ví dụ feature/enemy-animation.
   Commit, push nhánh và mở Pull Request vào main; nhờ bạn khác review trước merge.
   Không force-push main. Dùng tên/email Git của chính mình.

## Quy ước Unity

- Đọc AGENTS.md. Giữ .meta đi kèm asset.
- Chỉ một người sửa builder/content tại một thời điểm; phối hợp trước khi cùng
  sửa TopDownGameBuilder hoặc GameProgressManager.
- Không sửa tay scene/prefab sinh bởi builder. Nếu sửa builder: dựng hai lần,
  chạy validator, EditMode/PlayMode và xem diff trước commit.
- Không commit cache, build, save, token hay ảnh kiểm tra hàng loạt.
- Thống nhất phiên bản Unity/package; giữ manifest và packages-lock cùng nhau.
- Khi xung đột asset, thống nhất code builder rồi dựng lại, không chọn bừa.
- PR ghi tính năng, kết quả test thật, ảnh/video và phần chưa kiểm tra.

Mời người: Settings → Collaborators/Manage access → Add people. Thành viên phải
chấp nhận lời mời trước khi clone/push. Không chia sẻ mật khẩu/token.
Review hiện là quy ước nhóm, chưa bật branch protection bắt buộc.

## Nhận việc, sửa và gửi lại nhóm

Chạy từ thư mục vừa clone. Trước khi chuyển nhánh hoặc cập nhật, kiểm tra
`git status` và lưu/commit công việc còn dang dở; không dùng lệnh xóa thay đổi
để ép thư mục về trạng thái sạch.

```powershell
git switch main
git pull --ff-only
git switch -c feature/ten-cong-viec
```

`feature/ten-cong-viec` là ví dụ; mỗi người dùng tên nhánh riêng. Trong Unity,
sửa C# bằng trình soạn thảo mình chọn. Nếu sửa content, sửa builder trước rồi
dựng lại theo quy ước bên dưới. Chạy validator/test trước khi gửi code.

```powershell
git status
git diff
# Chỉ thêm những file mình đã kiểm tra; asset mới phải có .meta đi kèm.
git add <duong-dan-file-da-sua>
git commit -m "Mo ta thay doi"
git push -u origin feature/ten-cong-viec
```

Thay `<duong-dan-file-da-sua>` bằng đường dẫn thật (đặt trong dấu nháy nếu có
khoảng trắng); không chạy nguyên dòng chứa dấu `< >`. Sau đó mở Pull Request
trên GitHub vào `main`. Nếu Git chưa có danh tính, cấu hình tên/email của chính
mình; nếu bị từ chối push, kiểm tra quyền cộng tác bằng tài khoản của mình.
Không đăng nhập bằng tài khoản của chủ kho.

## Kiểm tra sau lần tải đầu

1. Xác nhận Unity đúng phiên bản và import/compile không có lỗi đỏ.
2. Chạy **Top Down Game → Validate Project**.
3. Mở MainMenu, Play, chọn Chơi mới và kiểm tra vào Lobby; thử di chuyển,
   mở/đóng hành trang, tới lò rèn và đi qua truyền tống trận.
4. Với thay đổi code, chạy cả EditMode và PlayMode trong Test Runner.
5. Báo lại phiên bản Windows/Unity, commit đang dùng và lỗi đầu tiên nếu có.

Không cần dựng lại content khi chỉ tải về chơi. Khi kiểm tra lần đầu trên máy
khác, xem thêm [DOWNLOAD_READINESS.md](Docs/DOWNLOAD_READINESS.md) để phân biệt kết quả
đã xác minh với phần còn cần thử trên thiết bị khác.
