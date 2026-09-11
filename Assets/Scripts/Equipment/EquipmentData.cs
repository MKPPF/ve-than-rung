using System.Text;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace TopDownGame.Equipment
{
    public enum EquipmentSlot
    {
        Weapon,
        Helmet,
        Armor,
        Pants,
        Necklace
    }

    public enum WeaponType
    {
        Sword,
        Greatsword,
        Broadsword,
        BattleAxe,
        WarHammer,
        Spear,
        DualBlades,
        Dagger
    }

    public enum EquipmentRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic,
        Divine
    }

    [CreateAssetMenu(menuName = "Top Down Game/Equipment Data", fileName = "EquipmentData")]
    [MovedFrom(true, "TopDownGame.Equipment", "Assembly-CSharp", "EquipmentData")]
    public sealed class EquipmentData : ScriptableObject
    {
        [SerializeField] private string itemId = "weapon";
        [SerializeField] private string displayName = "Weapon";
        [SerializeField] private EquipmentSlot slot = EquipmentSlot.Weapon;
        [SerializeField] private WeaponType weaponType;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private int price = 100;

        [Header("Stat Bonuses")]
        [SerializeField] private float bonusDamage;
        [SerializeField] private float bonusArmor;
        [SerializeField] private float bonusMaxHealth;
        [SerializeField, HideInInspector] private float bonusMoveSpeed;
        [SerializeField, Range(0f, 1f)] private float bonusCriticalChance;
        [SerializeField] private float bonusCooldownRate;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public EquipmentSlot Slot => slot;
        public WeaponType Type => weaponType;
        public string Description => description;
        public Sprite Icon => icon;
        public int Price => price;
        public float BonusDamage => bonusDamage;
        public float BonusArmor => bonusArmor;
        public float BonusMaxHealth => bonusMaxHealth;
        public float BonusMoveSpeed => 0f;
        public float BonusCriticalChance => bonusCriticalChance;
        public float BonusCooldownRate => bonusCooldownRate;
        public EquipmentRarity Rarity => PriceToRarity(price);
        public int DuplicateEssenceValue => Rarity switch
        {
            EquipmentRarity.Common => 2,
            EquipmentRarity.Uncommon => 4,
            EquipmentRarity.Rare => 7,
            EquipmentRarity.Epic => 12,
            EquipmentRarity.Legendary => 20,
            EquipmentRarity.Mythic => 32,
            _ => 50
        };
        public string RarityLabel => Rarity switch
        {
            EquipmentRarity.Common => "Thường",
            EquipmentRarity.Uncommon => "Tinh nhuệ",
            EquipmentRarity.Rare => "Hiếm",
            EquipmentRarity.Epic => "Sử thi",
            EquipmentRarity.Legendary => "Huyền thoại",
            EquipmentRarity.Mythic => "Thần thoại",
            _ => "Thần thánh"
        };
        public Color RarityColor => Rarity switch
        {
            EquipmentRarity.Common => new Color(0.68f, 0.76f, 0.84f),
            EquipmentRarity.Uncommon => new Color(0.3f, 0.92f, 0.5f),
            EquipmentRarity.Rare => new Color(0.25f, 0.78f, 1f),
            EquipmentRarity.Epic => new Color(0.72f, 0.42f, 1f),
            EquipmentRarity.Legendary => new Color(1f, 0.58f, 0.16f),
            EquipmentRarity.Mythic => new Color(1f, 0.25f, 0.22f),
            _ => new Color(0.72f, 0.96f, 1f)
        };
        public int PowerScore => Mathf.Max(1, Mathf.RoundToInt(
            bonusDamage * 5f + bonusArmor * 4f + bonusMaxHealth * 0.5f +
            bonusCriticalChance * 200f +
            bonusCooldownRate * 200f + price * 0.08f));

        public string TypeLabel => slot switch
        {
            EquipmentSlot.Helmet => "Mũ chiến binh",
            EquipmentSlot.Armor => "Giáp chiến binh",
            EquipmentSlot.Pants => "Quần chiến binh",
            EquipmentSlot.Necklace => "Dây chuyền",
            _ => weaponType switch
            {
                WeaponType.Sword => "Kiếm",
                WeaponType.Greatsword => "Đại kiếm",
                WeaponType.Broadsword => "Kiếm rộng",
                WeaponType.BattleAxe => "Rìu chiến",
                WeaponType.WarHammer => "Búa chiến",
                WeaponType.Spear => "Thương dài",
                WeaponType.DualBlades => "Song đao",
                WeaponType.Dagger => "Dao găm",
                _ => weaponType.ToString()
            }
        };

        public string BuildStatSummary()
        {
            StringBuilder text = new StringBuilder();
            AppendStat(text, "Sát thương", bonusDamage);
            AppendStat(text, "Giáp", bonusArmor);
            AppendStat(text, "Máu", bonusMaxHealth);
            AppendStat(text, "Chí mạng", bonusCriticalChance * 100f, "%");
            AppendStat(text, "Hồi chiêu", bonusCooldownRate * 100f, "%");
            return text.Length > 0 ? text.ToString() : "Không cộng chỉ số";
        }

        public string BuildComparisonSummary(EquipmentData current)
        {
            if (current == this)
            {
                return "Đang sử dụng";
            }

            StringBuilder text = new StringBuilder();
            AppendComparisonStat(text, "ST", bonusDamage - (current?.bonusDamage ?? 0f));
            AppendComparisonStat(text, "Giáp", bonusArmor - (current?.bonusArmor ?? 0f));
            AppendComparisonStat(text, "Máu", bonusMaxHealth - (current?.bonusMaxHealth ?? 0f));
            AppendComparisonStat(text, "CM",
                (bonusCriticalChance - (current?.bonusCriticalChance ?? 0f)) * 100f, "%");
            AppendComparisonStat(text, "Hồi",
                (bonusCooldownRate - (current?.bonusCooldownRate ?? 0f)) * 100f, "%");

            if (text.Length == 0)
            {
                return current == null ? "Không cộng chỉ số" : "Chỉ số tương đương";
            }

            return (current == null ? "Nhận: " : "So với đồ đang dùng: ") + text;
        }

        public string BuildDetailedStatSummary(float multiplier = 1f)
        {
            StringBuilder text = new StringBuilder();
            AppendDetailedStat(text, "Sát thương", bonusDamage * multiplier);
            AppendDetailedStat(text, "Giáp", bonusArmor * multiplier);
            AppendDetailedStat(text, "Máu tối đa", bonusMaxHealth * multiplier);
            AppendDetailedStat(text, "Chí mạng", bonusCriticalChance * multiplier * 100f, "%");
            AppendDetailedStat(text, "Tốc độ hồi chiêu", bonusCooldownRate * multiplier * 100f, "%");
            return text.Length > 0 ? text.ToString() : "Không cộng chỉ số";
        }

        public string BuildDetailedComparison(EquipmentData current,
            float multiplier = 1f, float currentMultiplier = 1f)
        {
            if (current == this)
            {
                return "Đây là trang bị đang sử dụng.";
            }

            StringBuilder text = new StringBuilder();
            AppendDetailedDelta(text, "Sát thương", bonusDamage * multiplier - (current?.bonusDamage ?? 0f) * currentMultiplier);
            AppendDetailedDelta(text, "Giáp", bonusArmor * multiplier - (current?.bonusArmor ?? 0f) * currentMultiplier);
            AppendDetailedDelta(text, "Máu", bonusMaxHealth * multiplier - (current?.bonusMaxHealth ?? 0f) * currentMultiplier);
            AppendDetailedDelta(text, "Chí mạng",
                (bonusCriticalChance * multiplier - (current?.bonusCriticalChance ?? 0f) * currentMultiplier) * 100f, "%");
            AppendDetailedDelta(text, "Hồi chiêu",
                (bonusCooldownRate * multiplier - (current?.bonusCooldownRate ?? 0f) * currentMultiplier) * 100f, "%");
            return text.Length > 0 ? text.ToString() : "Chỉ số tương đương món đang dùng.";
        }

        private static EquipmentRarity PriceToRarity(int itemPrice)
        {
            if (itemPrice <= 250) return EquipmentRarity.Common;
            if (itemPrice <= 350) return EquipmentRarity.Uncommon;
            if (itemPrice <= 450) return EquipmentRarity.Rare;
            if (itemPrice <= 550) return EquipmentRarity.Epic;
            if (itemPrice <= 700) return EquipmentRarity.Legendary;
            if (itemPrice <= 850) return EquipmentRarity.Mythic;
            return EquipmentRarity.Divine;
        }

        private static void AppendStat(StringBuilder builder, string label,
            float value, string suffix = "")
        {
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("  ");
            }

            builder.Append(value > 0f ? "+" : string.Empty)
                .Append(value.ToString("0.#"))
                .Append(suffix)
                .Append(' ')
                .Append(label);
        }

        private static void AppendComparisonStat(StringBuilder builder, string label,
            float value, string suffix = "")
        {
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("  ");
            }

            builder.Append(value > 0f ? "+" : string.Empty)
                .Append(value.ToString("0.#"))
                .Append(suffix)
                .Append(' ')
                .Append(label);
        }

        private static void AppendDetailedStat(StringBuilder builder, string label,
            float value, string suffix = "")
        {
            if (Mathf.Approximately(value, 0f)) return;
            if (builder.Length > 0) builder.AppendLine();
            builder.Append("<color=#9AAAC0>").Append(label).Append("</color>")
                .Append(value > 0f ? "  <color=#7CFF9B>" : "  <color=#FF8D83>")
                .Append(value > 0f ? "+" : string.Empty)
                .Append(value.ToString("0.#")).Append(suffix).Append("</color>");
        }

        private static void AppendDetailedDelta(StringBuilder builder, string label,
            float value, string suffix = "")
        {
            if (Mathf.Approximately(value, 0f)) return;
            if (builder.Length > 0) builder.AppendLine();
            string color = value > 0f ? "#62F58A" : "#FF6B6B";
            builder.Append("<color=").Append(color).Append('>')
                .Append(value > 0f ? "+" : string.Empty)
                .Append(value.ToString("0.#")).Append(suffix).Append(' ')
                .Append(label).Append("</color>");
        }
    }
}
