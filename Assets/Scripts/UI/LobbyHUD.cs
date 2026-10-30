using System.Collections;
using TMPro;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>The safe hub needs location, resources and interaction hints, not oversized combat skill cards.</summary>
    public sealed class LobbyHUD : MonoBehaviour
    {
        private PlayerStats stats;
        private PlayerInteraction interaction;
        private TMP_Text resources, prompt;
        private GameObject root;
        private float nextRefresh;

        private IEnumerator Start()
        {
            PlayerAutomationUI.Ensure(gameObject);
            HeroSelectionUI.Ensure(GetComponentInParent<Canvas>());
            GameObject player = GameObject.FindWithTag("Player");
            stats = player != null ? player.GetComponent<PlayerStats>() : null;
            interaction = player != null ? player.GetComponent<PlayerInteraction>() : null;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) yield break;
            root = new GameObject("LobbyHUDRoot", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsFirstSibling();
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform card = FantasyEquipmentUI.Frame(rect, "LocationCard", 22, 22, 340, 122,
                new Color(0.08f,0.12f,0.13f,0.94f));
            FantasyEquipmentUI.Text(card, "Location", "TRẠM NGHỈ TÁN RỪNG", 16, 12, 308, 32, 24, FantasyEquipmentUI.Gold).enableAutoSizing = false;
            resources = FantasyEquipmentUI.Text(card, "Resources", "", 16, 49, 308, 62, 22);
            resources.enableAutoSizing = false;
            var promptRect = FantasyEquipmentUI.Rect(rect, "Prompt", 0,0,700,76);
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = new Vector2(0.5f,0);
            promptRect.anchoredPosition = new Vector2(0,28);
            var backdrop = promptRect.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.04f,0.08f,0.09f,0.82f);
            backdrop.raycastTarget = false;
            prompt = FantasyEquipmentUI.Text(promptRect, "Label", "", 10,6,680,64,22,
                Color.white, TextAlignmentOptions.Center);
            prompt.enableAutoSizing = false;
        }

        private void Update()
        {
            if (resources == null || prompt == null) return;
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.25f;
                resources.text = "Cấp " + (stats != null ? stats.Level : 1) +
                    "   •   " + (stats != null ? stats.Gold.ToString("N0") : "0") +
                    " vàng\nTinh hoa: " + (GameProgressManager.Instance != null ? GameProgressManager.Instance.HunterEssence : 0);
            }
            prompt.text = interaction != null && interaction.HasTarget ? interaction.CurrentPrompt :
                "WASD / Chuột trái: di chuyển\nSPACE: né    •    E: tương tác    •    H: Nhân vật";
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root);
        }
    }
}
