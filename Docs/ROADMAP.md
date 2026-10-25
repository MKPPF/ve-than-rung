# Lộ trình phát triển tiếp dự án

Tài liệu này là backlog triển khai và bộ prompt sẵn sàng copy-paste cho AI coding
assistant. Mỗi hạng mục có phạm vi, phụ thuộc, tiêu chí nghiệm thu và cách kiểm tra.
Luôn dùng kèm `CLAUDE.md` và `Assets/Scripts/README.md`.

> Cập nhật theo code ngày 09/08/2026 — Unity `6000.5.6f1`.

## 1. Hiện trạng đã đối chiếu

- Luồng scene hiện tại: `MainMenu → Lobby → Map01 → Map02 → Map03 → BossMap01
  → CaveMap01 → Lobby`.
- Sáu quái thường: Slime, Goblin, Archer, Mage, Tank, Crystal Scorpion; một boss:
  Forest Guardian.
- Sáu skill: Fireball, IceDash, SwordSpin, LightningStrike, Healing, Shield.
- Năm slot trang bị: Weapon, Helmet, Armor, Pants, Necklace; 40 món trong catalog.
- Save dùng `GameProgressManager`, `PlayerPrefs` key `TopDownGame.Progress.v1`.
- Scene, prefab và ScriptableObject được sinh từ
  `Assets/Editor/TopDownGameBuilder.cs`; đây là source of truth.
- Dự án đã cài Unity Test Framework nhưng chưa có test gameplay riêng.

### Các điểm phải lưu ý trước khi triển khai

- Thư mục skill sét hiện tại là `Assets/Scripts/Skills/Lightning/`, không phải
  `LightningStrike/`.
- `MageEnemy` là `sealed`; Necromancer không thể kế thừa nó nếu chưa refactor.
  Ưu tiên tách phần cast AoE dùng chung hoặc cho Necromancer kế thừa
  `RangedEnemy`, tránh sao chép nguyên class.
- Enum đã có `Mythic`, nhưng hiện nó nằm dưới `Divine` và rarity được suy ra từ
  giá. Hạng mục trang bị phải migration lại ngưỡng rarity để Divine vẫn đúng tên
  và Mythic thật sự là bậc cao nhất.
- Input asset đã có action `Jump` gắn Space nhưng gameplay chưa dùng. Dodge nên
  đổi tên/tái sử dụng action này và vẫn có fallback bàn phím như movement hiện tại.
- Khi thêm boss băng, dùng scene riêng `IceBossMap01` để giữ cùng cấu trúc với
  `BossMap01`. Luồng mới dự kiến:
  `CaveMap01 → CaveMap02 → IceBossMap01 → Lobby`.

## 2. Quy tắc thực hiện chung

### Trạng thái

- `[ ]` Chưa làm
- `[-]` Đang làm
- `[x]` Hoàn thành và đã kiểm tra
- `[!]` Bị chặn, phải ghi rõ lý do ngay dưới hạng mục

Chỉ đổi sang `[x]` khi toàn bộ tiêu chí nghiệm thu của hạng mục đã đạt. Không đánh
dấu hoàn thành chỉ vì code đã compile.

### Definition of Done

Mọi hạng mục phải đáp ứng các điều kiện sau:

1. Đọc `CLAUDE.md`, `Assets/Scripts/README.md` và các class liên quan trước khi sửa.
2. Không sửa tay asset sinh tự động nếu thay đổi tương ứng phải nằm trong builder.
3. Không để placeholder, TODO, missing reference hoặc lỗi/warning C# mới.
4. Builder chạy lại được nhiều lần mà không sinh asset trùng hoặc mất reference.
5. Save cũ load an toàn; field mới có giá trị mặc định và collection mới được
   normalize. Nếu bắt buộc đổi schema, phải có migration hoặc bump key có chủ đích.
6. Có test EditMode cho logic thuần và PlayMode/smoke test cho hành vi quan trọng
   khi khả thi.
7. Chạy lại `Top Down Game > Build Complete MVP`, kiểm tra Console sạch, kiểm tra
   scene/prefab/Build Settings được sinh đúng.
8. Playtest đường đi bị ảnh hưởng và chụp ảnh vào `Assets/Screenshots/` theo dạng
   `TênTínhNăng_Verified.png`.
9. Báo cáo cuối cùng gồm: file đã đổi, hành vi đã thêm, test đã chạy, kết quả và
   phần còn cần kiểm tra thủ công.

### Prompt mở đầu dùng chung

Chèn đoạn này trước prompt của từng hạng mục:

> Hãy làm duy nhất hạng mục dưới đây trong Unity project này. Trước khi sửa, đọc
> toàn bộ `CLAUDE.md`, `Assets/Scripts/README.md` và inspect code liên quan để xác
> nhận giả định. Giữ `TopDownGameBuilder.cs` là source of truth cho scene, prefab
> và ScriptableObject. Lập kế hoạch ngắn, triển khai trọn vẹn, không để
> placeholder. Giữ tương thích save hiện có. Sau khi sửa, compile, chạy test phù
> hợp, chạy lại builder nếu có thể và báo cáo chính xác những gì đã/ chưa verify.

## 3. Thứ tự triển khai đề xuất

### [-] AI1 — Nhận diện cử chỉ tay để di chuyển và dùng kỹ năng (20/10/2026)

Theo yêu cầu đồ án AI: thu dữ liệu HaGRID/webcam, tự train MLP phân loại năm
tư thế tay, chạy suy luận webcam và gửi input cho Unity. Tay di chuyển dùng
tư thế xòe làm điều kiện bật cần điều khiển; tay còn lại gọi bốn slot hiện có.
Hướng dẫn và tiêu chí nghiệm thu: `Design/HAND_GESTURE_CONTROL.md`.
Đã thu 14.589 mẫu HaGRID, thực thi notebook và train MLP; test tĩnh 99,12%,
77/77 EditMode, 78/78 PlayMode và validator đạt. Báo cáo:
`Verification/HAND_GESTURE_CONTROL_VERIFICATION.md`. Giữ `[-]` đến khi đánh giá
webcam/người mới và playtest thao tác thực tế đạt.

Bổ sung nút bật/tắt tự đánh/tự nhặt để kiểm tra điều khiển thủ công và cử chỉ
độc lập. 77/77 EditMode, 82/82 PlayMode, validator và kiểm tra HUD Map01/Lobby đạt;
xem `Verification/AUTOMATION_CONTROLS_VERIFICATION.md`.

Điều khiển tay V2 sửa chọn nhầm tay/chưa đặt tâm và focus cửa sổ camera;
thêm nút TAY [F8], ACK, tự chọn tay/tự hiệu chỉnh và ngón trỏ chỉ bốn hướng.
Đã xác nhận bốn hướng qua UDP trên player thật; vẫn cần nghiệm thu hai tay
ngoài đời. Xem `Verification/HAND_GESTURE_V2_FIX_VERIFICATION.md`.

V2.1 sửa gate dùng chiêu trong notebook: dấu đầu/đổi dấu giữ 0,35 giây tự gửi,
ngón trỏ tay cast cho phép lặp cùng chiêu; chờ ACK trước khi đếm dấu đầu.
16/16 Python test đạt; vẫn cần người dùng nghiệm thu webcam thực tế.

