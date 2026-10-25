# Rà soát giao diện và luồng trải nghiệm — 22/10/2026

## Phạm vi và cách đánh giá

Đợt U5 này kiểm tra điểm vào game, quyền sở hữu modal, cài đặt, tạm dừng,
thất bại/thử lại, kho trang bị và nhận kỹ năng từ rương. Kết luận dựa trên
mã nguồn, scene được sinh, test runtime và ảnh render. Không chấm điểm cảm
tính hoặc coi ảnh đẹp là bằng chứng toàn bộ game đã sẵn sàng phát hành.

## Phát hiện và thay đổi

| Mức độ | Phát hiện | Xử lý |
|---|---|---|
| P1 | Catalog phần thưởng có chín reference null trong scene, trong khi asset kỹ năng còn tồn tại | Builder tải lại SkillData persistent trước khi tạo canvas; validator kiểm tra null, trùng và thiếu SkillId |
| P1 | Kho chỉ vô hiệu hóa movement; đánh/kỹ năng/pause vẫn có thể nhận input | Dùng GameplayState và focus scope chung, Esc chỉ đóng kho |
| P1 | Nhận kỹ năng tự đặt timeScale về 1; chưa lưu lựa chọn ngay | Pause theo owner, chặn submit lặp, capture tiến trình sau chọn |
| P2 | Chọn thưởng có thể rơi vào skill đã nâng tối đa | Chỉ lấy skill còn nâng được; có nút tiếp tục khi toàn catalog đã tối đa |
| P2 | Menu con chưa cô lập navigation của màn phía sau | CanvasGroup tạm ngắt tương tác nền, trả keyboard focus khi đóng |
| P2 | Menu chính/pause/màn thua dùng bố cục và kích thước khác nhau | Dùng khung tối viền đồng, nút chính rõ ràng, fit theo kích thước cửa sổ |
| P2 | Cài đặt âm thanh chỉ có ở pause, nhãn tiếng Anh, thiếu giá trị số | Cửa sổ chung MainMenu/Pause; bốn slider tiếng Việt và phần trăm |
| P2 | Chưa có luồng đổi hiển thị và hoàn tác | Chọn chế độ/độ phân giải; preview 15 giây unscaled, giữ hoặc hoàn tác; đóng cửa sổ cũng hoàn tác |
| P2 | Cường hóa không đóng bằng Esc như các bảng khác | Bổ sung Esc và giữ chốt ModalClosedThisFrame của GameplayState |

Các sửa đổi UI được ghép bởi controller runtime theo kiến trúc UI sẵn có.
Scene/prefab/catalog được dựng bằng builder, không sửa YAML hoặc Editor bằng tay.
Đợt menu/kho/phần thưởng không đổi schema save tiến trình, chỉ số chiến đấu
hoặc hình nhân vật/map. Đợt tutorial tiếp theo bổ sung hai field trạng thái
và migration tương thích save cũ như mô tả bên dưới.

## Quy tắc trải nghiệm sau sửa

- Có save: Tiếp tục là thao tác chính, thông báo trở về sảnh. Chưa có save:
  Chơi mới là thao tác chính, Tiếp tục bị khóa và giải thích bằng chữ.
- Chơi mới có save vẫn cần xác nhận; mặc định focus vào Hủy.
- Đóng cài đặt/hướng dẫn từ Pause vẫn giữ game tạm dừng; Esc tiếp theo mới resume.
- Đổi hiển thị chưa xác nhận không ghi preference. Hết hạn hoặc đóng cửa sổ
  trả về cấu hình trước. Thao tác an toàn mặc định là Hoàn tác.
- Âm lượng dùng các key Audio cũ. Hiển thị dùng `TopDownGame.Display.*`, tách
  khỏi tiến trình. New Game không xóa các key này.
- Test âm thanh/hiển thị dùng `TopDownGame.Tests.*`, không sửa cài đặt thật.
- Kho cho biết cất trang bị đang mặc sẽ tháo món đó. Hai phía có danh sách cuộn.
- Phần thưởng yêu cầu chọn một kỹ năng để tiếp tục; hướng dẫn này được ghi ngay
  trên cửa sổ. Kỹ năng mới tự vào ô trống, có thể đổi lại bằng K.

## Kiểm chứng

Kết quả trên bản cuối sau sửa catalog:

- Build Complete MVP: hai lượt hoàn tất; validator sau lượt hai đạt, 0 lỗi.
- 87 reference GUID trong catalog/prefab giữ nguyên qua hai lượt; không thêm
  asset trùng theo path. Builder cập nhật metadata Unity và sinh lại scene;
  không thay bitmap art. Project Settings có sẵn được giữ nguyên theo diff.
