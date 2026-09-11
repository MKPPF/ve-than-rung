using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.UI;
using UnityEngine;

namespace TopDownGame.Equipment
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class WeaponShopNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private WeaponShopUI shop;

        public bool CanInteract => shop != null && !shop.IsOpen;
        public string InteractionPrompt => "E / Nhấp đúp - Mở cửa hàng trang bị";

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Interact(PlayerInteraction player)
        {
            shop?.Open(player);
        }
    }
}
