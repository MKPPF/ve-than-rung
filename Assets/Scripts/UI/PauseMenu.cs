using TopDownGame.Core;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Save;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>
    /// Displays run achievements, current hero progress, and allows safe withdrawal
    /// to the Lobby or Main Menu with full progress captured.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        private MenuDialog dialog;
        private SettingsPanel settings;
        private Button resumeButton;
        private Button retreatLobbyButton;
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private bool isOpen;

        private TMP_Text heroText;
        private TMP_Text mapText;
        private TMP_Text levelExpText;
        private TMP_Text goldText;
        private TMP_Text vitalsText;
        private TMP_Text combatStatsText;

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

            Transform surface = EquipmentPanelLayout.CreateSurface(panel, new Vector2(960, 1020));
            Transform frame = FantasyEquipmentUI.Frame(surface, "PauseWindow", 570, 115, 780, 850, FantasyEquipmentUI.Ink);

            FantasyEquipmentUI.Text(frame, "Eyebrow", "NGHỈ CHÂN TRÊN HÀNH TRÌNH", 40, 24, 700, 26, 16,
                FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(frame, "PanelTitle", "TẠM DỪNG & BẢO TOÀN THÀNH TÍCH", 40, 52, 700, 48, 32,
                FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Line(frame, 40, 104, 700);

            // Run Achievements Card
            Transform statsCard = FantasyEquipmentUI.Frame(frame, "RunStatsCard", 40, 116, 700, 270, FantasyEquipmentUI.Panel);
            FantasyEquipmentUI.Text(statsCard, "CardTitle", "THÀNH TÍCH ĐẠT ĐƯỢC CHUYẾN ĐI NÀY", 20, 12, 660, 28, 20,
                FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(statsCard, "CardSubtitle", "Tiến trình và vật phẩm sẽ được bảo toàn khi bạn thoát ra",
                20, 40, 660, 22, 14, FantasyEquipmentUI.Green, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Line(statsCard, 20, 66, 660);

            heroText = FantasyEquipmentUI.Text(statsCard, "HeroStat", "Anh hùng: --", 26, 76, 330, 28, 17,
                FantasyEquipmentUI.TextColor);
            mapText = FantasyEquipmentUI.Text(statsCard, "MapStat", "Khu vực: --", 366, 76, 310, 28, 17,
                FantasyEquipmentUI.TextColor);

            levelExpText = FantasyEquipmentUI.Text(statsCard, "LevelExpStat", "Cấp độ: --", 26, 110, 330, 28, 17,
                FantasyEquipmentUI.TextColor);
            goldText = FantasyEquipmentUI.Text(statsCard, "GoldStat", "Vàng sở hữu: --", 366, 110, 310, 28, 17,
                FantasyEquipmentUI.Gold);

            vitalsText = FantasyEquipmentUI.Text(statsCard, "VitalsStat", "Sinh lực & Năng lượng: --", 26, 146, 648, 28, 17,
                FantasyEquipmentUI.TextColor);

            combatStatsText = FantasyEquipmentUI.Text(statsCard, "CombatStat", "Chỉ số chiến đấu: --", 26, 182, 648, 28, 17,
                FantasyEquipmentUI.TextColor);

            FantasyEquipmentUI.Text(statsCard, "SaveNotice", "💾 Tự động ghi nhớ tiến trình (Vàng, Cấp, Đồ, Kỹ năng) khi thoát",
                20, 228, 660, 26, 15, FantasyEquipmentUI.Green, TextAlignmentOptions.Center);

            FantasyEquipmentUI.Line(frame, 40, 398, 700);

            // Action Buttons
            resumeButton = FantasyEquipmentUI.Button(frame, "ResumeButton", "TIẾP TỤC CHIẾN ĐẤU", 50, 412, 680, 62, Resume, true);
            retreatLobbyButton = FantasyEquipmentUI.Button(frame, "RetreatLobbyButton", "RÚT VỀ SẢNH CHỜ (LƯU THÀNH TÍCH)", 50, 486, 680, 62, RetreatToLobby);
            FantasyEquipmentUI.Button(frame, "MenuButton", "LƯU VÀ VỀ MÀN HÌNH CHÍNH", 50, 560, 680, 62, ReturnToMenu);

            FantasyEquipmentUI.Button(frame, "SettingsButton", "CÀI ĐẶT", 50, 634, 330, 56, () => settings.Open(dialog));
            FantasyEquipmentUI.Button(frame, "HelpButton", "HƯỚNG DẪN", 400, 634, 330, 56, dialog.ShowHelp);

            FantasyEquipmentUI.Line(frame, 40, 704, 700);

            FantasyEquipmentUI.Text(frame, "SaveHint",
                "Mẹo: Bạn có thể rút về Sảnh Chờ để an toàn cất đồ, nâng cấp trang bị hoặc đổi nhân vật.",
                50, 718, 680, 46, 16, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(frame, "KeyHint", "ESC: Tiếp tục chơi   •   ENTER: Chọn hành động",
                50, 772, 680, 26, 15, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);

            foreach (Button button in frame.GetComponentsInChildren<Button>())
            {
                EquipmentPanelLayout.StyleText(button.GetComponentInChildren<TMP_Text>(), 23);
            }

            panel.SetActive(false);
        }

        private void Update()
        {
            if (!GameplayState.NavigationInputConsumedThisFrame &&
                Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (GameplayState.IsBlockedByOther(this) || GameplayState.ModalClosedThisFrame) return;
            if (isOpen)
            {
                Resume();
                return;
            }

            isOpen = true;
            RefreshRunSummary();
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

        /// <summary>
        /// Saves active player progress and safely retreats to the Lobby.
        /// </summary>
        public void RetreatToLobby()
        {
            if (GameplayState.IsBlockedByOther(this)) return;

            GameProgressManager progress = GameProgressManager.Instance;
            if (progress != null)
            {
                progress.CaptureActivePlayer();
                progress.SetCurrentMap("Lobby");
                progress.SaveProgress();
            }

            Resume();

            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "Lobby")
            {
                SceneManager.LoadScene("Lobby");
            }
        }

        /// <summary>
        /// Saves active player progress and returns to the Main Menu.
        /// </summary>
        public void ReturnToMenu()
        {
            if (GameplayState.IsBlockedByOther(this)) return;

            GameProgressManager progress = GameProgressManager.Instance;
            if (progress != null)
            {
                progress.CaptureActivePlayer();
                progress.SaveProgress();
            }

            MainMenuController.SkipSplashOnce = true;
            Resume();
            SceneManager.LoadScene("MainMenu");
        }

        private void RefreshRunSummary()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
            PlayerHealth health = player != null ? player.GetComponent<PlayerHealth>() : null;
            ThanhGiongHeroController thanhGiong = player != null ? player.GetComponent<ThanhGiongHeroController>() : null;

            HeroCharacterId activeHero = thanhGiong != null ? thanhGiong.CurrentHero : HeroCharacterId.ForestWarrior;
            if (heroText != null)
            {
                heroText.text = activeHero switch
                {
                    HeroCharacterId.ThanhGiong => "Anh hùng: <color=#F5D259>Thánh Gióng (Phù Đổng)</color>",
                    HeroCharacterId.SonTinh => "Anh hùng: <color=#6EE7A8>Sơn Tinh (Tản Viên Sơn Thánh)</color>",
                    _ => "Anh hùng: <color=#94B8FF>Chiến Binh Rừng</color>"
                };
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            string friendlyMapName = activeSceneName;
            if (MapManager.Instance != null && MapManager.Instance.Data != null)
            {
                friendlyMapName = MapManager.Instance.Data.DisplayName;
            }
            else if (activeSceneName == "Lobby")
            {
                friendlyMapName = "Sảnh Chờ Tán Rừng";
            }

            if (mapText != null)
            {
                mapText.text = "Khu vực: <color=#F5D259>" + friendlyMapName + "</color>";
            }

            if (levelExpText != null && stats != null)
            {
                levelExpText.text = "Cấp độ: <color=#F5D259>" + stats.Level + "</color> (XP: " + stats.Experience + " / " + stats.ExperienceToNextLevel + ")";
            }

            if (goldText != null && stats != null)
            {
                goldText.text = "Vàng sở hữu: <color=#FFD700>" + stats.Gold.ToString("N0") + " 🪙</color>";
            }

            if (vitalsText != null)
            {
                float curHp = health != null ? health.CurrentHealth : 0f;
                float maxHp = stats != null ? stats.MaxHealth : 100f;
                float curMp = stats != null ? stats.CurrentMana : 0f;
                float maxMp = stats != null ? stats.MaxMana : 100f;
                vitalsText.text = "Sinh lực: <color=#FF6666>" + Mathf.RoundToInt(curHp) + "/" + Mathf.RoundToInt(maxHp) + " HP</color>   |   Năng lượng: <color=#66CCFF>" + Mathf.RoundToInt(curMp) + "/" + Mathf.RoundToInt(maxMp) + " MP</color>";
            }

            if (combatStatsText != null && stats != null)
            {
                int remainingEnemies = MapManager.Instance != null ? MapManager.Instance.RemainingEnemies : 0;
                string enemyPart = activeSceneName == "Lobby" ? "Khu vực an toàn" : "Quái còn lại: " + remainingEnemies;
                combatStatsText.text = "Sát thương: " + Mathf.RoundToInt(stats.BaseDamage) + "   |   Giáp: " + Mathf.RoundToInt(stats.Armor) + "   |   " + enemyPart;
            }

            if (retreatLobbyButton != null)
            {
                bool inLobby = activeSceneName == "Lobby";
                TMP_Text label = retreatLobbyButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = inLobby ? "LƯU TIẾN TRÌNH TẠI SẢNH" : "RÚT VỀ SẢNH CHỜ (LƯU THÀNH TÍCH)";
                }
            }
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