- EditMode: **77/77 đạt** sau hai lượt dựng, gồm validator mở rộng và font.
- PlayMode: **93/93 đạt**, 0 failed/skipped, trên bản cuối sau hai lượt dựng.
  Kết quả: `Logs/UXReview_PlayMode_Final.json` và `Logs/UXReview_EditMode_Final.json`.
- Đã xem ảnh phần thưởng thật (ba thẻ kỹ năng), cài đặt và kho ở 1024×768;
  không thấy chữ tràn hoặc nút ngoài khung.

Tiêu chí:

- Hai lượt Build Complete MVP, reference/GUID catalog không nhân đôi.
- Validator với kiểm tra catalog phần thưởng mới.
- Toàn bộ EditMode và PlayMode.
- Test Esc qua cài đặt/kho, quyền pause, focus, lưu độc lập với New Game,
  hoàn tác preview, chuyển đồ, lưu/khôi phục skill và submit lặp.
- Ảnh 1920×1080, 1280×800, 1024×768: menu, cài đặt, preview, pause, thất bại,
  kho và nhận kỹ năng; kiểm tra text overflow, bounds và fill của slider.

Các lần kiểm tra đầu bắt được fill/handle slider sai kích thước, cleanup
serialized null của Unity và catalog thưởng rỗng; các lỗi này đã được sửa.
Các test quyền modal và ảnh chạy bằng scene thật, save/cài đặt test riêng.

## Phần còn thiếu được ưu tiên tiếp

| Ưu tiên | Hạng mục | Điều kiện hoàn thành tiếp theo |
|---|---|---|
| P1 | T2 — cân bằng chiến đấu | Bỏ cấu hình thử nghiệm 50.000 HP/25.000 mana theo bảng before/after và playtest ba loadout |
| P2 | U4 — hướng dẫn lần đầu | Sáu bước đã có chỉ dẫn né đòn Space/gamepad và giới hạn miễn đòn; còn đánh giá với người chơi mới |
| P2 | U1 — âm thanh theo vùng | Hệ thống phát/fade/pool đã qua test; cần clip thật và nghiệm thu theme từng map |
| P2 | U2/U3 — camera feedback | Đã có shake/hitstop đúng pause và hai tùy chọn tắt; U3 đạt kỹ thuật, U2 còn Windows |
| P2 | T3 — kiểm tra bản Windows | Đổi màn hình/độ phân giải thực, xác nhận/timeout/rollback, đóng mở app và toàn vòng chơi với save cũ/mới |

Lượt này không xác nhận toàn bộ U2/U5/T3 hoàn tất. Đổi kích thước cửa sổ được
bỏ qua trong Editor; cần kiểm tra Screen.SetResolution trên Windows player.

## U4 tiếp theo — đã kiểm chứng onboarding theo binding hiện tại

Sau kết quả U5 ở trên, đã thêm `TutorialGuide`, `LobbyTutorialUI` và reference
trong builder/validator. Sáu bước có Quay lại/Tiếp theo/Bỏ qua, không tự chuyển
theo thời gian; nội dung khớp di chuyển, tự đánh, Q/E/R/F, ưu tiên E tương tác,
I/P/K, truyền tống và lưu từ Pause. Chưa có chỉ dẫn dodge vì G1 chưa hoàn tất.

Quy tắc save:

- Chơi mới: `tutorialInitialized=true`, `tutorialCompleted=false`; tự mở ở Lobby.
- Hoàn tất hoặc Bỏ qua/Esc: lưu ngay trạng thái đã xem, không capture lại loadout.
- Continue: giữ trạng thái. Save v1 cũ thiếu field được migrate là đã xem để
  không ép người chơi cũ qua tutorial; progression còn nguyên.
- Xem lại từ Hướng dẫn MainMenu/Pause không sửa tiến trình.
- Teardown/disable/chuyển scene không được tính là người chơi chủ động bỏ qua.

Thêm sáu PlayMode test cho entry/skip/continue/new game, migration, interruption,
replay/focus/pause, chờ modal khác và render cả sáu bước ở ba tỷ lệ màn hình.
Fixture menu và scenery chọn trạng thái đã xem, còn fixture tutorial đi qua entry
thật; không bỏ qua onboarding theo cờ automated test trong runtime.

Kiểm tra hiện có: bốn assembly Runtime/Editor/PlayModeTests/EditModeTests compile
độc lập thành công bằng compiler/reference của Unity; logs ở `Logs/TutorialCompile/`.
Sau khi người dùng đồng ý dừng Play, đã refresh và dựng hai lượt; validator cả
hai đạt, 0 lỗi (`Logs/Onboarding_Build1.done`, `Logs/Onboarding_Build2.done`).
94 GUID catalog/prefab không đổi và Project Settings giữ nguyên theo hash.

