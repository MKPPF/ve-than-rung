using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Maps
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class MapDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string nextScene = "Map02";
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color lockedColor = new Color(0.45f, 0.12f, 0.12f);
        [SerializeField] private Color openColor = new Color(0.15f, 0.8f, 0.35f);

        public bool IsLocked { get; private set; }
        public bool CanInteract => !IsLocked;
        public string InteractionPrompt => IsLocked
            ? "Portal unavailable"
            : "E - Qua " + nextScene + " (không cần diệt hết quái)";

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (visual == null)
            {
                visual = GetComponent<SpriteRenderer>();
            }
        }

        private void Start()
        {
            SetLocked(false);
        }

        public void SetLocked(bool locked)
        {
            IsLocked = locked;
            if (visual != null)
            {
                visual.color = locked ? lockedColor : openColor;
            }
        }

        public void Interact(PlayerInteraction player)
        {
            if (CanInteract)
            {
                TopDownGame.Save.GameProgressManager.Instance?.UnlockRegion(nextScene);
                SceneTransition.LoadScene(nextScene, player);
            }
        }
    }
}
