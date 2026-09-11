using System;
using System.Collections;
using TopDownGame.Combat;
using TopDownGame.Enemies.Progression;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Items;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private DamagePopup damagePopupPrefab;
        [SerializeField, Min(0.01f)] private float deathDelay = 0.15f;

        private EnemyData data;
        private EnemyRuntimeProfile profile;
        private Rigidbody2D body;
        private Collider2D[] colliders;
        private float currentHealth;
        private bool rewardDelivered;

        public event Action<float, float> HealthChanged;
        public event Action<EnemyHealth> Died;

        public bool IsAlive { get; private set; } = true;
        public bool IsInitialized { get; private set; }
        public float CurrentHealth => currentHealth;
        public float MaxHealth => profile != null
            ? profile.MaxHealth
            : data != null ? data.MaxHealth : 1f;
        public float HealthRatio => MaxHealth > 0f ? currentHealth / MaxHealth : 0f;
        public EnemyRuntimeProfile Profile => profile;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            colliders = GetComponentsInChildren<Collider2D>(true);
        }

        public void Initialize(EnemyData enemyData,
            EnemyRuntimeProfile runtimeProfile = null)
        {
            if (IsInitialized) return;
            ResetForSpawn(enemyData, runtimeProfile);
        }

        public void ResetForSpawn(EnemyData enemyData,
            EnemyRuntimeProfile runtimeProfile)
        {
            StopAllCoroutines();
            data = enemyData;
            profile = runtimeProfile != null
                ? runtimeProfile
                : GetComponent<EnemyRuntimeProfile>();
            currentHealth = MaxHealth;
            IsAlive = true;
            IsInitialized = true;
            rewardDelivered = false;

            colliders ??= GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D enemyCollider in colliders)
            {
                if (enemyCollider != null) enemyCollider.enabled = true;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = true;
            }

            HealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive) return;
            if (!IsInitialized)
            {
                EnemyBase enemy = GetComponent<EnemyBase>();
                Initialize(enemy != null ? enemy.Data : null,
                    enemy != null ? enemy.Profile : null);
            }

            float armor = !damageInfo.IgnoreArmor
                ? profile != null ? profile.Armor : data != null ? data.Armor : 0f
                : 0f;
            float amount = Mathf.Max(1f,
                damageInfo.Amount * (100f / (100f + armor)));
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth, MaxHealth);
            DamagePopup.Spawn(transform.position + Vector3.up, amount,
                damageInfo.IsCritical, damagePopupPrefab);

            GetComponent<EnemyCharacterAnimator>()?.PlayHit(damageInfo.Direction);
            EnemyMovement movement = GetComponent<EnemyMovement>();
            if (movement != null && damageInfo.Knockback > 0f)
            {
                movement.AddImpulse(damageInfo.Direction.normalized * damageInfo.Knockback);
            }

            if (currentHealth <= 0f) Die();
        }

        private void Die()
        {
            if (!IsAlive) return;
            IsAlive = false;
            DeliverRewards();
            profile?.ApplyDeathModifier();
            Died?.Invoke(this);

            foreach (Collider2D enemyCollider in colliders)
            {
                if (enemyCollider != null) enemyCollider.enabled = false;
            }

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            EnemyCharacterAnimator animator = GetComponent<EnemyCharacterAnimator>();
            animator?.PlayDeath();
            StartCoroutine(FinishDeathRoutine(Mathf.Max(deathDelay,
                animator != null ? animator.DeathDuration : 0f)));
        }

        private IEnumerator FinishDeathRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            PooledEnemy pooled = GetComponent<PooledEnemy>();
            if (pooled != null && pooled.IsManaged)
            {
                pooled.ReturnToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void DeliverRewards()
        {
            if (rewardDelivered || data == null) return;
            rewardDelivered = true;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;
            if (stats == null) return;

            int gold = profile != null ? profile.GoldReward : data.GoldReward;
            int experience = profile != null
                ? profile.ExperienceRewardFor(stats.Level)
                : data.ExperienceReward;
            EquipmentData rolledEquipment = TryRollEquipment(player,
                profile != null ? profile.ItemDropChance : data.ItemDropChance);
            EnemyLootPickup.Spawn(transform.position, experience, gold,
                rolledEquipment);
        }

        private static EquipmentData TryRollEquipment(GameObject player,
            float dropChance)
        {
            if (player == null || UnityEngine.Random.value > Mathf.Clamp01(dropChance))
            {
                return null;
            }

            EquipmentInventory inventory = player.GetComponent<EquipmentInventory>();
            EquipmentData[] catalog = inventory != null ? inventory.Catalog : null;
            if (catalog == null || catalog.Length == 0) return null;

            int start = UnityEngine.Random.Range(0, catalog.Length);
            for (int offset = 0; offset < catalog.Length; offset++)
            {
                EquipmentData candidate = catalog[(start + offset) % catalog.Length];
                if (candidate != null) return candidate;
            }

            return null;
        }
    }
}
