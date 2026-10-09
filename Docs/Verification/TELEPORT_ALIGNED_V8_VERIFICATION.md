# Truyền tống trận — căn theo bệ đá V7

## Thay đổi

Nguồn sửa: `Assets/Editor/TopDownGameBuilder.cs`,
`Assets/Scripts/Maps/TeleportCircleVisual.cs`,
`Assets/Scripts/Maps/LobbyAtmosphere.cs`. Cập nhật test trong
`LobbySceneryTests.cs`, `CatalogSmokeTests.cs`, `ZZLobbySceneryPlayModeTests.cs`,
cùng README/roadmap. Scene và nội dung sinh được cập nhật qua builder.

- Builder đặt tâm tại (8.73, -0.20), theo pixel (1353, 498) của tranh V7.
- Bán kính vòng khắc (1.22, 0.87), viền ngoài (1.95, 1.44); bỏ vòng cũ
  quá dẹt với tỷ lệ Y/X = 0.55.
- Hai vòng trong cố định, nửa viền ngoài phía trước, hai vệt sáng thuôn chạy
  chậm và 16 điểm sáng tại mép vòng. Viền ngoài phía sau không vẽ đè chân cột.
- Màu ngọc lam dịu, giảm ánh phủ giữa; toàn bộ nằm dưới nhân vật.
- Không thay tranh, gameplay truyền tống, catalog hay save.
- Test bổ sung kiểm tra vị trí/bán kính sinh từ builder, phương trình elip,
  pause, bật/tắt và số object cố định (23).
- Backup nội dung sinh và ba file nguồn tại Backups/TeleportCircleAlignedV8/.

## Xác minh

- Hai lượt `BuildCompleteMvpAutomated` hoàn tất (Logs/Editor.log, marker MVP
  built tại dòng 4317 và 6751 của phiên này). Cả hai lượt vẫn dùng cùng path asset.
- Validator đạt, không lỗi cấu trúc (marker automated validation passed dòng 6920).
- EditMode: 64/64 Passed, `Logs/TeleportAlignedV8_EditMode.xml`.
  Job: `5d82a125fb574d99841d8fe399ff0826`.
- PlayMode toàn bộ: 62/62 Passed, `Logs/TeleportAlignedV8_PlayMode.xml`.
  Job: `a2caed48b27244118a8961b7a3ba42d3`.
- Đã xem ảnh cận cảnh và tinh chỉnh bỏ nửa viền ngoài phía sau vì đè chân cột.
  Hai test Lobby/truyền tống chạy lại đạt 2/2 trên bản tinh chỉnh cuối,
  `Logs/TeleportAlignedV8_FinalPolish_PlayMode.xml`, job
  `db811d6e946a40dab9f6885b698c4993`.
- Ảnh cận cảnh: `Assets/Screenshots/TruyenTongTranV8_CanKhop_Verified.png`.
  Đã kiểm tra bằng mắt: hai vòng trong đồng tâm với nền, không còn viền chạy
  qua cột phía sau, vân đá vẫn rõ, nhân vật không bị ánh sáng che.
- Console sau test không có exception/lỗi runtime; dòng thông báo lưu XML
  được connector xếp nhầm vào bộ lọc error. Dự án vẫn có warning API Unity
  obsolete trong các test cũ không liên quan; không tuyên bố sạch mọi warning.
