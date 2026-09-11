using TMPro;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Reusable menu dialogs retain pause ownership and return keyboard focus safely.</summary>
    public sealed class MenuDialog : MonoBehaviour
    {
        private GameObject panel;
        private GameObject previousSelection;
        public bool IsOpen => panel != null && panel.activeSelf;

        public const string HelpText =
            "<b>KHỞI ĐẦU</b>\nMua đồ tại lò rèn, mở I để trang bị. Đứng trong trận truyền tống ở bên phải sảnh để chọn vùng đã mở khóa.\n\n" +
            "<b>ĐIỀU KHIỂN</b>\nWASD / phím mũi tên / chuột trái: di chuyển.\nChuột phải / J: đánh thường. Q / E / R / F: dùng kỹ năng.\nE ưu tiên tương tác khi ở gần lò rèn, kho, rương hoặc cổng. Có thể nhấp đúp vào đối tượng để đến tương tác.\n\n" +
            "<b>PHÁT TRIỂN NHÂN VẬT</b>\nI: hành trang • P: tiềm năng • K: kỹ năng • Esc: đóng bảng / tạm dừng. Săn quái, nhặt đồ và lên cấp để khám phá vùng khó hơn, đối đầu Vệ Thần Rừng. Quái thường có thể hồi sinh; không cần dọn sạch mới được đi tiếp.\n\n" +
            "<b>LƯU TIẾN TRÌNH</b>\nTiếp tục đưa bạn về sảnh và giữ tiến trình. Dùng Về màn hình chính trong menu tạm dừng để lưu trước khi rời game. Chơi mới thay thế tiến trình cũ sau khi xác nhận.";

        public void ShowHelp() => Show("HƯỚNG DẪN PHIÊU LƯU", HelpText);

        public void Show(string title, string body, string confirmLabel = null, UnityAction confirm = null)
        {
            if (IsOpen) return;
            if (panel != null) Destroy(panel);
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            panel = new GameObject("MenuDialogPanel", typeof(RectTransform), typeof(Image));
            Canvas canvas = GetComponentInParent<Canvas>();
            panel.transform.SetParent(canvas != null ? canvas.transform : transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel);
            Transform frame = FantasyEquipmentUI.Frame(surface, "DialogWindow", 430, 115, 1060, 850, FantasyEquipmentUI.Ink);
            FantasyEquipmentUI.Text(frame, "Title", title, 45, 35, 970, 65, 34, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(frame, "Body", body, 55, 120, 950, 590, 25, null, TextAlignmentOptions.TopLeft);
            Button cancel = FantasyEquipmentUI.Button(frame, "CloseDialog", confirm == null ? "ĐÃ HIỂU — ĐÓNG" : "HỦY — GIỮ TIẾN TRÌNH",
                confirm == null ? 300 : 55, 750, 460, 60, Close);
            if (confirm != null)
                FantasyEquipmentUI.Button(frame, "ConfirmDialog", confirmLabel, 545, 750, 460, 60, () =>
                {
                    if (!IsOpen) return;
                    Close();
                    confirm.Invoke();
                }, true);
            GameplayState.EnterModal(this);
            cancel.Select();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            GameplayState.ExitModal(this);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        private void OnDisable() => Close();
        private void OnDestroy()
        {
            GameplayState.ExitModal(this);
            if (panel != null) Destroy(panel);
        }
    }
}
