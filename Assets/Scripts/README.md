# Top-Down Game C# Source

Toàn bộ gameplay của dự án được viết bằng C# và nằm trong thư mục này.

## Chạy và chỉnh sửa

- Chọn `Top Down Game > Open Main Menu` để mở màn hình bắt đầu.
- Nếu Unity mở `Untitled` với nền xanh, đó là scene trống, không có nghĩa mất
  dữ liệu game. `TopDownEditorStartup` tự mở MainMenu một lần mỗi phiên khi chỉ
  có một scene tạm sạch. Không tự đóng scene có thay đổi chưa lưu, scene thật,
  phiên nhiều scene hoặc batch test. Với `Untitled*`, dùng `Open Main Menu` và
  kiểm tra hộp thoại lưu trước khi quyết định bỏ scene tạm. Không cần build lại.
- Chọn `Top Down Game > Edit Map01` để mở map chính và tự căn Scene View.
- Chọn `Top Down Game > Validate Project` để kiểm tra scene đích, Build Settings,
  catalog và reference bắt buộc trước khi playtest/build.
- Trong game: WASD/phím mũi tên hoặc chuột trái để di chuyển,
  chuột phải/J đánh cận chiến. Đã khôi phục tự đánh mặc định và tương tác nhấp đúp.
  Click bắn thử nghiệm đã tắt trong builder; không thay đổi bộ bước đi V11.
  Q/E/R/F dùng kỹ năng, E tương tác, I mở hành trang và Esc tạm dừng.

## Chuẩn bị trình diễn đồ án (10/09/2026)

- Sửa mất chữ sau build: builder tạo sẵn toàn bộ dấu tiếng Việt trong font chính
  dạng static. `Prepare Vietnamese Font` có thể tạo lại riêng font; build guard
  ngăn đóng gói nếu thiếu ký tự. Xem `VIETNAMESE_FONT_VERIFICATION.md`.

- Menu chính có hướng dẫn tiếng Việt; menu tạm dừng mở lại cùng bảng hướng dẫn.
- Chơi mới khi đã có save phải xác nhận; Hủy/Esc giữ nguyên tiến trình.
- Bỏ tự cấp 2.000.000 vàng lúc khởi động. Không trừ vàng trong save cũ;
  lần mở đầu không tự tạo save, Chơi mới vẫn bắt đầu với 2.500 vàng.
- Pause, hướng dẫn và màn thua quản lý khóa input bằng GameplayState;
  đóng hướng dẫn từ Pause không tự tiếp tục game.
- Những bảng này được ghép bởi controller runtime, giống UI trang bị hiện có;
  không sửa tay scene và không cần chạy builder vì không đổi content/builder.
- Chưa cân bằng lại cấu hình thử nghiệm 50.000 HP / 25.000 mana của player.
  Không coi toàn bộ roadmap mở rộng là yêu cầu bắt buộc cho một đồ án cơ bản.

## Chạy kiểm thử

- Mở `Window > General > Test Runner` để chạy EditMode và PlayMode test.
- Test nằm trong `Assets/Tests/EditMode` và `Assets/Tests/PlayMode`.
- Chạy command line bằng Unity với `-runTests -testPlatform EditMode` hoặc
  `-runTests -testPlatform PlayMode`; thêm `-testResults <path.xml>` để lưu báo cáo.
- Test command line tự dùng PlayerPrefs key `TopDownGame.Tests.*` và dọn sau khi
  chạy, không đọc/ghi save `TopDownGame.Progress.v1` của người chơi.
- Khi builder thay đổi, chạy `Build Complete MVP` hai lần rồi chạy validator và
  cả hai test suite để kiểm tra idempotence/regression.

## Trang bị

- Hành trang có 5 ô: vũ khí, mũ, giáp, quần và dây chuyền.
- Nhấn vật phẩm trong túi để mở khung chi tiết, sau đó dùng nút `TRANG BỊ`;
  chọn món đang mặc và nhấn `THÁO TRANG BỊ` để gỡ ra.
- Màn nhân vật chia ba cột: trang bị đang dùng, túi đồ có bộ lọc và khung
  chi tiết/so sánh chỉ số.
