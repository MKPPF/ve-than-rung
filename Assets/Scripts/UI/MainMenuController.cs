using TMPro;
using TopDownGame.Core;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Protects existing progress from accidental replacement at the entry screen.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private MenuDialog dialog;
        private void Awake()
        {
            dialog = gameObject.AddComponent<MenuDialog>();
            SetLabel("StartButton", "CHƠI MỚI");
            SetLabel("QuitButton", "THOÁT GAME");
            SetLabel("ContinueButton", "TIẾP TỤC");
            Button continueButton = FindButton("ContinueButton");
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(ContinueGame);
                continueButton.interactable = GameProgressManager.Instance != null &&
                    GameProgressManager.Instance.HasSavedGame;
                if (continueButton.interactable)
                {
                    TMP_Text label = continueButton.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                    {
                        GameProgressManager progress = GameProgressManager.Instance;
                        label.text = "TIẾP TỤC  •  Cấp " +
                            progress.PlayerLevel;
                    }
                }
            }

            FindButton("StartButton")?.onClick.AddListener(StartNewGame);
            FindButton("QuitButton")?.onClick.AddListener(QuitGame);
            Transform menu = transform.Find("MenuPanel");
            if (menu != null)
            {
                // Runtime composition matches the existing equipment and pause UI;
                // no generated scene edits are required or lost when rebuilding.
                FantasyEquipmentUI.Button(menu, "HelpButton", "HƯỚNG DẪN", 100, 470, 420, 56, dialog.ShowHelp);
                Transform controls = menu.Find("Controls");
                if (controls != null)
                {
                    controls.GetComponent<TMP_Text>().text = "I: Hành trang • P: Tiềm năng • K: Kỹ năng\nE ưu tiên tương tác khi ở gần đối tượng";
                    controls.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -255);
                }
                Transform subtitle = menu.Find("Subtitle");
                if (subtitle != null) subtitle.GetComponent<TMP_Text>().text = "PHIÊU LƯU HÀNH ĐỘNG 2D";
                Transform title = menu.Find("Title");
                if (title != null)
                {
                    TMP_Text label = title.GetComponent<TMP_Text>();
                    label.text = "VỆ THẦN RỪNG";
                    label.fontSize = 44;
                }
            }
        }

        public void ContinueGame()
        {
            if (GameplayState.IsInputBlocked) return;
            GameProgressManager progress = GameProgressManager.Instance;
            if (progress == null || !progress.HasSavedGame)
            {
                return;
            }

            // The lobby is the persistent hub for shopping, storage and map
            // entry. Continuing must restore progress there instead of
            // skipping the hub and loading the last combat scene directly.
            SceneManager.LoadScene("Lobby");
        }

        public void StartNewGame()
        {
            if (GameplayState.IsInputBlocked) return;
            if (GameProgressManager.Instance != null && GameProgressManager.Instance.HasSavedGame)
            {
                dialog.Show("BẮT ĐẦU LẠI?", "Bạn đang có tiến trình đã lưu.\n\nChơi mới sẽ thay thế cấp độ, vàng, trang bị, kỹ năng và các vùng đã mở khóa.\n\nKhông thể hoàn tác trong game. Chọn Hủy hoặc nhấn Esc để giữ nguyên tiến trình.",
                    "XÁC NHẬN CHƠI MỚI", ConfirmNewGame);
                return;
            }
            ConfirmNewGame();
        }

        private void ConfirmNewGame()
        {
            GameProgressManager.Instance?.BeginNewGame();
            SceneManager.LoadScene("Lobby");
        }

        public void QuitGame()
        {
            if (GameplayState.IsInputBlocked) return;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void SetLabel(string name, string value)
        {
            TMP_Text label = FindButton(name)?.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = value;
        }

        private Button FindButton(string objectName)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name == objectName)
                {
                    return button;
                }
            }

            return null;
        }
    }
}