| Milestone | Mục tiêu | Hạng mục | Điều kiện hoàn tất |
|---|---|---|---|
| M0 | Baseline đáng tin cậy | R0, R1, R2 | Compile sạch, có validator và smoke test |
| M1 | Combat nền tảng | G1, G2 | Dodge và status effect ổn định |
| M2 | Vertical slice hang băng | C1–C4 | Chơi liền mạch đến boss băng |
| M3 | Meta progression | P1–P3 | Quest, achievement, Mythic lưu đúng |
| M4 | Cảm giác chơi và UX | U1–U5 | Audio/settings/tutorial/feedback hoàn chỉnh |
| M5 | Tối ưu và cân bằng | T1–T3 | Pooling, balance, regression pass |

Không nên triển khai đồng thời hai hạng mục cùng sửa
`TopDownGameBuilder.cs` hoặc `GameProgressManager.cs`.

---

## M0 — Baseline và hàng rào an toàn

### [x] R0 — Audit baseline và lập bảng thông số

**Mục tiêu:** tạo ảnh chụp hiện trạng trước khi thay đổi gameplay.

**Prompt:**

> Audit dự án mà chưa thay đổi gameplay. Ghi lại bảng scene flow, enemy
> HP/damage/gold, skill damage/mana/cooldown/range, toàn bộ rarity/ngưỡng giá và
> các input binding thực tế. Tìm reference hỏng, scene thiếu trong Build Settings,
> lỗi compile và khác biệt giữa `CLAUDE.md` với code. Cập nhật tài liệu nếu phát
> hiện số liệu sai. Không tự cân bằng chỉ số trong bước này.

**Nghiệm thu:**

- Có bảng baseline trước/sau hoặc báo cáo lưu trong tài liệu dự án.
- Xác nhận đủ scene trong Build Settings và không có lỗi compile.
- Liệt kê rõ mọi sai lệch giữa tài liệu và code; không âm thầm bỏ qua.

### [x] R1 — Scene/build validator

**Phụ thuộc:** R0.

**Prompt:**

> Tạo Editor validator cạnh `TopDownGameBuilder.cs` với menu
> `Top Down Game > Validate Project`. Validator phải kiểm tra: scene do builder
> khai báo tồn tại và được enable trong Build Settings; mọi `MapData.NextScene`,
> `MapDoor`, `LobbyPortal` và field scene đích liên quan không rỗng và trỏ tới
> scene hợp lệ; prefab/ScriptableObject bắt buộc không null. Tổng hợp tất cả lỗi
> trong một lần chạy, log có object/path để sửa, trả về kết quả pass/fail có thể
> gọi từ test. Không dừng ở lỗi đầu tiên.

**Nghiệm thu:**

- Validator pass trên baseline vừa build.
- Cố ý nhập một scene sai khiến validator fail với thông báo chỉ đúng asset/object.
- Khôi phục dữ liệu thử và validator pass lại.

### [x] R2 — Test scaffold và smoke test builder

**Phụ thuộc:** R0, R1.

**Prompt:**

> Tạo cấu trúc test tối thiểu cho EditMode và PlayMode, dùng assembly definition
> phù hợp. Viết test cho logic không phụ thuộc frame và một smoke test xác nhận
> scene chính, skill data, enemy data, equipment catalog có thể load sau khi chạy
> builder. Test không được phụ thuộc thứ tự chạy hoặc save thật của người chơi;
> PlayerPrefs test phải dùng key riêng và cleanup trong teardown.

**Nghiệm thu:**

- Test chạy độc lập, chạy lặp lại cho cùng kết quả.
- Không xóa hoặc ghi đè save thật.
- Có hướng dẫn câu lệnh/menu chạy test.

---

## M1 — Nền tảng combat

### [-] G0 — Liền mạch bước đi nhân vật

V11: đồng bộ animation với chuyển động hiển thị, giữ hướng khi dừng, tăng/giảm
tốc ngắn; giữ di chuyển bốn hướng và bộ hình đang dùng. Đã quay trước/sau,
56/56 EditMode, 54/54 PlayMode và validator đạt; cần người chơi đánh giá
cảm giác thực tế. Xem Docs/Verification/PLAYER_WALK_V11_VERIFICATION.md.

### [-] G1 — Dodge roll có i-frame

22/10/2026 — Đã triển khai né tám hướng bằng Space/nút dưới gamepad, 0,28 giây,
miễn đòn trực tiếp 0,18 giây đầu và hồi 1 giây; solver va chạm, combat/cast gate,
HUD và tutorial được nối qua builder. Hai lượt dựng đã hoàn tất; validator 0 lỗi,
78/78 EditMode, 130/130 PlayMode đạt; có 18 ảnh ba trạng thái/ba tỷ lệ ở Lobby và
Map01. Console 0 error. Giữ `[-]` cho đánh giá khoảng né/tư thế khi giao tranh
thật; hiện dùng pose trên sprite hướng sẵn có, chưa có hình lộn vòng riêng.
Xem `Verification/DODGE_FLOW_VERIFICATION.md`.

**Phụ thuộc:** R2.

**Prompt:**

> Thêm dodge roll cho player. Tái sử dụng/đổi action `Jump` thành `Dodge` trong
> Input System, Space và gamepad button South; có keyboard fallback. Khi bấm,
> player lướt theo input hiện tại, nếu đứng yên thì theo hướng nhìn cuối cùng.
> Dùng Rigidbody2D/MovePosition nhất quán với `PlayerMovement`; không teleport.
> Trong thời gian dodge, khóa attack/skill xung đột, có i-frame ngắn và cooldown
> riêng dùng unscaled/scaled time có chủ đích. I-frame phải nằm trong API rõ ràng
> của `PlayerHealth`, không disable collider toàn cục. HUD hiển thị cooldown nhỏ,
> builder phải wire mọi reference. Giá trị khởi đầu gợi ý: 0,18 giây i-frame,
> 0,28 giây dodge, cooldown 1,0 giây; để SerializeField nhằm dễ tune.

**Nghiệm thu:**

- Dodge đúng tám hướng, đứng yên dùng hướng nhìn cuối, không xuyên collider map.
- Damage bị bỏ qua chỉ trong i-frame; nhận damage lại ngay sau đó.
- Spam Space không bỏ qua cooldown; pause/game over/UI focus không kích hoạt dodge.
- Không làm hỏng click-to-move, attack, skill hoặc animation direction.

### [ ] G2 — Status effect dùng chung

**Phụ thuộc:** R2. Nên hoàn thành trước C1 và C2.

**Prompt:**

> Thiết kế hệ status effect dùng chung cho player và enemy gồm Poison, Slow, Stun.
> Tách contract nhận effect khỏi `IDamageable` để object chỉ nhận damage không bị
> ép hỗ trợ effect. Mỗi effect có source, duration, potency và policy refresh/stack
> được định nghĩa rõ. Poison tick damage qua combat API; Slow không cộng dồn vô
> hạn; Stun khóa hành động nhưng không phá state chết. Cleanup coroutine/timer khi
> disable hoặc chết. Refactor IceDash dùng Slow mới, giữ hành vi nhìn thấy tương
> đương. Thêm test cho refresh, stack, hết hạn, miễn nhiễm và cleanup.

**Nghiệm thu:**

