# Phát triển cùng nhóm

Kho private: https://github.com/MKPPF/ve-than-rung

1. Dùng tài khoản GitHub riêng, chấp nhận lời mời vào kho.
2. Cài Git, Unity Hub và Unity **6000.5.6f1**.
3. Clone: `git clone https://github.com/MKPPF/ve-than-rung.git`.
4. Unity Hub → Add project from disk → chọn thư mục có Assets, Packages,
   ProjectSettings. Chờ import rồi chọn Top Down Game → Open Main Menu.
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
