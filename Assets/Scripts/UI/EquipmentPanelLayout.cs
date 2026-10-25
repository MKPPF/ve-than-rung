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
        private Vector2 designBounds = new Vector2(1920f, 1080f);
        public static readonly Color Gold = new Color(0.94f, 0.76f, 0.43f);
        public static readonly Color Accent = new Color(0.25f, 0.78f, 0.66f);

        public static Transform CreateSurface(GameObject panel, Vector2? windowBounds = null)
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
            layout.designBounds = windowBounds ?? new Vector2(1920f, 1080f);
            layout.LateUpdate();
            return desktop.transform;
        }

        public static float FitScale(Vector2 available)
        {
            return Mathf.Max(0.01f, Mathf.Min(available.x / 1920f, available.y / 1080f));
        }

        public void SetDesignBounds(Vector2 value)
        {
            designBounds = new Vector2(Mathf.Max(1f, value.x), Mathf.Max(1f, value.y));
            LateUpdate();
        }

        public void ConfigureWindow(Transform window, bool compact, TMP_Text balance, TMP_Text status)
        {
            SetDesignBounds(new Vector2(compact ? 1440 : 1920, 1080));
            var frame = (RectTransform)window;
            frame.anchoredPosition = new Vector2(compact ? 292 : 104, -80);
            frame.sizeDelta = new Vector2(compact ? 1336 : 1712, compact ? 960 : 920);
            SetLayer(window, "Brass", compact ? 1332 : 1708, compact ? 956 : 916);
            SetLayer(window, "Inset", compact ? 1328 : 1704, compact ? 952 : 912);
            SetLayer(window, "Highlight", compact ? 1326 : 1702, 1);
            var shadow = (RectTransform)window.parent.Find("WindowShadow");
            shadow.anchoredPosition = new Vector2(compact ? 282 : 94, -76);
            shadow.sizeDelta = new Vector2(compact ? 1360 : 1736, compact ? 978 : 938);
            SetHorizontal(window.Find("Header"), 6, compact ? 1324 : 1700);
            SetHorizontal(window.Find("Title"), 32, compact ? 650 : 950);
            SetHorizontal(window.Find("Eyebrow"), 34, compact ? 750 : 1050);
            SetHorizontal(window.Find("Close"), compact ? 1250 : 1626, 54);
            SetHorizontal(balance.transform, compact ? 814 : 1190, 392);
            var statusRect = (RectTransform)status.transform;
            statusRect.anchoredPosition = new Vector2(compact ? 32 : 720, compact ? -910 : -874);
            statusRect.sizeDelta = new Vector2(compact ? 1272 : 960, 26);
            status.alignment = compact ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Right;
            foreach (Transform child in window)
            {
                if (child.name == "Divider") SetHorizontal(child, 28, compact ? 1280 : 1656);
                if (child.name != "CornerStud") continue;
                var rect = (RectTransform)child;
                Vector2 position = rect.anchoredPosition;
                if (position.x > 1000) position.x = compact ? 1318 : 1694;
                if (position.y < -800) position.y = compact ? -942 : -902;
                rect.anchoredPosition = position;
            }
        }

        private static void SetLayer(Transform window, string name, float width, float height)
        {
            var rect = (RectTransform)window.Find(name);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetHorizontal(Transform target, float x, float width)
        {
            var rect = (RectTransform)target;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }

        private void LateUpdate()
        {
            if (surface != null && bounds != null)
            {
                float scale = Mathf.Max(0.01f, Mathf.Min(bounds.rect.width / designBounds.x,
                    bounds.rect.height / designBounds.y));
                surface.localScale = Vector3.one * scale;
            }
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
