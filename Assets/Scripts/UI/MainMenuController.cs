using TMPro;
using TopDownGame.Core;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Composes a readable entry screen and protects progress before starting over.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private MenuDialog dialog;
        private Button firstButton;
        private Button continueButton;
        private Button startButton;
        private TMP_Text saveSummary;
        private bool leaving;
        private bool recoveryPromptPending;

        public static bool SkipSplashOnce { get; set; } = false;
        private static bool hasShownInitialSplash = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            SkipSplashOnce = false;
            hasShownInitialSplash = false;
        }

        private GameObject splashPanel;
        private Transform menuSurface;
        private bool inSplash = true;
        private float splashLoadProgress;
        private float splashAutoAdvanceTimer;
        private RectTransform splashFillRect;
        private TMP_Text splashPromptText;
        private TMP_Text splashStatusText;

        private void Awake()
        {
            dialog = gameObject.AddComponent<MenuDialog>();
            SettingsPanel settings = gameObject.AddComponent<SettingsPanel>();
            Transform menu = transform.Find("MenuPanel");
            if (menu == null) return;
            FantasyEquipmentUI.Clear(menu);
            RectTransform rect = menu.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            menuSurface = EquipmentPanelLayout.CreateSurface(menu.gameObject, new Vector2(900, 1080));
            menu.GetComponent<Image>().color = new Color(0.012f, 0.016f, 0.027f, 0.55f);
            Transform frame = FantasyEquipmentUI.Frame(menuSurface, "MainMenuWindow", 590, 80, 740, 920, FantasyEquipmentUI.Ink);
            FantasyEquipmentUI.Text(frame, "Eyebrow", "PHIÊU LƯU HÀNH ĐỘNG 2D", 55, 42, 630, 30, 20,
                FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(frame, "Title", "VỆ THẦN RỪNG", 40, 88, 660, 90, 52,
                FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Line(frame, 70, 207, 600);
            GameProgressManager progress = GameProgressManager.Instance;
            bool hasSave = progress != null && progress.HasSavedGame;
            saveSummary = FantasyEquipmentUI.Text(frame, "SaveSummary", hasSave
                ? "Đã có tiến trình  •  Cấp " + progress.PlayerLevel + "\nTiếp tục từ sảnh để chuẩn bị chuyến đi."
                : "Hành trình mới đang chờ bạn.\nBắt đầu ở sảnh, chuẩn bị trang bị rồi khám phá khu rừng.",
                65, 232, 610, 70, 21, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            continueButton = FantasyEquipmentUI.Button(frame, "ContinueButton", hasSave ? "TIẾP TỤC" : "TIẾP TỤC  •  CHƯA CÓ TIẾN TRÌNH", 70, 338, 600, 72, ContinueGame, hasSave || progress != null && progress.NeedsSaveRecovery);
            continueButton.interactable = hasSave;
            Button start = FantasyEquipmentUI.Button(frame, "StartButton", "CHƠI MỚI", 70, 434, 600, 72, StartNewGame, !hasSave);
            startButton = start;
            FantasyEquipmentUI.Button(frame, "SettingsButton", "CÀI ĐẶT", 70, 540, 290, 62, () => settings.Open(dialog));
            FantasyEquipmentUI.Button(frame, "HelpButton", "HƯỚNG DẪN", 380, 540, 290, 62, dialog.ShowHelp);
            FantasyEquipmentUI.Button(frame, "QuitButton", "THOÁT GAME", 70, 630, 600, 62, QuitGame);
            FantasyEquipmentUI.Line(frame, 70, 736, 600);
            FantasyEquipmentUI.Text(frame, "Controls", "WASD / Chuột trái: di chuyển  •  J / Chuột phải: đánh\nI: hành trang  •  P: tiềm năng  •  K: kỹ năng\nE ưu tiên tương tác khi ở gần đối tượng",
                55, 766, 630, 100, 20, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            firstButton = hasSave ? continueButton : start;
            foreach (Button button in frame.GetComponentsInChildren<Button>())
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                EquipmentPanelLayout.StyleText(label, 24);
            }
            RefreshProgressView();
            recoveryPromptPending = progress != null && progress.NeedsSaveRecovery;

            bool shouldShowSplash = !SkipSplashOnce && !hasShownInitialSplash && !GamePreferences.IsAutomatedTestRun;
            SkipSplashOnce = false;

            if (shouldShowSplash)
            {
                hasShownInitialSplash = true;
                inSplash = true;
                BuildSplashScreen(menu);
            }
            else
            {
                inSplash = false;
                if (menuSurface != null)
                {
                    menuSurface.gameObject.SetActive(true);
                }
            }
        }

        private void BuildSplashScreen(Transform parent)
        {
            splashPanel = new GameObject("SplashScreenRoot", typeof(RectTransform), typeof(CanvasGroup));
            splashPanel.transform.SetParent(parent, false);
            RectTransform splashRect = splashPanel.GetComponent<RectTransform>();
            splashRect.anchorMin = Vector2.zero;
            splashRect.anchorMax = Vector2.one;
            splashRect.offsetMin = splashRect.offsetMax = Vector2.zero;

            // Fullscreen click catcher
            Button clickCatcher = splashPanel.AddComponent<Button>();
            clickCatcher.onClick.AddListener(DismissSplash);
            Image bg = splashPanel.AddComponent<Image>();
            bg.color = new Color(0.015f, 0.02f, 0.035f, 0.98f);
            clickCatcher.targetGraphic = bg;

            Transform surface = EquipmentPanelLayout.CreateSurface(splashPanel, new Vector2(1000, 1080));

            // Epic Title Banner
            FantasyEquipmentUI.Text(surface, "SplashEyebrow", "PHIÊU LƯU HÀNH ĐỘNG 2D  •  VỆ THẦN RỪNG",
                0, 240, 1920, 36, 22, FantasyEquipmentUI.Green, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(surface, "SplashTitle", "VỆ THẦN RỪNG",
                0, 280, 1920, 110, 74, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(surface, "SplashSubtitle", "HUYỀN THOẠI PHÙ ĐỔNG THIÊN VƯƠNG",
                0, 400, 1920, 40, 26, FantasyEquipmentUI.TextColor, TextAlignmentOptions.Center);

            FantasyEquipmentUI.Line(surface, 660, 470, 600);

            // Loading Section
            Transform track = FantasyEquipmentUI.Frame(surface, "LoadingTrack", 680, 680, 560, 22, FantasyEquipmentUI.Ink);
            Image fillImage = FantasyEquipmentUI.Box(track, "LoadingFill", 2, 2, 0, 18, FantasyEquipmentUI.Gold);
            splashFillRect = fillImage.rectTransform;

            splashStatusText = FantasyEquipmentUI.Text(surface, "LoadingStatus", "Đang nạp dữ liệu thế giới...",
                0, 720, 1920, 36, 20, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);

            // Press Any Key Prompt (hidden initially until loading finishes)
            splashPromptText = FantasyEquipmentUI.Text(surface, "PressPrompt", "✦ NHẤN NÚT BẤT KỲ HAY DÙNG CHUỘT ĐỂ TIẾP TỤC ✦",
                0, 690, 1920, 50, 26, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            splashPromptText.gameObject.SetActive(false);

            if (menuSurface != null)
            {
                menuSurface.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (!inSplash)
            {
                firstButton?.Select();
            }
        }

        private void Update()
        {
            if (inSplash)
            {
                UpdateSplashScreen();
                return;
            }

            TryShowPendingRecovery();
        }

        private void UpdateSplashScreen()
        {
            if (splashLoadProgress < 1f)
            {
                splashLoadProgress += Time.unscaledDeltaTime * 1.15f;
                if (splashFillRect != null)
                {
                    splashFillRect.sizeDelta = new Vector2(Mathf.Clamp01(splashLoadProgress) * 556f, 18f);
                }

                if (splashStatusText != null)
                {
                    splashStatusText.text = splashLoadProgress < 0.35f
                        ? "Đang khởi tạo tài nguyên đại ngàn..."
                        : splashLoadProgress < 0.75f
                            ? "Đang nạp kỹ năng và anh hùng Phù Đổng..."
                            : "Sẵn sàng phiêu lưu!";
                }

                if (splashLoadProgress >= 1f)
                {
                    if (splashFillRect != null && splashFillRect.parent != null)
                    {
                        splashFillRect.parent.gameObject.SetActive(false);
                    }
                    if (splashStatusText != null)
                    {
                        splashStatusText.gameObject.SetActive(false);
                    }
                    if (splashPromptText != null)
                    {
                        splashPromptText.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                splashAutoAdvanceTimer += Time.unscaledDeltaTime;

                // Pulsing glow animation on prompt text
                if (splashPromptText != null)
                {
                    float alpha = 0.38f + Mathf.PingPong(Time.unscaledTime * 1.4f, 0.62f);
                    Color c = FantasyEquipmentUI.Gold;
                    c.a = alpha;
                    splashPromptText.color = c;
                }

                bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
                bool mousePressed = Mouse.current != null &&
                    (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);

                // Tự động chuyển vào Main Menu sau khi loading hoặc bấm phím / nhấp chuột ngay
                if (keyPressed || mousePressed || splashAutoAdvanceTimer >= 1.2f)
                {
                    DismissSplash();
                }
            }
        }

        public void DismissSplash()
        {
            if (!inSplash) return;
            inSplash = false;
            if (splashPanel != null)
            {
                splashPanel.SetActive(false);
            }
            if (menuSurface != null)
            {
                menuSurface.gameObject.SetActive(true);
            }
            firstButton?.Select();
            TryShowPendingRecovery();
        }

        private void TryShowPendingRecovery()
        {
            if (!recoveryPromptPending || GameplayState.IsInputBlocked) return;
            recoveryPromptPending = false;
            ShowSaveRecovery();
        }

        private void RefreshProgressView()
        {
            if (saveSummary == null || continueButton == null) return;
            var progress = GameProgressManager.Instance;
            bool needsRecovery = progress != null && progress.NeedsSaveRecovery;
            bool hasSave = progress != null && progress.HasSavedGame;
            saveSummary.text = needsRecovery
                ? "Tiến trình hiện tại chưa đọc được.\nKiểm tra phương án khôi phục trước khi tiếp tục."
                : hasSave ? (progress.LoadStatus == ProgressLoadStatus.Recovered ? "Đã khôi phục bản dự phòng" : "Đã có tiến trình") +
                    "  •  Cấp " + progress.PlayerLevel + "\nTiếp tục từ sảnh để chuẩn bị chuyến đi."
                : "Hành trình mới đang chờ bạn.\nBắt đầu ở sảnh, chuẩn bị trang bị rồi khám phá khu rừng.";
            saveSummary.color = needsRecovery ? FantasyEquipmentUI.Gold : FantasyEquipmentUI.Muted;
            continueButton.GetComponentInChildren<TMP_Text>().text = needsRecovery ? "KIỂM TRA TIẾN TRÌNH"
                : hasSave ? "TIẾP TỤC" : "TIẾP TỤC  •  CHƯA CÓ TIẾN TRÌNH";
            continueButton.interactable = hasSave || needsRecovery;
            firstButton = hasSave || needsRecovery ? continueButton : startButton;
        }

        public void ContinueGame()
        {
            if (!isActiveAndEnabled || leaving || GameplayState.IsInputBlocked) return;
            GameProgressManager progress = GameProgressManager.Instance;
            if (progress == null) return;
            if (!progress.LoadProgress())
            {
                RefreshProgressView();
                ShowSaveRecovery();
                return;
            }
            leaving = true;
            SceneManager.LoadScene("Lobby");
        }

        public void StartNewGame()
        {
            if (!isActiveAndEnabled || leaving || GameplayState.IsInputBlocked) return;
            if (GameProgressManager.Instance != null && GameProgressManager.Instance.HasStoredProgress)
            {
                string body = GameProgressManager.Instance.NeedsSaveRecovery
                    ? "Tiến trình hiện tại chưa đọc được. Bạn có thể hủy và kiểm tra phương án khôi phục.\n\nChơi mới bắt đầu lại từ cấp 1 với 2.500 vàng; bản dự phòng cũng sẽ bắt đầu theo hành trình mới.\n\nBản dữ liệu lỗi gần nhất được giữ riêng để kiểm tra, nhưng game không đảm bảo đọc lại được. Chọn Hủy hoặc nhấn Esc để giữ nguyên mọi dữ liệu."
                    : "Bạn đang có tiến trình đã lưu.\n\nChơi mới sẽ thay thế cấp độ, vàng, trang bị, kỹ năng và các vùng đã mở khóa.\n\nKhông thể hoàn tác trong game. Chọn Hủy hoặc nhấn Esc để giữ nguyên tiến trình.";
                dialog.Show("BẮT ĐẦU LẠI?", body,
                    "XÁC NHẬN CHƠI MỚI", ConfirmNewGame);
                return;
            }
            ConfirmNewGame();
        }

        private void ConfirmNewGame()
        {
            if (!isActiveAndEnabled || leaving || GameplayState.IsInputBlocked) return;
            GameProgressManager.Instance?.BeginNewGame();
            leaving = true;
            SceneManager.LoadScene("Lobby");
        }

        private void ShowSaveRecovery()
        {
            var progress = GameProgressManager.Instance;
            if (!isActiveAndEnabled || leaving || GameplayState.IsInputBlocked || progress == null || !progress.NeedsSaveRecovery) return;
            bool canRecover = progress.TryGetBackupSummary(out int level, out int savedGold, out string revision);
            string body = "Game không đọc được tiến trình hiện tại. Dữ liệu gốc vẫn được giữ nguyên; chưa có thay đổi nào được ghi đè.\n\n" +
                (canRecover ? "Có bản dự phòng hợp lệ: Cấp " + level + "  •  " + savedGold.ToString("N0") + " vàng.\nKhôi phục có thể lùi về lần lưu trước và bỏ phần tiến trình mới hơn.\n\nSau khi khôi phục, chọn Tiếp tục để trở về sảnh. Bản dữ liệu lỗi gần nhất được giữ riêng để kiểm tra."
                : "Hiện không có bản dự phòng đọc được. Bạn có thể để sau để giữ nguyên dữ liệu, hoặc quay lại menu chọn Chơi mới và xác nhận bắt đầu lại.\n\nGame không tự tạo hành trình mới hoặc xóa tiến trình lỗi.");
            dialog.Show("KHÔNG ĐỌC ĐƯỢC TIẾN TRÌNH", body,
                canRecover ? "KHÔI PHỤC BẢN DỰ PHÒNG" : null,
                canRecover ? (UnityEngine.Events.UnityAction)(() => RestoreSave(revision)) : null, "ĐỂ SAU — GIỮ NGUYÊN");
        }

        private void RestoreSave(string expectedRevision)
        {
            if (!isActiveAndEnabled || leaving || GameplayState.IsInputBlocked) return;
            var progress = GameProgressManager.Instance;
            if (progress == null) return;
            bool restored = progress.TryRestoreBackup(expectedRevision);
            RefreshProgressView();
            firstButton?.Select();
            if (!restored) ShowSaveRecovery();
        }

        private void OnDisable()
        {
            recoveryPromptPending = false;
            dialog?.Close();
        }

        public void QuitGame()
        {
            if (!isActiveAndEnabled || leaving || GameplayState.IsInputBlocked) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
