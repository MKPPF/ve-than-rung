using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Items
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemData item;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || item == null)
            {
                return;
            }

            GameProgressManager.Instance?.AddItem(item.ItemId);
            Destroy(gameObject);
        }
    }
}