- Cả player và ít nhất hai archetype enemy nhận effect đúng.
- Poison ghi nhận source damage đúng; Slow/Stun hết hạn và phục hồi state chuẩn.
- Boss có thể cấu hình resistance/immunity thay vì hard-code theo tên class.
- IceDash không còn logic slow riêng.

---

## M2 — Vertical slice “Hang băng”

### [ ] C1 — CaveMap02 và luồng scene mới

**Phụ thuộc:** R1, G2.

**Prompt:**

> Thêm `CaveMap02` bằng builder, chủ đề hang băng. Tạo `MapData`, scene, môi trường,
> spawn point, camera, HUD, reward chest và cửa như các combat scene hiện có.
> Dùng Mage/Tank làm chủ lực; độ khó mục tiêu cao hơn CaveMap01 khoảng 20% dựa trên
> tổng effective HP và damage pressure, không chỉ tăng số lượng. Thêm biến thể màu/
> props băng có fallback an toàn nếu art chưa có. Đổi luồng thành
> `CaveMap01 → CaveMap02 → IceBossMap01 → Lobby`, cập nhật Build Settings và
> validator. Builder phải idempotent.

**Nghiệm thu:**

- New Game/Continue và chuyển cửa vào/ra CaveMap02 đúng, không scene-not-found.
- Clear condition đếm đúng toàn bộ enemy; cửa/reward chỉ mở đúng lúc.
- Camera không lộ nền trống, spawn không đè obstacle/enemy.
- Chạy builder hai lần không sinh object/asset trùng.

### [ ] C2 — Boss Ice Queen hai pha

**Phụ thuộc:** C1, G2.

**Prompt:**

> Tạo `IceQueenBoss` trong namespace boss và scene `IceBossMap01`, theo contract
> chung của `EnemyBase`/`EnemyHealth`, không copy nguyên `ForestGuardianBoss`.
> Boss có phase 1 trên 50% HP và phase 2 từ 50% trở xuống; transition chỉ chạy một
> lần và có feedback rõ. Có ít nhất: projectile/ice shard, AoE được telegraph,
> cone/line attack báo hướng trước khi gây damage và một kỹ năng dùng Slow. Phase 2
> tăng nhịp hoặc pattern, không chỉ nhân damage. Tất cả coroutine phải dừng khi boss
> chết. Builder tạo EnemyData, prefab, health bar, scene, reward và gắn reference.
> Khi chết lưu boss id ổn định `ice_queen`.

**Nghiệm thu:**

- Mọi đòn nguy hiểm có thời gian phản ứng nhìn thấy được và hitbox khớp telegraph.
- Phase transition đúng một lần ở ngưỡng 50%, kể cả một hit vượt qua ngưỡng.
- Boss chết không tiếp tục cast/gây damage; cửa và reward mở đúng.
- Save/Continue ghi nhận boss đã hạ mà không ảnh hưởng Forest Guardian.

### [ ] C3 — Necromancer triệu hồi có giới hạn

**Phụ thuộc:** G2. Có thể làm sau C1.

**Prompt:**

> Thêm Necromancer dựa trên `RangedEnemy` và tái sử dụng phần cast AoE chung sau
> khi refactor; không kế thừa `MageEnemy` khi class còn sealed và không sao chép
> toàn bộ logic. Khi lần đầu vào combat, summon tối đa hai Slime ở vị trí hợp lệ,
> có telegraph và delay. Minion phải được MapManager tính vào clear condition một
> cách có chủ đích. Có cap tổng minion, không summon lại khi re-enable/đổi target,
> cleanup subscription khi chết. Builder tạo EnemyData, prefab và đặt Necromancer
> với mật độ vừa phải ở CaveMap02.

**Nghiệm thu:**

- Mỗi Necromancer chỉ tạo tối đa hai minion theo cấu hình.
- Không spawn trong collider, ngoài arena hoặc sau khi caster chết.
- Clear map không mở cửa khi minion hợp lệ còn sống và không bị kẹt vĩnh viễn.
- Builder/validator nhận đúng prefab và data mới.

### [ ] C4 — Skill Chain Lightning

**Phụ thuộc:** R2.

**Prompt:**

> Thêm `SkillId.ChainLightning` và skill trong
> `Assets/Scripts/Skills/Lightning/`. Khi cast, chọn mục tiêu đầu tiên hợp lệ trong
> range theo quy tắc rõ ràng, sau đó lan đến tối đa hai mục tiêu khác (tối đa ba
> mục tiêu tổng cộng), mỗi mục tiêu chỉ trúng một lần. Mỗi lần lan giảm 25% damage:
> 100%, 75%, 56,25%. Khoảng cách lan, layer, damage falloff là cấu hình; thứ tự chọn
> deterministic (khoảng cách rồi instance id) để dễ test. VFX bolt nối đúng từng
> mục tiêu và tự cleanup. Builder tạo SkillData/icon fallback, prefab component,
> thêm vào skill selection và save bằng enum name ổn định.

**Nghiệm thu:**

- 0/1/2/3 mục tiêu hoạt động không exception; không đánh trùng một mục tiêu.
- Không nhảy sang player, object chết, object ngoài layer/range hoặc xuyên điều kiện
  lọc đã chọn.
- Damage falloff đúng bằng test với sai số float hợp lý.
- Skill có thể học, nâng cấp, trang bị, lưu và load lại.

---

## M3 — Meta progression

### [ ] P1 — Quest tiêu diệt quái

**Phụ thuộc:** R2; nên làm sau C3 để có đủ event spawn/death.

**Prompt:**

> Thêm hệ quest tối giản theo data, không hard-code riêng một nhiệm vụ. Quest đầu:
> “Tiêu diệt N quái ở Map0X”. Tạo NPC ở Lobby để nhận/trả quest, panel HUD nhỏ hiển
> thị mục tiêu và tiến độ. Tạo event/domain service ghi nhận enemy death có enemy id,
> scene id và source; chỉ tính kill do player gây ra, không tính object bị cleanup.
> `GameProgressManager` lưu quest id, state, progress và claimed flag; normalize save
> cũ. Claim reward một lần, cộng vàng qua API hiện có và save ngay.

**Nghiệm thu:**

- Nhận, theo dõi, rời scene, Continue và hoàn thành quest giữ đúng tiến độ.
- Kill sai map, minion cleanup hoặc damage không phải player không tăng progress.
- Không thể nhận thưởng hai lần khi spam interact/reload.
- Panel ẩn khi không có quest và không che HUD/menu.

### [ ] P2 — Achievement

**Phụ thuộc:** P1 hoặc event progression tương đương.

**Prompt:**

> Thêm achievement data-driven, lưu tập id đã unlock trong
> `GameProgressManager`. Triển khai ba achievement: hạ Forest Guardian lần đầu,
> số dư vàng đạt 2.000.000, sở hữu hoặc trang bị đủ bộ Divine theo định nghĩa ghi
> rõ. Dùng event hiện có thay vì poll mỗi frame. Popup xếp hàng nếu mở nhiều cái,
> dùng unscaled time, không hiện lại achievement đã unlock. Save cũ phải normalize
> list mới. Viết test cho unlock một lần và điều kiện biên.

**Nghiệm thu:**

- Ba điều kiện unlock đúng và chỉ lưu một id mỗi achievement.
- Nhiều popup không chồng lên nhau; pause không làm popup kẹt.
- New Game xử lý achievement theo quyết định thiết kế được ghi rõ (profile-wide
  hoặc per-run), không vô tình xóa/giữ.

