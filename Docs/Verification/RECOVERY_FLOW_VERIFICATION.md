# Hồi phục sau thất bại — U5 (22/10/2026)

## Phát hiện và tiêu chí

Đợt trước đã kiểm tra hình thức và khóa modal màn thua, chưa đi đủ nhánh hồi
phục qua lần gục thật. Mã cũ chỉ gọi PrepareRetry từ nút Thử lại; không chụp
stats/inventory ngay lúc chết. AddExperience cập nhật PlayerStats nhưng không
đồng bộ PlayerLevel/XP trong manager cho đến lần capture tiếp theo.

Hai bài tái hiện trên bản cũ đã thất bại:

- Nhận XP lên cấp 2, thêm kỹ năng, rồi gục: checkpoint vẫn ở cấp 1.
- Gọi Restart khi bảng đang đóng: scene vẫn bị nạp lại và có thể ghi save.

Bằng chứng: `Logs/Recovery_Reproduction.json`, job
`b1eee7f292d74f53beb75a7ce1b94580`. Lỗi teardown kèm theo trường hợp nạp lại
được xử lý trong fixture bằng cách đợi queued scene load ổn định trước unload.

Tiêu chí: tiến trình đã nhận được giữ sau gục và reload manager; hồi đầy tài
nguyên ở avatar mới; có đường về sảnh trực tiếp; không callback ẩn/chuyển lặp;
modal/focus đúng; UI đủ chữ, vừa màn hình ở ba tỷ lệ.

## Thay đổi runtime

- Tách phần chụp field của CapturePlayer thành UpdatePlayerSnapshot dùng chung.
  CapturePlayer thông thường giữ hành vi lưu cũ. API CaptureDefeatedPlayer chỉ
  nhận avatar đã gục, chụp stats/skill/loadout và giữ các collection manager,
  sau đó lưu CurrentHealth=stats.MaxHealth và CurrentMana=stats.MaxMana.
- GameOverUI gọi API này khi nhận Died, trước khi người chơi chọn hồi phục.
  Avatar trong scene hiện tại vẫn IsAlive=false, movement tắt, health HUD=0.
  Full HP/mana chỉ được ApplyToPlayer cho avatar của scene tiếp theo.
- Lưu cả cấp/XP/điểm tiềm năng, vàng, skill/slot, trang bị, tinh hoa, kho và vùng
  đã mở hiện có. Không đổi schema/key v1, catalog, giá đồ hoặc balance. Không
  sửa builder/content; scene/prefab không được chỉnh tay.
- Ba nút hiển thị rõ: **THỬ LẠI KHU VỰC**, **VỀ SẢNH CHUẨN BỊ**, **VỀ MÀN HÌNH
  CHÍNH**. Tên vùng xuất hiện ở đầu; mô tả rõ phần giữ lại và đích của mỗi nhánh.
- Khung 740×780, fit theo design bounds 900×940. Nút Thử lại giữ focus mặc định;
  các lựa chọn dùng navigation/Submit của EventSystem, đồ họa nền bị suspend.
- IsOpen/leaving chặn hành động khi bảng đóng, component disable, modal khác
  đang sở hữu input hoặc đã bắt đầu rời scene. Disable/destroy dọn quyền pause
  và focus; enable lại khi player còn chết mở lại màn thua.
- Probe bố cục menu cũ chuyển sang disable/enable controller để cleanup đúng
  vòng đời, thay vì tự sửa active panel, focus và GameplayState ngoài controller.

Backup trước sửa: `Logs/RecoveryFlowBackup/`.

## Kiểm chứng tập trung

Sáu RecoveryFlowTests và một regression menu có sẵn:

1. Gục thật sau XP/cấp/skill mới; checkpoint được cập nhật ngay. Submit Thử lại
   reload đúng map, giữ XP/vàng/skill/slot/tinh hoa/boss marker, hồi đầy tài nguyên.
2. Callback khi đóng và CaptureDefeatedPlayer với avatar sống không ghi save,
   nạp lại map hoặc rời gameplay.
3. Về sảnh qua pointer click giữ weapon đang mặc, đồ trong kho, XP/vàng/skill,
   vùng đã mở và tutorial; modal khác chặn cả ba nhánh; gọi lặp chỉ load Lobby một lần.
4. Về menu qua Submit, hủy và tạo lại GameProgressManager từ key test, rồi Tiếp
   tục thật: cấp/XP/vàng/skill giữ nguyên, avatar Lobby sống và đầy HP/mana.
5. Show lặp không ghi lại; disable từ màn thua dọn pause/focus, callback disable
   bị chặn, enable lại vẫn giữ avatar chết và trả quyền modal màn thua.
6. Gục thật tại Map01 và BossMap01; ba lựa chọn navigation ở trong modal; render
   1920×1080, 1280×800, 1024×768: text không tràn, font ≥14 pixel vật lý, nút trong canvas.
7. Regression cài đặt/Pause/màn thua/kho/phần thưởng ở ba tỷ lệ tiếp tục đạt.

**7/7 đạt**, 11,72 giây; `Logs/Recovery_Focused_Final.json`, job
`0d7da4e5e0544cedae5d024f140eb7b3`.

Sáu ảnh `Assets/Screenshots/HoiSinh_{Map01|BossMap01}_*_Verified.png`; đã xem
Map01 4:3 và BossMap01 1080p. Những ảnh ThatBaiUX cũ cũng được test tạo lại.
Save test và trạng thái automation được snapshot/restore, không dùng DeleteAll.

## Regression cuối

| Gate | Kết quả bản cuối | Bằng chứng |
| --- | --- | --- |
| EditMode | 78/78 đạt, 16,53 giây | `Logs/Recovery_EditMode_Final.json`, job `3db16fb725e5401d976022e9fe7007df` |
| Validator | IsValid=true, 0 lỗi | `Logs/Recovery_Validator_Final.json` |
| PlayMode | 151/151 đạt, 0 failed/skipped, 314,46 giây | `Logs/Recovery_PlayMode_Final.json`, job `2bf143363d9c4c189ec00937bad9f51c` |
| Console/Editor | 0 lỗi, 4 warning; MainMenu; không Play/compile; timeScale=1 | `Logs/Recovery_EditorFinal.json` |

Bốn warning native vẫn là Music_Exploration, Ambient_Forest, Ambient_Cave thiếu
clip và AudioMissingClipProbe chủ động của test. Console reader còn trả log
no-audio-listener ở scene probe và cleanup Test Runner. Không có lỗi mới về
scene load hoặc save. Scoped `git diff --check` đạt.

Không sửa runtime/test sau lượt full green. Không chạy lại builder vì không
thay content/builder trong đợt này. Giữ U5/T3 đang mở cho các phần chưa nghiệm thu.

## Giới hạn và theo dõi tiếp

- Recreate manager xác nhận deserialize trong cùng tiến trình Unity; chưa thay
  cho chạy/đóng/mở Windows player thật và toàn bộ vòng chơi với save cũ/mới.
- Không đổi quy tắc respawn quái hoặc rương theo lượt vào map; không cân bằng
  lại HP/mana thử nghiệm. U5/T3 vẫn chưa nghiệm thu toàn bộ.
- Ngoài đợt này, MapDoor vẫn dùng scene id trong prompt và có chuỗi khóa tiếng
  Anh; cần đồng bộ tên vùng như picker Lobby. LoadProgress cũng chưa có luồng
  thông báo/phục hồi khi save bị lỗi. Đây là các điểm cần rà soát tiếp trong goal.
