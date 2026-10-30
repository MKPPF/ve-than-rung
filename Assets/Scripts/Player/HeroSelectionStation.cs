using TopDownGame.Core;
using TopDownGame.UI;
using UnityEngine;

namespace TopDownGame.Player
{
    /// <summary>
    /// Interactive shrine in the Lobby allowing the player to open the Hero Selection
    /// panel and switch to Thanh Giong (or back to the Forest Warrior).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HeroSelectionStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private HeroSelectionUI selectionUI;
        [SerializeField] private string stationPrompt = "E / Nhấp đúp - Đài Anh Hùng (Chọn Thánh Gióng)";

        public string InteractionPrompt => stationPrompt;
        public string Prompt => stationPrompt;
        public bool CanInteract => true;

        private void Awake()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
            {
                sr.sprite = ThanhGiongVisualFactory.GetShrineStatueSprite();
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