EditMode **77/77 đạt**, PlayMode **105/105 đạt** (0 failed/skipped), gồm sáu
test tutorial và sáu test audio. Kết quả trong `Logs/Onboarding_EditMode.json`,
`Logs/Onboarding_PlayMode.json`. Đã sinh 18 ảnh sáu bước × ba tỷ lệ; xem ảnh bước
1 ở 1920×1080, bước 4 ở 1280×800, bước 6 ở 1024×768 và Help 4:3: đủ chữ/nút,
không thấy tràn. Không đánh dấu toàn bộ U4 theo roadmap vì còn phụ thuộc G1/dodge.

## U1 — sửa lỗi audio đã qua test runtime

Trong lúc giữ nguyên phiên Play của người dùng, đọc mã xác nhận ba lỗi:

- `CrossfadeLoop` giữ target volume từ đầu fade, ghi đè slider vừa chỉnh ở frame
  sau; RefreshPlayingVolumes cũng bỏ qua hệ số fade.
- Request chuyển nhạc đến trước khi fade cũ xong có thể để lại loop không được
  giải phóng; request cùng theme khởi động clip lại.
- `AcquireVoice` kích hoạt 18 voice có sẵn nhưng trả voice mới ở trạng thái tắt
  khi mở rộng pool (voice 19–32).

Đã sửa bằng hai channel Music/Ambient có Current/Outgoing và một transition mỗi
channel; tách FadeGain khỏi âm lượng bus, chỉ qua một đường ApplyVolume. Request
cùng cue giữ source/playback; request mới hủy transition và giải phóng outgoing
cũ. Voice mới được bật, pool vẫn cap 32. Loop không giảm nhầm concurrency count
của one-shot dùng cùng cue. Reset pitch loop về 1. Cue có entry null chọn trong
clip hợp lệ, toàn bộ thiếu clip cảnh báo một lần mỗi cue/manager.

`AudioFlowTests` bổ sung sáu ca: giữ source/playback; mute/đổi bus trong fade khi
pause; chuyển nhanh 50 lần; kích hoạt đủ 32 voice và cap; giới hạn one-shot khi
loop cùng cue bị release; thiếu clip/mảng trộn null. Chỉ dùng clip tạo trong test,
không thêm asset nhạc. Bốn assembly compile độc lập đạt sau sửa (logs cập nhật
trong `Logs/TutorialCompile/`). Cả sáu test audio đã đạt trong lượt **105/105
PlayMode** nêu trên. Clip test tổng hợp chỉ xác nhận hành vi phát/fade/pool;
không thay thế nghiệm thu chất lượng âm thanh thật.

Nhạc và ambient Resources hiện vẫn có `clips: []`; hai WAV chém kiếm V3 là âm
thanh được builder tổng hợp riêng. Việc sửa hệ thống không chứng minh game đã
có soundtrack hoặc audio theo vùng hoàn chỉnh. Giữ U1 đang làm và cần nghiệm
thu âm thanh thật khi có clip phù hợp.

## U5 tiếp theo — chọn vùng truyền tống đã kiểm chứng

Phát hiện tên vùng trong picker lệch MapData và thiếu focus/default selection,
background keyboard navigation chưa bị khóa vì picker dùng canvas riêng.
Đã thêm catalog MapData vào portal qua builder; validator kiểm tra null/trùng/
thiếu vùng. Tên ba vùng còn tiếng Anh được đổi trong builder sang Đền Kỹ Năng,
Lãnh Địa Vệ Thần, Hang Pha Lê. Scene id, điều kiện mở khóa và save không đổi.

Picker hiển thị tên/cấp đề nghị/trạng thái bằng chữ, có giải thích mở vùng mới
qua cửa ra vùng trước. Mặc định focus Ở lại sảnh; scope khóa cả canvas khác và
khôi phục focus khi đóng. Đã thêm hai test luồng thật, Esc, duplicate Open,
navigation và ảnh khóa/đã mở ở ba tỷ lệ.

Hai lượt builder cuối đạt validator, 0 lỗi (`Logs/TravelFinal_Build1.done`,
`Logs/TravelFinal_Build2.done`), 94 GUID không đổi, Project Settings cùng hash
bản backup. EditMode 77/77 đạt (`Logs/Travel_EditMode_Final.json`). Sau đó chỉ
sửa runtime label thành ba dòng và fixture render, không sửa builder/content;
chạy lại EditMode trên source cuối vẫn **77/77 đạt**, 0 failed/skipped
(job `6c4a09823d884116bd4a072fd713d390`, cùng file kết quả).

