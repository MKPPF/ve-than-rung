using System.Linq;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.UI;
using UnityEngine;

namespace TopDownGame.Equipment
{
    public sealed class StorageChest : MonoBehaviour, IInteractable
    {
        [SerializeField] private EquipmentData[] startingItems;
        [SerializeField] private string[] startingItemIds =
        {
            "iron_helmet",
            "iron_armor",
            "iron_pants"
        };
        [SerializeField] private StorageChestUI chestUI;

        public bool CanInteract => chestUI != null && !chestUI.IsOpen;
        public string InteractionPrompt => "E / Nhấp đúp - Mở rương đồ";

        private void Start()
        {
            string[] objectIds = startingItems == null
                ? new string[0]
                : startingItems.Where(item => item != null)
                    .Select(item => item.ItemId).ToArray();
            string[] ids = objectIds.Length > 0 ? objectIds : startingItemIds;
            GameProgressManager.Instance?.InitializeChest(ids);
        }

        public void Interact(PlayerInteraction player)
        {
            if (CanInteract && player != null)
            {
                chestUI.Open(player);
            }
        }
    }
}
