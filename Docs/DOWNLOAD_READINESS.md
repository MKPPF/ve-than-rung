# Kiểm tra tải mã nguồn để tiếp tục phát triển

Clone lần đầu: 28/09/2026; kiểm tra tiếp: 09/10/2026. Phạm vi: clone từ GitHub, đủ dữ liệu Unity và kiểm thử
trên bản clone mới. Không phải nghiệm thu bản phát hành hay thử trên máy thứ hai.

**Kết luận 09/10/2026:** đủ mã nguồn/tài nguyên để thành viên có quyền truy cập
clone và tiếp tục phát triển trên Unity 6000.5.6f1. Không có dấu hiệu phải chép
thêm file riêng từ máy chủ kho. Chưa đạt trạng thái toàn bộ test xanh: còn một
bài PlayMode click-bắn thất bại, nêu chi tiết bên dưới. Chưa kiểm tra trực tiếp
tài khoản hoặc máy của thành viên khác.

## Nguồn được kiểm tra

- Kho: `MKPPF/ve-than-rung`, nhánh `main`.
- Commit: `4e42a028e9493a110e28bb5b1f43de1de23d941d`.
- Unity: `6000.5.6f1` trên Windows.
- Bản kiểm tra được clone trực tiếp từ GitHub vào thư mục riêng, không sao chép
  `Library`, `Temp` hoặc `UserSettings` từ dự án đang phát triển.
- Đây vẫn là cùng máy Windows: package cache toàn máy, Git và giấy phép Unity
  đã có sẵn. Không coi đây là kiểm tra máy mới hoàn toàn hoặc mọi hệ điều hành.

## Kết quả

- Clone GitHub thành công; commit nhận được trùng `main` đang phát triển.
- 1.818 file được theo dõi, khoảng 253,9 MiB nội dung không nén; không thiếu
  file `.meta` đi kèm asset, không có `.meta` mồ côi hoặc trùng đường dẫn do
  khác chữ hoa/thường trong danh sách file.
- Có `Assets`, `Packages`, `ProjectSettings`, manifest và package lock.
- Kiểm tra 924 file `.meta` trong Assets của bản clone: không có GUID trùng.
- Git `fsck --full` trên bản clone: không báo lỗi tính toàn vẹn.
- Cả 8 scene đã nằm trong Build Settings; MainMenu đứng đầu.
- Không tìm thấy đường dẫn ổ đĩa cá nhân/`file:` trong phạm vi mã C#, cấu hình
  package và ProjectSettings đã quét.
- Import lần đầu và biên dịch thành công, không dùng Library của dự án gốc.
- Validator ngày 28/09: `PASS — no project errors found`, Unity thoát mã 0.
- Ngày 09/10 đối chiếu trực tiếp GitHub: `main` vẫn cùng commit nêu trên.
- EditMode ngày 09/10: **74/74 đạt**, 0 lỗi, 0 bỏ qua.
- PlayMode ngày 09/10: **74/75 đạt**, 1 lỗi, 0 bỏ qua.
- Đã xem ảnh menu 1024×768 và Lobby 1920×1080 do PlayMode tạo: nền, nhân vật
  và chữ tiếng Việt hiển thị. Đây là smoke test tự động, không thay thế toàn bộ
  playtest thao tác thủ công trên máy đồng đội.

### Lỗi kiểm thử còn mở

`TopDownGame.Tests.PlayMode.ClickShotTests.LeftClickShootsButUIClickDoesNot`
thất bại tại dòng 73: sau khi mô phỏng nhấn chuột trái, không tìm thấy `PlayerShot`
(`Expected: not null; But was: null`). Chạy riêng bài này vẫn thất bại (0/1).

- Lỗi tương tự đã được ghi trong `Docs/Verification/MAP02_V3_VERIFICATION.md`, liên quan input
  giả lập khi chạy batch. Lần kiểm tra này xác nhận tái hiện, chưa khẳng định
  nguyên nhân gốc chỉ từ kết quả assert.
- Builder đặt `clickToShoot=false`, prefab Player có `clickToShoot: 0`; tính năng
  thử nghiệm này không bật trong cấu hình game hiện tại. Không bật nó để né lỗi.
- Hai bài logic đạn còn lại đạt; menu/save, giao diện và bài truyền tống qua
  scene cũng đạt. Không thấy lỗi tải scene hay thiếu asset gây chặn việc mở game.
- Không sửa hoặc bỏ qua bài test để báo kết quả xanh. Nên xử lý bài này trước
  khi đặt yêu cầu toàn bộ PlayMode phải đạt trong quy trình tích hợp.

## Lưu ý khi mở lần đầu

- Unity tự tạo lại một số thư mục rỗng có `.meta` vì Git không lưu thư mục rỗng.
  Đây không phải thiếu sprite hay mã nguồn.
