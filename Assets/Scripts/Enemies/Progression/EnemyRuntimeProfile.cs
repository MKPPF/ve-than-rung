using TopDownGame.Combat;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Enemies.Progression
{
    public enum EnemyVariant
    {
        Normal,
        Swift,
        Armored,
        Vampiric,
        Explosive,
        Elite
    }

    [DisallowMultipleComponent]
    public sealed class EnemyRuntimeProfile : MonoBehaviour
    {
        public EnemyData Data { get; private set; }
        public int Level { get; private set; } = 1;
        public EnemyVariant Variant { get; private set; }
        public float MaxHealth { get; private set; } = 1f;
        public float Damage { get; private set; }
        public float Armor { get; private set; }
        public float MoveSpeed { get; private set; }
        public int GoldReward { get; private set; }
        public float ItemDropChance { get; private set; }
        public Color VisualTint { get; private set; } = Color.white;
        public float VisualScale { get; private set; } = 1f;
        public bool IsElite => Variant == EnemyVariant.Elite;
        public bool IsBoss => Data != null && Data.Archetype == EnemyArchetype.Boss;
        public string DisplayName => Variant == EnemyVariant.Normal
            ? Data != null ? Data.DisplayName : "Enemy"
            : Variant + " " + (Data != null ? Data.DisplayName : "Enemy");

        public void Configure(EnemyData data, int level, EnemyVariant variant)
        {
            Data = data;
            Level = Mathf.Max(1, level);
            Variant = variant;
            float healthMultiplier = 1f + (Level - 1) * 0.14f;
            float damageMultiplier = 1f + (Level - 1) * 0.075f;
            float rewardMultiplier = 1f + (Level - 1) * 0.09f;
            float armorBonus = (Level - 1) * 0.7f;
            float speedMultiplier = 1f;
            float dropMultiplier = 1f;
            VisualTint = Color.white;
            VisualScale = 1f;

            switch (variant)
            {
                case EnemyVariant.Swift:
                    healthMultiplier *= 0.86f;
                    damageMultiplier *= 1.08f;
                    speedMultiplier = 1.32f;
                    rewardMultiplier *= 1.45f;
                    VisualTint = new Color(0.62f, 0.92f, 1f);
                    break;
                case EnemyVariant.Armored:
                    healthMultiplier *= 1.28f;
                    speedMultiplier = 0.88f;
                    armorBonus += 32f;
                    rewardMultiplier *= 1.65f;
                    VisualTint = new Color(0.68f, 0.76f, 0.92f);
                    break;
                case EnemyVariant.Vampiric:
                    healthMultiplier *= 1.18f;
                    damageMultiplier *= 1.12f;
                    rewardMultiplier *= 1.8f;
                    VisualTint = new Color(0.95f, 0.4f, 0.58f);
                    break;
                case EnemyVariant.Explosive:
                    healthMultiplier *= 0.92f;
                    damageMultiplier *= 1.22f;
                    rewardMultiplier *= 1.9f;
                    VisualTint = new Color(1f, 0.55f, 0.18f);
                    break;
                case EnemyVariant.Elite:
                    healthMultiplier *= 2.8f;
                    damageMultiplier *= 1.55f;
                    speedMultiplier = 1.08f;
                    armorBonus += 22f;
                    rewardMultiplier *= 4.25f;
                    dropMultiplier = 3.5f;
                    VisualTint = new Color(1f, 0.68f, 0.16f);
                    VisualScale = 1.22f;
                    break;
            }

            MaxHealth = Mathf.Max(1f, (data != null ? data.MaxHealth : 1f) *
                healthMultiplier);
            Damage = Mathf.Max(0f, (data != null ? data.Damage : 0f) *
                damageMultiplier);
            Armor = Mathf.Max(0f, (data != null ? data.Armor : 0f) + armorBonus);
            MoveSpeed = Mathf.Max(0.1f, (data != null ? data.MoveSpeed : 1f) *
                speedMultiplier);
            GoldReward = Mathf.Max(1, Mathf.RoundToInt(
                (data != null ? data.GoldReward : 1) * rewardMultiplier));
            ItemDropChance = Mathf.Clamp01(
                (data != null ? data.ItemDropChance : 0f) * dropMultiplier);
        }

        public int ExperienceRewardFor(int playerLevel)
        {
            int baseReward = Data != null ? Data.ExperienceReward : 5;
            float scaled = baseReward * (1f + (Level - 1) * 0.1f);
            float variantMultiplier = Variant switch
            {
                EnemyVariant.Swift => 1.4f,
                EnemyVariant.Armored => 1.6f,
                EnemyVariant.Vampiric => 1.75f,
                EnemyVariant.Explosive => 1.85f,
                EnemyVariant.Elite => 4.2f,
                _ => 1f
            };
            int gap = Level - Mathf.Max(1, playerLevel);
            float gapMultiplier = gap < 0
                ? Mathf.Max(0.12f, 1f + gap * 0.11f)
                : Mathf.Min(2.25f, 1f + gap * 0.15f);
            return Mathf.Max(1, Mathf.RoundToInt(scaled * variantMultiplier *
                gapMultiplier));
        }

        public void ApplyLifeSteal(float damageDealt)
        {
            if (Variant == EnemyVariant.Vampiric && damageDealt > 0f)
            {
                GetComponent<EnemyHealth>()?.Heal(damageDealt * 0.16f);
            }
        }

        public void ApplyDeathModifier()
        {
            if (Variant != EnemyVariant.Explosive) return;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || Vector2.Distance(transform.position,
                player.transform.position) > 2.2f) return;
            IDamageable target = player.GetComponent<IDamageable>();
            Vector2 direction = player.transform.position - transform.position;
            target?.TakeDamage(new DamageInfo(Damage * 0.75f, gameObject,
                player.transform.position, direction, 2.5f, false, true));
            if (Data != null && (Data.Archetype == EnemyArchetype.Goblin ||
                Data.Archetype == EnemyArchetype.Archer))
            {
                EnemyCombatVfx.ForestLeafBurst(transform.position, 2.2f,
                    new Color(1f, 0.35f, 0.08f, 0.86f), 0.38f);
            }
            else
            {
                EnemyCombatVfx.Burst(transform.position, 2.2f,
                    new Color(1f, 0.35f, 0.08f, 0.86f), 0.38f);
            }
        }
    }
}