### [ ] P3 — Chuẩn hóa rarity và bộ Mythic

**Phụ thuộc:** R0.

**Prompt:**

> Sửa mô hình rarity để thứ tự cuối là `Legendary < Divine < Mythic`, với Mythic
> là bậc cao nhất. Hiện rarity suy ra từ price và enum đã có Mythic dưới Divine;
> hãy audit mọi nơi dùng rarity, cập nhật enum/switch/ngưỡng/màu/label để các món
> Divine hiện tại vẫn hiển thị Divine. Ưu tiên lưu rarity trực tiếp trong
> EquipmentData thay vì dùng giá làm nguồn sự thật nếu migration an toàn. Thêm bộ
> Mythic năm món đủ slot, chỉ số từng stat liên quan khoảng 30% trên món Divine
> tương ứng, item id ổn định. Không bán ở WeaponShopNpc thường; tạo NPC ẩn được mở
> bằng điều kiện rõ ràng (gợi ý: hạ Ice Queen), có UI giải thích khi chưa mở.
> Builder tạo data/icon fallback/NPC và test rarity mapping.

**Nghiệm thu:**

- Tất cả món cũ giữ đúng tên rarity mong muốn sau migration.
- Năm món Mythic đủ slot, không trùng item id, stat được so bằng test/data audit.
- Shop thường không chứa Mythic; NPC ẩn khóa/mở đúng và giao dịch được lưu.
- Save đang sở hữu/trang bị đồ cũ vẫn load đúng vì dùng item id.

---

## M4 — Cảm giác chơi và UI/UX

### [-] MAP2 — Rừng Cổ Lá Đồng (08/09/2026)

Clarity V2: bổ sung chỉnh độ nét độ sáng giới hạn riêng trên nền phục hồi Map02;
giữ camera/bố cục/gameplay. Trạng thái ở `Docs/Verification/MAP02_CLARITY_V2_VERIFICATION.md`.
Đã dựng hai lượt + validator, 73/73 EditMode và 69/69 PlayMode đạt; đã xem
ảnh so sánh và runtime. Đây là tăng nét nhẹ, không bổ sung chi tiết tranh gốc.

Living V6: người dùng không thích nét đỏ/đen chồng lên quái trong V5. Bỏ
viền đen và cạnh nối tâm của báo chém, nét mảnh dưới actor; thêm cá bơi ở
hồ và sông qua habitat builder, không collider/reward/save. Kiểm tra đang
thực hiện; xem `Docs/Verification/MAP02_LIVING_V6_VERIFICATION.md`.

Living V6 đã dựng hai lượt cuối + validator đạt; toàn bộ 70/70 EditMode,
69/69 PlayMode đạt. Sau căn thêm vùng cá tránh đá nổi, 7/7 Map02 đạt. Đã
xem ảnh hồ, sông và cảnh báo trong combat; 8 cá hồ, 12 cá sông. Chưa thêm
cơ chế câu/bắt cá, không thay gameplay/save. Phần cảnh báo V5 bên dưới là
lịch sử đã được thay bằng nét mảnh dưới actor trong V6.

Color V5 theo yêu cầu khắc phục màu nhạt: shader riêng trên tranh Map02 giảm
vàng xám, tăng tương phản có chọn lọc và giữ màu hồ; cảnh báo có viền tối.
Không vẽ lại bitmap hay sửa chỉ số/kỹ năng. Kiểm thử/so ảnh đang được ghi ở
`Docs/Verification/MAP02_COLOR_V5_VERIFICATION.md`; phần lịch sử V4/V3 bên dưới được giữ lại.

Color V5 đã dựng hai lượt + validator đạt, 70/70 EditMode và 69/69 PlayMode
trong Editor đạt. Đã so ảnh cùng góc trước/sau và xem cảnh báo trong combat.
Chỉnh rộng cửa vào V4 cũng đã được dựng/kiểm tra: route clearance đến mọi
điểm quái đạt. Trạng thái CHƯA dựng bên dưới là lịch sử, đã được xử lý trong
lượt V5. MAP2 tổng thể giữ `[-]` cho cảm nhận người dùng/loadout balance.

V4 theo phản hồi người dùng: không chấp nhận bản V3 quá đơn giản. Thay bằng
Tàn Tích Suối Ngọc: tranh cảnh hoàn chỉnh, collider theo tranh, navigation
tránh vật cản và hai bộ kỹ năng/di chuyển mới cho Goblin/cung thủ. Chi tiết
và kiểm thử đang được ghi tại `Docs/Verification/MAP02_V4_VERIFICATION.md`. Các ghi chú V3
bên dưới là lịch sử, không phải mô tả bản hiện tại.

V4: tranh/collider và kỹ năng đã dựng hai lượt + validator; 6/6 PlayMode riêng
đạt, có video `Captures/Map02V4_Combat.mp4`. Kiểm tra navigation bổ sung phát
hiện góc cổng vào hẹp cho quái; đã chỉnh source, CHƯA dựng lại chỉnh sửa cuối
vì người dùng đang Play tại Lobby. Cần Stop rồi builder hai lượt và chạy lại
test; giữ `[-]`. Không tự dừng Play hoặc ghi đè phiên người dùng.

Làm lại cảnh quan Map02 bằng nền lát riêng, viền đá thấp, tán cây ngoài đường
và bãi giao chiến rộng hơn. Giữ tiến trình/cấp/chỉ số quái; sửa Goblin chém
sau lưng/xuyên cảnh và cung thủ phát tên sau chết. Nghiệm thu: builder hai
lượt, validator, EditMode/PlayMode, đường đi và ảnh ở camera gameplay.

Đã triển khai và kiểm tra riêng Map02: builder hai lượt + validator đạt,
70/70 EditMode, 3/3 bài PlayMode mới đạt, đi tới rương/cửa và ảnh runtime đã
kiểm tra. Toàn bộ PlayMode đạt 64/65; bài click-bắn cũ vẫn lỗi input giả lập
trong batch kể cả chạy riêng trước khi Map02 được load. Giữ `[-]` đến khi
đóng lỗi hồi quy và kiểm tra cảm giác giao chiến thủ công theo loadout.
Chi tiết, ảnh, nguồn art imagegen và giới hạn: `Docs/Verification/MAP02_V3_VERIFICATION.md`.

### [-] U1 — AudioManager và audio event

22/10/2026 — Rà soát phát hiện fade ghi đè âm lượng vừa chỉnh, các lần chuyển
nhanh có thể giữ loop cũ, và nguồn âm thanh thứ 19 trở đi chưa được kích hoạt.
Đã sửa arbitration/gain/pool, tránh restart cùng cue, bỏ entry clip null và cảnh
báo một lần khi cue không có clip. Sáu test dùng clip tổng hợp đã đạt trong
lượt 105/105 PlayMode sau hai lượt dựng + validator. Nhạc/ambient trong
Resources hiện vẫn là placeholder, chưa có bộ theme theo vùng đã nghiệm thu.

**Phụ thuộc:** baseline ổn định.

**Prompt:**

