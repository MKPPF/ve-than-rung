using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>
    /// Fits the equipment desktop to its canvas without cropping on 4:3 or ultrawide
    /// screens. The full-canvas backdrop continues to intercept world clicks.
    /// </summary>
    public sealed class EquipmentPanelLayout : MonoBehaviour
    {
        private RectTransform surface;
        private RectTransform bounds;
        public static readonly Color Gold = new Color(0.94f, 0.76f, 0.43f);
        public static readonly Color Accent = new Color(0.25f, 0.78f, 0.66f);

        public static Transform CreateSurface(GameObject panel)
        {
            panel.GetComponent<Image>().color = new Color(0.012f, 0.016f, 0.027f, 0.94f);
            var desktop = new GameObject("Surface", typeof(RectTransform), typeof(Image));
            desktop.transform.SetParent(panel.transform, false);
            var rect = desktop.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1920f, 1080f);
            desktop.GetComponent<Image>().color = Color.clear;
            var layout = panel.AddComponent<EquipmentPanelLayout>();
            layout.surface = rect;
            layout.bounds = panel.GetComponent<RectTransform>();
            layout.LateUpdate();
            return desktop.transform;
        }

        public static float FitScale(Vector2 available)
        {
            return Mathf.Max(0.01f, Mathf.Min(available.x / 1920f, available.y / 1080f));
        }

        private void LateUpdate()
        {
            if (surface != null && bounds != null)
                surface.localScale = Vector3.one * FitScale(bounds.rect.size);
        }

        public static void StyleText(TMP_Text text, float size)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.8f;
            text.fontSizeMax = size;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static void StyleButton(Button button)
        {
            var colors = button.colors;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f);
            colors.pressedColor = new Color(0.7f, 0.8f, 0.78f);
            colors.selectedColor = new Color(1.1f, 1.15f, 1.12f);
            colors.disabledColor = new Color(0.42f, 0.46f, 0.48f, 0.8f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;
        }

        public static void AddScrollbar(ScrollRect scroll)
        {
            var track = new GameObject("ScrollTrack", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            track.transform.SetParent(scroll.transform, false);
            var rect = track.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.offsetMin = new Vector2(-10f, 8f);
            rect.offsetMax = new Vector2(-2f, -8f);
            track.GetComponent<Image>().color = new Color(0.09f, 0.15f, 0.19f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(track.transform, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.offsetMin = handleRect.offsetMax = Vector2.zero;
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = Accent;
            var scrollbar = track.GetComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
        }
    }
}
