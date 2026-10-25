using System.Collections.Generic;
using TMPro;
using TopDownGame.Audio;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Shares audio and reversible display settings between the entry screen and pause menu.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        private MenuDialog dialog;
        private List<Vector2Int> resolutions;
        private int selectedResolution;
        private FullScreenMode selectedMode, previousMode;
        private Vector2Int previousSize;
        private TMP_Text modeLabel, resolutionLabel, status;
        private Button applyButton, confirmButton, revertButton, modeButton, previousButton, nextButton;
        private Button shakeButton, hitstopButton;
        private float confirmationDeadline;
        public bool PendingConfirmation { get; private set; }

        public void Open(MenuDialog owner)
        {
            if (owner == null || owner.IsOpen) return;
            dialog = owner;
            resolutions = DisplayPreferences.AvailableResolutions();
            selectedResolution = resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height));
            if (selectedResolution < 0) selectedResolution = 0;
            selectedMode = Screen.fullScreenMode == FullScreenMode.Windowed
                ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            dialog.ShowContent("CÀI ĐẶT", Build, onClose: OnClosed);
        }

        private void Build(Transform frame)
        {
            FantasyEquipmentUI.Text(frame, "AudioHeading", "ÂM THANH & PHẢN HỒI", 55, 120, 440, 36, 24, FantasyEquipmentUI.Gold);
            FantasyEquipmentUI.Text(frame, "DisplayHeading", "HIỂN THỊ", 565, 120, 440, 36, 24, FantasyEquipmentUI.Gold);
            GameAudioManager audio = GameAudioManager.EnsureExists();
            CreateSlider(frame, "Master", "Âm lượng tổng", 190, audio.MasterVolume, audio.SetMasterVolume);
            CreateSlider(frame, "Music", "Nhạc nền", 295, audio.MusicVolume, audio.SetMusicVolume);
            CreateSlider(frame, "Ambient", "Âm thanh môi trường", 400, audio.AmbientVolume, audio.SetAmbientVolume);
            CreateSlider(frame, "SFX", "Hiệu ứng", 505, audio.SfxVolume, audio.SetSfxVolume);
            FantasyEquipmentUI.Text(frame, "AudioHint", "Âm lượng và phản hồi được lưu ngay.",
                55, 590, 430, 32, 19, FantasyEquipmentUI.Muted, TextAlignmentOptions.TopLeft);
            shakeButton = FantasyEquipmentUI.Button(frame,"CameraShake","",55,630,430,44,() =>
            {
                if (dialog == null || !dialog.IsOpen) return;
                FeedbackPreferences.SetCameraShakeEnabled(!FeedbackPreferences.CameraShakeEnabled);
                RefreshFeedback();
            });
            hitstopButton = FantasyEquipmentUI.Button(frame,"Hitstop","",55,688,430,44,() =>
            {
                if (dialog == null || !dialog.IsOpen) return;
                FeedbackPreferences.SetHitstopEnabled(!FeedbackPreferences.HitstopEnabled);
                RefreshFeedback();
            });
            RefreshFeedback();

            FantasyEquipmentUI.Text(frame, "ModeHeading", "Chế độ màn hình", 565, 184, 430, 30, 21);
            modeButton = FantasyEquipmentUI.Button(frame, "DisplayMode", "", 565, 226, 440, 58, () =>
            {
                selectedMode = selectedMode == FullScreenMode.Windowed ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                Refresh();
            });
            modeLabel = modeButton.GetComponentInChildren<TMP_Text>();
            FantasyEquipmentUI.Text(frame, "ResolutionHeading", "Độ phân giải", 565, 322, 430, 30, 21);
            previousButton = FantasyEquipmentUI.Button(frame, "PreviousResolution", "‹", 565, 365, 58, 58, () => Cycle(-1));
            resolutionLabel = FantasyEquipmentUI.Text(frame, "Resolution", "", 631, 365, 308, 58, 24,
                FantasyEquipmentUI.TextColor, TextAlignmentOptions.Center);
            nextButton = FantasyEquipmentUI.Button(frame, "NextResolution", "›", 947, 365, 58, 58, () => Cycle(1));
            applyButton = FantasyEquipmentUI.Button(frame, "ApplyDisplay", "ÁP DỤNG HIỂN THỊ", 565, 465, 440, 58, ApplyDisplay, true);
            status = FantasyEquipmentUI.Text(frame, "DisplayStatus", "", 565, 550, 440, 78, 20,
                FantasyEquipmentUI.Muted, TextAlignmentOptions.TopLeft);
            confirmButton = FantasyEquipmentUI.Button(frame, "KeepDisplay", "GIỮ THAY ĐỔI", 565, 650, 212, 56, ConfirmDisplay, true);
            revertButton = FantasyEquipmentUI.Button(frame, "RevertDisplay", "HOÀN TÁC", 793, 650, 212, 56, RevertDisplay);
            Refresh();
        }

        private void RefreshFeedback()
        {
            shakeButton.GetComponentInChildren<TMP_Text>().text = "RUNG CAMERA: " + (FeedbackPreferences.CameraShakeEnabled ? "BẬT" : "TẮT");
            hitstopButton.GetComponentInChildren<TMP_Text>().text = "DỪNG NHẸ KHI TRÚNG: " + (FeedbackPreferences.HitstopEnabled ? "BẬT" : "TẮT");
        }

        private static void CreateSlider(Transform frame, string name, string label, float y,
            float value, UnityAction<float> change)
        {
            FantasyEquipmentUI.Text(frame, name + "Label", label, 55, y, 330, 30, 21);
            TMP_Text amount = FantasyEquipmentUI.Text(frame, name + "Value", Mathf.RoundToInt(value * 100) + "%",
                385, y, 100, 30, 21, FantasyEquipmentUI.Gold, TextAlignmentOptions.Right);
            Image track = FantasyEquipmentUI.Box(frame, name + "Volume", 55, y + 40, 430, 32, FantasyEquipmentUI.Panel);
            track.raycastTarget = true;
            Slider slider = track.gameObject.AddComponent<Slider>();
            RectTransform area = FantasyEquipmentUI.Rect(track.transform, "Area", 12, 0, 406, 32);
            Image fill = FantasyEquipmentUI.Box(area, "Fill", 0, 8, 406, 16, FantasyEquipmentUI.Green);
            Image handle = FantasyEquipmentUI.Box(area, "Handle", 0, 2, 20, 28, FantasyEquipmentUI.Gold);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(0, 8);
            fill.rectTransform.offsetMax = new Vector2(0, -8);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.anchoredPosition = Vector2.zero;
            handle.rectTransform.sizeDelta = new Vector2(20, -4);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => { amount.text = Mathf.RoundToInt(v * 100) + "%"; change(v); });
            slider.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        }

        private void Cycle(int direction)
        {
            selectedResolution = (selectedResolution + direction + resolutions.Count) % resolutions.Count;
            Refresh();
        }

        public void ApplyDisplay()
        {
            if (dialog == null || !dialog.IsOpen || PendingConfirmation) return;
            previousSize = new Vector2Int(Screen.width, Screen.height);
            previousMode = Screen.fullScreenMode;
            PendingConfirmation = true;
            confirmationDeadline = Time.unscaledTime + 15;
            DisplayPreferences.Apply(resolutions[selectedResolution], selectedMode);
            Refresh();
            // Revert is the safe keyboard default while testing a new display mode.
            revertButton.Select();
        }

        public void ConfirmDisplay()
        {
            if (!PendingConfirmation) return;
            DisplayPreferences.Save(resolutions[selectedResolution], selectedMode);
            PendingConfirmation = false;
            Refresh();
            status.text = "Đã lưu cài đặt hiển thị.";
            applyButton.Select();
        }

        public void RevertDisplay()
        {
            if (!PendingConfirmation) return;
            DisplayPreferences.Apply(previousSize, previousMode);
            PendingConfirmation = false;
            selectedResolution = resolutions.IndexOf(previousSize);
            if (selectedResolution < 0) selectedResolution = 0;
            selectedMode = previousMode == FullScreenMode.Windowed ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            Refresh();
            status.text = "Đã hoàn tác về hiển thị trước đó.";
            applyButton.Select();
        }

        private void Refresh()
        {
            modeLabel.text = selectedMode == FullScreenMode.Windowed ? "CỬA SỔ" : "TOÀN MÀN HÌNH";
            Vector2Int size = resolutions[selectedResolution];
            resolutionLabel.text = size.x + " × " + size.y;
            modeButton.interactable = previousButton.interactable = nextButton.interactable = !PendingConfirmation;
            applyButton.interactable = !PendingConfirmation;
            confirmButton.gameObject.SetActive(PendingConfirmation);
            revertButton.gameObject.SetActive(PendingConfirmation);
            status.text = "Chọn chế độ và độ phân giải rồi nhấn Áp dụng. Có 15 giây để xác nhận.";
#if UNITY_EDITOR
            if (!PendingConfirmation) status.text = "Trong Editor, lựa chọn chỉ được lưu; cửa sổ Game giữ nguyên kích thước.";
#endif
        }

        private void Update()
        {
            if (!PendingConfirmation) return;
            if (Time.unscaledTime >= confirmationDeadline) { RevertDisplay(); return; }
            status.text = "Giữ hiển thị mới?\nTự hoàn tác sau " + Mathf.CeilToInt(confirmationDeadline - Time.unscaledTime) + " giây.";
        }

        private void OnClosed()
        {
            RevertDisplay();
            dialog = null;
        }

        private void OnDisable() => RevertDisplay();
    }
}
