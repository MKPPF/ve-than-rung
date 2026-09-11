using TMPro;
using UnityEngine;

namespace TopDownGame.UI
{
    /// <summary>A brief location caption welcomes the player without permanently covering the town artwork.</summary>
    public sealed class LobbyWelcomeUI : MonoBehaviour
    {
        private GameObject caption;
        private CanvasGroup group;
        private float elapsed;

        private void Start()
        {
            caption = new GameObject("LobbyLocationCaption", typeof(RectTransform), typeof(CanvasGroup));
            caption.transform.SetParent(transform, false);
            RectTransform rect = caption.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -125);
            rect.sizeDelta = new Vector2(600, 95);
            group = caption.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            FantasyEquipmentUI.Text(rect, "RegionName", "TRẠM NGHỈ TÁN RỪNG",
                0, 0, 600, 45, 27, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(rect, "RegionHint", "Lò rèn  •  Kho trang bị  •  Cổng phiêu lưu",
                0, 46, 600, 30, 17, Color.white, TextAlignmentOptions.Center);
        }

        private void Update()
        {
            if (group == null) return;
            elapsed += Time.deltaTime;
            group.alpha = 1 - Mathf.InverseLerp(5, 7, elapsed);
            if (elapsed >= 7) { Destroy(caption); enabled = false; }
        }

        private void OnDestroy()
        {
            if (caption != null) Destroy(caption);
        }
    }
}
