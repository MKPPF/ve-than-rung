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
