using TMPro;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>One guided reading flow is shared by first entry and manual replay, with explicit dismissal persistence.</summary>
    public static class TutorialGuide
    {
        private readonly struct Step
        {
            public readonly string Title, Keys, Body, Hint;
            public Step(string title, string keys, string body, string hint)
            { Title = title; Keys = keys; Body = body; Hint = hint; }
        }

        private static readonly Step[] Steps =
        {
            new Step("DI CHUYỂN TRONG THẾ GIỚI", "W A S D  /  PHÍM MŨI TÊN\nhoặc CHUỘT TRÁI",
                "Giữ phím để di chuyển. Nhấp chuột trái lên mặt đất để đi đến vị trí đó. Sảnh là nơi an toàn để làm quen trước khi vào rừng.",
                "Bạn có thể đọc từng bước theo nhịp của mình. Game đang tạm dừng."),
            new Step("ĐÁNH VÀ NÉ ĐÒN", "CHUỘT PHẢI / J: ĐÁNH  •  T: TỰ ĐÁNH\nSPACE / NÚT DƯỚI GAMEPAD: NÉ",
                "Né theo hướng đang di chuyển; đứng yên sẽ né theo hướng nhìn cuối. Né hồi sau 1 giây, miễn sát thương trực tiếp trong 0,18 giây đầu.",
                "Tự đánh mặc định bật. Né khi thấy vùng báo đòn; không thể né giữa lúc đang đánh hoặc dùng kỹ năng."),
            new Step("BỐN Ô KỸ NĂNG", "Q  /  E  /  R  /  F\nK: QUẢN LÝ KỸ NĂNG",
                "Mỗi phím dùng kỹ năng ở ô tương ứng. Cần đủ mana và hết hồi chiêu. Mở K để xem, trang bị hoặc nâng cấp những kỹ năng đã học.",
                "Khi đứng gần đối tượng tương tác, E ưu tiên tương tác thay vì dùng kỹ năng."),
            new Step("TƯƠNG TÁC VÀ CHUẨN BỊ", "E KHI ĐỨNG GẦN\nhoặc NHẤP ĐÚP VÀO ĐỐI TƯỢNG",
                "Ghé lò rèn để mua trang bị, đến kho để cất hoặc lấy đồ. Bạn có 2.500 vàng khi chơi mới. Kiểm tra giá và chỉ số trước khi mua.",
                "Tắt TỰ NHẶT trên HUD nếu muốn tự chọn đồ; khi đó đến gần và dùng E để nhặt."),
            new Step("TRANG BỊ VÀ TĂNG SỨC MẠNH", "I: HÀNH TRANG  •  P: TIỀM NĂNG\nK: KỸ NĂNG",
                "Mua đồ ở lò rèn sẽ tự trang bị ngay. Mở I để đổi hoặc tháo đồ đã sở hữu. Khi lên cấp, mở P để phân bổ điểm tiềm năng.",
                "Nhấn lại phím mở bảng hoặc Esc để đóng. Các bảng quản lý sẽ tạm dừng game."),
            new Step("BẮT ĐẦU CHUYẾN PHIÊU LƯU", "TRẬN TRUYỀN TỐNG Ở BÊN PHẢI SẢNH\nESC: TẠM DỪNG",
                "Đứng trong vòng đá để chọn vùng đã mở khóa. Hạ quái, nhặt vàng và XP, mở rương rồi tiến đến vùng tiếp theo. Quái thường có thể hồi sinh.",
                "Để rời game, dùng Esc → LƯU VÀ VỀ MÀN HÌNH CHÍNH. Có thể xem lại các bước này từ HƯỚNG DẪN.")
        };

        public static void Show(MenuDialog dialog, bool recordCompletion = false)
        {
            if (dialog == null || dialog.IsOpen) return;
            int index = 0;
            TMP_Text count = null, title = null, keys = null, body = null, hint = null;
            Button previous = null, next = null;
            void Refresh()
            {
                Step step = Steps[index];
                count.text = "LÀM QUEN VỚI HÀNH TRÌNH  •  " + (index + 1) + " / " + Steps.Length;
                title.text = step.Title;
                keys.text = step.Keys;
                body.text = step.Body;
                hint.text = step.Hint;
                previous.interactable = index > 0;
                next.GetComponentInChildren<TMP_Text>().text = index == Steps.Length - 1 ? "BẮT ĐẦU PHIÊU LƯU" : "TIẾP THEO  ›";
                next.Select();
            }
            dialog.ShowContent("HƯỚNG DẪN TỪNG BƯỚC", frame =>
            {
                count = FantasyEquipmentUI.Text(frame, "TutorialStep", "", 55, 120, 950, 35, 21, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
                title = FantasyEquipmentUI.Text(frame, "TutorialTitle", "", 55, 180, 950, 65, 32, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
                Transform card = FantasyEquipmentUI.Frame(frame, "TutorialKeys", 80, 265, 900, 120, FantasyEquipmentUI.Panel);
                keys = FantasyEquipmentUI.Text(card, "Keys", "", 25, 15, 850, 90, 27, null, TextAlignmentOptions.Center);
                body = FantasyEquipmentUI.Text(frame, "TutorialBody", "", 80, 420, 900, 135, 28, null, TextAlignmentOptions.TopLeft);
                hint = FantasyEquipmentUI.Text(frame, "TutorialHint", "", 80, 565, 900, 80, 22, FantasyEquipmentUI.Muted, TextAlignmentOptions.TopLeft);
                previous = FantasyEquipmentUI.Button(frame, "PreviousTutorial", "‹  QUAY LẠI", 55, 665, 280, 60, () =>
                {
                    if (!dialog.IsOpen || index == 0) return;
                    index--;
                    Refresh();
                });
                next = FantasyEquipmentUI.Button(frame, "NextTutorial", "", 365, 665, 640, 60, () =>
                {
                    if (!dialog.IsOpen) return;
                    if (index == Steps.Length - 1) { dialog.Dismiss(); return; }
                    index++;
                    Refresh();
                }, true);
            }, "BỎ QUA  •  ESC", onDismiss: () =>
            {
                if (recordCompletion) GameProgressManager.Instance?.CompleteTutorial();
            });
            Refresh();
        }
    }
}