- Trang bị có độ hiếm, màu nhận diện và Điểm Sức Mạnh. Khung nhân vật hiển thị
  tổng Máu, Sát thương, Giáp, Tốc chạy, Chí mạng và Hồi chiêu.
- Lò rèn bán đủ 40 món theo từng nhóm. Rương đồ cho phép cất/lấy vật phẩm.
- Mua, trang bị, tháo và chuyển đồ qua rương đều được lưu ngay.
- Hành trang/lò rèn dùng khung 1920×1080 tự thu vừa canvas, giữ toàn bộ nút
  trong màn hình 16:9, 16:10 và 4:3; danh sách có thanh cuộn kéo được.
- Lò rèn chia 5 nhóm, ghi rõ giá ưu đãi, đồ đang dùng và trạng thái thiếu vàng;
  không đổi ưu đãi khi đã sở hữu toàn bộ catalog.
- Chi tiết hành trang và chỉ số ở lò rèn tính cả cấp cường hóa. Tổng tốc độ
  hồi chiêu hiển thị theo phần trăm (100% là tốc độ cơ bản).
- Khi mở hành trang/lò rèn, game tạm dừng và khóa đánh/kỹ năng/tương tác;
  Esc chỉ đóng cửa sổ, không đồng thời mở menu tạm dừng.

## Vòng chơi hiện tại

Wildlife Detail V2: cá Lobby/Map02 thêm mang, mắt đôi, vây ngực, mảng màu và
vảy sáng; chim Lobby có chân/mỏ/lông rõ hơn, gập cánh lúc đậu và xòe lúc bay.
Giữ số lượng, vùng bơi và save. Đây là hình nhiều lớp theo phong cách wildlife
hiện có, không phải bộ sprite vẽ tay mới. Xem `WILDLIFE_DETAIL_V2_VERIFICATION.md`.

Truyền tống V10: tia sáng song song thẳng đứng, không thu vào tâm ở phía trên;
hạt sáng bay theo đường thẳng thay xoắn vào trong. Giữ màu, vòng sáng và thời
gian chuyển map. Kiểm thử: `TELEPORT_STRAIGHT_V10_VERIFICATION.md`.

Sân Lobby V1: thêm ba lữ khách áo xanh lá/đỏ đất/tím đi theo vòng ngắn, tăng
giảm tốc và nghỉ lệch nhịp, nhường người chơi ở phía trước. Dùng lại hình bước
chân hiện có, không phải NPC vẽ mới. Không chiến đấu/tương tác/save/collider.
Hai cụm thùng hàng–đèn trang trí mép sân có footprint nhỏ. Xem
`LOBBY_VISITORS_V1_VERIFICATION.md` cho kiểm thử và giới hạn.

Tiềm năng/Kỹ năng V2: bảng tối viền đồng giống Hành trang, tự thu vừa màn hình.
Tiềm năng có bốn thẻ chỉ số; Kỹ năng có danh sách hình, hình lớn và icon ở bốn
ô Q/E/R/F, chọn/trang bị/tháo/nâng cấp bằng chuột hoặc phím cũ. P/K/Esc đóng;
pause/input qua GameplayState, giữ chi phí và save API. Không tự mở khóa chiêu.
Xem `PROGRESSION_UI_V2_VERIFICATION.md` cho ảnh và kiểm thử.

Map02 Clarity V2: tăng tương phản chi tiết độ sáng có giới hạn, chỉ trên tranh
Map02, không đổi ảnh/camera/quái/save. Không phải thêm độ phân giải hoặc vẽ lại.
Kiểm thử và ảnh so sánh: `MAP02_CLARITY_V2_VERIFICATION.md`.

Vườn dưới Lobby: `LobbyGardenWildlife` bổ sung 6 bướm xanh/tím/hồng và 3 chim
nhỏ bay–đậu giữa các vị trí do builder khai báo. Giữ cá và bướm vàng cũ;
không collider/thưởng/save, dừng khi pause. Trạng thái kiểm tra ở
`LOWER_GARDEN_WILDLIFE_VERIFICATION.md`.