Các lượt đầu bắt được helper Set/SetArray dùng sai trong builder và fixture lấy
Canvas tại HUD thay vì cha; đã sửa. Ảnh đầu cho thấy trạng thái ĐÃ/CHƯA MỞ bị
ngắt dòng vụn dù chưa overflow; đã tách tên/cấp/trạng thái thành ba dòng cố định
và test kiểm tra lineCount. Hai test tập trung cuối đạt 2/2
(`Logs/Travel_Focused_Final.json`). Đã xem ảnh khóa 1024×768 sau sửa, đủ chữ và
ba dòng rõ ràng; đã xem thêm ảnh đã mở 1920×1080 trên bản cuối.
Toàn bộ **107/107 PlayMode đạt**, 0 failed/skipped, trong 228,24 giây
(`Logs/Travel_PlayMode_Final.json`). Lượt lỗi fixture ban đầu không được dùng
làm bằng chứng hoàn tất.

Editor cuối ở `Assets/Scenes/MainMenu.unity`, không Play/compile. Bộ đếm native
Console ghi 0 error, 3 warning: các cue nhạc/ambient thiếu clip và probe thiếu
clip chủ đích của test. Công cụ đọc Console còn trả thông báo thiếu AudioListener
ở scene test và log Test Runner; không coi các log này là exception runtime.
`git diff --check` mã nguồn/test/docs đạt; kiểm tra toàn worktree còn whitespace
ở YAML scene do Unity sinh và ProjectSettings có sẵn, không sửa tay các file này.

## Đánh giá sau đợt rà soát

- Luồng vào game, menu con, hướng dẫn, kho, nhận thưởng và chọn vùng có quy tắc
  focus/Esc/pause rõ ràng, được kiểm tra trên scene thật và save test riêng.
- Hướng dẫn có thể đọc theo nhịp người chơi, bỏ qua và xem lại; chọn vùng lấy
  thông tin từ MapData thay vì lặp tên/cấp trong UI. Có 24 ảnh tutorial/chọn vùng
  ở 16:9, 16:10 và 4:3, bên cạnh bộ ảnh menu/kho/phần thưởng trước đó.
- Rủi ro trải nghiệm lớn nhất còn lại là cấu hình thử nghiệm 50.000 HP/25.000
  mana làm mất sức ép chiến đấu. T2 cần bảng before/after và ba loadout trước
  khi thay số. Nhạc/ambient còn thiếu clip, shake/hitstop/dodge chưa hoàn thiện.
- Chưa nghiệm thu Windows player: đổi hiển thị thật, restart, rollback và toàn
  vòng chơi với save cũ/mới. Giữ U1/U2/U4/U5 đang làm, không xác nhận release.

## U5 — lò rèn, giao dịch và bố cục màn hình hẹp

Đọc `WeaponShopUI` phát hiện lựa chọn món hủy/tạo lại toàn bộ thẻ, mất keyboard
focus. Cửa hàng chưa cô lập navigation các canvas nền. Lượt đổi ưu đãi dùng
`rotation * 7 % count`, nên khi còn bảy món sẽ lặp đúng cùng ưu đãi dù thu phí.
Phí đổi lượt cập nhật manager nhưng chưa ghi save ngay.

Đã sửa:

- Giữ nguyên card khi lựa chọn/mua/đổi giá; chỉ dựng lại khi thay nhóm hàng.
  `MenuModalFocus` cô lập canvas nền, mặc định chọn tab nhóm hiện tại và trả
  focus khi Esc/disable. Nút giao dịch không nhận lệnh khi đã đóng cửa hàng.
- Mỗi nhóm còn ít nhất hai món chuyển sang món kế tiếp khác lượt trước. Khi
  toàn bộ nhóm còn tối đa một món, khóa đổi lượt để không thu phí vô ích.
  Phí 100 vàng lưu ngay bằng luồng CaptureActivePlayer sẵn có.
- Theo dõi GoldChanged để cập nhật giá/nút; giải thích số vàng thiếu. Mua thành
  công chuyển sang ĐANG TRANG BỊ; món đã có được trang bị lại miễn phí.
  Chọn món khác xóa thông báo mua cũ để tránh hiểu nhầm món đang chọn.
- Màn hình hẹp hơn tỷ lệ 1,5 dùng hai cột hàng hóa/chi tiết, ẩn chân dung và
  tăng diện tích đọc; màn hình rộng giữ ba cột. `ScrollSelectionIntoView` đưa
  card nhận focus bàn phím vào viewport khi cần cuộn.

Đây là thay đổi runtime UI, không sửa builder, catalog, scene hay schema save.
Bản sao trước sửa: `Logs/ShopReviewBackup/WeaponShopUI.cs`.

