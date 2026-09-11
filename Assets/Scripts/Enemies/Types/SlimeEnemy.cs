using System.Collections;
using TopDownGame.Combat;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public enum SlimeCombatState
    {
        Idle,
        Tracking,
        HopWindup,
        Airborne,
        Landing,
        Biting,
        CastingPoisonSpray,
        CastingAcidBloom = CastingPoisonSpray,
        SignatureSkill
    }

    /// <summary>
    /// Slime-specific controller built around discrete squash-and-stretch hops.
    /// Its body collider stays grounded while the authored visual follows an arc,
    /// preserving reliable top-down collision and hit detection.
    /// </summary>
    public sealed class SlimeEnemy : MeleeEnemy
    {
        public const string PoisonSprayName = "Phun Độc Ăn Mòn";
        public const string AcidBloomName = PoisonSprayName;

        [Header("Hop Locomotion")]
        [SerializeField, Min(0.05f)] private float hopWindup = 0.16f;
        [SerializeField, Min(0.08f)] private float hopAirTime = 0.34f;
        [SerializeField, Min(0.04f)] private float landingRecovery = 0.13f;
        [SerializeField, Min(0.2f)] private float hopImpulse = 7.4f;
        [SerializeField, Min(0f)] private float hopPause = 0.06f;
        [SerializeField, Min(0.1f)] private float approachStopDistance = 1.05f;

        [Header("Bite")]
        [SerializeField, Min(0.05f)] private float biteWindup = 0.2f;
        [SerializeField, Min(0.05f)] private float biteActive = 0.08f;
        [SerializeField, Min(0.05f)] private float biteRecovery = 0.16f;
        [SerializeField, Min(0f)] private float biteKnockback = 1.8f;

        [Header("Poison Spray")]
        [SerializeField, Min(2)] private int poisonSprayEveryAttacks = 2;
        [SerializeField, Min(0.1f)] private float poisonSprayWindup = 0.22f;
        [SerializeField, Min(0.2f)] private float poisonSprayDuration = 0.68f;
        [SerializeField, Min(0.05f)] private float poisonSprayRecovery = 0.1f;
        [SerializeField, Range(0.1f, 2f)] private float poisonSprayCooldownMultiplier = 0.42f;
        [SerializeField, Min(1f)] private float poisonSprayRange = 5.2f;
        [SerializeField, Range(15f, 90f)] private float poisonSprayAngle = 54f;
        [SerializeField, Range(3, 7)] private int poisonStreamCount = 5;
        [SerializeField, Min(0.05f)] private float poisonBurstInterval = 0.1f;
        [SerializeField, Min(0.1f)] private float poisonProjectileSpeed = 6.8f;
        [SerializeField, Range(0f, 1f)] private float poisonApplicationChance = 0.45f;
        [SerializeField, Min(0.1f)] private float poisonDuration = 3.6f;
        [SerializeField, Min(0.15f)] private float poisonTickInterval = 0.6f;
        [SerializeField, Min(0f)] private float poisonDamagePerTickMultiplier = 0.12f;

        private bool performingAction;
        private int attackPatternIndex;
        private float nextHopTime;
        private bool poisonRollResolved;

        public SlimeCombatState CurrentState { get; private set; }
        public int CompletedHops { get; private set; }
        public int PoisonSprayCasts { get; private set; }
        public int AcidBloomCasts => PoisonSprayCasts;
        public string NewSkillName => PoisonSprayName;
        public bool UsesCircularCombatVfx => false;

        protected override void Tick(float targetDistance)
        {
            if (Data == null)
            {
                return;
            }

            if (performingAction)
            {
                return;
            }

            if (targetDistance > DetectionRange)
            {
                CurrentState = SlimeCombatState.Idle;
                PatrolOrReturn();
                return;
            }

            if (targetDistance > Mathf.Max(AttackRange,
                approachStopDistance) + 0.12f)
            {
                CurrentState = SlimeCombatState.Tracking;
                if (Time.time >= nextHopTime)
                {
                    StartCoroutine(HopTowardsTargetRoutine());
                }
                else
                {
                    Movement.Stop();
                }
                return;
            }

            Movement.Stop();
            if (CanAttack)
            {
                attackPatternIndex++;
                if (attackPatternIndex % Mathf.Max(2,
                    poisonSprayEveryAttacks) == 0)
                {
                    StartCoroutine(PoisonSprayRoutine());
                }
                else
                {
                    StartCoroutine(BiteRoutine());
                }
            }
        }

        private IEnumerator HopTowardsTargetRoutine()
        {
            performingAction = true;
            CurrentState = SlimeCombatState.HopWindup;
            Movement.StopImmediately();
            Vector2 direction = DirectionToPredictedTarget(0.15f);
            CharacterAnimator?.PlayHop(direction, hopWindup, hopAirTime,
                landingRecovery);
            yield return new WaitForSeconds(hopWindup);

            if (!CanContinueAction())
            {
                CancelAction();
                yield break;
            }

            CurrentState = SlimeCombatState.Airborne;
            Movement.AddImpulse(direction * Mathf.Max(hopImpulse,
                MoveSpeed * 2.8f));
            yield return new WaitForSeconds(hopAirTime);

            if (!CanContinueAction())
            {
                CancelAction();
                yield break;
            }

            Movement.StopImmediately();
            CurrentState = SlimeCombatState.Landing;
            CompletedHops++;
            CreateLandingSplash(direction);
            yield return new WaitForSeconds(landingRecovery);
            performingAction = false;
            CurrentState = SlimeCombatState.Tracking;
            nextHopTime = Time.time + hopPause;
        }

        private void CreateLandingSplash(Vector2 direction)
        {
            Vector2 side = new Vector2(-direction.y, direction.x);
            Color splashColor = new Color(0.36f, 1f, 0.24f, 0.32f);
            EnemyCombatVfx.PoisonMistPuff((Vector2)transform.position -
                direction * 0.12f, side, 0.42f, 0.12f, splashColor, 0.18f);
            EnemyCombatVfx.PoisonMistPuff((Vector2)transform.position +
                side * 0.2f, side, 0.3f, 0.09f, splashColor, 0.16f);
            EnemyCombatVfx.PoisonMistPuff((Vector2)transform.position -
                side * 0.2f, -side, 0.3f, 0.09f, splashColor, 0.16f);
        }

        private IEnumerator BiteRoutine()
        {
            performingAction = true;
            CurrentState = SlimeCombatState.Biting;
            BeginCooldown();
            Movement.StopImmediately();
            Vector2 direction = DirectionToPredictedTarget(0.04f);
            CharacterAnimator?.PlayAttackPhased(direction, biteWindup,
                biteActive, biteRecovery);
            yield return new WaitForSeconds(biteWindup);

            if (CanContinueAction())
            {
                DamageTargetNear(transform.position, AttackRange + 0.22f,
                    1f, biteKnockback);
            }

            yield return new WaitForSeconds(biteActive + biteRecovery);
            performingAction = false;
            CurrentState = SlimeCombatState.Idle;
        }

        private IEnumerator PoisonSprayRoutine()
        {
            performingAction = true;
            CurrentState = SlimeCombatState.CastingPoisonSpray;
            BeginCooldown(poisonSprayCooldownMultiplier);
            Movement.StopImmediately();
            Vector2 direction = DirectionToPredictedTarget(0.18f);
            Color acidColor = new Color(0.38f, 1f, 0.06f, 0.86f);
            Vector2 nozzle = (Vector2)transform.position + direction * 0.38f;
            EnemyCombatVfx.TelegraphCone(nozzle, direction,
                poisonSprayRange, poisonSprayAngle, poisonSprayWindup,
                new Color(acidColor.r, acidColor.g, acidColor.b, 0.58f));
            CharacterAnimator?.PlaySkillPhased(direction,
                poisonSprayWindup, poisonSprayDuration, poisonSprayRecovery,
                acidColor);
            yield return new WaitForSeconds(poisonSprayWindup);

            if (!CanContinueAction())
            {
                CancelAction();
                yield break;
            }

            PoisonSprayCasts++;
            poisonRollResolved = false;
            float elapsed = 0f;
            float nextDamagePulse = 0f;
            int waveIndex = 0;
            while (elapsed < poisonSprayDuration)
            {
                if (!CanContinueAction())
                {
                    CancelAction();
                    yield break;
                }

                EmitPoisonSprayVolley(direction, acidColor, waveIndex++);
                if (elapsed >= nextDamagePulse)
                {
                    DamageTargetInPoisonCone(direction, Damage * 0.34f);
                    nextDamagePulse += 0.32f;
                }
                yield return new WaitForSeconds(poisonBurstInterval);
                elapsed += poisonBurstInterval;
            }

            yield return new WaitForSeconds(poisonSprayRecovery);
            performingAction = false;
            CurrentState = SlimeCombatState.Idle;
        }

        private void EmitPoisonSprayVolley(Vector2 forward, Color color,
            int waveIndex)
        {
            int count = Mathf.Max(3, poisonStreamCount);
            Vector2 side = new Vector2(-forward.y, forward.x);
            Vector2 nozzle = (Vector2)transform.position + forward * 0.46f;
            for (int i = 0; i < count; i++)
            {
                float lane = count == 1 ? 0f : i / (count - 1f) - 0.5f;
                float flutter = Mathf.Sin(waveIndex * 1.71f + i * 2.23f) *
                    2.4f;
                float angle = lane * poisonSprayAngle + flutter;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * forward;
                Color dropletColor = Color.Lerp(color,
                    new Color(0.78f, 1f, 0.12f, color.a),
                    Mathf.Abs(lane) * 0.42f);
                EnemyProjectile projectile = EnemyProjectilePool.Acquire(null,
                    nozzle + side * lane * 0.18f, direction, dropletColor,
                    new Vector3(0.38f + 0.08f * Mathf.Abs(lane),
                        0.13f, 1f));
                float speed = poisonProjectileSpeed *
                    Mathf.Lerp(0.9f, 1.08f,
                        Mathf.Repeat(waveIndex * 0.37f + i * 0.19f, 1f));
                projectile.Launch(direction, speed, Damage * 0.22f,
                    gameObject, poisonSprayRange / speed + 0.08f);
            }

            Vector2 mistPosition = nozzle + forward *
                (0.36f + waveIndex % 3 * 0.2f);
            EnemyCombatVfx.PoisonMistPuff(mistPosition, forward,
                1.2f, 0.34f + (waveIndex % 2) * 0.08f,
                new Color(color.r, color.g, color.b, 0.42f), 0.3f);
        }

        private void DamageTargetInPoisonCone(Vector2 forward, float damage)
        {
            if (!CanContinueAction()) return;
            Vector2 offset = (Vector2)Target.position -
                (Vector2)transform.position;
            if (offset.magnitude > poisonSprayRange + 0.25f ||
                Vector2.Angle(forward, offset) > poisonSprayAngle * 0.5f + 4f)
            {
                return;
            }

            IDamageable target = Target.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;
            target.TakeDamage(new DamageInfo(damage, gameObject,
                Target.position, forward, 0.65f));
            Profile?.ApplyLifeSteal(damage);
            TryApplyPoison();
        }

        private void TryApplyPoison()
        {
            if (poisonRollResolved || Target == null)
            {
                return;
            }

            poisonRollResolved = true;
            if (Random.value > Mathf.Clamp01(poisonApplicationChance))
            {
                return;
            }

            PlayerPoisonStatus poison =
                Target.GetComponentInParent<PlayerPoisonStatus>();
            if (poison == null)
            {
                PlayerHealth playerHealth =
                    Target.GetComponentInParent<PlayerHealth>();
                if (playerHealth == null)
                {
                    return;
                }
                poison = playerHealth.gameObject.AddComponent<
                    PlayerPoisonStatus>();
            }
            poison.ApplyPoison(Damage * poisonDamagePerTickMultiplier,
                poisonDuration, poisonTickInterval);
        }

        private void DamageTargetNear(Vector2 position, float radius,
            float damageMultiplier, float knockback)
        {
            if (!CanContinueAction() ||
                Vector2.Distance(position, Target.position) > radius)
            {
                return;
            }

            IDamageable target = Target.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive)
            {
                return;
            }

            Vector2 direction = (Vector2)Target.position - position;
            float dealtDamage = Damage * damageMultiplier;
            target.TakeDamage(new DamageInfo(dealtDamage, gameObject,
                Target.position, direction, knockback));
            Profile?.ApplyLifeSteal(dealtDamage);
        }

        private Vector2 DirectionToPredictedTarget(float predictionSeconds)
        {
            return DirectionTo(PredictTargetPosition(predictionSeconds));
        }

        private Vector2 PredictTargetPosition(float predictionSeconds)
        {
            if (Target == null) return transform.position;
            Vector2 position = Target.position;
            Rigidbody2D targetBody = Target.GetComponent<Rigidbody2D>();
            if (targetBody != null)
            {
                position += targetBody.linearVelocity *
                    Mathf.Clamp(predictionSeconds, 0f, 0.4f);
            }
            return position;
        }

        private Vector2 DirectionTo(Vector2 position)
        {
            Vector2 direction = position - (Vector2)transform.position;
            return direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
        }

        private bool CanContinueAction()
        {
            return Health != null && Health.IsAlive && Target != null;
        }

        private void CancelAction()
        {
            performingAction = false;
            CurrentState = SlimeCombatState.Idle;
            Movement.Stop();
        }

        protected override void ResetCombatState()
        {
            performingAction = false;
            attackPatternIndex = 0;
            nextHopTime = Time.time + Random.Range(0.05f, 0.24f);
            CompletedHops = 0;
            PoisonSprayCasts = 0;
            poisonRollResolved = false;
            CurrentState = SlimeCombatState.Idle;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            performingAction = false;
            CurrentState = SlimeCombatState.Idle;
        }
    }
}
