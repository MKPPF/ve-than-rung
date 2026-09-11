# Prompt phát triển chuyển động mượt và animation khớp chiêu: CaveMap01

> Nâng cấp Mage, Tank và Bọ Cạp Pha Lê trong `CaveMap01` dựa trên bộ hành vi
> chiến đấu đã có. Giữ nguyên vai trò, khoảng cách và pattern đòn; cải thiện cảm
> giác chuyển động và đồng bộ hình thể với thời điểm chiêu thực sự có hiệu lực.
>
> **Chuyển động mượt:** `EnemyMovement` phải tăng tốc, giảm tốc và xoay vector
> vận tốc có giới hạn theo thời gian. Khi AI đổi từ truy đuổi sang orbit/strafe,
> quái phải bo hướng qua một cung ngắn thay vì bẻ góc hoặc đảo vector tức thì.
> Rigidbody2D tiếp tục là nguồn chuyển động, bật interpolation và không sửa
> transform trực tiếp. Lệnh di chuyển thường giảm tốc mượt; khi bắt đầu telegraph
> dùng phanh tức thời để quái không trượt khỏi dấu báo đòn.
>
> **Animation khớp chiêu:** `EnemyCharacterAnimator` phải hiểu ba pha tuyệt đối
> `Windup`, `Active`, `Recovery`. AI truyền đúng thời lượng từng pha thay vì chỉ
> truyền một duration tổng rồi để animator tự chia tỷ lệ. Trong Windup hình thể
> nén/lùi và tích lực; đúng lúc coroutine hết windup và damage/charge bắt đầu,
> animator chuyển sang Active với động tác bung/đâm/lao; sau đó trở về tư thế
> thường trong Recovery. Locomotion bob phải giảm mạnh khi đang ra chiêu và mặt
> quái phải khóa theo hướng đã chốt, không flip lại vì residual velocity.
>
> Mage đồng bộ Rune Burst/Greater Rune; Tank đồng bộ Shield Bash/Ground Slam;
> Bọ Cạp đồng bộ Pincer Snap/Crystal Charge/Tail Crystal Spike. Charge animation
> chuyển Active đúng lúc impulse được thêm; đòn vùng chuyển Active đúng lúc gọi
> damage. Khi chết hoặc pool/re-enable phải xóa toàn bộ phase/timer/velocity.
>
> Builder là source of truth cho acceleration, deceleration, turn speed và tốc
> độ visual follow. Thêm PlayMode test chứng minh đổi hướng không snap và animator
> đi đúng `Windup → Active → Recovery → None`; các test damage timing hiện tại
> phải tiếp tục pass. Chạy builder hai lần, validator, toàn bộ EditMode/PlayMode
> và kiểm tra Game View trước khi nghiệm thu.

## Nghiệm thu

- Chuyển từ pursuit sang orbit/retreat không bẻ góc tức thì hoặc rung hướng.
- Telegraph đứng đúng chỗ; không còn trượt do vận tốc cũ.
- Pose bung chiêu bắt đầu cùng thời điểm damage/impulse, không sớm hơn windup.
- Recovery pose và chuyển động tái định vị kết thúc tự nhiên.
- Builder idempotent, validator và toàn bộ test pass, runtime không có error.