> Tạo `AudioManager` persistent trong `Scripts/Core` với hai mixer bus Music/SFX,
> nhạc theo Lobby/forest/cave/boss và SFX cho attack, hit, skill, gold, chest,
> dodge, quest/achievement. Dùng event/API tập trung, không Find mỗi lần phát.
> Chuyển nhạc có fade và không restart clip khi reload cùng theme. Builder gắn
> AudioSource/reference; clip thiếu phải bỏ qua an toàn và log warning một lần,
> không sinh exception. Không dùng asset âm thanh không rõ license; nếu chưa có
> clip, hoàn thiện hệ thống và ghi manifest asset cần bổ sung.

**Nghiệm thu:**

- Music/SFX chỉnh độc lập, không tạo AudioSource vô hạn.
- Chuyển scene/fade/pause hoạt động đúng; không có hai bản nhạc chồng nhau.
- Thiếu clip không làm hỏng builder hoặc gameplay.

### [-] U2 — Settings dùng chung cho Main Menu và Pause

22/10/2026 — Trong đợt U5 đã thêm cửa sổ cài đặt âm lượng/hiển thị dùng chung,
lưu riêng progress và hoàn tác preview sau 15 giây. U3 đã thêm rung camera/
hitstop cùng hai nút tắt có tác dụng thật, lưu qua New Game/reload. Giữ `[-]`
để nghiệm thu restart/đổi hiển thị trên Windows player.
Kết quả cuối ở `Verification/UI_FLOW_REVIEW_2026-10-22.md`.

**Phụ thuộc:** U1; camera shake service từ U3.

**Prompt:**

> Thêm Settings panel dùng chung, mở từ Main Menu và Pause Menu. Có Music volume,
> SFX volume, fullscreen/window mode, danh sách resolution hợp lệ, toggle camera
> shake. Lưu bằng key settings riêng, không nhét vào save tiến trình; load/apply
> trước khi scene gameplay phát audio. UI keyboard/mouse dùng được, đóng panel trả
> focus và trạng thái pause đúng. Builder sinh UI nhất quán style hiện có.

**Nghiệm thu:**

- Setting tồn tại sau restart, New Game không reset setting.
- Resolution list không trùng, chọn mode/resolution không crash trong Editor.
- Mở/đóng từ Pause không vô tình resume game; từ Main Menu không tạo overlay trùng.

### [x] U3 — Camera shake, hitstop và combat feedback

22/10/2026 — Đã triển khai driver hitstop có cap/recovery dùng chung quyền pause,
rung offset không tích drift và hai setting lưu riêng. Bảy bài tập trung đạt,
gồm boss slam/pause thật, direct/periodic/offscreen, smoothing/bounds/cleanup
và 12 ảnh cài đặt ở ba tỷ lệ. Source cuối đạt 78/78 EditMode, 141/141 PlayMode,
validator 0 lỗi, Console 0 error; timeScale=1 sau test. Ba tiêu chí kỹ thuật
U3 đã đạt; còn theo dõi cảm giác khi chơi dài. Chi tiết và giới hạn tại
`Verification/COMBAT_FEEDBACK_VERIFICATION.md`.

**Phụ thuộc:** G1. Có thể làm trước U2.

**Prompt:**

> Tạo service camera shake có intensity/duration và tôn trọng setting disable.
> Thêm shake nhẹ cho heavy hit/AoE/boss impact. Thêm hitstop rất ngắn khi player
> đánh trúng enemy, có request arbitration để nhiều hit cùng frame không kéo dài
> vô hạn. Hitstop không dùng cho poison tick, không phá pause, UI unscaled time,
> cooldown hoặc coroutine boss. CameraFollow2D vẫn sở hữu vị trí camera; shake chỉ
> thêm offset và trả về zero sau khi kết thúc.

**Nghiệm thu:**

- Shake không gây camera drift và tắt hoàn toàn qua Settings.
- Multi-hit không đóng băng quá cap đã cấu hình.
- Pause trong lúc hitstop và resume không làm `Time.timeScale` sai.

### [-] U4 — Tutorial lần đầu

22/10/2026 — Đã triển khai sáu bước theo binding hiện tại, bỏ qua bằng Esc/nút,
xem lại từ Hướng dẫn, migration save cũ và nối reference qua builder. Hai lượt
dựng + validator đạt, 77/77 EditMode và 105/105 PlayMode đạt; có 18 ảnh sáu bước
ở ba tỷ lệ màn hình. Đã cập nhật bước 2 theo G1: Space/nút dưới gamepad,
miễn đòn 0,18 giây và không né giữa attack/cast; 14/14 bài tập trung né/tutorial
và toàn bộ 130/130 PlayMode đạt, ảnh bước 2 ở 4:3 đã xem. Giữ `[-]` để đánh giá
với người chơi mới, không cần đợi bổ sung binding né nữa.

**Phụ thuộc:** G1 để nội dung phím điều khiển là cuối cùng.

**Prompt:**

> Thêm tutorial ngắn lần đầu vào Lobby, trình bày di chuyển, attack, dodge,
> interact, bốn skill và inventory theo binding hiện tại. Dùng các bước nhỏ có thể
> skip, không tự ẩn trước khi người chơi kịp đọc; hỗ trợ mở lại từ Settings/Help.
> Lưu `tutorialCompleted` trong progress nhưng phân biệt “đã xem trong save” với
> cài đặt profile nếu cần. Builder tạo panel và reference.

**Nghiệm thu:**

- Chỉ tự hiện đúng lần đầu theo rule đã chọn; Continue/New Game nhất quán.
- Text khớp binding thật, không còn xung đột mô tả phím E.
- Skip/complete save ngay; có thể xem lại thủ công.

### [-] U5 — Accessibility và UI regression pass

23/10/2026 — Tiếp phần charge của Vệ Thần Rừng: đã tái hiện side hit ngoài dải
báo và nổ chuyển vị trí khi caster bị dời. Chốt điểm đáp bằng Rigidbody/cast
collider; vòng cam khớp radius, MovePosition theo đường đã báo, ngắt nổ khi có
vật cản mới/knockback/chết/disable. Giữ quãng đường nominal, thời gian và damage.
Giữ marker tới khi đáp/hủy và ưu tiên không thu hồi marker khi pool đầy.
19/19 boss tập trung, 1/1 stress pool, 89/89 EditMode và 189/189 PlayMode toàn bộ
đạt (gồm 20 bài boss); validator đạt, Console gốc 0 lỗi/4 warning âm thanh đã
biết. Sáu ảnh ba tỷ lệ đã xem; Editor ở MainMenu, không Play/compile, timeScale=1,
settings/input native khôi phục. Chi tiết/giới hạn:
`Verification/FOREST_GUARDIAN_CHARGE_VERIFICATION.md`.
Giữ `[-]`: HUD 4:3 còn che một phần biên phải vòng báo; summon sát tường,
cảnh vật/camera rìa map và toàn trận thắng/thưởng/ra cửa vẫn cần hoàn thiện.

