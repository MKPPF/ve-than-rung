using TMPro;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.UI
{
    /// <summary>Displays the dodge's scaled cooldown without introducing another gameplay input control.</summary>
    public sealed class DodgeHUD : MonoBehaviour
    {
        [SerializeField] private PlayerDodge dodge;
        [SerializeField] private TMP_Text status;
        [SerializeField] private RectTransform fill;
        public bool HasRequiredReferences => dodge != null && status != null && fill != null;

        private void Update()
        {
            if (!HasRequiredReferences) return;
            bool alive = dodge.GetComponent<PlayerHealth>().IsAlive;
            float remaining = dodge.CooldownRemaining;
            status.text = !alive ? "KHÔNG KHẢ DỤNG" : dodge.IsDodging ? "ĐANG NÉ" : remaining > 0f ?
                "HỒI: " + remaining.ToString("0.0") + " GIÂY" : "SẴN SÀNG";
            status.color = remaining > 0f ? FantasyEquipmentUI.Gold : FantasyEquipmentUI.Green;
            fill.localScale = new Vector3(alive ? 1f - Mathf.Clamp01(remaining / dodge.CooldownDuration) : 0f, 1, 1);
        }
    }
}
