# Dữ liệu và model có sẵn

## HaGRID

HaGRID / HaGRIDv2 — Alexander Kapitanov, Andrey Makhlyarchuk, Karina Kvanchiani,
Aleksandr Nagaev, Roman Kraynov, Anton Nuzhdin và các tác giả được ghi trong
[repository chính thức](https://github.com/hukenovs/hagrid).

Phần dữ liệu sử dụng: annotation tọa độ bàn tay của các lớp `fist`, `peace`,
`like`, `stop`, `no_gesture`; không tải ảnh người tham gia.

Giấy phép riêng của tác giả: [HAGRID_LICENSE.pdf](HAGRID_LICENSE.pdf), tải nguyên
bản từ https://github.com/hukenovs/hagrid/blob/master/license/en_us.pdf.
Theo README của tác giả, đây là biến thể CC BY-SA 4.0. Bản giấy phép cụ thể
được ưu tiên khi xem xét quyền sử dụng và phân phối dữ liệu dẫn xuất.

Đã biến đổi: chọn tập con, dời gốc tọa độ về cổ tay, chuẩn hóa tỉ lệ và chiều
phản chiếu, xuất đặc trưng CSV. Chi tiết nguồn, ETag, cấu hình và checksum ở
`data/hagrid.manifest.json` sau khi tải.

## Google MediaPipe

MediaPipe Hand Landmarker: model **đã train sẵn**, chỉ dùng để lấy 21 điểm tay.
[Tài liệu và model](https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker).
Giữ riêng với `gesture_mlp.npz`, là bộ phân loại được huấn luyện từ trọng số
ngẫu nhiên bằng code notebook của dự án. Không nhận model landmark là phần
do dự án tự huấn luyện.
