# Baseline audit

Ảnh chụp hiện trạng phục vụ milestone M0. Số liệu dưới đây được đối chiếu trực
tiếp với code và generated asset ngày 09/08/2026, trước khi thêm gameplay mới.

## Môi trường

- Unity: `6000.5.6f1`.
- Render pipeline: URP `17.5.0`.
- Input System: `1.20.0`.
- Unity Test Framework: `1.7.0`.
- Content source of truth: `Assets/Editor/TopDownGameBuilder.cs`.
- Save chính: PlayerPrefs key `TopDownGame.Progress.v1`.

## Scene và luồng chơi

| Scene | Nội dung được builder tạo | Scene kế tiếp |
|---|---|---|
| MainMenu | New Game, Continue, Quit | Lobby hoặc map đã lưu |
| Lobby | Player, shop, storage, portal | Map01 |
| Map01 | 10 Slime | Map02 |
| Map02 | 8 Goblin, 4 Archer | Map03 |
| Map03 | Skill shrine, không có quái đặt sẵn | BossMap01 |
| BossMap01 | Forest Guardian, Slime được boss summon | CaveMap01 |
| CaveMap01 | 6 Mage, 4 Tank | Lobby |

Cả bảy scene đều tồn tại trong `Assets/Scenes` và đang được builder khai báo cho
Build Settings. `MapManager.demoMinimumEnemyCount` mặc định là 10; nó bổ sung bản
sao chỉ khi scene có ít nhất một enemy nhưng chưa đủ 10.

## Enemy baseline

Mọi enemy có detection range 10 và item drop chance 15%. Armor mặc định là 3;
Tank và Boss có armor 25.

| Enemy id | Archetype | HP | Damage | Armor | Move | Attack cooldown | Attack range | Gold |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| slime | Slime | 90 | 8 | 3 | 2.0 | 1.2 | 1.1 | 3 |
| goblin | Goblin | 140 | 12 | 3 | 3.2 | 1.2 | 1.2 | 5 |
| archer | Archer | 100 | 10 | 3 | 2.2 | 6.0 | 7.0 | 6 |
| mage | Mage | 120 | 16 | 3 | 1.8 | 7.0 | 8.0 | 8 |
| tank | Tank | 360 | 25 | 25 | 1.0 | 1.5 | 1.5 | 15 |
| forest guardian | Boss | 1200 | 35 | 25 | 1.1 | 5.0 | 2.3 | 100 |

## Skill baseline

Builder đang ép toàn bộ cooldown về `DemoSkillCooldown = 0.5` giây và max level
về 5.

| SkillId | Damage | Mana | Cooldown | Range |
|---|---:|---:|---:|---:|
| Fireball | 40 | 15 | 0.5 | 6 |
| IceDash | 35 | 20 | 0.5 | 4 |
| SwordSpin | 50 | 22 | 0.5 | 2 |
| LightningStrike | 45 | 30 | 0.5 | 7 |
| Healing | 0 | 25 | 0.5 | 0 |
| Shield | 0 | 20 | 0.5 | 0 |

## Equipment baseline

Catalog có 40 asset: tám weapon và tám bộ bốn món Helmet/Armor/Pants/Necklace.
Rarity hiện không được lưu trực tiếp mà suy ra hoàn toàn từ `price`:

| Giá | Rarity hiện tại | Nhãn |
|---:|---|---|
| ≤ 250 | Common | Thường |
| 251–350 | Uncommon | Tinh nhuệ |
| 351–450 | Rare | Hiếm |
| 451–550 | Epic | Sử thi |
| 551–700 | Legendary | Huyền thoại |
| 701–850 | Mythic | Thần thoại |
| > 850 | Divine | Thần thánh |

Giá weapon: Steel Sword 200, Greatsword 450, Broadsword 380, Battle Axe 500,
War Hammer 520, Long Spear 420, Dual Blades 600, Dagger 350.

Các set armor trong catalog: Iron, Warrior, Guardian, Knight, Lion, Destroyer,
Gold và Divine. Do rarity dựa vào giá, tên set và rarity UI đang lệch nhau ở một
số món: Divine Helmet giá 850 hiện thành Mythic; Gold Armor giá 850, Gold Pants
giá 720 và Gold Necklace giá 800 cũng thành Mythic. Đây là dữ liệu đầu vào cho
hạng mục P3, chưa sửa tại M0.

## Input baseline

| Hành động | Binding thực tế |
|---|---|
| Di chuyển | WASD, phím mũi tên, gamepad left stick |
| Click-to-move | Chuột trái |
| Attack | Chuột phải hoặc J |
| Skill slot 1–4 | Q, E, R, F |
| Interact | E hoặc nhấp đúp target |
| Inventory | I |
| Skill equipment | K; 1–4 chọn slot |
| Pause/back | Esc |
| Action chưa dùng | `Jump`: Space, gamepad button South |

E được dùng cho cả skill slot 2 và interact. `SkillManager` chủ động không cast
slot 2 khi `PlayerInteraction.HasTarget`, nên không kích hoạt cả hai trong cùng
frame; tuy vậy tutorial/UI phải giải thích ưu tiên này hoặc đổi binding về sau.

## Sai lệch và rủi ro đã phát hiện

1. `MageEnemy` là sealed, trái với prompt cũ yêu cầu Necromancer kế thừa trực tiếp.
2. Folder Lightning thật là `Skills/Lightning/`, không phải `LightningStrike/`.
3. `Mythic` đã tồn tại dưới `Divine`, trái với yêu cầu Mythic là bậc cao nhất.
4. `Healing` và `Shield` được builder gán range 0 dù field khai báo `[Min(0.1f)]`.
5. Mô tả SkillData và một số thông báo map vẫn là tiếng Anh dù quy ước UI là
   tiếng Việt.
6. `GameProgressManager` có one-time debug grant 2.000.000 vàng qua
   `TopDownGame.Progress.GoldGrant.v1`; nó làm sai dữ liệu economy khi balance.
7. Content runtime chưa có test assembly/test riêng dù package test đã được cài.
8. Dự án chưa là Git repository, nên chưa có diff/history an toàn cho thay đổi lớn.

Các rủi ro gameplay/economy được ghi nhận để xử lý tại milestone tương ứng, không
được âm thầm sửa trong baseline audit.

## Kết quả milestone M0

- Thêm `Top Down Game > Validate Project`; baseline pass sau builder.
- Thêm bốn EditMode test và một PlayMode smoke test; kết quả 5/5 pass.
- Tách runtime/editor/test assembly và khai báo migration cho generated data cũ.
- Sửa builder reload MapData persistent sau khi tạo scene mới; trước đó năm
  `MapManager` có thể bị lưu với `mapData` null.
- Chạy builder hai lần: số lượng giữ nguyên 7 scene, 6 skill, 6 enemy data,
  6 enemy prefab và 40 equipment; không sinh bản trùng.
