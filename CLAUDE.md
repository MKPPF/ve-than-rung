# CLAUDE.md — Ngữ cảnh dự án cho AI

File này để bất kỳ AI coding assistant nào đọc trước khi sửa code, giúp giữ đúng
kiến trúc và quy ước đã có thay vì đoán lại từ đầu.

## Dự án là gì

Game hành động top-down 2D làm bằng Unity (URP, 2D Renderer, Input System mới).
Người chơi bắt đầu ở Lobby → mua/trang bị đồ ở lò rèn → đi qua các map chiến đấu
(Map01 → Map02 → Map03 → BossMap01 → CaveMap01 → RiftMap01) → tiêu diệt quái, nhặt vàng/XP,
mở rương → học/nâng cấp skill → đối đầu boss Forest Guardian. Toàn bộ tiến trình
(map, HP/mana, vàng, level/XP, trang bị, skill, rương) được lưu tự động.

Điều khiển: WASD/phím mũi tên hoặc chuột trái để di chuyển, chuột phải hoặc J để
đánh thường, Q/E/R/F dùng 4 skill đã trang bị, E tương tác, I mở hành trang, Esc
tạm dừng.

## QUAN TRỌNG NHẤT: nội dung được sinh bằng code, không sửa tay trong Editor

Toàn bộ scene, prefab và ScriptableObject (skill, quái, trang bị, map) đều do
**`Assets/Editor/TopDownGameBuilder.cs`** tạo ra bằng một menu editor duy nhất:
**`Top Down Game > Build Complete MVP`**. Đây là nguồn sự thật (source of truth)
của toàn bộ nội dung game.

Hệ quả cho việc phát triển tiếp:

- Muốn thêm quái mới, skill mới, món trang bị mới, map mới → sửa
  `TopDownGameBuilder.cs` (thêm lời gọi `CreateEnemy`, `CreateSkill`,
  `CreateEquipment`, `CreateMap`, ...) rồi chạy lại
  `Top Down Game > Build Complete MVP`. Không tạo asset/prefab thủ công trong
  Editor rồi để builder ghi đè mất công sửa.
- Builder này idempotent theo path: nếu asset đã tồn tại ở đúng path thì nó load
  lại và ghi đè field thay vì tạo bản mới — nên chạy lại nhiều lần là an toàn.
- Hai menu khác: `Top Down Game > Open Main Menu` và
  `Top Down Game > Edit Map01` chỉ mở scene có sẵn, không tạo gì mới.
- Menu `Top Down Game > Validate Project` kiểm tra scene graph, Build Settings,
  catalog, prefab và reference bắt buộc; phải chạy sau builder.
- Khi test trong Editor: dữ liệu chỉ lưu khi bắt đầu game mới, chuyển scene, hoặc
  quay về Main Menu — bấm Stop giữa chừng không ghi đè save thật.

## Cấu trúc thư mục

```
Assets/
  Editor/
    TopDownGameBuilder.cs          # nguồn sinh toàn bộ content
    TopDownProjectValidator.cs     # validate project trước build/playtest
  Scripts/
    Combat/     # DamageInfo, Hitbox/Hurtbox, IDamageable, DamagePopup
    Core/       # interface dùng chung, CameraFollow2D, RuntimeSpriteFactory
    Enemies/
      Base/     # EnemyBase, EnemyData, EnemyHealth, EnemyMovement, animator, vfx
      Types/    # MeleeEnemy, RangedEnemy, MageEnemy
      Bosses/   # ForestGuardianBoss
    Equipment/  # EquipmentData, EquipmentInventory, StorageChest, WeaponShopNpc
    Items/      # ItemData, ItemPickup
    Maps/       # MapManager, MapData, MapDoor, LobbyPortal, RewardChest, transition
    Player/     # movement, combat, health, stats, interaction, animator
    Save/       # GameProgressManager (PlayerPrefs)
    Skills/
      Base/     # data, manager, inventory, slot, cooldown, projectile
      Fireball/ IceDash/ SwordSpin/ Lightning/ Healing/ Shield/
    UI/         # HUD, inventory, pause, game over, menu, skill, shop, storage
    TopDownGame.Runtime.asmdef
  Tests/
    EditMode/   # catalog, validator và test logic thuần
    PlayMode/   # runtime smoke test
  Prefabs/Player/, Prefabs/Enemies/
  ScriptableObjects/Skills|Enemies|Equipment|Items|Maps/
  Scenes/       # MainMenu, Lobby, Map01-03, BossMap01, CaveMap01, RiftMap01
  Art/Generated/Characters|Props|SkillIcons|Environment|EquipmentIcons/
  Settings/     # URP assets, InputSystem_Actions.inputactions
  Screenshots/  # ảnh chụp verify tính năng
```

