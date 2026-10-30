using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Maps;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Offers explicit recovery choices after preserving the defeated player's earned progress and isolating death-screen input.</summary>
    public sealed class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text message;
        private PlayerHealth playerHealth;
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private Button retryButton;
        private bool isOpen, leaving;
        public bool IsOpen => isOpen;

        private void Start()
        {
            if (panel != null)
            {
                FantasyEquipmentUI.Clear(panel.transform);
                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                Transform surface = EquipmentPanelLayout.CreateSurface(panel, new Vector2(900, 940));
                Transform frame = FantasyEquipmentUI.Frame(surface, "GameOverWindow", 590, 150, 740, 780, FantasyEquipmentUI.Ink);
                string region = MapManager.Instance != null && MapManager.Instance.Data != null ?
                    MapManager.Instance.Data.DisplayName.ToUpperInvariant() : "HÀNH TRÌNH VẪN CÒN TIẾP";
                FantasyEquipmentUI.Text(frame, "Eyebrow", region, 50, 38, 640, 30, 18,
                    FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
                message = FantasyEquipmentUI.Text(frame, "PanelTitle", "BẠN ĐÃ GỤC NGÃ", 40, 92, 660, 70, 40,
                    FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
                FantasyEquipmentUI.Text(frame, "RetryHint", "Giữ vàng, kinh nghiệm, trang bị và kỹ năng đã nhận.\nHồi đầy máu và mana khi tiếp tục cuộc phiêu lưu.",
                    60, 190, 620, 110, 22, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
                retryButton = FantasyEquipmentUI.Button(frame, "RestartButton", "THỬ LẠI KHU VỰC", 70, 330, 600, 68, Restart, true);
                FantasyEquipmentUI.Button(frame, "GameOverLobbyButton", "VỀ SẢNH CHUẨN BỊ", 70, 426, 600, 64, ReturnToLobby);
                FantasyEquipmentUI.Button(frame, "GameOverMenuButton", "VỀ MÀN HÌNH CHÍNH", 70, 514, 600, 64, ReturnToMenu);
                FantasyEquipmentUI.Text(frame, "MenuHint", "Thử lại: trở về điểm xuất phát của khu vực.\nVề sảnh: đổi trang bị, kỹ năng hoặc chọn vùng khác.\nTiếp tục từ menu cũng đưa bạn về sảnh.", 60, 620, 620, 100, 21,
                    FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
                foreach (Button button in frame.GetComponentsInChildren<Button>())
                    EquipmentPanelLayout.StyleText(button.GetComponentInChildren<TMP_Text>(), 24);
                panel.SetActive(false);
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.Died += Show;
                }
            }
        }

        private void Show()
        {
            if (!isActiveAndEnabled || isOpen || leaving) return;
            isOpen = true;
            GameplayState.EnterModal(this);
            if (playerHealth != null && !playerHealth.IsAlive)
                GameProgressManager.Instance?.CaptureDefeatedPlayer(playerHealth.GetComponent<PlayerStats>(),
                    playerHealth, playerHealth.GetComponent<TopDownGame.Skills.SkillInventory>());
            if (message != null)
            {
                message.text = "BẠN ĐÃ GỤC NGÃ";
            }

            if (panel != null) panel.SetActive(true);
            if (panel != null)
            {
                panel.transform.SetAsLastSibling();
                focus.Capture(panel.transform);
                retryButton?.Select();
            }
        }

        public void Restart()
        {
            LeaveFor(SceneManager.GetActiveScene().name);
        }

        public void ReturnToLobby() => LeaveFor("Lobby");

        public void ReturnToMenu()
        {
            MainMenuController.SkipSplashOnce = true;
            LeaveFor("MainMenu");
        }

        private void LeaveFor(string scene)
        {
            if (!isActiveAndEnabled || !isOpen || leaving || GameplayState.IsBlockedByOther(this)) return;
            leaving = true;
            isOpen = false;
            GameplayState.ExitModal(this);
            SceneManager.LoadScene(scene);
        }

        private void OnEnable()
        {
            if (playerHealth != null && !playerHealth.IsAlive) Show();
        }

        private void OnDisable()
        {
            isOpen = false;
            if (panel != null) panel.SetActive(false);
            GameplayState.ExitModal(this);
            focus.Release();
        }

        private void OnDestroy()
        {
            if (playerHealth != null) playerHealth.Died -= Show;
            GameplayState.ExitModal(this);
            focus.Release();
        }
    }
}