23/10/2026 — Vệ Thần Rừng: đã tái hiện bốn lỗi đòn sau chết/disable, tâm Slam
lệch vòng báo và animation radial ra pha sớm. Boss nay hủy routine/signature/
quán tính/vòng báo/tư thế cũ, khóa vị trí Slam/radial và giữ nhịp hồi phục.
VFX giữ biên vùng báo; đạn theo scene thu hồi khi unload, guard source đã bị hủy.
Slam cũng hất lùi ra xa tâm đã báo khi caster bị dời. Nguồn cuối đạt 89/89
EditMode, 180/180 PlayMode, validator; Console gốc 0 lỗi/4 warning âm thanh đã
biết. Đã xem sáu ảnh ba tỷ lệ bằng camera theo player. Sửa fixture Ancient Quake
để chờ nhịp đầu thật; 13/13 bài input/quake tập trung đạt sau cô lập input test
khỏi focus/thiết bị thật. Khôi phục settings/thiết bị; Editor về MainMenu,
không Play/compile, timeScale=1. Chi tiết:
`Verification/FOREST_GUARDIAN_FLOW_VERIFICATION.md`.
Giữ `[-]`: chưa nghiệm thu hình học line/impact của charge, summon sát tường,
camera/cảnh vật rìa map hoặc cân bằng toàn trận. Không đổi builder/content/stat.

23/10/2026 — Đền Kỹ Năng Map03 đã được thống nhất giữa hai đường dựng: 8 Cung
Thủ/4 Pháp Sư seed, target 18, pool 26 và 21 anchor trong hành lang. Dời seed
trùng rương/sát tường; validator chặn vùng săn không thể khởi tạo population.
Việt hóa tên quái/biến thể/di vật, giữ id. Sửa clone pool tạo hai thanh máu và
nhãn cũ chồng nhau; kiểm tra mỗi quái chỉ có một thanh, nhãn theo profile mới.
Hai lượt builder cùng nguồn cuối, validator, 6/6 tập trung, 89/89 EditMode và
169/169 PlayMode đạt; Console 0 lỗi/4 warning âm thanh đã biết, chín ảnh ba tỷ lệ.
Editor ở MainMenu, không Play/compile, timeScale=1. Chi tiết:
`Verification/MAP03_HUNTING_FLOW_VERIFICATION.md`.
Giữ `[-]`; phạm vi mới tiếp tục hoàn thiện chuyển động/kỹ năng/quái/boss/cảnh vật
trên các map sẵn có. Ảnh cửa với camera đặt riêng chưa xác minh camera chơi thật
ở rìa map; soundtrack, balance và nghiệm thu người chơi/Windows vẫn còn mở.

22/10/2026 — Đã tái hiện và sửa việc xóa primary rỗng/JSON lỗi lúc LoadProgress.
DTO parse/validate độc lập giữ live manager; backup lần lưu khác gần nhất, không
cho autosave ghi đè khi chưa giải quyết. Menu xem trước cấp/vàng, mặc định Để sau,
khôi phục xác nhận lại revision; primary mới hợp lệ được ưu tiên. Chơi mới có
xác nhận, reset cả backup và giữ raw lỗi gần nhất. Chặn callback nền/lặp/panel cũ,
Esc/Hủy trả focus và giữ record. 12/12 tập trung, 86/86 EditMode, 165/165 PlayMode,
validator đạt; Console 0 lỗi/4 warning âm thanh đã biết; 15 ảnh năm trạng thái/ba
tỷ lệ. Không đổi builder/content/schema v1. Chi tiết và giới hạn PlayerPrefs:
`Verification/SAVE_RECOVERY_FLOW_VERIFICATION.md`. Giữ `[-]` cho Map03 thiếu seed
quái, soundtrack, nghiệm thu accessibility/người chơi và Windows restart thật.

22/10/2026 — Tiếp tục kiểm tra sáu cửa giữa các map: builder gắn metadata đích,
biển/lời nhắc hiển thị tên vùng/cấp tiếng Việt, nền prompt dễ đọc. Chặn callback
xa/disable/chết/modal/scene sai và chuyến lặp; giữ quyền input/pause trong fade,
hủy trả điều khiển, vùng mới chỉ mở khi đến. Đã đi đủ chuỗi đến Lobby với tiến
trình/loadout/kho được giữ và kiểm tra nghi thức Lobby. Hai lượt dựng, validator,
9/9 tập trung, 80/80 EditMode và 157/157 PlayMode đạt; Console 0 lỗi, 18 ảnh ba
tỷ lệ. Kết quả cuối tại `Verification/MAP_TRANSITION_FLOW_VERIFICATION.md`.
Giữ `[-]`: còn luồng save hỏng, Map03 thiếu seed quái trong builder và nghiệm thu
người chơi/Windows; chưa đánh dấu toàn bộ UI hoặc game hoàn tất.

22/10/2026 — Đã tái hiện và sửa mất cấp/XP/kỹ năng mới khi gục và callback
Thử lại vẫn hoạt động lúc màn thua đóng. CaptureDefeatedPlayer chụp tiến trình
hiện tại và lưu HP/mana hồi đầy cho avatar scene kế tiếp, không hồi sinh xác
đang gục. Ba nhánh: thử lại, về sảnh trực tiếp, về menu rồi Tiếp tục; khóa hành
động lặp/modal khác và dọn focus/pause theo vòng đời. 7/7 tập trung (6 bài mới),
78/78 EditMode, 151/151 PlayMode và validator đạt; Console 0 lỗi. Sáu ảnh ba
tỷ lệ tại map thường/boss. Chi tiết: `Verification/RECOVERY_FLOW_VERIFICATION.md`.
Không đổi builder/schema/balance; giữ `[-]` cho nghiệm thu người chơi/Windows
và các luồng còn thiếu đã ghi trong báo cáo.

22/10/2026 — Đã thống nhất launcher 300×54 và scale tối thiểu cho chữ trên
Lobby/combat, tăng chữ thẻ Lobby/né/prompt; minimap có tên vùng hai dòng và
bốn chú giải. Tab/View vào/rời nhóm nút; Navigate/Submit dùng action UI thật
và không đồng thời điều khiển nhân vật. Modal giữ focus scope, tự động hóa giữ
trạng thái riêng; thông báo cử chỉ dùng HUD, tránh overlay trùng ở đáy màn hình.
4/4 bài tập trung, 78/78 EditMode, 145/145 PlayMode và validator đạt; Console
0 lỗi. 15 ảnh ba tỷ lệ, gồm trạng thái chờ camera. Chi tiết tại
`Verification/HUD_UTILITY_VERIFICATION.md`. Giữ `[-]` cho người chơi mới,
tay cầm/webcam thật và Windows player/restart.

22/10/2026 — HUD chiến đấu đã thêm số HP/mana, vùng/cấp/quái tách dòng, thanh
skill 596×180 có tên/phím/cooldown/thiếu mana/ô trống và khung boss có số máu.
Bốn bài tập trung đạt; 30 ảnh sáu map/ba tỷ lệ đã sinh, tên dài 4:3 đã xem.
78/78 EditMode, 134/134 PlayMode, validator đạt; Console 0 error. Chi tiết ở
`Verification/COMBAT_HUD_VERIFICATION.md`. Còn nghiệm thu
chữ launcher cũ, accessibility người mới, Windows và phản hồi combat U3.

22/10/2026 — Đã kiểm chứng kỹ năng/tiềm năng: focus scope, cache thẻ kỹ năng,
navigation/Submit thống nhất, phản hồi hành động và chống chi tiêu lặp mỗi frame.
Khung gọn tăng cỡ chữ ở 4:3; sửa tutorial cho đúng việc mua đồ tự trang bị.
77/77 EditMode, 122/122 PlayMode, validator đạt; 0 lỗi Console và 15 ảnh mới.
Giữ `[-]` cho nghiệm thu accessibility, người chơi mới và Windows player.

