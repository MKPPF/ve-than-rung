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