Restored V1 (10/09/2026): Lobby và Map02 chuyển sang tranh phục hồi Real-ESRGAN
đã được người dùng duyệt, lần lượt 6476×3884 và 6144×4096. Builder giữ nguyên
pixel với cap 8192, không nén/mipmap/CPU copy; nước Lobby lấy cùng ảnh mới.
Giữ world bounds, collider, camera, tương tác, quái và cá. Map02 giữ màu V5,
bỏ sharpen phụ. Tranh gốc vẫn được lưu; các tên V7/V4 dưới đây là lịch sử bố cục.
Xem `SCENERY_RESTORED_V1_VERIFICATION.md` cho trạng thái kiểm thử thực tế.

Sảnh dùng cảnh Trạm nghỉ Tán Rừng, dựng bởi builder từ
`Art/Generated/Lobby/WoodlandWaystationV7_GardenPond.png`. Sân đá là vùng đi lại;
nhà, bờ suối và hàng cây phía ngoài bị chặn bằng 18 đoạn collider. Camera được
giới hạn trong ảnh nền 26×15,6 đơn vị, không ảnh hưởng camera các combat map.
Thợ rèn là nhân vật tĩnh vẽ trong nền, có đối tượng tương tác riêng. Lò rèn,
kho đồ, cổng Map01 vẫn hoạt động như cũ. HUD gọn riêng chỉ dùng tại Lobby.

Hiệu ứng sảnh V4: LobbyWaterFlow phủ lớp nước biến dạng nhẹ đúng lòng suối/thác,
bọt trôi và giọt nước ở chân thác; LobbyFireEffects thêm ngọn lửa mềm, tàn lửa
và khói ở lò rèn/lửa trại. Dùng thời gian gameplay nên đứng yên khi pause.
Hiệu ứng có số object cố định, không collider; mesh/material/texture riêng được
dọn khi rời scene. Tranh gốc và đường đi vẫn giữ nguyên.
LobbyWildlife có 10 cá lớn hơn với 5 màu, nhảy luân phiên kèm vòng nước,
6 bướm và 7 lá rơi nhẹ. V7 mở rộng ao và căn lại bờ chặn/nước/vùng cá bơi;
loại 5 cư dân của V6, chỉ giữ thợ rèn, thay bằng vườn hoa rực rỡ và cây ven ao.
Lửa trại cũ được thay bằng vườn, hiệu ứng lửa chỉ còn ở lò rèn.
Các sinh vật là chi tiết trang trí, không collider, không phần thưởng hay tương tác.
Chuyển động được giới hạn theo vùng builder khai báo và dừng khi pause.

Truyền tống trận: bỏ sprite cổng đứng tại Lobby, dùng vòng đá sẵn trong tranh.
Đứng trong tâm trận khoảng 1,15 giây để mở bảng chọn 6 vùng, chỉ vùng đã mở khóa
mới chọn được. Esc/Ở lại sảnh đóng bảng; E mở lại hoặc bước ra rồi vào lại.
Khi chưa chọn vùng, ánh mặt đất/rune nằm dưới nhân vật. Khi chọn nơi đến,
V9 đóng bảng chọn, khóa input nhưng không pause, camera lùi ra 18% trong
giới hạn tranh, tia sáng và hạt sáng bốc lên quanh nhân vật trong 3 giây.
Sau đó mới capture save và gọi chuyển scene/fade có sẵn. Chọn lặp bị chặn;
nhân vật chết, ra ngoài trận hoặc portal bị tắt sẽ hủy và trả camera/input.
V8 căn tâm trận theo tranh V7 tại (8,73; -0,20), vòng khắc elip bán kính
(1,22; 0,87), viền ngoài (1,95; 1,44). V9 bật lại toàn bộ vòng ngoài,
cả hai vành có vệt sáng chạy, sáng mạnh hơn khi chuẩn bị rời sảnh.
Không đổi thời gian mở bảng chọn, điều kiện mở khóa vùng hay schema save.
Bước ra trước khi đủ thời gian sẽ hủy; pause/menu dừng tiến độ. Tranh V3 gốc
vẫn được giữ; V7 chỉ chỉnh bờ trái theo ao lớn, các trạm giữ nguyên.

