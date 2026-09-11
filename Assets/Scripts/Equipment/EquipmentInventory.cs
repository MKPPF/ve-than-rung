using System;
using System.Collections.Generic;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Equipment
{
    [RequireComponent(typeof(PlayerStats))]
    public sealed class EquipmentInventory : MonoBehaviour
    {
        public const int MaximumEnhancementLevel = 20;
        public const float BonusPerEnhancementLevel = 0.08f;

        [SerializeField] private EquipmentData[] catalog;

        private PlayerStats stats;
        private PlayerHealth health;

        public event Action<EquipmentData> EquipmentChanged;

        private EquipmentData equippedWeapon;
        private EquipmentData equippedHelmet;
        private EquipmentData equippedArmor;
        private EquipmentData equippedPants;
        private EquipmentData equippedNecklace;

        public EquipmentData CurrentEquipment => equippedWeapon;
        public EquipmentData[] Catalog => catalog;
        public float BonusDamage => Sum(item => item.BonusDamage);
        public float BonusArmor => Sum(item => item.BonusArmor);
        public float BonusMaxHealth => Sum(item => item.BonusMaxHealth);
        public float BonusMoveSpeed => 0f;
        public float BonusCriticalChance => Sum(item => item.BonusCriticalChance);
        public float BonusCooldownRate => Sum(item => item.BonusCooldownRate);

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            health = GetComponent<PlayerHealth>();
            LoadEquippedItems();
        }

        public bool Owns(EquipmentData item)
        {
            return item != null && GameProgressManager.Instance != null &&
                GameProgressManager.Instance.HasItem(item.ItemId);
        }

        public bool TryBuyAndEquip(EquipmentData item, out string message)
        {
            return TryBuyAndEquip(item, item != null ? item.Price : 0,
                out message);
        }

        public bool TryBuyAndEquip(EquipmentData item, int purchasePrice,
            out string message)
        {
            if (item == null || stats == null || GameProgressManager.Instance == null)
            {
                message = "Trang bị không hợp lệ.";
                return false;
            }

            if (!Owns(item))
            {
                int safePrice = Mathf.Clamp(purchasePrice, 0, item.Price);
                if (!stats.TrySpendGold(safePrice))
                {
                    message = "Không đủ vàng để mua " + item.DisplayName + ".";
                    return false;
                }

                GameProgressManager.Instance?.AddUniqueItem(item.ItemId);
                message = safePrice < item.Price
                    ? "Đã mua ưu đãi và trang bị " + item.DisplayName + "."
                    : "Đã mua và trang bị " + item.DisplayName + ".";
            }
            else
            {
                message = "Đã trang bị " + item.DisplayName + ".";
            }

            Equip(item);
            return true;
        }

        public bool Equip(EquipmentData item)
        {
            if (item == null || !Owns(item))
            {
                return false;
            }

            float previousMaximumHealth = stats != null ? stats.MaxHealth : 0f;
            SetEquipped(item.Slot, item);
            GameProgressManager.Instance?.SetEquippedItem(item.Slot, item.ItemId);
            float newMaximumHealth = stats != null ? stats.MaxHealth : previousMaximumHealth;
            health?.ApplyEquipmentHealthChange(previousMaximumHealth, newMaximumHealth);
            EquipmentChanged?.Invoke(item);
            PersistChanges();
            return true;
        }

        public bool Unequip(EquipmentSlot slot, out EquipmentData removedItem)
        {
            removedItem = GetEquipped(slot);
            if (removedItem == null)
            {
                return false;
            }

            float previousMaximumHealth = stats != null ? stats.MaxHealth : 0f;
            SetEquipped(slot, null);
            GameProgressManager.Instance?.SetEquippedItem(slot, string.Empty);
            float newMaximumHealth = stats != null ? stats.MaxHealth : previousMaximumHealth;
            health?.ApplyEquipmentHealthChange(previousMaximumHealth, newMaximumHealth);
            EquipmentChanged?.Invoke(null);
            PersistChanges();
            return true;
        }

        public EquipmentData GetEquipped(EquipmentSlot slot)
        {
            return slot switch
            {
                EquipmentSlot.Helmet => equippedHelmet,
                EquipmentSlot.Armor => equippedArmor,
                EquipmentSlot.Pants => equippedPants,
                EquipmentSlot.Necklace => equippedNecklace,
                _ => equippedWeapon
            };
        }

        public void UnequipIfMatching(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                EquipmentData item = GetEquipped(slot);
                if (item != null && string.Equals(item.ItemId, itemId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    Unequip(slot, out _);
                    return;
                }
            }
        }

        public IEnumerable<EquipmentData> OwnedItems()
        {
            if (catalog == null)
            {
                yield break;
            }

            foreach (EquipmentData item in catalog)
            {
                if (Owns(item))
                {
                    yield return item;
                }
            }
        }

        public EquipmentData FindById(string itemId)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            foreach (EquipmentData item in catalog)
            {
                if (item != null && string.Equals(item.ItemId, itemId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }

        public int GetEnhancementLevel(EquipmentData item)
        {
            return item != null && GameProgressManager.Instance != null
                ? GameProgressManager.Instance.GetEnhancementLevel(item.ItemId)
                : 0;
        }

        public static float EnhancementMultiplier(int enhancementLevel)
        {
            return 1f + Mathf.Max(0, enhancementLevel) * BonusPerEnhancementLevel;
        }

        public int EffectivePowerScore(EquipmentData item)
        {
            return item != null ? Mathf.Max(1, Mathf.RoundToInt(item.PowerScore *
                EnhancementMultiplier(GetEnhancementLevel(item)))) : 0;
        }

        public bool TryEnhanceEquipped(EquipmentSlot slot, out string message)
        {
            EquipmentData item = GetEquipped(slot);
            GameProgressManager progress = GameProgressManager.Instance;
            if (item == null || progress == null)
            {
                message = "Hãy trang bị vật phẩm vào ô này trước.";
                return false;
            }

            int level = GetEnhancementLevel(item);
            if (level >= MaximumEnhancementLevel)
            {
                message = item.DisplayName + " đã đạt cường hóa tối đa.";
                return false;
            }

            int required = GameProgressManager.EnhancementEssenceCost(level);
            float previousMaximumHealth = stats != null ? stats.MaxHealth : 0f;
            if (!progress.TryEnhanceEquipment(item.ItemId, MaximumEnhancementLevel,
                out int spent))
            {
                message = "Cần " + required + " Tinh hoa săn để cường hóa.";
                return false;
            }

            float newMaximumHealth = stats != null ? stats.MaxHealth : previousMaximumHealth;
            health?.ApplyEquipmentHealthChange(previousMaximumHealth, newMaximumHealth);
            EquipmentChanged?.Invoke(item);
            message = "Cường hóa " + item.DisplayName + " lên +" +
                GetEnhancementLevel(item) + " (-" + spent + " Tinh hoa).";
            PersistChanges();
            return true;
        }

        private void LoadEquippedItems()
        {
            GameProgressManager progress = GameProgressManager.Instance;
            if (progress == null)
            {
                return;
            }

            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                SetEquipped(slot, FindById(progress.GetEquippedItemId(slot)));
            }
        }

        private void SetEquipped(EquipmentSlot slot, EquipmentData item)
        {
            switch (slot)
            {
                case EquipmentSlot.Helmet:
                    equippedHelmet = item;
                    break;
                case EquipmentSlot.Armor:
                    equippedArmor = item;
                    break;
                case EquipmentSlot.Pants:
                    equippedPants = item;
                    break;
                case EquipmentSlot.Necklace:
                    equippedNecklace = item;
                    break;
                default:
                    equippedWeapon = item;
                    break;
            }
        }

        private float Sum(Func<EquipmentData, float> selector)
        {
            float total = 0f;
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                EquipmentData item = GetEquipped(slot);
                if (item != null)
                {
                    total += selector(item) * EnhancementMultiplier(
                        GetEnhancementLevel(item));
                }
            }

            return total;
        }

        private static void PersistChanges()
        {
            GameProgressManager.Instance?.CaptureActivePlayer();
        }
    }
}
