using System;
using System.Collections;
using TopDownGame.Combat;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Player
{
    [RequireComponent(typeof(PlayerStats))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.35f;
        [SerializeField] private DamagePopup damagePopupPrefab;

        private PlayerStats stats;
        private Rigidbody2D body;
        private PlayerMovement movement;
        private float currentHealth;
        private bool invulnerable;
        private float shieldReduction;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public bool IsAlive { get; private set; } = true;
        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            stats = GetComponent<PlayerStats>();
            body = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement>();
            currentHealth = stats.MaxHealth;
        }

        private void Start()
        {
            GameProgressManager.Instance?.ApplyToPlayer(
                stats, this, GetComponent<Skills.SkillInventory>());
            HealthChanged?.Invoke(currentHealth, stats.MaxHealth);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || invulnerable)
            {
                return;
            }

            float amount = damageInfo.IgnoreArmor
                ? damageInfo.Amount
                : stats.ReduceDamage(damageInfo.Amount);
            amount *= 1f - Mathf.Clamp01(shieldReduction);
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth, stats.MaxHealth);
            DamagePopup.Spawn(transform.position + Vector3.up, amount,
                damageInfo.IsCritical, damagePopupPrefab);
            GetComponent<PlayerCharacterAnimator>()?.PlayHit(damageInfo.Direction);

            if (body != null && damageInfo.Knockback > 0f)
            {
                Vector2 impulse = damageInfo.Direction.normalized *
                    damageInfo.Knockback;
                if (movement != null)
                {
                    movement.AddImpulse(impulse);
                }
                else if (body.bodyType == RigidbodyType2D.Dynamic)
                {
                    body.AddForce(impulse, ForceMode2D.Impulse);
                }
            }

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }

            StartCoroutine(InvulnerabilityRoutine());
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(stats.MaxHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, stats.MaxHealth);
        }

        /// <summary>
        /// Applies damage-over-time without consuming or being blocked by the
        /// short invulnerability window created by direct attacks. Poison
        /// bypasses armor, but an active shield still reduces it.
        /// </summary>
        public float TakeStatusDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return 0f;
            }

            float applied = amount *
                (1f - Mathf.Clamp01(shieldReduction));
            float previousHealth = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - applied);
            float healthLost = previousHealth - currentHealth;
            HealthChanged?.Invoke(currentHealth, stats.MaxHealth);
            DamagePopup.Spawn(transform.position + Vector3.up * 0.82f,
                healthLost, false, damagePopupPrefab);

            if (currentHealth <= 0f)
            {
                Die();
            }

            return healthLost;
        }

        public void LoadProgress(float savedHealth)
        {
            currentHealth = Mathf.Clamp(savedHealth, 1f, stats.MaxHealth);
            IsAlive = true;
            HealthChanged?.Invoke(currentHealth, stats.MaxHealth);
        }

        public void ApplyEquipmentHealthChange(float previousMaximum,
            float newMaximum)
        {
            float increase = Mathf.Max(0f, newMaximum - previousMaximum);
            currentHealth = Mathf.Clamp(currentHealth + increase, 0f, newMaximum);
            HealthChanged?.Invoke(currentHealth, newMaximum);
        }

        public Coroutine ActivateShield(float reduction, float duration)
        {
            return StartCoroutine(ShieldRoutine(reduction, duration));
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            invulnerable = true;
            yield return new WaitForSeconds(invulnerabilityDuration);
            invulnerable = false;
        }

        private IEnumerator ShieldRoutine(float reduction, float duration)
        {
            shieldReduction = Mathf.Max(shieldReduction, Mathf.Clamp01(reduction));
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
            shieldReduction = 0f;
        }

        private void Die()
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            Died?.Invoke();
            if (movement != null)
            {
                movement.enabled = false;
            }

            GetComponent<PlayerCharacterAnimator>()?.PlayDeath();
        }
    }
}