22/10/2026 — Đã kiểm chứng hành trang/cường hóa: giữ slot/focus, lọc trống
không giữ chi tiết món khác nhóm, chọn năm ô bằng chuột/phím số, xem trước chỉ
số/chi phí và cleanup khi disable/destroy. Hai bảng dùng khung responsive chung
với lò rèn; 77/77 EditMode, 117/117 PlayMode, validator và 23 ảnh ba tỷ lệ đạt.

22/10/2026 — Tiếp tục kiểm tra lò rèn: giữ focus khi chọn đồ, cô lập canvas nền,
đổi ưu đãi thực sự sang món khác khi còn lựa chọn và lưu phí ngay. Bố cục hai
cột ở màn hình hẹp, tên dài hai dòng. Kiểm tra cả 40 món, 14 ảnh; 77/77 EditMode,
111/111 PlayMode và validator đạt. Giữ `[-]`: hành trang/cường hóa vẫn cần
rà soát focus, thao tác chuột và cleanup; còn kiểm tra Windows/accessibility.

22/10/2026 — Đang rà soát luồng MainMenu → cài đặt/hướng dẫn → Pause → thử lại.
Ưu tiên bố cục vừa màn hình, focus bàn phím, khóa thao tác nền và cài đặt dùng
chung. Tiêu chí: validator, toàn bộ EditMode/PlayMode và ảnh 16:9, 16:10, 4:3;
chỉ xác nhận phần đã kiểm tra, không đóng toàn bộ backlog U5.

Mở rộng cùng lượt sang kho/nhận thưởng: sửa modal ownership, lưu lựa chọn skill,
catalog thưởng null trong scene và thêm kiểm tra vào builder/validator. Chi tiết:
`Verification/UI_FLOW_REVIEW_2026-10-22.md`.

Chọn vùng truyền tống đã đồng bộ tên/cấp từ MapData, Việt hóa ba tên còn lại,
tách ba dòng tên/cấp/trạng thái và khóa navigation xuyên canvas. Hai lượt dựng
+ validator đạt; toàn bộ 107/107 PlayMode đạt (0 failed/skipped), gồm tutorial,
audio và hai bài chọn vùng. Đã xem ảnh cuối ở 1080p/4:3; giữ `[-]` do còn
backlog accessibility và kiểm tra bản Windows.

10/09/2026 — Sửa lỗi font sau build: bake tiếng Việt vào atlas chính static,
giữ GUID, bổ sung guard và test glyph; không dựa vào fallback động để kiểm tra
tiếng Việt. Kết quả ở `Docs/Verification/VIETNAMESE_FONT_VERIFICATION.md`.

10/09/2026 — Đợt chuẩn bị đồ án: bổ sung hướng dẫn mở từ MainMenu/Pause,
xác nhận ghi đè save, tiếng Việt cho thao tác menu/màn thua và quyền sở hữu pause
khi mở hướng dẫn. Bỏ gold grant thử nghiệm nhưng giữ save cũ. Đây là đợt runtime
UI, không thay builder/content. Không đánh dấu U4/U5/T3 hoàn tất: chưa có tutorial
lần đầu, settings hiển thị đầy đủ, balance chính thức hay kiểm tra toàn bộ release.
Kết quả thực tế được ghi ở `Docs/Verification/GRADUATION_GAME_VERIFICATION.md`.

Wildlife Detail V2: chi tiết cá và tư thế chim tự nhiên hơn, giữ vùng hoạt động.
Validator, 73/73 EditMode và 69/69 PlayMode đạt; đã xem ảnh cá/chim bản cuối.
Xem `Docs/Verification/WILDLIFE_DETAIL_V2_VERIFICATION.md`.

Truyền tống V10: sửa cột sáng thẳng đứng và hạt sáng không hội tụ, giữ thời
gian/logic cổng. Trạng thái kiểm thử ở `Docs/Verification/TELEPORT_STRAIGHT_V10_VERIFICATION.md`.

Sân giữa Lobby: ba lữ khách đi–nghỉ và hai cụm hàng/đèn, giữ đường đến ba trạm.
Đã dựng hai lượt + validator, 73/73 EditMode và 69/69 PlayMode đạt; có ảnh
và video chuyển động. Chi tiết ở `Docs/Verification/LOBBY_VISITORS_V1_VERIFICATION.md`.

Tiềm năng/Kỹ năng V2: nâng giao diện khung đồng, biểu tượng chiêu và thao tác
chuột cho danh sách/bốn ô/nâng cấp, giữ progression. Validator và 73/73 EditMode,
69/69 PlayMode đạt; ảnh 1080p/720p/4:3 đã chụp, kiểm tra lớp hiển thị. Xem
`Docs/Verification/PROGRESSION_UI_V2_VERIFICATION.md`.

Vườn dưới Lobby: bổ sung bướm màu và chim trang trí qua builder, không đổi
nền/đường đi/save. Đang kiểm tra ở `Docs/Verification/LOWER_GARDEN_WILDLIFE_VERIFICATION.md`.

10/09/2026 — Restored V1: thay toàn bộ tranh Lobby và Map02 bằng hai ảnh
Real-ESRGAN đã duyệt; giữ bố cục/gameplay/save, đồng bộ ảnh hiệu ứng nước.
Đã dựng hai lượt + validator, 73/73 EditMode và 69/69 PlayMode đạt; đã xem
ảnh runtime Lobby/Map02. Xem `Docs/Verification/SCENERY_RESTORED_V1_VERIFICATION.md`.

Đợt 05/09/2026: hành trang và cửa hàng — bố cục vừa màn hình, cuộn catalog,
giá/khả năng mua rõ ràng, chỉ số cường hóa, khóa input và Esc. Các màn hình
Settings/Tutorial và phần accessibility toàn game vẫn ngoài phạm vi đợt này.

Đợt phong cảnh sảnh 05/09/2026: đã kiểm tra Trạm nghỉ Tán Rừng, tranh nền riêng,
HUD sảnh gọn, giới hạn camera, đường biên và ba trạm tương tác. Không thay gameplay
các map chiến đấu; 49/49 EditMode, 51/51 PlayMode và validator đạt.
Chi tiết/giới hạn: Docs/Verification/LOBBY_V3_VERIFICATION.md. Chưa đánh dấu toàn bộ U5 hoàn thành.

08/09/2026: căn truyền tống trận theo bệ đá V7, ánh ngọc lam nhẹ, chỉ sáng
nửa viền ngoài phía trước để không đè cột. Hai lượt builder, validator,
64/64 EditMode và 62/62 PlayMode đạt; sau tinh chỉnh ảnh chạy lại 2/2 test
Lobby/truyền tống đạt. Xem Docs/Verification/TELEPORT_ALIGNED_V8_VERIFICATION.md.

Tiếp 08/09/2026 — V9: bật vòng ngoài đầy đủ. Sau chọn vùng, camera lùi ra,
tia sáng/hạt sáng bay lên trong 3 giây rồi mới chuyển map. Khóa chọn lặp,
pause dừng tiến độ, ngắt quá trình trả camera/input. Hai lượt builder và
validator đạt; 64/64 EditMode, 62/62 PlayMode. Ảnh/giới hạn và danh sách
file: Docs/Verification/TELEPORT_CINEMATIC_V9_VERIFICATION.md. Không thêm âm thanh mới.

