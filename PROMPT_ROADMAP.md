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
cảm giác thực tế. Xem PLAYER_WALK_V11_VERIFICATION.md.

### [ ] G1 — Dodge roll có i-frame

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
giữ camera/bố cục/gameplay. Trạng thái ở `MAP02_CLARITY_V2_VERIFICATION.md`.
Đã dựng hai lượt + validator, 73/73 EditMode và 69/69 PlayMode đạt; đã xem
ảnh so sánh và runtime. Đây là tăng nét nhẹ, không bổ sung chi tiết tranh gốc.

Living V6: người dùng không thích nét đỏ/đen chồng lên quái trong V5. Bỏ
viền đen và cạnh nối tâm của báo chém, nét mảnh dưới actor; thêm cá bơi ở
hồ và sông qua habitat builder, không collider/reward/save. Kiểm tra đang
thực hiện; xem `MAP02_LIVING_V6_VERIFICATION.md`.

Living V6 đã dựng hai lượt cuối + validator đạt; toàn bộ 70/70 EditMode,
69/69 PlayMode đạt. Sau căn thêm vùng cá tránh đá nổi, 7/7 Map02 đạt. Đã
xem ảnh hồ, sông và cảnh báo trong combat; 8 cá hồ, 12 cá sông. Chưa thêm
cơ chế câu/bắt cá, không thay gameplay/save. Phần cảnh báo V5 bên dưới là
lịch sử đã được thay bằng nét mảnh dưới actor trong V6.

Color V5 theo yêu cầu khắc phục màu nhạt: shader riêng trên tranh Map02 giảm
vàng xám, tăng tương phản có chọn lọc và giữ màu hồ; cảnh báo có viền tối.
Không vẽ lại bitmap hay sửa chỉ số/kỹ năng. Kiểm thử/so ảnh đang được ghi ở
`MAP02_COLOR_V5_VERIFICATION.md`; phần lịch sử V4/V3 bên dưới được giữ lại.

Color V5 đã dựng hai lượt + validator đạt, 70/70 EditMode và 69/69 PlayMode
trong Editor đạt. Đã so ảnh cùng góc trước/sau và xem cảnh báo trong combat.
Chỉnh rộng cửa vào V4 cũng đã được dựng/kiểm tra: route clearance đến mọi
điểm quái đạt. Trạng thái CHƯA dựng bên dưới là lịch sử, đã được xử lý trong
lượt V5. MAP2 tổng thể giữ `[-]` cho cảm nhận người dùng/loadout balance.

V4 theo phản hồi người dùng: không chấp nhận bản V3 quá đơn giản. Thay bằng
Tàn Tích Suối Ngọc: tranh cảnh hoàn chỉnh, collider theo tranh, navigation
tránh vật cản và hai bộ kỹ năng/di chuyển mới cho Goblin/cung thủ. Chi tiết
và kiểm thử đang được ghi tại `MAP02_V4_VERIFICATION.md`. Các ghi chú V3
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
Chi tiết, ảnh, nguồn art imagegen và giới hạn: `MAP02_V3_VERIFICATION.md`.

### [ ] U1 — AudioManager và audio event

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

### [ ] U2 — Settings dùng chung cho Main Menu và Pause

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

### [ ] U3 — Camera shake, hitstop và combat feedback

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

### [ ] U4 — Tutorial lần đầu

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

10/09/2026 — Sửa lỗi font sau build: bake tiếng Việt vào atlas chính static,
giữ GUID, bổ sung guard và test glyph; không dựa vào fallback động để kiểm tra
tiếng Việt. Kết quả ở `VIETNAMESE_FONT_VERIFICATION.md`.

10/09/2026 — Đợt chuẩn bị đồ án: bổ sung hướng dẫn mở từ MainMenu/Pause,
xác nhận ghi đè save, tiếng Việt cho thao tác menu/màn thua và quyền sở hữu pause
khi mở hướng dẫn. Bỏ gold grant thử nghiệm nhưng giữ save cũ. Đây là đợt runtime
UI, không thay builder/content. Không đánh dấu U4/U5/T3 hoàn tất: chưa có tutorial
lần đầu, settings hiển thị đầy đủ, balance chính thức hay kiểm tra toàn bộ release.
Kết quả thực tế được ghi ở `GRADUATION_GAME_VERIFICATION.md`.

Wildlife Detail V2: chi tiết cá và tư thế chim tự nhiên hơn, giữ vùng hoạt động.
Validator, 73/73 EditMode và 69/69 PlayMode đạt; đã xem ảnh cá/chim bản cuối.
Xem `WILDLIFE_DETAIL_V2_VERIFICATION.md`.

