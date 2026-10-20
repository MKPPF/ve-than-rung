using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Items
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemData item;
        private bool collected;
        public bool CanInteract => !collected && item != null && !PlayerAutomationState.AutoPickupEnabled;
        public string InteractionPrompt => "E / Nhấp đúp: Nhặt " + (item != null ? item.DisplayName : "vật phẩm");

        public void Interact(PlayerInteraction collector)
        {
            if (CanInteract && collector != null &&
                Vector2.Distance(transform.position, collector.transform.position) <= 1.5f)
                Collect(collector.gameObject);
        }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryAutoCollect(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryAutoCollect(other);
        }

        private void TryAutoCollect(Collider2D other)
        {
            if (PlayerAutomationState.AutoPickupEnabled && other.CompareTag("Player"))
                Collect(other.gameObject);
        }

        private void Collect(GameObject collector)
        {
            if (collected || item == null || !PlayerAutomationState.CanCollect(collector) ||
                GameProgressManager.Instance == null) return;

            collected = true;
            GameProgressManager.Instance?.AddItem(item.ItemId);
            Destroy(gameObject);
        }
    }
}