Thêm bốn `ShopFlowTests` trên Lobby thật: focus/Esc/exclusivity, mua giá giảm/
trang bị lại/lưu/thiếu vàng, đổi ưu đãi bảy lựa chọn và không thu phí khi không
có phương án khác, render/bounds/text/auto-scroll. Kiểm tra từng món trong cả
năm nhóm đã bắt được tên Dây Chuyền Chiến Binh bị cắt; tăng vùng tên lên hai
dòng và tách giá xuống dưới. Năm bài tập trung (gồm test hành trang/lò rèn cũ)
đã đạt (`Logs/Shop_Focused_Final.json`). Chín ảnh ba trạng thái × ba tỷ lệ và
năm ảnh nhóm hàng ở 4:3 được sinh;
đã xem ưu đãi 1080p, đã mua 1280×800 và thiếu vàng 4:3 trước/sau bố cục hai cột.
Đã xem thêm nhóm dây chuyền 4:3: đủ tên dài, giá không đè chữ.
Lượt regression đầu 111 bài có một assertion cũ đo cả Surface trong suốt
1920 px thay vì cửa sổ hiển thị. Đã đổi sang đo bốn góc cửa sổ/bóng và mọi nút
active so với panel, test cũ đã đạt trong lượt tập trung. Source cuối đạt:

- **77/77 EditMode**, 0 failed/skipped (`Logs/Shop_EditMode_Final.json`).
- **111/111 PlayMode**, 0 failed/skipped, 232,14 giây
  (`Logs/Shop_PlayMode_Final.json`, job `34c97937dbc44789a5da2452c0b5dbe1`).
- Validator đạt, 0 lỗi (`Logs/Shop_Validator.json`). Builder/content không đổi,
  nên không dựng lại content trong đợt lò rèn này.
- `git diff --check` cho mã nguồn/test/docs đạt. Không dùng lượt lỗi đầu làm
  bằng chứng hoàn tất; logs lỗi giữ ở `Logs/Shop_PlayMode_Initial.json` và
  `Logs/Shop_NameOverflow_Failed.json`.
- Native Console cuối có 0 error, 3 warning liên quan cue thiếu clip đã ghi
  trong manifest/test chủ đích. Editor ở MainMenu, không Play/compile
  (`Logs/Shop_EditorFinal.json`). Bộ đọc Console còn trả thông báo AudioListener
  của scene test và log cleanup; không khẳng định audio content đã hoàn thiện.

Kiểm tra tiếp theo của U5: hành trang cũng còn dựng lại slot khi chọn món,
chưa có focus scope; cường hóa còn dùng bảng chữ và thiếu lựa chọn slot bằng
chuột. Những luồng này chưa được tính là đã xử lý trong đợt lò rèn.

## U5 — hành trang và cường hóa (đã kiểm chứng)

### Phát hiện

- Hành trang hủy/tạo lại ô túi và ô đang mặc khi chọn đồ, lọc và sắp xếp;
  keyboard focus có thể trỏ vào đối tượng đã bị hủy. Chưa có focus scope.
- Lọc nhóm trống vẫn giữ chi tiết món cũ; mở lại với bộ lọc cũ cũng có thể
  chọn vũ khí không thuộc nhóm đang xem. Chỉ số nhân vật dùng lineSpacing 14
  trong vùng cao 166 px, không đủ chỗ cho sáu dòng ở cỡ chữ đã cấu hình.
- Cường hóa chỉ cho chọn ô qua phím trong một bảng chữ. Không xem trước chỉ
  số cấp tiếp theo; không giữ thông báo kết quả TryEnhanceEquipped.
- Thiếu OnDisable cleanup; destroy controller không dọn panel/launcher là
  sibling của controller. Hai bảng chưa khóa navigation của canvas nền.

### Thay đổi

- `FantasyEquipmentUI.RefreshItemSlot` cập nhật icon/rarity/badge/trạng thái
  trên ô đã tạo. Hành trang giữ cache ô đồ, ô trống và năm ô đang mặc; lọc/sắp
  xếp thay active/order, không hủy focus. Chọn món qua ô đang mặc cũng giữ ô.
- MenuModalFocus, lựa chọn mặc định, trả focus/khôi phục movement và subscription
  theo vòng đời. Không nhận lệnh trang bị/cường hóa khi bảng đóng/controller tắt.
  Khôi phục focus hợp lệ khi Unity tự bỏ chọn nút vừa bị khóa sau giao dịch.
- Lọc trống xóa chi tiết, mở lại giữ đúng nhóm. Cuộn đưa ô có focus vào viewport
  cả sau sắp xếp. Chú giải E là dấu đang mặc, không mô tả thành phím trang bị.