Truyền tống V10: sửa cột sáng thẳng đứng và hạt sáng không hội tụ, giữ thời
gian/logic cổng. Trạng thái kiểm thử ở `TELEPORT_STRAIGHT_V10_VERIFICATION.md`.

Sân giữa Lobby: ba lữ khách đi–nghỉ và hai cụm hàng/đèn, giữ đường đến ba trạm.
Đã dựng hai lượt + validator, 73/73 EditMode và 69/69 PlayMode đạt; có ảnh
và video chuyển động. Chi tiết ở `LOBBY_VISITORS_V1_VERIFICATION.md`.

Tiềm năng/Kỹ năng V2: nâng giao diện khung đồng, biểu tượng chiêu và thao tác
chuột cho danh sách/bốn ô/nâng cấp, giữ progression. Validator và 73/73 EditMode,
69/69 PlayMode đạt; ảnh 1080p/720p/4:3 đã chụp, kiểm tra lớp hiển thị. Xem
`PROGRESSION_UI_V2_VERIFICATION.md`.

Vườn dưới Lobby: bổ sung bướm màu và chim trang trí qua builder, không đổi
nền/đường đi/save. Đang kiểm tra ở `LOWER_GARDEN_WILDLIFE_VERIFICATION.md`.

10/09/2026 — Restored V1: thay toàn bộ tranh Lobby và Map02 bằng hai ảnh
Real-ESRGAN đã duyệt; giữ bố cục/gameplay/save, đồng bộ ảnh hiệu ứng nước.
Đã dựng hai lượt + validator, 73/73 EditMode và 69/69 PlayMode đạt; đã xem
ảnh runtime Lobby/Map02. Xem `SCENERY_RESTORED_V1_VERIFICATION.md`.

Đợt 05/09/2026: hành trang và cửa hàng — bố cục vừa màn hình, cuộn catalog,
giá/khả năng mua rõ ràng, chỉ số cường hóa, khóa input và Esc. Các màn hình
Settings/Tutorial và phần accessibility toàn game vẫn ngoài phạm vi đợt này.

Đợt phong cảnh sảnh 05/09/2026: đã kiểm tra Trạm nghỉ Tán Rừng, tranh nền riêng,
HUD sảnh gọn, giới hạn camera, đường biên và ba trạm tương tác. Không thay gameplay
các map chiến đấu; 49/49 EditMode, 51/51 PlayMode và validator đạt.
Chi tiết/giới hạn: LOBBY_V3_VERIFICATION.md. Chưa đánh dấu toàn bộ U5 hoàn thành.

08/09/2026: căn truyền tống trận theo bệ đá V7, ánh ngọc lam nhẹ, chỉ sáng
nửa viền ngoài phía trước để không đè cột. Hai lượt builder, validator,
64/64 EditMode và 62/62 PlayMode đạt; sau tinh chỉnh ảnh chạy lại 2/2 test
Lobby/truyền tống đạt. Xem TELEPORT_ALIGNED_V8_VERIFICATION.md.

Tiếp 08/09/2026 — V9: bật vòng ngoài đầy đủ. Sau chọn vùng, camera lùi ra,
tia sáng/hạt sáng bay lên trong 3 giây rồi mới chuyển map. Khóa chọn lặp,
pause dừng tiến độ, ngắt quá trình trả camera/input. Hai lượt builder và
validator đạt; 64/64 EditMode, 62/62 PlayMode. Ảnh/giới hạn và danh sách
file: TELEPORT_CINEMATIC_V9_VERIFICATION.md. Không thêm âm thanh mới.

V4 không khí sảnh: đã kiểm tra nước chảy, thác/bọt, lửa và tàn lửa. Không thêm
asset bên ngoài; lớp hiệu ứng procedural lấy tranh nền hiện có làm nguồn.
Mở rộng V4: 4 cá bơi trong ao, 6 bướm tại ba cụm hoa và 7 lá rơi thưa.
Không thêm va chạm, thưởng, nhiệm vụ hoặc thay đổi save.
56/56 EditMode, 54/54 PlayMode và validator đạt. Hai lượt builder hoàn tất;
video và giới hạn xem LOBBY_LIVING_V4_VERIFICATION.md.

Truyền tống trận (06/09): giữ nền V3, không áp dụng vườn bàn ghế V5.
Đã bỏ cổng đứng Lobby; bước vào vòng đá kích hoạt rune và chuyển Map01 sau
1,15 giây, ra ngoài hủy, pause dừng tiến độ. 56/56 EditMode (gồm validator)
và 55/55 PlayMode đạt. Xem TELEPORT_CIRCLE_VERIFICATION.md.

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
