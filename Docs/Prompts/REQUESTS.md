# Yêu cầu phát triển đã lưu

Các yêu cầu trước đây được gộp tại đây để tra cứu. Chúng không tự xác nhận tính năng
đã hoàn tất; xem [lộ trình](../ROADMAP.md) và [báo cáo kiểm tra](../README.md#bao-cao-kiem-tra).

## Mục lục

- [Prompt cải thiện đồng bộ toàn bộ map](#prompt-all-maps-visual-guidance)
- [Prompt phát triển di chuyển và tấn công: quái thường trong CaveMap01](#prompt-cave-enemy-movement-combat)
- [Prompt phát triển chuyển động mượt và animation khớp chiêu: CaveMap01](#prompt-cave-enemy-smooth-movement-action-sync)
- [Prompt phát triển quái: Bọ Cạp Pha Lê](#prompt-crystal-scorpion)
- [Prompt phát triển di chuyển và tấn công: Bọ Cạp Pha Lê](#prompt-crystal-scorpion-movement-combat)
- [Prompt triển khai vòng lặp lên cấp và nhặt chiến lợi phẩm](#prompt-leveling-loot-pass-through)
- [Prompt sửa chuyển động quái và nền ngoài: Map01 Forest Clearing](#prompt-map01-smooth-enemies-forest-background)

---

<a id="prompt-all-maps-visual-guidance"></a>

## PROMPT_ALL_MAPS_VISUAL_GUIDANCE.md

# Prompt cải thiện đồng bộ toàn bộ map

> Cải thiện `Lobby`, `Map01`, `Map02`, `Map03`, `BossMap01` và `CaveMap01` mà
> không thay đổi vòng lặp săn quái, nhặt đồ và lên cấp. Builder tiếp tục là source
> of truth; không sửa scene bằng tay.
>
> Giữ topology mê cung và collider hiện tại. Nới nền môi trường đủ lớn cho toàn bộ
> khung nhìn camera ở sát biên, đồng thời đặt màu clear của camera cùng bảng màu
> biome để không còn vùng hoặc viền đen. Cảnh trang trí và hiệu ứng dẫn hướng mới
> không được có collider.
>
> Tạo lớp dẫn hướng chung cho mỗi map gồm vòng sáng ở điểm xuất phát, dấu hiệu riêng
> cho rương/phần thưởng, cột sáng và nhiều vòng pulse ở cổng thoát. Thêm nhãn thế
> giới ngắn, dễ đọc để phân biệt điểm xuất phát, rương và nơi đến của cổng. Lobby
> dùng cùng ngôn ngữ hình ảnh cho điểm hồi sinh, rương đồ và cổng vào trận.
>
> Mỗi khu phải có bản sắc riêng: Lobby ấm và an toàn; Map01 xanh non/vàng; Map02
> rừng sâu xanh/cam; Map03 xanh lam/tím ma thuật; BossMap01 đỏ/hổ phách cảnh báo;
> CaveMap01 lam ngọc/tím tinh thể. Hạt môi trường phải đổi màu, mật độ, tên và cách
> trôi theo từng khu thay vì chỉ phân biệt rừng/hang.
>
> Thêm test xác nhận mọi map có `MapGuidance`, đủ ba anchor đúng vị trí gameplay,
> không có collider trong lớp chỉ dẫn, nền phủ rộng hơn viewport camera và camera
> không clear về màu gần đen. Chạy builder hai lần, validator, toàn bộ EditMode và
> PlayMode test, sau đó playtest/chụp ảnh từng map.

## Nghiệm thu

- Không còn nền đen khi người chơi đi sát rìa map.
- Nhìn vào cảnh có thể nhận ra điểm xuất phát, rương và cổng đi tiếp.
- Sáu khu có bảng màu và không khí khác nhau, nhưng UI/gameplay vẫn dễ đọc.
- Không hiệu ứng trang trí nào cản người chơi, quái hoặc vật phẩm rơi.
- Vòng lặp săn quái–nhặt đồ–lên cấp và quyền rời map bất cứ lúc nào không đổi.

---

<a id="prompt-cave-enemy-movement-combat"></a>

## PROMPT_CAVE_ENEMY_MOVEMENT_COMBAT.md

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

---

<a id="prompt-cave-enemy-smooth-movement-action-sync"></a>

## PROMPT_CAVE_ENEMY_SMOOTH_MOVEMENT_ACTION_SYNC.md

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

---

<a id="prompt-crystal-scorpion"></a>

## PROMPT_CRYSTAL_SCORPION.md

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

---

<a id="prompt-crystal-scorpion-movement-combat"></a>

## PROMPT_CRYSTAL_SCORPION_MOVEMENT_COMBAT.md

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

---

<a id="prompt-leveling-loot-pass-through"></a>

## PROMPT_LEVELING_LOOT_PASS_THROUGH.md

# Prompt triển khai vòng lặp lên cấp và nhặt chiến lợi phẩm

Phát triển game Unity top-down theo vòng lặp săn quái, nhặt chiến lợi phẩm và lên cấp.

Yêu cầu:

1. Nhân vật và quái có thể đi xuyên qua nhau. Va chạm thân thể không được chặn đường,
   nhưng hitbox/hurtbox và mọi đòn đánh vẫn phải gây sát thương bình thường.
2. Mỗi quái bị tiêu diệt phải tạo một vật phẩm chiến lợi phẩm nhìn thấy được tại vị trí
   chết. Vàng và XP chỉ được cộng khi người chơi đi qua để nhặt. Trang bị hiếm có thể
   nằm trong cùng chiến lợi phẩm; trang bị trùng được đổi thành Tinh hoa săn.
3. Các map thường là khu săn liên tục. Quái tái sinh và người chơi có thể sang map kế
   tiếp bất kỳ lúc nào, không phải tiêu diệt toàn bộ quái.
4. HUD phải nói rõ mục tiêu là lên cấp, nhặt đồ và tăng sức mạnh; số quái chỉ thể hiện
   mật độ đang hoạt động, không phải điều kiện hoàn thành màn.
5. Builder là nguồn sự thật: tạo layer PlayerCharacter và EnemyCharacter, gán layer cho
   prefab, loại EnemyCharacter khỏi movement mask của người chơi và thiết lập physics
   để hai layer không va chạm thân thể.
6. Viết kiểm thử xác nhận vật phẩm chỉ trao thưởng sau khi nhặt, quái pooled chỉ trả
   thưởng một lần, cửa map luôn mở và player/enemy bỏ qua va chạm.

Sau khi triển khai, chạy builder hai lần, validator, toàn bộ EditMode/PlayMode test và
playtest Map01 để xác nhận vật phẩm xuất hiện, có thể nhặt và nhân vật xuyên qua quái.

---

<a id="prompt-map01-smooth-enemies-forest-background"></a>

## PROMPT_MAP01_SMOOTH_ENEMIES_FOREST_BACKGROUND.md

# Prompt sửa chuyển động quái và nền ngoài: Map01 Forest Clearing

> Sửa hai vấn đề nhìn thấy trực tiếp trong `Map01`: quái cận chiến tiến tới người
> chơi bị khựng ở rìa tầm đánh, và vùng ngoài hành lang rừng bị nhuộm gần đen.
>
> **Di chuyển:** Slime và các quái dùng `MeleeEnemy` phải dùng arrival steering:
> chạy đủ tốc độ khi ở xa, giảm tốc liên tục trong vùng tiếp cận trước AttackRange,
> rồi phanh tức thời khi bắt đầu windup. Không được chạy hết tốc rồi đổi sang Stop
> tại một frame duy nhất. `EnemyMovement` vẫn điều khiển Rigidbody2D, có
> interpolation, acceleration/deceleration và giới hạn góc lái.
>
> **Khớp animation:** đòn cắn/đánh thường dùng phase `Windup → Active → Recovery`
> với Active bắt đầu đúng lúc gọi damage. Leap của Slime phải nén trong windup,
> bung người đúng lúc thêm impulse và hồi pose sau khi chạm đất. Các skill chung
> khác cũng truyền phase thật thay vì duration tổng.
>
> **Khung cảnh:** giữ phân biệt đường đi và vùng ngoài, nhưng không dùng màu gần
> đen. Nền ngoài phải là ForestFloor màu xanh rừng tối vừa phải, vẫn đọc được chi
> tiết lá/cỏ. Camera clear color phải cùng họ màu để không xuất hiện viền đen nếu
> camera chạm biên. Không thay đổi collider hoặc topology mê cung.
>
> Builder tiếp tục là source of truth. Thêm test xác nhận arrival giảm tốc liên
> tục, prefab Slime có cấu hình steering/animator, nền Map01 có độ sáng tối thiểu
> và camera không clear về gần đen. Chạy builder, validator, EditMode, PlayMode và
> kiểm tra Game View tại Map01.

## Nghiệm thu

- Slime không giật hoặc trượt qua người chơi khi vào tầm đánh.
- Pose Active trùng đúng thời điểm gây damage/Leap impulse.
- Vùng ngoài hành lang vẫn là nền rừng có chi tiết, không còn mảng đen.
- Không thay đổi đường đi/collider; toàn bộ validator và test pass.