Map01 V8 bỏ lớp tranh toàn-map EmeraldSanctuary. Nền đất rêu dùng ô ảnh
Map01_MossSoilV8 lặp theo tọa độ thế giới mỗi 6 đơn vị, pha lớp thứ hai để
giảm lặp; không phóng ảnh nhỏ phủ cả map. Đường đi sáng hơn nền rừng, ranh giới
đá rêu thấp xen cây và cụm cây lớn ngoài vùng đi. Giữ graph, collider, địch,
rương, cửa Map02, nest slime và tiến trình. Mọi nội dung do builder tạo.

Chất lượng nền: Lobby/Environment dùng Bilinear thay Point để giảm ô pixel
khi phóng. Tranh toàn Map01 dùng Clamp/cap 4096, nền lát vẫn Repeat.
Đây là cải thiện lấy mẫu, chưa phải ảnh 4K: ảnh gốc vẫn Lobby 1619×971,
Map01 1484×1060. Không đổi zoom/collider hoặc upscale file giả.

Map02 V4 — Tàn Tích Suối Ngọc thay bản V3 người dùng không ưng ý. Tranh
`Map02_AncientWoodlandV4` phủ vùng 36×24, có trại Goblin, đền cổ, hồ thác và
cầu qua suối. Va chạm được vẽ theo đường đi trên tranh trong phần builder
`TopDownGameBuilder.Map02.cs`; không còn viền đá/cây lát kiểu mê cung.
Camera giới hạn trong tranh, lửa trại và nước có hiệu ứng chạy theo thời gian
gameplay. Vị trí cũ ngoài vùng hợp lệ được đưa về cửa vào, không reset save.

Goblin dùng Vòng Sườn Truy Kích + Ảnh Bộ Song Trảm: vòng sườn, truy đuổi vị
trí vừa thấy, lao bằng vật lý, hai nhịp chém có cảnh báo riêng rồi rút lui.
Cung thủ dùng Đổi Chốt Né Bắn + Vũ Tiễn Phong Tỏa: chọn vị trí bắn có tầm
nhìn, bật lùi khi bị áp sát, mưa tên khóa vùng rơi 0,95 giây trước sát thương.
Hai bộ AI tự quản lý hành động, không cộng thêm chiêu signature cũ lên cùng
chu kỳ. `ForestNavigation` chia sẻ lưới tránh vật cản, `ForestPathAgent` lưu
góc đường riêng cho từng quái. Giữ 8 Goblin + 4 cung thủ đặt sẵn, mục tiêu
quần thể 16, cấp 8–20 và 12 điểm hồi sinh có kiểm tra. Không có sprite bước
chân bốn hướng mới; animator hiện tại vẫn sở hữu hình và hướng nhân vật.
Kết quả kiểm thử/nguồn tranh/giới hạn xem `MAP02_V4_VERIFICATION.md`.

Map02 Color V5: material `Map02ColorV5` chỉnh riêng màu tranh nền (giảm vàng
ở cây/đất, bóng rừng sâu hơn, bảo vệ màu hồ). Không đổi bitmap/camera/UI hay
màu quái. Viền báo đòn đỏ cam có lớp tối bên ngoài, giữ đúng phạm vi và thời
gian chiêu. Độ nét tăng nhẹ qua shader, không phải ảnh độ phân giải cao mới.
Ảnh so sánh và kết quả kiểm tra: `MAP02_COLOR_V5_VERIFICATION.md`.

Living V6 theo phản hồi ảnh chụp: bỏ viền đen dày V5 và cạnh tam giác nối
tâm vùng chém. Cung/vòng báo đòn mảnh màu hổ phách nằm dưới actor, không
đổi sát thương hay thời gian né. `ForestFishSchool` thêm 8 cá hồ + 12 cá
sông trong 8 vùng nước do builder khai báo, tránh cầu/đá/bờ. Cá có thân,
vây, quẫy đuôi và gợn nước, dừng khi pause, không collider/loot/save. Không
thay tranh hoặc màu V5. Kiểm thử: `MAP02_LIVING_V6_VERIFICATION.md`.