- Hành trang ở tỷ lệ dưới 1,5 cho chuyển giữa Túi đồ và Nhân vật & Chỉ số;
  chi tiết món được chọn giữ bên phải. Màn rộng giữ ba cột. Giảm lineSpacing
  bảng chỉ số để đủ sáu dòng, không giảm dữ liệu hoặc thay số chiến đấu.
- Cường hóa có năm thẻ chọn bằng chuột/phím số, xem trước năm chỉ số và sức
  mạnh hiện tại/cấp tiếp theo, chi phí và Tinh hoa còn lại/thiếu. Ô trống và
  cấp +20 có trạng thái riêng. Nút/Enter và U không thể thu hai lần cùng frame.
  Giữ API cost/multiplier/save và HealthChange sẵn có; không đổi balance.
- Ô trống có nút Mở hành trang; I chuyển từ cường hóa sang túi đồ qua Close/Open
  của hai controller. Không để hai modal chồng lên nhau hoặc tiêu Tinh hoa.
- Tách phần khung responsive đã dùng ở lò rèn vào EquipmentPanelLayout để ba
  bảng dùng chung; lò rèn được đưa vào regression. Không sửa builder/content,
  schema save hoặc scene YAML; backup ở `Logs/EquipmentFlowReviewBackup/`.

### Kiểm tra

Sáu bài `EquipmentFlowTests` chạy trên Lobby thật với key save test riêng:
focus/cache/lọc/sort/cuộn/Esc; equip/unequip/save/lọc trống/disable; chọn ô
chuột/phím số và modal; preview/giao dịch một lần/thiếu Tinh hoa/lưu; +20 và
disable/reenable/destroy cleanup; render các trạng thái ở ba tỷ lệ. Các lượt
tập trung đã đạt 11/11 gồm regression hành trang cũ và bốn bài lò rèn. Lượt
đầu bắt được focus null sau nút bị khóa, đã sửa theo hành vi native của Unity;
bằng chứng lỗi ở `Logs/EquipmentFlow_Focused_Initial.json`.

Test viewport thấp bắt được sort đặt verticalNormalizedPosition về đầu sau khi
Reveal vừa cuộn tới ô có focus. Đã đổi thứ tự reset trước refresh; assertion
phần tử phải nằm trong viewport giữ nguyên và đã đạt. Log lỗi:
`Logs/EquipmentFlow_SortScroll_Failed.json`. Sáu bài trên source cuối đạt 6/6,
bao gồm chuyển cường hóa → hành trang (`Logs/EquipmentFlow_Focused_SourceFinal.json`).

Đã xem cường hóa sẵn sàng/ô trống ở 1024×768, thành công ở 1280×800, +20 ở
1080p, nhân vật/túi đầy ở 4:3 và hành trang 1080p. Có 23 ảnh: tám ảnh túi/nhân
vật, 15 ảnh cường hóa. Nút mở hành trang trên ô trống nằm gọn trong khung.

- **77/77 EditMode**, validator đạt 0 lỗi (`Logs/EquipmentFlow_EditMode_Final.json`,
  `Logs/EquipmentFlow_Validator.json`).
- **117/117 PlayMode**, 0 failed/skipped, 239,71 giây
  (`Logs/EquipmentFlow_PlayMode_Final.json`, job `98066077a18c4bf68874906b8d32c675`).
- Kết quả này thuộc source trước đợt kỹ năng/tiềm năng và sửa tutorial dưới đây;
  đợt tiếp theo có kiểm chứng riêng.

## U5 — kỹ năng và tiềm năng (đã kiểm chứng)

### Phát hiện và thay đổi

- Hai bảng chưa có focus scope hoặc lựa chọn mặc định, navigation có thể sang
  nút nền. Thêm MenuModalFocus, trả focus khi đóng, khóa mở controller đã tắt,
  unsubscribe/subscribe khi disable/reenable và dọn panel/launcher khi destroy.
- Thư viện kỹ năng hủy/tạo lại thẻ khi equip/upgrade. Chuyển sang cache theo id,
  cập nhật cấp trên thẻ, giữ lựa chọn và cuộn thẻ có focus vào viewport.
- Enter trước đây vừa là phím trang bị riêng vừa đi qua EventSystem Submit.
  Mũi tên/Enter giờ dùng navigation/Submit chuẩn; 1–4 chọn ô, U nâng cấp,
  Backspace tháo. Không tự trang bị khi Enter chỉ chọn thẻ/ô.
- Chặn nâng kỹ năng/cộng tiềm năng nhiều lần trong cùng frame; không đổi cost,
  chỉ số, luật không trùng kỹ năng hoặc API save. Thông báo thành công/thiếu
  vàng/tháo đồ được giữ qua refresh; cấp kỹ năng trên ô viết tiếng Việt.
