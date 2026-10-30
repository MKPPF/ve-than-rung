using TMPro;
using TopDownGame.Core;
using TopDownGame.UI;
using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Interactive shrine in the Lobby allowing the player to open the Hero Selection
    /// panel and switch between Forest Warrior, Thanh Giong, and Son Tinh.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HeroSelectionStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private HeroSelectionUI selectionUI;

        public string InteractionPrompt =>
            (ThanhGiongHeroController.IsHeroUnlocked(HeroCharacterId.ThanhGiong) ||
             ThanhGiongHeroController.IsHeroUnlocked(HeroCharacterId.SonTinh))
                ? "E / Nhấp đúp - Đài Anh Hùng (Chọn Chiến Binh / Thánh Gióng / Sơn Tinh)"
                : "E / Nhấp đúp - Đài Anh Hùng (Xem điều kiện mở khóa Anh Hùng)";
        public string Prompt => InteractionPrompt;
        public bool CanInteract => true;

        private void Awake()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = ThanhGiongVisualFactory.GetShrineStatueSprite();
            }

            Transform labelTransform = transform.Find("HeroShrineLabel");
            if (labelTransform != null)
            {
                TMP_Text labelText = labelTransform.GetComponent<TMP_Text>();
                if (labelText != null)
                {
                    labelText.text = "ĐÀI ANH HÙNG\nE / NHẤP ĐÚP  •  PHÍM H";
                }
            }
        }

        public void Interact(PlayerInteraction player)
        {
            if (selectionUI == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                selectionUI = HeroSelectionUI.Ensure(canvas);
            }

            selectionUI?.Open();
        }
    }
}
