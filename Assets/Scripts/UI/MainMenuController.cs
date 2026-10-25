using TMPro;
using TopDownGame.Core;
using TopDownGame.Save;
using UnityEngine;
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
            Transform surface = EquipmentPanelLayout.CreateSurface(menu.gameObject, new Vector2(900, 1080));
            menu.GetComponent<Image>().color = new Color(0.012f, 0.016f, 0.027f, 0.55f);
            Transform frame = FantasyEquipmentUI.Frame(surface, "MainMenuWindow", 590, 80, 740, 920, FantasyEquipmentUI.Ink);
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
        }

        private void Start()
        {
            firstButton?.Select();
            TryShowPendingRecovery();
        }

        private void Update() => TryShowPendingRecovery();

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
