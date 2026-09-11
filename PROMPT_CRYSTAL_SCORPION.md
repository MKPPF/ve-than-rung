# Prompt phát triển quái: Bọ Cạp Pha Lê

> Phát triển một quái thường mới tên **Bọ Cạp Pha Lê** (`Crystal Scorpion`),
> dùng `enemyId` ổn định `crystalscorpion`, dành cho `CaveMap01`.
>
> Tuân thủ kiến trúc `EnemyBase` → `EnemyData` → subclass hành vi và coi
> `Assets/Editor/TopDownGameBuilder.cs` là source of truth. Thêm archetype riêng,
> data, prefab và placement bằng builder; không tạo content thủ công chỉ tồn tại
> trong scene. Không sao chép toàn bộ AI của Mage/Tank.
>
> Bọ Cạp Pha Lê chiến đấu ở tầm trung: tiến vào cự ly, lùi khi người chơi áp sát
> và thỉnh thoảng strafe. Đòn đặc trưng là **Crystal Charge**: khóa hướng ở đầu
> đòn, vẽ telegraph đường thẳng màu cyan ít nhất 0,5 giây, sau đó lao theo hướng
> đã báo và tạo vụ nổ tinh thể nhỏ ở điểm kết thúc. Damage chỉ xảy ra sau
> telegraph, tối đa một lần mỗi lần dùng; áp dụng knockback và life-steal variant
> qua API sẵn có. Coroutine phải không gây damage sau khi quái chết và trạng thái
> phải reset đúng khi object được tái sử dụng từ pool.
>
> Chỉ số gốc mục tiêu: 210 HP, 18 damage, 14 armor, 2,5 move speed, 4,6 attack
> range, 2,4 giây cooldown, 12 vàng; vẫn nhận level scaling và enemy variant từ
> `EnemyRuntimeProfile`. Tích hợp 3 con vào các vị trí spawn còn trống của
> `CaveMap01`, đồng thời giữ Mage và Tank để không làm mất archetype hiện có.
>
> Dùng sprite pixel-art riêng `CrystalScorpion.png` với alpha trong suốt thật,
> không dùng nền caro bị bake, không dùng placeholder đổi màu. Builder phải import
> và gắn sprite theo quy ước `GeneratedCharacterSprite` hiện tại.
>
> Cập nhật EditMode test để xác nhận catalog có đúng 7 enemy id duy nhất, prefab
> có `CrystalScorpionEnemy` và đúng data, sprite có alpha, còn `CaveMap01` chứa
> Mage, Tank và Crystal Scorpion. Chạy builder hai lần để kiểm tra idempotence,
> chạy validator, EditMode và PlayMode test; không kết luận hoàn thành nếu còn lỗi
> compile hoặc Console error.

## Nghiệm thu

- Builder chạy hai lần không sinh asset, prefab hoặc enemy scene object trùng.
- Crystal Charge có telegraph rõ, gây damage sau windup và reset an toàn khi pool.
- CaveMap01 giữ cả ba nhóm Mage, Tank và Crystal Scorpion.
- Sprite có alpha thật và được prefab sử dụng.
- Validator, EditMode và PlayMode test đều pass.