Namespace theo đúng thư mục: `TopDownGame.Player`, `TopDownGame.Enemies`,
`TopDownGame.Enemies.Bosses`, `TopDownGame.Skills`, `TopDownGame.Equipment`,
`TopDownGame.Items`, `TopDownGame.Maps`, `TopDownGame.Save`, `TopDownGame.UI`,
`TopDownGame.Core`, `TopDownGame.Combat`, `TopDownGame.EditorTools`.

## Quy ước code đang dùng

- Class MonoBehaviour: `public sealed class`, field private + `[SerializeField]`,
  property public dạng `=> field`, không có setter public bừa bãi.
- Header rõ ràng bằng `[Header("...")]`, validate bằng `[Min(...)]`/`[Range(...)]`
  ngay trên field thay vì check tay trong code.
- Comment XML `/// <summary>` ở đầu class giải thích lý do thiết kế, không chỉ mô
  tả lại tên hàm.
- Di chuyển: `Rigidbody2D` + `MovePosition` trong `FixedUpdate`, có accel/decel
  riêng, không set velocity thẳng. Input đọc qua Input System action, có fallback
  đọc bàn phím trực tiếp nếu action không tìm thấy.
- Animation/hướng nhân vật do animator component sở hữu; script khác không tự ý
  set `flipX` hay transform visual để tránh đè lên nhau.
- Combat dùng interface `IDamageable` + `Hitbox`/`Hurtbox`, không gọi thẳng
  `TakeDamage` giữa các script không liên quan.
- Enemy có ba lớp: `EnemyBase` (contract) → `EnemyData` (ScriptableObject) →
  subclass hành vi (`MeleeEnemy`/`RangedEnemy`/`MageEnemy`/boss). Ưu tiên kế thừa
  hoặc tái dùng các lớp này thay vì viết lại.
- Skill theo mẫu `SkillBase` + `SkillData` + hiệu ứng riêng. `SkillManager` đọc
  Q/E/R/F, trừ mana và quản lý cooldown qua `SkillCooldown`/`SkillSlot`.

## Gói mở rộng Tàn Tích Nhật Thực

- `RiftMap01` nối sau `CaveMap01`, cấp đề nghị 50–75, có Rift Storm báo trước
  vùng nguy hiểm và liên tục tạo áp lực di chuyển.
- Ba nhóm địch khu vực: Đao Phủ Hư Không (`VoidReaverEnemy`), Pháp Sư Khe Nứt
  và Vệ Binh Hắc Diện. Hai nhóm sau tái sử dụng Mage/Tank với profile riêng.
- Ba skill mới: `ChainLightning`, `MeteorShower`, `VoidPulse`; tổng catalog 9 skill.
- Lò rèn có một ưu đãi -20% cho mỗi nhóm trang bị trong mỗi lượt, đổi lượt tốn
  100 vàng; mua giá giảm vẫn lưu sở hữu/trang bị bằng item id như cũ.
- Trang bị có năm slot (vũ khí, mũ, giáp, quần, dây chuyền), nhiều độ hiếm và
  nhiều stat nhỏ; UI tổng hợp thành Điểm Sức Mạnh.
- Text hiển thị cho người chơi viết tiếng Việt; tên biến/hàm/class dùng tiếng Anh.

## Save và test

Save người chơi dùng key `TopDownGame.Progress.v1`. Test command line (`-runTests`)
tự chuyển sang key `TopDownGame.Tests.*` và cleanup sau test; không được viết test
dùng `PlayerPrefs.DeleteAll()`.

Test assembly:

- `TopDownGame.EditModeTests`: test catalog, validator, migration và logic thuần.
- `TopDownGame.PlayModeTests`: smoke test hành vi runtime.

Mở `Window > General > Test Runner` hoặc chạy Unity command line với
`-runTests -testPlatform EditMode|PlayMode -testResults <path.xml>`.

## Art & assets

`Assets/Art/Generated/**` là sprite do AI sinh ra. Builder load bằng path cố định
(`CharacterArtRoot`, `SkillIconArtRoot`, `EquipmentIconArtRoot`, ...) nên khi thêm
asset mới phải đặt đúng tên/path hoặc cập nhật hằng số tương ứng.

`Assets/Screenshots/**` là ảnh chụp kết quả trong lúc phát triển để verify bằng
mắt. Đặt tên theo dạng `TênTínhNăng_TrạngThái.png` để dễ theo dõi lịch sử.

## Khi bắt đầu một phiên làm việc mới với AI

1. Đọc `Assets/Scripts/README.md`, file này và hạng mục tương ứng trong
   `PROMPT_ROADMAP.md`.
2. Xác định tính năng nằm ở namespace/thư mục nào.
3. Nếu liên quan content (quái/skill/trang bị/map) → sửa builder trước, rồi mới
   viết logic runtime nếu cần.
4. Sau khi sửa, chạy lại `Top Down Game > Build Complete MVP` hai lần nếu builder
   thay đổi, rồi chạy `Top Down Game > Validate Project`.
5. Chạy EditMode và PlayMode test, kiểm tra Console, playtest và chụp screenshot
   trước khi đánh dấu hạng mục hoàn thành.
