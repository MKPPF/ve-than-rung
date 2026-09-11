# Prompt phát triển di chuyển và tấn công: Bọ Cạp Pha Lê

> Nâng cấp `CrystalScorpionEnemy` thành quái tầm trung có vòng chiến đấu dễ đọc,
> công bằng và không rung hướng liên tục. Dùng state rõ ràng để người chơi có thể
> nhận biết quái đang truy đuổi, đi vòng, lùi, báo đòn, lao hoặc hồi chiêu.
>
> **Di chuyển:** khi người chơi còn xa nhưng trong detection range, quái truy đuổi;
> khi vào vành đai khoảng 2,2–4,0 world unit, quái orbit quanh người chơi với tốc
> độ thấp hơn tốc chạy; hướng orbit đổi sau một khoảng ngẫu nhiên thay vì đổi mỗi
> frame. Nếu người chơi áp sát dưới 2,2 unit, quái lùi ra. Sau mỗi đòn, quái có
> khoảng recovery ngắn và tái định vị bằng lùi hoặc strafe. Mọi chuyển động phải
> đi qua `EnemyMovement`, không sửa transform trực tiếp và không tự flip sprite.
>
> **Tấn công:** có ba đòn theo khoảng cách và nhịp pattern. `Pincer Snap` dùng khi
> người chơi áp sát: khóa điểm đánh, telegraph vòng tròn rồi kẹp, gây damage một
> lần nếu người chơi vẫn trong vùng. `Crystal Charge` dùng ở tầm trung: khóa hướng,
> telegraph đường thẳng tối thiểu 0,5 giây, sau đó lao và nổ ở điểm kết thúc.
> `Tail Crystal Spike` xuất hiện theo chu kỳ mỗi ba đòn: khóa vị trí người chơi,
> telegraph vòng tròn lâu hơn rồi tinh thể trồi lên. Không đòn nào được bám theo
> vị trí mới của người chơi sau khi telegraph đã bắt đầu.
>
> Mỗi đòn chỉ gây damage sau telegraph, tối đa một lần, dùng `DamageInfo`, có
> knockback phù hợp và gọi life-steal variant qua `EnemyRuntimeProfile`. Không gây
> damage nếu quái đã chết. Khi object được pool/re-enable, reset coroutine, combat
> state, attack pattern, orbit timer và hướng strafe; không giữ state từ vòng đời
> trước.
>
> Builder phải cấu hình các tham số chiến đấu trên prefab để tiếp tục là source of
> truth. Thêm PlayMode test chứng minh ba dải khoảng cách lần lượt tạo Pursuing,
> Orbiting, Retreating; thêm test chứng minh Tail Crystal Spike không gây damage
> trước khi hết telegraph nhưng gây damage sau đó. Chạy builder hai lần, validator,
> EditMode và PlayMode; không hoàn thành nếu còn compile error hoặc test fail.

## Nghiệm thu

- Quái không đổi hướng orbit mỗi frame và không vừa di chuyển vừa windup.
- Ba đòn có vai trò/range khác nhau, telegraph khóa trước và né được.
- Damage chỉ xảy ra sau telegraph, một lần mỗi đòn.
- State và counter reset đúng khi pool.
- Builder idempotent, validator và toàn bộ test pass.