Người chơi bắt đầu ở Lobby, mua/trang bị vật phẩm, đi qua các map chiến đấu,
tiêu diệt quái để nhận vàng và XP, mở rương hồi phục/lấy thưởng, học hoặc nâng
cấp kỹ năng rồi đối đầu Forest Guardian. Tiến trình, trang bị, kỹ năng, vàng,
Level và XP được lưu để dùng với nút Continue.

Sau Hang Pha Lê là `RiftMap01` — Tàn Tích Nhật Thực (Lv.50–75), gồm Rift Storm
có telegraph, Đao Phủ Hư Không, Pháp Sư Khe Nứt và Vệ Binh Hắc Diện. Catalog có
thêm Sét Liên Hoàn, Mưa Thiên Thạch và Xung Kích Hư Không. Lò rèn hiển thị ưu
đãi luân phiên -20% cho từng nhóm và cho phép đổi lượt bằng 100 vàng.

## File thường cần chỉnh

### Tung chiêu và hiệu ứng

V4 mở rộng phong cách vẽ tay cho tám chiêu ngoài Kiếm Luân: bảy sheet nguyên tố
(Thiên Lôi/Sét Liên Hoàn dùng chung nét sét), mỗi sheet 6 frame; icon atlas 3×3
thay đủ chín biểu tượng. SkillPresentationProfile giữ frame/material/pose giải phóng.
SpellVfx chọn nhánh painted khi có profile, giữ shader V2 làm fallback/gather/warning.
Thiên Lôi bỏ bolt hình học cũ khi có painted art; sét liên hoàn dùng painted link.
Nhân vật có 6 pose niệm phép mới trên nền magenta khóa màu, trái/phải mirror;
chưa phải animation đủ bốn hướng. Kiếm Luân V3 giữ nguyên.

Kiếm Luân mẫu V3: SwordComboProfile do builder tạo chứa sáu pose mới,
sáu frame vệt chém vẽ tay và cue âm thanh. Chỉ chiêu này thay vòng V2 bằng
SwordComboVisual đồng bộ từng damage beat, mirrored trái/phải; chưa có sheet
riêng trên/dưới. Actor dùng shader chroma-key nền magenta vì ảnh nguồn không
có alpha thật, slash blend cộng trên nền đen. Âm thanh tổng hợp qua SFX manager.
Chặn chiêu khác trong đủ 1,2 giây chuỗi chém; giữ sát thương/bán kính/sáu hit.
Các chiêu khác vẫn V2, không coi là hoàn thiện theo chất lượng của mẫu này.

V2: SkillManager chỉ nhận lệnh/kiểm tra tài nguyên, SkillCastController sở hữu
Ready→Windup→Release→Recovery và khóa context, các SkillBase giữ tác dụng,
SkillPresentationProfile do builder tạo định nghĩa hệ/màu/thời gian,
SpellVfx + ElementalSpell shader chỉ vẽ hiệu ứng và tự dọn material.
Chín profile được gắn trực tiếp vào SkillData. Chiêu lướt lấy đà 0,08s,
thiên thạch 0,25s, các chiêu còn lại 0,18s. Animator có chuyển trọng tâm nhỏ
trong lúc tung chiêu; không đổi thân vật lý hay hướng di chuyển.
Hiệu ứng V2 thay lớp chính của lửa, kiếm, khiên, hồi phục, thiên thạch,
hư không; băng/sét bổ sung tinh thể và đường điện. Không dùng icon UI làm VFX.
Các ghi chú ngay dưới là lịch sử V1; thông số V2 lấy từ profile.

- SkillManager khóa hướng/điểm ngắm khi bấm; lấy đà 0,12 giây trước khi gọi
  Cast, rồi thu chiêu (tổng 0,42 giây; lướt băng 0,25 giây). Không thay damage,
  range, mana cost hay cooldown trong SkillData. Mana/cooldown bắt đầu ngay khi
  nhận lệnh; chết/vô hiệu hóa trước lúc phát sẽ hủy, không hoàn lại chi phí.