- Unity tự thay `m_RuntimeSettings.m_List` thành danh sách rỗng trong
  `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` ở bản clone.
  Đã đối chiếu mã package: `RenderPipelineGraphicsSettingsContainer` xóa danh
  sách runtime khi deserialize trong Editor, dùng danh sách Editor riêng và
  sinh lại danh sách runtime khi build. Không sửa tay asset này ở dự án gốc.
  Xem diff trước khi commit, không gộp thay đổi tự sinh vào tính năng một cách
  máy móc.
- Có cảnh báo từ package Cinemachine về mẫu HDRP và lỗi truy cập cache biên
  dịch; Unity vẫn biên dịch thành công và EditMode đạt. Không tuyên bố Console
  hoàn toàn không có cảnh báo.
- Sau khi mở/test, bản clone cũng có thay đổi tự sinh trong
  `ProjectSettings/ProjectSettings.asset`: danh sách icon Android rỗng được
  khai báo thêm và define `SENTIS_ANALYTICS_ENABLED` bị bỏ. Không áp dụng các
  thay đổi này sang bản gốc; cần review riêng nếu chúng xuất hiện trên máy khác.
- Một số scene thử nghiệm báo thiếu AudioListener. Không coi log test là sạch
  cảnh báo chỉ vì một bài test đạt.

## Bằng chứng cục bộ

- `Logs/DownloadCheck-20260928-validate.log`.
- `Logs/DownloadCheck-20261009-edit.xml` và `.log`.
- `Logs/DownloadCheck-20261009-play.xml` và `.log`.
- `Logs/DownloadCheck-20261009-click.xml` và `.log` cho lượt tái hiện riêng.
- Bản clone kiểm tra: `Logs/DownloadCheck-20260928`.

Log, ảnh và cache kiểm tra được giữ cục bộ, không commit lên GitHub. Không thay
đổi builder/content trong đợt này, không chạy builder ghi đè dữ liệu đã tải.

## Điều kiện để thành viên khác làm tiếp

1. Kho hiện là **private**. Chủ kho mời đúng tài khoản GitHub; người nhận chấp
   nhận lời mời trước khi clone. Kết quả clone của chủ kho không chứng minh
   tài khoản khác đã có quyền.
2. Cài Git, Unity Hub và Unity đúng phiên bản nêu trên. Lần đầu cần mạng để
   tải package; Git phải có trong PATH cho package Unity MCP.
3. Clone bằng chính tài khoản Windows của mình; không chép thư mục `.git`
   thuộc tài khoản khác. Lỗi ownership ở bản gốc không xuất hiện ở bản clone mới.
4. Chờ import/compile; mở MainMenu, chạy validator và Test Runner theo
   `CONTRIBUTING.md`. Không cần dựng lại content chỉ để mở dự án.
5. Tạo nhánh riêng, sửa code, kiểm tra rồi push nhánh và mở Pull Request.

Không cần bản Windows đóng gói để phát triển. Không thay đổi quyền riêng tư,
không mời tài khoản mới và không chia sẻ thông tin đăng nhập trong lần kiểm tra.
README, CONTRIBUTING và báo cáo này bổ sung hướng dẫn tải và tiếp tục phát triển.
Mã game đã kiểm tra là commit nêu trong mục Nguồn được kiểm tra; đợt cập nhật
tài liệu này không thay đổi mã game hoặc tài nguyên Unity.

## Cách lặp lại kiểm tra

Clone vào thư mục mới và dùng đúng Unity trong `ProjectVersion.txt`. Không chép
cache dự án cũ vào thư mục này. Với command line, chỉ chạy khi thư mục kiểm thử
không đang được mở bởi một Unity khác:

- Validator: `-batchmode -projectPath <thu-muc-clone> -executeMethod
  TopDownGame.EditorTools.TopDownProjectValidator.ValidateProjectCommandLine
  -quit -logFile <duong-dan-log>`.
- EditMode: `-batchmode -projectPath <thu-muc-clone> -runTests
  -testPlatform EditMode -testResults <ket-qua.xml> -logFile <duong-dan-log>`.
- PlayMode: như trên, thay `EditMode` bằng `PlayMode`. Không dùng `-nographics`
  vì một số bài test cần render ảnh. Không thêm `-quit` vào lệnh chạy test.

Các dòng trên là tham số truyền cho Unity.exe; thay đường dẫn trong `<...>`
bằng đường dẫn thật và đặt trong dấu nháy nếu có khoảng trắng. Chạy lần lượt,
đợi Unity thoát trước khi bắt đầu lượt kế tiếp. Kết quả test dùng save key
`TopDownGame.Tests.*`, không xóa save thật của người chơi.
