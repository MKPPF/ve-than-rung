using TMPro;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Reusable menu dialogs retain pause ownership and return keyboard focus safely.</summary>
    public sealed class MenuDialog : MonoBehaviour
    {
        private GameObject panel;
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private UnityAction closed;
        private UnityAction dismissed;
        public bool IsOpen => panel != null && panel.activeSelf;

        public const string HelpText =
            "<b>KHỞI ĐẦU</b>\nMua đồ ở lò rèn tự trang bị ngay; mở I để đổi hoặc tháo. Vào trận truyền tống bên phải sảnh để chọn vùng đã mở.\n\n" +
            "<b>ĐIỀU KHIỂN</b>\nWASD / mũi tên / chuột trái: di chuyển. Chuột phải / J: đánh.\nSpace / nút dưới gamepad: né; hồi 1 giây, miễn đòn trực tiếp 0,18 giây đầu. Q / E / R / F: kỹ năng. T: bật/tắt tự đánh.\nE ưu tiên tương tác ở gần đối tượng; có thể nhấp đúp để đến tương tác.\n\n" +
            "<b>PHÁT TRIỂN NHÂN VẬT</b>\nI: hành trang • P: tiềm năng • K: kỹ năng • Esc: đóng bảng / tạm dừng. Săn quái, nhặt đồ và lên cấp để khám phá vùng khó hơn, đối đầu Vệ Thần Rừng. Quái thường có thể hồi sinh; không cần dọn sạch mới được đi tiếp.\n\n" +
            "<b>LƯU TIẾN TRÌNH</b>\nTiếp tục đưa bạn về sảnh và giữ tiến trình. Dùng Về màn hình chính trong menu tạm dừng để lưu trước khi rời game. Chơi mới thay thế tiến trình cũ sau khi xác nhận.";

        public void ShowHelp()
        {
            ShowContent("HƯỚNG DẪN PHIÊU LƯU", frame =>
            {
                FantasyEquipmentUI.Text(frame, "Body", HelpText, 55, 120, 950, 510, 25, null, TextAlignmentOptions.TopLeft);
                FantasyEquipmentUI.Button(frame, "ReplayTutorial", "XEM HƯỚNG DẪN TỪNG BƯỚC", 180, 660, 700, 60, () =>
                {
                    if (!IsOpen) return;
                    Close();
                    TutorialGuide.Show(this);
                }, true);
            });
        }

        public void Show(string title, string body, string confirmLabel = null, UnityAction confirm = null, string closeLabel = null)
        {
            ShowContent(title, frame =>
            {
                FantasyEquipmentUI.Text(frame, "Body", body, 55, 120, 950, 590, 25, null, TextAlignmentOptions.TopLeft);
                if (confirm == null) return;
                GameObject ownedPanel = panel;
                FantasyEquipmentUI.Button(frame, "ConfirmDialog", confirmLabel, 545, 750, 460, 60, () =>
                {
                    if (!IsOpen || panel != ownedPanel || GameplayState.IsBlockedByOther(this)) return;
                    Close();
                    confirm.Invoke();
                }, true);
            }, closeLabel ?? (confirm == null ? "ĐÃ HIỂU — ĐÓNG" : "HỦY — GIỮ TIẾN TRÌNH"), confirm != null);
        }

        public void ShowContent(string title, System.Action<Transform> build,
            string closeLabel = "ĐÓNG  •  ESC", bool hasConfirmation = false, UnityAction onClose = null,
            UnityAction onDismiss = null)
        {
            if (IsOpen) return;
            if (panel != null) Destroy(panel);
            closed = onClose;
            dismissed = onDismiss;
            panel = new GameObject("MenuDialogPanel", typeof(RectTransform), typeof(Image));
            Canvas canvas = GetComponentInParent<Canvas>();
            panel.transform.SetParent(canvas != null ? canvas.transform : transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel, new Vector2(1220, 1010));
            Transform frame = FantasyEquipmentUI.Frame(surface, "DialogWindow", 430, 115, 1060, 850, FantasyEquipmentUI.Ink);
            FantasyEquipmentUI.Text(frame, "Title", title, 45, 35, 970, 65, 34, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            build?.Invoke(frame);
            GameObject ownedPanel = panel;
            Button cancel = FantasyEquipmentUI.Button(frame, "CloseDialog", closeLabel,
                hasConfirmation ? 55 : 300, 750, 460, 60, () => { if (panel == ownedPanel && IsOpen) Dismiss(); });
            foreach (Button button in frame.GetComponentsInChildren<Button>())
                EquipmentPanelLayout.StyleText(button.GetComponentInChildren<TMP_Text>(), 22);
            focus.Capture(panel.transform);
            GameplayState.EnterModal(this);
            cancel.Select();
        }

        public void Close()
        {
            if (!IsOpen) return;
            UnityAction callback = closed;
            closed = null;
            dismissed = null;
            callback?.Invoke();
            panel.SetActive(false);
            GameplayState.ExitModal(this);
            focus.Release();
        }

        public void Dismiss()
        {
            if (!IsOpen) return;
            UnityAction callback = dismissed;
            Close();
            callback?.Invoke();
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Dismiss();
        }

        private void OnDisable() => Close();
        private void OnDestroy()
        {
            GameplayState.ExitModal(this);
            focus.Release();
            if (panel != null) Destroy(panel);
        }
    }
}