V4 không khí sảnh: đã kiểm tra nước chảy, thác/bọt, lửa và tàn lửa. Không thêm
asset bên ngoài; lớp hiệu ứng procedural lấy tranh nền hiện có làm nguồn.
Mở rộng V4: 4 cá bơi trong ao, 6 bướm tại ba cụm hoa và 7 lá rơi thưa.
Không thêm va chạm, thưởng, nhiệm vụ hoặc thay đổi save.
56/56 EditMode, 54/54 PlayMode và validator đạt. Hai lượt builder hoàn tất;
video và giới hạn xem Docs/Verification/LOBBY_LIVING_V4_VERIFICATION.md.

Truyền tống trận (06/09): giữ nền V3, không áp dụng vườn bàn ghế V5.
Đã bỏ cổng đứng Lobby; bước vào vòng đá kích hoạt rune và chuyển Map01 sau
1,15 giây, ra ngoài hủy, pause dừng tiến độ. 56/56 EditMode (gồm validator)
và 55/55 PlayMode đạt. Xem Docs/Verification/TELEPORT_CIRCLE_VERIFICATION.md.

**Phụ thuộc:** U2, U4.

**Prompt:**

> Rà soát toàn bộ HUD/menu ở các resolution phổ biến 16:9, 16:10 và 4:3. Sửa anchor,
> Safe Area, text overflow, sorting và navigation. Không chỉ dùng màu để truyền đạt
> cooldown/rarity/status; thêm icon/text phù hợp. Đảm bảo popup quest, achievement,
> tutorial, inventory, pause và settings có quy tắc ưu tiên, không cùng bắt input.

**Nghiệm thu:**

- Không có text bị cắt hoặc panel ngoài màn hình ở các resolution đã chọn.
- Chỉ một modal nhận input; gameplay bị khóa đúng khi modal yêu cầu.
- Có screenshot xác nhận ít nhất HUD, Inventory, Settings, Tutorial.

---

## M5 — Hiệu năng, balance và phát hành

### [ ] T1 — Object pooling

**Phụ thuộc:** C2, C4, U3 để biết đủ loại object cần pool.

**Prompt:**

> Profile trước khi tối ưu rồi thêm pool dùng chung cho object ngắn hạn có tần suất
> cao: EnemyProjectile, SkillProjectile/Fireball, DamagePopup và VFX phù hợp. Dùng
> interface lifecycle Spawn/Despawn để reset rigidbody, collider, timer, material,
> event subscription và status. Không pool object phức tạp nếu profiler không cho
> thấy lợi ích. Pool prewarm/cap theo cấu hình, tự mở rộng có giới hạn, cleanup khi
> chuyển context. Thay các đường Instantiate/Destroy mục tiêu và thêm test tái sử
> dụng không giữ state cũ.

**Nghiệm thu:**

- Có số đo profiler trước/sau trong cùng combat scenario.
- GC allocation/spike từ các object mục tiêu giảm rõ; không tuyên bố con số chưa đo.
- Projectile/VFX tái dùng không giữ damage, target, material hay callback cũ.

### [ ] T2 — Balance pass có số liệu

**Phụ thuộc:** hoàn thành M2, P3.

**Prompt:**

> Dựa trên bảng baseline và build hiện tại, tính effective HP, damage pressure,
> time-to-kill và reward/time cho từng map, boss, skill, equipment tier. Đề xuất
> bảng before/after trước khi sửa. Mục tiêu difficulty curve tăng đều từ Map01 đến
> IceBossMap01, tránh spike ở BossMap01/CaveMap01 và tránh bộ Mythic làm mất thử
> thách. Chỉ sửa số sau khi giải thích giả định; giữ số trong data/builder, không
> rải magic number vào runtime class. Sau đó playtest ít nhất ba loadout: yếu,
> trung bình, endgame.

**Nghiệm thu:**

- Có bảng before/after và lý do cho từng nhóm thay đổi.
- Không có skill vượt trội mọi tình huống hoặc boss one-shot ngoài đòn telegraph rõ.
- Gold/XP/giá shop tạo progression hợp lý, không phụ thuộc grant debug 2.000.000.
- Xem xét loại bỏ/migrate `GoldGrantKey` debug trước bản phát hành.

### [ ] T3 — Regression và release candidate

**Phụ thuộc:** tất cả hạng mục dự kiến cho release.

**Prompt:**

> Thực hiện release-candidate pass. Chạy toàn bộ EditMode/PlayMode test, builder hai
> lần, project validator và build player development. Test cả New Game và Continue
> bằng save cũ/mới qua toàn bộ luồng scene. Kiểm tra pause, game over, inventory,
> shop, chest, skill selection, quest, achievement, tutorial, settings và audio.
> Thu thập warning/error, sửa regression trong phạm vi thay đổi; lập danh sách issue
> còn lại có severity/reproduction. Cập nhật `CLAUDE.md`, README và roadmap theo
> trạng thái thật.

**Nghiệm thu:**

- Zero compiler error, missing reference, scene load error và exception trong luồng
  smoke test.
- Build player khởi động được từ MainMenu và hoàn thành vertical slice hang băng.
- Save cũ hợp lệ không mất inventory/equipment/skill/progress.
- Có checklist release, ảnh xác nhận và danh sách known issues minh bạch.

---

## 4. Ma trận kiểm thử tối thiểu

| Khu vực | EditMode | PlayMode/thủ công |
|---|---|---|
| Save | normalize, migration, idempotent reward | New Game, Continue, save cũ |
| Scene | validator path/build index | đi toàn bộ cửa theo hai chiều cần thiết |
| Combat | damage/status/falloff/cooldown | dodge, boss phase, death cleanup |
| Progression | quest/achievement condition | reload giữa tiến độ, claim một lần |
| Equipment | rarity/stat/item-id uniqueness | mua, equip, storage, reload |
| UI | data binding cơ bản | modal priority, resolution, pause |
| Pooling | reset lifecycle | profiler stress scenario |

## 5. Checklist cho mỗi phiên làm việc

- [ ] Chọn đúng một mã hạng mục và ghi trạng thái `[-]`.
- [ ] Chụp `git diff`/backup tương đương trước thay đổi lớn nếu dự án chưa dùng Git.
- [ ] Đọc file liên quan và xác nhận source of truth.
- [ ] Ghi tiêu chí kiểm thử trước khi sửa.
- [ ] Triển khai runtime, builder, save migration và UI wiring liên quan.
- [ ] Compile và chạy test.
- [ ] Chạy builder hai lần nếu builder bị sửa.
- [ ] Chạy `Top Down Game > Validate Project`.
- [ ] Playtest và chụp screenshot.
- [ ] Cập nhật tài liệu/trạng thái, chỉ chuyển `[x]` khi đủ Definition of Done.

## 6. Ngoài phạm vi hiện tại

Các mục sau chưa nên làm trước khi T3 ổn định: multiplayer, procedural map, đổi
save sang cloud/backend, thay toàn bộ combat architecture, Addressables quy mô lớn,
mobile controls và localization nhiều ngôn ngữ. Nếu đưa vào scope, hãy tạo milestone
riêng với yêu cầu, migration và tiêu chí hiệu năng cụ thể.
