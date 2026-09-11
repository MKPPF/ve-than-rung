using System;
using TopDownGame.Equipment;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Player
{
    public enum PotentialAttribute
    {
        Vitality,
        Power,
        Guard,
        Haste
    }

    public sealed class PlayerStats : MonoBehaviour
    {
        public const float DefaultMoveSpeed = 3f;
        public const float VitalityHealthPerPoint = 8f;
        public const float PowerDamagePerPoint = 1.5f;
        public const float GuardArmorPerPoint = 1.25f;
        public const float HasteCooldownPerPoint = 0.025f;
        public const float ExperimentalMaxHealth = 50000f;
        public const float ExperimentalMaxMana = 25000f;
        public const float ExperimentalManaRegeneration = 400f;

        [Header("Testing")]
        [SerializeField] private bool experimentalVitals;

        [Header("Primary")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float baseDamage = 20f;
        [SerializeField, Min(0.1f)] private float moveSpeed = DefaultMoveSpeed;
        [SerializeField, Min(0f)] private float armor = 5f;
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.1f;
        [SerializeField, Min(1f)] private float criticalMultiplier = 1.75f;

        [Header("Mana")]
        [SerializeField, Min(0f)] private float maxMana = 100f;
        [SerializeField, Min(0f)] private float manaRegeneration = 8f;

        [Header("Utility")]
        [SerializeField, Min(0.1f)] private float cooldownRate = 1f;
        [SerializeField, Min(0)] private int gold;

        [Header("Progression")]
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int experience;
        [SerializeField, Min(10)] private int baseExperienceToLevel = 100;
        [SerializeField, Min(0f)] private float healthPerLevel = 10f;
        [SerializeField, Min(0f)] private float damagePerLevel = 2f;
        [SerializeField, Min(0f)] private float armorPerLevel = 0.75f;

        [Header("Potential")]
        [SerializeField, Min(0)] private int availablePotentialPoints;
        [SerializeField, Min(0)] private int vitalityPoints;
        [SerializeField, Min(0)] private int powerPoints;
        [SerializeField, Min(0)] private int guardPoints;
        [SerializeField, Min(0)] private int hastePoints;

        private float currentMana;
        private EquipmentInventory equipmentInventory;

        public event Action<float, float> ManaChanged;
        public event Action<int> GoldChanged;
        public event Action<int, int, int> ProgressionChanged;
        public event Action<int> LevelChanged;
        public event Action PotentialChanged;

        private EquipmentInventory Equipment =>
            equipmentInventory ??= GetComponent<EquipmentInventory>();

        public bool UsesExperimentalVitals => experimentalVitals;
        public float PersistentMaxHealth => experimentalVitals
            ? Mathf.Max(maxHealth, ExperimentalMaxHealth)
            : maxHealth;
        public float MaxHealth => PersistentMaxHealth + vitalityPoints * VitalityHealthPerPoint +
            (Equipment?.BonusMaxHealth ?? 0f);
        public float BaseDamage => baseDamage + powerPoints * PowerDamagePerPoint +
            (Equipment?.BonusDamage ?? 0f);
        public float MoveSpeed => Mathf.Max(0.1f, moveSpeed);
        public float Armor => Mathf.Max(0f,
            armor + guardPoints * GuardArmorPerPoint +
            (Equipment?.BonusArmor ?? 0f));
        public float CriticalChance => Mathf.Clamp01(
            criticalChance + (Equipment?.BonusCriticalChance ?? 0f));
        public float CriticalMultiplier => criticalMultiplier;
        public float MaxMana => experimentalVitals
            ? Mathf.Max(maxMana, ExperimentalMaxMana)
            : maxMana;
        public float CurrentMana => currentMana;
        public float CooldownRate => Mathf.Max(0.1f,
            cooldownRate + hastePoints * HasteCooldownPerPoint +
            (Equipment?.BonusCooldownRate ?? 0f));
        public int Gold => gold;
        public int Level => level;
        public int Experience => experience;
        public int AvailablePotentialPoints => availablePotentialPoints;
        public int VitalityPoints => vitalityPoints;
        public int PowerPoints => powerPoints;
        public int GuardPoints => guardPoints;
        public int HastePoints => hastePoints;
        public int TotalPotentialPoints => availablePotentialPoints + vitalityPoints +
            powerPoints + guardPoints + hastePoints;
        public int ExperienceToNextLevel
        {
            get
            {
                int step = Mathf.Max(0, level - 1);
                return Mathf.Max(10, Mathf.RoundToInt(baseExperienceToLevel +
                    step * 42f + 7.5f * Mathf.Pow(step, 1.55f)));
            }
        }
        public float PersistentBaseDamage => baseDamage;
        public float PersistentArmor => armor;
        public float PersistentCooldownRate => cooldownRate;

        private void Awake()
        {
            equipmentInventory = GetComponent<EquipmentInventory>();
            currentMana = MaxMana;
        }

        private void Update()
        {
            float maximumMana = MaxMana;
            if (currentMana >= maximumMana || manaRegeneration <= 0f)
            {
                return;
            }

            currentMana = Mathf.Min(maximumMana,
                currentMana + manaRegeneration * Time.deltaTime);
            ManaChanged?.Invoke(currentMana, maximumMana);
        }

        public bool SpendMana(float amount)
        {
            if (amount < 0f || currentMana < amount)
            {
                return false;
            }

            currentMana -= amount;
            ManaChanged?.Invoke(currentMana, MaxMana);
            return true;
        }

        public void RestoreMana(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentMana = Mathf.Min(MaxMana, currentMana + amount);
            ManaChanged?.Invoke(currentMana, MaxMana);
        }

        public float RollDamage(float multiplier, out bool critical)
        {
            critical = UnityEngine.Random.value < CriticalChance;
            float result = BaseDamage * Mathf.Max(0f, multiplier);
            return critical ? result * criticalMultiplier : result;
        }

        public float ReduceDamage(float incomingDamage)
        {
            return Mathf.Max(1f, incomingDamage * (100f / (100f + Armor)));
        }

        public void AddGold(int amount)
        {
            gold = Mathf.Max(0, gold + amount);
            GameProgressManager.Instance?.AddGold(amount);
            GoldChanged?.Invoke(gold);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount < 0 || gold < amount)
            {
                return false;
            }

            gold -= amount;
            GameProgressManager.Instance?.AddGold(-amount);
            GoldChanged?.Invoke(gold);
            return true;
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            experience += amount;
            while (experience >= ExperienceToNextLevel)
            {
                int required = ExperienceToNextLevel;
                experience -= required;
                level++;
                maxHealth += healthPerLevel;
                baseDamage += damagePerLevel;
                armor += armorPerLevel;
                availablePotentialPoints++;
                GetComponent<PlayerHealth>()?.Heal(MaxHealth);
                LevelChanged?.Invoke(level);
            }

            ProgressionChanged?.Invoke(level, experience, ExperienceToNextLevel);
            PotentialChanged?.Invoke();
        }

        public bool TrySpendPotential(PotentialAttribute attribute)
        {
            if (availablePotentialPoints <= 0)
            {
                return false;
            }

            availablePotentialPoints--;
            switch (attribute)
            {
                case PotentialAttribute.Vitality:
                    vitalityPoints++;
                    GetComponent<PlayerHealth>()?.Heal(VitalityHealthPerPoint);
                    break;
                case PotentialAttribute.Power:
                    powerPoints++;
                    break;
                case PotentialAttribute.Guard:
                    guardPoints++;
                    break;
                case PotentialAttribute.Haste:
                    hastePoints++;
                    break;
                default:
                    availablePotentialPoints++;
                    return false;
            }

            PotentialChanged?.Invoke();
            GameProgressManager.Instance?.CaptureActivePlayer();
            return true;
        }

        public void IncreaseMaxHealth(float amount)
        {
            maxHealth = Mathf.Max(1f, maxHealth + amount);
        }

        public void IncreaseDamage(float amount)
        {
            baseDamage = Mathf.Max(0f, baseDamage + amount);
        }

        public void ImproveCooldown(float amount)
        {
            cooldownRate = Mathf.Max(0.1f, cooldownRate + amount);
        }

        public void LoadProgress(float savedMaxHealth, float savedMana, int savedGold,
            float savedBaseDamage, float savedArmor, float savedCooldownRate,
            int savedLevel, int savedExperience)
        {
            maxHealth = experimentalVitals
                ? Mathf.Max(ExperimentalMaxHealth, savedMaxHealth)
                : Mathf.Max(1f, savedMaxHealth);
            baseDamage = Mathf.Max(0f, savedBaseDamage);
            armor = Mathf.Max(0f, savedArmor);
            cooldownRate = Mathf.Max(0.1f, savedCooldownRate);
            level = Mathf.Max(1, savedLevel);
            experience = Mathf.Clamp(savedExperience, 0, ExperienceToNextLevel - 1);
            currentMana = experimentalVitals
                ? MaxMana
                : Mathf.Clamp(savedMana, 0f, MaxMana);
            gold = Mathf.Max(0, savedGold);
            ManaChanged?.Invoke(currentMana, MaxMana);
            GoldChanged?.Invoke(gold);
            ProgressionChanged?.Invoke(level, experience, ExperienceToNextLevel);
        }

        public void LoadPotentialProgress(int available, int vitality, int power,
            int guard, int haste)
        {
            availablePotentialPoints = Mathf.Max(0, available);
            vitalityPoints = Mathf.Max(0, vitality);
            powerPoints = Mathf.Max(0, power);
            guardPoints = Mathf.Max(0, guard);
            hastePoints = Mathf.Max(0, haste);
            PotentialChanged?.Invoke();
        }
    }
}