- Khung 1440×1080 chung như cường hóa: thư viện + chi tiết/bốn ô 2×2; tiềm năng
  có tổng quan và bốn thẻ 2×2. Khi tiêu điểm cuối, focus chuyển về nút đóng.
- Xem ảnh phát hiện Canvas riêng của hai bảng làm HUD vẽ đè khi render ở
  ScreenSpaceCamera. Bỏ override canvas để dùng thứ tự sibling của HUD như
  hành trang/cường hóa; đã xem lại ảnh 4:3 và xác nhận hết che nút/chữ.
- Nút nâng kỹ năng chuyển thành một hàng rộng ở cuối chi tiết, ghi đủ giá vàng;
  mô tả không lặp lại giá. Không còn nhãn nhỏ chia nhiều dòng cạnh icon.
- Tutorial bước 5 mô tả sai mua đồ chưa mặc. Sửa thành mua ở lò rèn tự trang bị,
  I dùng đổi/tháo đồ đã sở hữu. Đã kiểm tra ảnh bước 5 ở 4:3.

### Kết quả kiểm tra

Năm `ProgressionFlowTests` trên Lobby và save test riêng kiểm tra focus/modal,
thẻ bền vững/cuộn, Enter không tự equip, bốn ô/duplicate/equip/tháo/save, nâng
cấp một lần/thiếu vàng/max, bốn thuộc tính/cạn điểm/save, vòng đời và render.
Lượt đầu **11/11** gồm sáu bài tutorial đạt
(`Logs/ProgressionFlow_Focused_Initial.json`). Lượt sau sửa thứ tự Canvas,
bổ sung Enter qua InputSystem và mô tả cả chín kỹ năng cùng bốn ô đã trang bị
tiếp tục **11/11** (`Logs/ProgressionFlow_Focused_Final.json`).

Sau cùng đổi nút nâng kỹ năng thành hàng rộng: **77/77 EditMode** đạt
(`Logs/ProgressionFlow_EditMode_Final.json`), validator đạt 0 lỗi
(`Logs/ProgressionFlow_Validator.json`). Toàn bộ **122/122 PlayMode** đạt trên
source cuối, 0 failed/skipped, 243,59 giây, gồm năm bài mới và toàn bộ regression
trước (`Logs/ProgressionFlow_PlayMode_Final.json`, job
`e730f46ea684450d83f12844ff025bfe`). Không thay builder/content trong đợt này,
không cần dựng lại scene.

`git diff --check` cho mã nguồn/test/docs đạt. Native Console cuối: 0 error,
3 warning; Editor ở MainMenu, không Play/compile
(`Logs/ProgressionFlow_EditorFinal.json`). Cảnh báo nội dung âm thanh còn được
theo dõi theo manifest; kết quả UI không xác nhận U1 đã đủ clip.

Đã xem ảnh source cuối: kỹ năng đầy đủ ở 4:3, tối đa ở 1080p, tiềm năng có
điểm ở 4:3 và cạn điểm ở 1280×800. Nút nâng kỹ năng hiện giá trên một hàng,
bốn ô đủ tên/cấp, không có HUD đè lên khung. Đợt này có 15 ảnh mới
`KyNangUX_*`/`TiemNangUX_*`; 18 ảnh tutorial được render lại với câu bước 5 đúng.

### Nhận định trải nghiệm sau các đợt UI

| Luồng | Trạng thái đã kiểm tra | Phần còn cần nghiệm thu |
|---|---|---|
| Bắt đầu/tiếp tục → hướng dẫn → Lobby | Có lựa chọn mặc định, skip/replay và save riêng | Người chơi mới hiểu thao tác mà không cần giải thích ngoài game |
| Lò rèn → hành trang → cường hóa | Giao dịch/lưu/focus, lọc trống và xem trước chi phí | Chơi dài với tiến trình thật để đánh giá tốc độ nhận đồ/Tinh hoa |
| Nhận skill → quản lý bốn ô → nâng cấp | Navigation, feedback, chống trùng/chống chi tiêu lặp | Kiểm tra tốc độ đổi loadout bằng bàn phím/gamepad thực tế |
| Lên cấp → cộng tiềm năng → trở lại chơi | Bốn thuộc tính, lưu ngay và cạn điểm có đường đóng | Cân bằng sức mạnh mỗi điểm sau khi bỏ cấu hình thử nghiệm |
| Pause/cài đặt → quay lại/lưu về menu | Quyền modal, focus và hoàn tác hiển thị trong Editor | Windows player: chuyển chế độ, timeout/rollback, đóng mở ứng dụng |

