using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TopDownGame.Save;
using TopDownGame.Audio;
using TMPro;
using TopDownGame.Core;

namespace TopDownGame.UI
{
    /// <summary>Owns its pause independently of nested help and other modal screens.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        private MenuDialog dialog;
        private bool isOpen;

        private void Awake()
        {
            // A nested help dialog must never release this menu's pause token.
            dialog = gameObject.AddComponent<MenuDialog>();
            if (panel != null)
            {
                panel.SetActive(false);
            }

            if (panel != null)
            {
                BuildAudioControls();
                Button[] buttons = panel.GetComponentsInChildren<Button>(true);
                if (buttons.Length > 0) buttons[0].onClick.AddListener(Resume);
                if (buttons.Length > 1) buttons[1].onClick.AddListener(ReturnToMenu);
                if (buttons.Length > 0) buttons[0].GetComponentInChildren<TMP_Text>().text = "TIẾP TỤC CHƠI";
                if (buttons.Length > 1) buttons[1].GetComponentInChildren<TMP_Text>().text = "LƯU VÀ VỀ MÀN HÌNH CHÍNH";
                FantasyEquipmentUI.Button(panel.transform, "HelpButton", "HƯỚNG DẪN", 40, 435, 440, 46, dialog.ShowHelp);
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (GameplayState.IsBlockedByOther(this) || GameplayState.ModalClosedThisFrame) return;
            if (isOpen) { Resume(); return; }
            isOpen = true;
            GameplayState.EnterModal(this);
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Resume()
        {
            if (GameplayState.IsBlockedByOther(this)) return;
            isOpen = false;
            GameplayState.ExitModal(this);
            if (panel != null)
            {
                panel.SetActive(false);
            }
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
        }

        private void BuildAudioControls()
        {
            if (panel.transform.Find("AudioSettings") != null) return;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null) panelRect.sizeDelta = new Vector2(520f, 620f);
            Transform title = panel.transform.Find("PanelTitle");
            if (title != null)
            {
                title.GetComponent<TMP_Text>().text = "TẠM DỪNG";
                title.GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(0f, 250f);
            }

            Transform audioRoot = new GameObject("AudioSettings",
                typeof(RectTransform)).transform;
            audioRoot.SetParent(panel.transform, false);
            RectTransform audioRect = audioRoot.GetComponent<RectTransform>();
            audioRect.anchorMin = audioRect.anchorMax = new Vector2(0.5f, 0.5f);
            audioRect.sizeDelta = new Vector2(440f, 300f);
            audioRect.anchoredPosition = new Vector2(0f, 30f);

            GameAudioManager audio = GameAudioManager.EnsureExists();
            CreateVolumeSlider(audioRoot, "Master", 105f, audio.MasterVolume,
                audio.SetMasterVolume);
            CreateVolumeSlider(audioRoot, "Music", 40f, audio.MusicVolume,
                audio.SetMusicVolume);
            CreateVolumeSlider(audioRoot, "Ambient", -25f, audio.AmbientVolume,
                audio.SetAmbientVolume);
            CreateVolumeSlider(audioRoot, "SFX", -90f, audio.SfxVolume,
                audio.SetSfxVolume);

            Button[] buttons = panel.GetComponentsInChildren<Button>(true);
            if (buttons.Length > 0)
                buttons[0].GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(0f, -205f);
            if (buttons.Length > 1)
                buttons[1].GetComponent<RectTransform>().anchoredPosition =
                    new Vector2(0f, -280f);
        }

        private static void CreateVolumeSlider(Transform parent, string label,
            float y, float value, UnityEngine.Events.UnityAction<float> callback)
        {
            GameObject labelObject = new GameObject(label + "Label",
                typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(5f, y);
            labelRect.sizeDelta = new Vector2(105f, 36f);
            TMP_Text text = labelObject.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = new Color(0.84f, 0.9f, 1f);

            GameObject sliderObject = new GameObject(label + "Volume",
                typeof(RectTransform), typeof(Image), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);
            RectTransform rect = sliderObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(118f, y);
            rect.sizeDelta = new Vector2(300f, 22f);
            Image background = sliderObject.GetComponent<Image>();
            background.color = new Color(0.08f, 0.1f, 0.16f, 0.95f);

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform),
                typeof(Image));
            fillObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            fillObject.GetComponent<Image>().color = new Color(0.3f, 0.82f, 0.65f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fillRect;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(callback);
        }
    }
}