- Không cho chồng chiêu hoặc đánh thường trong nhịp tung chiêu. Modal/pause dừng
  lấy đà. Animator dùng ba pose hiện có theo nhịp lấy đà/phát/thu, quay đúng hướng
  ngắm tự động thay vì luôn hướng về chuột. Không tạo thêm sprite/rig bốn hướng.
- SpellPresentation dùng sprite nhỏ: ánh tụ, vòng sóng mặt đất và tia văng thay
  đĩa sáng đặc; dư ảnh băng; vòng bảo vệ; thiên thạch bay vào trước va chạm.
  Cầu lửa có lõi nóng thay icon, sét có tia va chạm, VoidPulse co vào theo lực hút.
- Kiếm xoay giữ kích thước đã tính từ bán kính khi animation thay scale.

### Chuyển động nhân vật V11

- Prefab bật tăng tốc 60 và giảm tốc 90 đơn vị/giây²: vẫn đi bốn hướng, không
  phanh lại ở mỗi góc đường chuột và dừng chính xác tại đích.
- Animation đi lấy quãng đường hiển thị ở LateUpdate, không lấy ý định nhả phím
  làm lý do đổi sang idle trước khi bước vật lý hiển thị xong.
- Khi dừng, kết thúc chuyển tiếp ngắn khoảng 0,1 giây rồi trở về Idle_00 trung tính;
  không giữ hình bước chân làm tư thế đứng. Hiện chưa có idle riêng bốn hướng,
  nên dừng đi lên/xuống sẽ dùng hình đứng nhìn trước.
- Bộ hình V10 ngang / V6 lên xuống vẫn có 8 pose. Sửa này không tạo thêm hình
  trung gian, không tăng tốc phát giả và không crossfade hai sprite.
- Có video so sánh Captures/WalkContinuity_Comparison.mp4 (trước trái / sau phải).

- `Player/PlayerMovement.cs`: di chuyển WASD, phím mũi tên và click chuột trái.
- `Player/PlayerCombat.cs`: tấn công thường bằng chuột phải hoặc phím J.
- `Player/PlayerHealth.cs`: HP và trạng thái chết của nhân vật.
- `Enemies/EnemyHealth.cs`: HP, nhận sát thương và chết của quái.
- `Enemies/EnemyMovement.cs`: di chuyển của quái.
- `Skills/SkillManager.cs`: đọc Q/E/R/F, mana và cooldown.
- `Skills/FireballSkill.cs`: Fireball.
- `Skills/IceDashSkill.cs`: Ice Dash.
- `Skills/SwordSpinSkill.cs`: Sword Spin.
- `Skills/LightningStrikeSkill.cs`: Lightning Strike.
- `Skills/Lightning/ChainLightningSkill.cs`: Sét Liên Hoàn.
- `Skills/Fireball/MeteorShowerSkill.cs`: Mưa Thiên Thạch.
- `Skills/Void/VoidPulseSkill.cs`: Xung Kích Hư Không.
- `Skills/HealingSkill.cs`: Healing.
- `Skills/ShieldSkill.cs`: Shield.
- `Skills/SkillEffect.cs`: hiệu ứng hình ảnh khi dùng skill.
- `UI/GameplayHUD.cs`: HP, mana, Level/XP, số quái và bốn ô skill.
- `../Editor/TopDownProjectValidator.cs`: kiểm tra graph scene, Build Settings,
  catalog, prefab và reference bắt buộc.

## Thư mục

- `Bosses`: boss.
- `Combat`: damage, hitbox và hurtbox.
- `Core`: interface và sprite runtime dùng chung.
- `Enemies`: AI, HP và các archetype quái, gồm Crystal Scorpion ở CaveMap01.
- `Items`: item nhặt được.
- `Maps`: cửa, rương, chuyển scene và quản lý map.
- `Player`: di chuyển, HP, stats và combat của người chơi.
- `Save`: lưu tiến trình.
- `Skills`: dữ liệu và logic skill.
- `UI`: HUD và menu.
- `Utilities`: camera và tiện ích nhỏ.

Khi thử trong Editor, dữ liệu chỉ được lưu khi bắt đầu game mới, chuyển scene
hoặc quay về Main Menu; dữ liệu thử tạm thời không bị ghi chỉ vì bấm Stop.