UI quản lý đã nhất quán hơn về khung, điều khiển và phản hồi. Rủi ro lớn tiếp
theo nằm ở nhịp chơi: HP/mana thử nghiệm làm khó đánh giá độ khó; chưa đủ âm
thanh theo vùng và phản hồi đòn đánh. Giữ U5 đang làm cho tới nghiệm thu
accessibility/Windows và người chơi mới; không dùng test tự động để thay thế
đánh giá cảm giác chơi.

## Cập nhật G1 và U4 — né đòn đã kiểm chứng kỹ thuật

Các nhận định chưa có dodge ở các đợt trước là lịch sử. Bước 2 tutorial hiện
có Space/nút dưới gamepad, miễn đòn trực tiếp 0,18 giây đầu và giới hạn không
né giữa attack/cast. Tám bài né + sáu tutorial đạt 14/14; toàn bộ source cuối
đạt **78/78 EditMode, 130/130 PlayMode**, validator 0 lỗi, Console 0 error.
Builder đã chạy hai lượt; 18 ảnh né ba trạng thái trên Lobby/Map01 và 18 ảnh
sáu bước tutorial đã render lại. Chi tiết: `DODGE_FLOW_VERIFICATION.md`.

Đã xem Map01 1080p và 4:3, Lobby 1280×800/4:3: thẻ né không đè UI cũ.
Ưu tiên U5 tiếp theo là HUD chiến đấu: tên vùng/cấp xuống dòng sát đếm quái,
thiếu số HP/mana, thẻ kỹ năng quá lớn; chữ launcher/giúp đỡ ở 4:3 còn nhỏ.
Cần thu gọn thanh skill, làm rõ thông tin chiến đấu/cooldown và kiểm tra khả
năng đọc, giữ đủ đường mở các bảng hiện có. U3 (shake/hitstop), audio clip,
balance và Windows/accessibility vẫn chưa nghiệm thu toàn phần.
## U5 tiếp theo — HUD chiến đấu

Đã xử lý ưu tiên HUD ghi trong đợt G1: khung vùng/tài nguyên riêng, số HP/mana,
thanh bốn skill 596×180 với tên/phím/cooldown/thiếu mana/ô trống, khung boss
có số máu. Phần hiển thị không nhận raycast; modal tiếp tục sở hữu focus.
Bốn bài tập trung đạt, có 30 ảnh sáu map/ba tỷ lệ cùng loadout/trạng thái.
Source cuối và gate hồi quy được ghi ở `COMBAT_HUD_VERIFICATION.md`.

Ảnh loadout hai Map01 ở 4:3 đủ Sét Liên Hoàn/Mưa Thiên Thạch trên hai dòng;
RiftMap01 ở 1280×800 và boss 4:3 không chồng minimap/khung tài nguyên/thẻ né.
Chữ launcher, trạng thái cử chỉ và minimap cũ chưa thuộc kiểm tra cỡ chữ của
HUD mới. Ưu tiên tiếp là U3: phản hồi đòn đánh có shake/hitstop tôn trọng
setting và pause, rồi nghiệm thu người chơi mới/Windows và balance có dữ liệu.

Gate HUD cuối: **78/78 EditMode, 134/134 PlayMode**, validator 0 lỗi, Console
0 error/4 warning âm thanh. Editor ở MainMenu, không Play/compile. Các nhận định
HUD chiến đấu cần thu gọn ở các mục trước đã được xử lý trong đợt này; vẫn
giữ U5 đang làm cho các phần accessibility/launcher/Windows còn lại.

## U3 đã kiểm chứng — rung camera/hitstop và cài đặt phản hồi

Đã nối feedback sau direct damage, loại periodic/poison/hit bị né/offscreen;
rung cho đòn nặng, incoming hit, dậm/lao boss và AoE. Camera giữ vị trí gốc riêng
để smoothing không tích drift; clamp bounds, dọn khi pause/cinematic/disable/
mất target hoặc tắt setting. Hitstop dùng chung quyền thời gian GameplayState,
cap 65 ms mỗi đợt, recovery 120 ms; mở modal hủy đợt và vẫn giữ PauseOwners.

Cài đặt có Rung camera/Dừng nhẹ khi trúng lưu riêng, dùng từ MainMenu/Pause.
Bảy test mới đạt, có 12 ảnh ba tỷ lệ; đã xem source cuối ở 4:3. Toàn bộ
**78/78 EditMode, 141/141 PlayMode**, validator 0 lỗi, native Console 0 error,
timeScale=1, MainMenu không Play/compile. Chi tiết: `COMBAT_FEEDBACK_VERIFICATION.md`.
U3 đạt các tiêu chí kỹ thuật trong roadmap. U2/U5 vẫn còn kiểm tra Windows,
launcher/accessibility và người chơi mới; soundtrack, balance HP/mana thử
nghiệm và đánh giá cảm giác chơi dài chưa được bộ test này xác nhận hoàn tất.