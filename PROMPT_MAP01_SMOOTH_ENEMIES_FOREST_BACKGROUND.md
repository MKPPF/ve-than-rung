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
