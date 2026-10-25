using TopDownGame.Core;
using TopDownGame.Save;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Keeps pause ownership and keyboard focus while shared settings or help are open.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        private MenuDialog dialog;
        private SettingsPanel settings;
        private Button resumeButton;
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private bool isOpen;

        private void Awake()
        {
            dialog = gameObject.AddComponent<MenuDialog>();
            settings = gameObject.AddComponent<SettingsPanel>();
            if (panel == null) return;
            FantasyEquipmentUI.Clear(panel.transform);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel, new Vector2(780, 930));
            Transform frame = FantasyEquipmentUI.Frame(surface, "PauseWindow", 650, 155, 620, 770, FantasyEquipmentUI.Ink);
            FantasyEquipmentUI.Text(frame, "Eyebrow", "NGHỈ CHÂN TRÊN HÀNH TRÌNH", 40, 35, 540, 30, 17,
                FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(frame, "PanelTitle", "TẠM DỪNG", 40, 78, 540, 60, 40,
                FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            resumeButton = FantasyEquipmentUI.Button(frame, "ResumeButton", "TIẾP TỤC CHƠI", 60, 185, 500, 66, Resume, true);
            FantasyEquipmentUI.Button(frame, "SettingsButton", "CÀI ĐẶT", 60, 275, 500, 62, () => settings.Open(dialog));
            FantasyEquipmentUI.Button(frame, "HelpButton", "HƯỚNG DẪN", 60, 359, 500, 62, dialog.ShowHelp);
            FantasyEquipmentUI.Line(frame, 60, 463, 500);
            FantasyEquipmentUI.Button(frame, "MenuButton", "LƯU VÀ VỀ MÀN HÌNH CHÍNH", 60, 500, 500, 66, ReturnToMenu);
            FantasyEquipmentUI.Text(frame, "SaveHint", "Lưu tiến trình trước khi trở về menu.\nĐóng cài đặt hoặc hướng dẫn sẽ trở lại đây.",
                60, 595, 500, 74, 20, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(frame, "KeyHint", "ESC  Tiếp tục chơi", 60, 705, 500, 30, 17,
                FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            foreach (Button button in frame.GetComponentsInChildren<Button>())
                EquipmentPanelLayout.StyleText(button.GetComponentInChildren<TMP_Text>(), 24);
            panel.SetActive(false);
        }

        private void Update()
        {
            if (!GameplayState.NavigationInputConsumedThisFrame && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle()
        {
            if (GameplayState.IsBlockedByOther(this) || GameplayState.ModalClosedThisFrame) return;
            if (isOpen) { Resume(); return; }
            isOpen = true;
            GameplayState.EnterModal(this);
            if (panel == null) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            focus.Capture(panel.transform);
            resumeButton.Select();
        }

        public void Resume()
        {
            if (GameplayState.IsBlockedByOther(this)) return;
            isOpen = false;
            if (panel != null) panel.SetActive(false);
            GameplayState.ExitModal(this);
            focus.Release();
        }

        public void ReturnToMenu()
        {
            if (GameplayState.IsBlockedByOther(this)) return;
            GameProgressManager.Instance?.CaptureActivePlayer();
            Resume();
            SceneManager.LoadScene("MainMenu");
        }

        private void OnDisable()
        {
            if (dialog != null) dialog.Close();
            isOpen = false;
            if (panel != null) panel.SetActive(false);
            GameplayState.ExitModal(this);
            focus.Release();
        }
    }
}
