# Prompt phát triển di chuyển và tấn công: quái thường trong CaveMap01

> Nâng cấp hai quái đã có trong `CaveMap01` là Mage và Tank để mỗi loại có
> cách giữ vị trí, nhịp tấn công và telegraph riêng, đạt cùng mức độ dễ đọc và
> công bằng như Bọ Cạp Pha Lê. Không thay đổi sprite, chỉ số `EnemyData`, số
> lượng quái hoặc bố cục map.
>
> **Mage:** khi ở xa thì tiến vào tầm phép; trong vành đai chiến đấu thì orbit
> quanh người chơi với hướng đổi theo timer, không đổi ngẫu nhiên mỗi frame; khi
> bị áp sát thì lùi ra. Đòn thường `Rune Burst` khóa vị trí người chơi, hiện vòng
> telegraph rồi mới nổ. Mỗi đòn thứ ba dùng `Greater Rune`: telegraph lâu hơn,
> vùng lớn hơn và sát thương cao hơn, nhưng vẫn chỉ gây damage một lần tại vị trí
> đã khóa. Sau khi cast, Mage strafe/lùi trong một khoảng recovery ngắn.
>
> **Tank:** khi ở xa thì truy đuổi; ở khoảng chuyển tiếp thì ép góc bằng strafe
> chậm; khi vào tầm đánh thì giữ thế. Đòn thường `Shield Bash` khóa hướng, hiện
> telegraph đường thẳng rồi lao ngắn và gây damage ở điểm va chạm. Mỗi đòn thứ ba
> dùng `Ground Slam`: khóa tâm tại vị trí Tank bắt đầu, telegraph vòng tròn tối
> thiểu 0,7 giây rồi mới gây damage. Sau mỗi đòn Tank lùi hoặc strafe để tái định
> vị thay vì đứng dính vào người chơi.
>
> Mọi chuyển động phải đi qua `EnemyMovement`; không sửa transform và không tự
> flip sprite. Trong telegraph phải dừng di chuyển chủ động. Mỗi đòn chỉ damage
> sau telegraph, tối đa một lần, không damage nếu quái đã chết và dùng
> `EnemyRuntimeProfile` cho life-steal. Khi pool/re-enable phải reset coroutine,
> state, attack counter, orbit timer và hướng strafe.
>
> Builder tiếp tục là source of truth: prefab Mage và Tank phải nhận đúng
> controller/tham số sau mọi lần rebuild. Thêm EditMode test cho cấu hình prefab;
> thêm PlayMode test cho ba dải di chuyển của Mage, vùng di chuyển của Tank và
> thời điểm damage của Greater Rune/Ground Slam. Chạy builder hai lần, validator,
> toàn bộ EditMode và PlayMode trước khi nghiệm thu.

## Nghiệm thu

- Mage và Tank có silhouette chuyển động khác nhau, không rung hướng mỗi frame.
- Mỗi quái có đòn thường và đòn chu kỳ thứ ba với telegraph khóa trước.
- Không damage trong windup; damage đúng sau windup và không bám theo mục tiêu.
- Recovery thực sự tạo chuyển động tái định vị.
- State/counter reset đúng khi pooling; builder idempotent và toàn bộ test pass.
