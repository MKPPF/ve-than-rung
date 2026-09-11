using System.Collections;
using TopDownGame.Combat;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public enum CrystalScorpionCombatState
    {
        Idle,
        Pursuing,
        Orbiting,
        Retreating,
        Telegraphing,
        Charging,
        Recovering
    }

    /// <summary>
    /// Maintains a readable combat ring around the player and commits every
    /// dangerous action to a locked telegraph so movement, timing and counters
    /// remain fair even when this enemy is reused by the population pool.
    /// </summary>
    public sealed class CrystalScorpionEnemy : EnemyBase
    {
        [Header("Positioning")]
        [SerializeField, Min(0.1f)] private float retreatDistance = 2.2f;
        [SerializeField, Min(0.1f)] private float orbitOuterDistance = 4f;
        [SerializeField, Min(0.1f)] private float strafeSpeedMultiplier = 0.78f;
        [SerializeField] private Vector2 orbitDirectionSeconds =
            new Vector2(1.2f, 2.2f);
        [SerializeField, Min(0.05f)] private float recoveryDuration = 0.32f;

        [Header("Pincer Snap")]
        [SerializeField, Min(0.1f)] private float pincerRange = 1.55f;
        [SerializeField, Min(0.1f)] private float pincerWindup = 0.36f;
        [SerializeField, Min(0.1f)] private float pincerRadius = 0.78f;
        [SerializeField, Min(0f)] private float pincerKnockback = 2.2f;

        [Header("Crystal Charge")]
        [SerializeField, Min(0.1f)] private float chargeWindup = 0.58f;
        [SerializeField, Min(0.1f)] private float chargeImpulse = 11f;
        [SerializeField, Min(0.05f)] private float chargeDuration = 0.24f;
        [SerializeField, Min(0.1f)] private float telegraphLength = 3.3f;
        [SerializeField, Min(0.1f)] private float impactRadius = 1.05f;
        [SerializeField, Min(0f)] private float impactKnockback = 3.4f;

        [Header("Tail Crystal Spike")]
        [SerializeField, Min(2)] private int spikeEveryAttacks = 3;
        [SerializeField, Min(0.1f)] private float spikeWindup = 0.72f;
        [SerializeField, Min(0.1f)] private float spikeRadius = 0.9f;
        [SerializeField, Min(0f)] private float spikeKnockback = 1.6f;

        private bool performingAction;
        private float strafeDirection = 1f;
        private float nextOrbitDirectionTime;
        private int attackPatternIndex;

        public CrystalScorpionCombatState CurrentState { get; private set; }

        protected override void Tick(float targetDistance)
        {
            if (HandleSignatureAbility(targetDistance, performingAction))
            {
                return;
            }

            if (Data == null || targetDistance > DetectionRange)
            {
                CurrentState = CrystalScorpionCombatState.Idle;
                PatrolOrReturn();
                return;
            }

            if (performingAction)
            {
                if (CurrentState != CrystalScorpionCombatState.Recovering)
                {
                    Movement.Stop();
                }
                return;
            }

            UpdateOrbitDirection();
            if (targetDistance < retreatDistance)
            {
                CurrentState = CrystalScorpionCombatState.Retreating;
                Movement.MoveAway(Target.position, MoveSpeed);
            }
            else if (targetDistance > orbitOuterDistance)
            {
                CurrentState = CrystalScorpionCombatState.Pursuing;
                Movement.Pursue(Target, MoveSpeed, 0.2f);
            }
            else
            {
                CurrentState = CrystalScorpionCombatState.Orbiting;
                Movement.Orbit(Target.position,
                    MoveSpeed * strafeSpeedMultiplier,
                    (retreatDistance + orbitOuterDistance) * 0.5f,
                    strafeDirection);
            }

            if (targetDistance <= AttackRange && CanAttack)
            {
                StartSelectedAttack(targetDistance);
            }
        }

        private void StartSelectedAttack(float targetDistance)
        {
            attackPatternIndex++;
            if (attackPatternIndex % Mathf.Max(2, spikeEveryAttacks) == 0)
            {
                StartCoroutine(TailCrystalSpikeRoutine());
            }
            else if (targetDistance <= pincerRange)
            {
                StartCoroutine(PincerSnapRoutine());
            }
            else
            {
                StartCoroutine(CrystalChargeRoutine());
            }
        }

        private IEnumerator PincerSnapRoutine()
        {
            BeginAction();
            Vector2 lockedPosition = Target != null
                ? (Vector2)Target.position
                : (Vector2)transform.position;
            Vector2 direction = DirectionTo(lockedPosition);
            EnemyCombatVfx.TelegraphCircle(lockedPosition, pincerRadius,
                pincerWindup, new Color(1f, 0.48f, 0.18f, 0.76f));
            const float pincerActivePose = 0.08f;
            CharacterAnimator?.PlayAttackPhased(direction, pincerWindup,
                pincerActivePose,
                Mathf.Max(0.05f, recoveryDuration - pincerActivePose));
            yield return new WaitForSeconds(pincerWindup);

            if (!Health.IsAlive)
            {
                CancelAction();
                yield break;
            }

            EnemyCombatVfx.Burst(lockedPosition, pincerRadius,
                new Color(1f, 0.38f, 0.12f, 0.88f), 0.26f);
            DamagePlayerNear(lockedPosition, pincerRadius, 0.9f,
                pincerKnockback, direction);
            yield return RecoverRoutine();
        }

        private IEnumerator CrystalChargeRoutine()
        {
            BeginAction();
            Vector2 direction = DirectionTo(Target != null
                ? (Vector2)Target.position
                : (Vector2)transform.position + Vector2.right);
            Color crystalColor = new Color(0.12f, 0.88f, 1f, 0.76f);
            EnemyCombatVfx.TelegraphLine(transform.position, direction,
                telegraphLength, 0.42f, chargeWindup, crystalColor);
            CharacterAnimator?.PlaySkillPhased(direction, chargeWindup,
                chargeDuration, recoveryDuration,
                new Color(0.1f, 0.78f, 1f));
            yield return new WaitForSeconds(chargeWindup);

            if (!Health.IsAlive)
            {
                CancelAction();
                yield break;
            }

            CurrentState = CrystalScorpionCombatState.Charging;
            Movement.AddImpulse(direction * chargeImpulse);
            yield return new WaitForSeconds(chargeDuration);

            if (!Health.IsAlive)
            {
                CancelAction();
                yield break;
            }

            Vector2 impactPosition = transform.position;
            EnemyCombatVfx.Burst(impactPosition, impactRadius,
                new Color(0.08f, 0.84f, 1f, 0.9f), 0.34f);
            DamagePlayerNear(impactPosition, impactRadius, 1.25f,
                impactKnockback, direction);
            yield return RecoverRoutine();
        }

        private IEnumerator TailCrystalSpikeRoutine()
        {
            BeginAction();
            Vector2 lockedPosition = Target != null
                ? (Vector2)Target.position
                : (Vector2)transform.position;
            Vector2 direction = DirectionTo(lockedPosition);
            EnemyCombatVfx.TelegraphCircle(lockedPosition, spikeRadius,
                spikeWindup, new Color(0.2f, 0.72f, 1f, 0.78f));
            const float spikeActivePose = 0.1f;
            CharacterAnimator?.PlaySkillPhased(direction, spikeWindup,
                spikeActivePose,
                Mathf.Max(0.05f, recoveryDuration - spikeActivePose),
                new Color(0.18f, 0.68f, 1f));
            yield return new WaitForSeconds(spikeWindup);

            if (!Health.IsAlive)
            {
                CancelAction();
                yield break;
            }

            EnemyCombatVfx.Burst(lockedPosition, spikeRadius,
                new Color(0.18f, 0.78f, 1f, 0.92f), 0.38f);
            DamagePlayerNear(lockedPosition, spikeRadius, 1.05f,
                spikeKnockback, direction);
            yield return RecoverRoutine();
        }

        private void BeginAction()
        {
            performingAction = true;
            CurrentState = CrystalScorpionCombatState.Telegraphing;
            Movement.StopImmediately();
            BeginCooldown();
        }

        private IEnumerator RecoverRoutine()
        {
            CurrentState = CrystalScorpionCombatState.Recovering;
            if (Target != null && Vector2.Distance(transform.position,
                Target.position) < retreatDistance + 0.35f)
            {
                Movement.MoveAway(Target.position, MoveSpeed * 0.82f);
            }
            else if (Target != null)
            {
                Movement.Strafe(Target.position,
                    MoveSpeed * strafeSpeedMultiplier, -strafeDirection);
            }

            yield return new WaitForSeconds(recoveryDuration);
            Movement.Stop();
            performingAction = false;
            CurrentState = CrystalScorpionCombatState.Idle;
            strafeDirection *= -1f;
            ScheduleOrbitDirection();
        }

        private void DamagePlayerNear(Vector2 position, float radius,
            float damageMultiplier, float knockback, Vector2 direction)
        {
            if (!Health.IsAlive || Target == null ||
                Vector2.Distance(position, Target.position) > radius)
            {
                return;
            }

            IDamageable damageable = Target.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            float dealtDamage = Damage * damageMultiplier;
            damageable.TakeDamage(new DamageInfo(dealtDamage, gameObject,
                Target.position, direction, knockback));
            Profile?.ApplyLifeSteal(dealtDamage);
        }

        private Vector2 DirectionTo(Vector2 position)
        {
            Vector2 direction = position - (Vector2)transform.position;
            return direction.sqrMagnitude > 0.01f
                ? direction.normalized
                : Vector2.right;
        }

        private void UpdateOrbitDirection()
        {
            if (Time.time < nextOrbitDirectionTime)
            {
                return;
            }

            strafeDirection *= -1f;
            ScheduleOrbitDirection();
        }

        private void ScheduleOrbitDirection()
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(
                orbitDirectionSeconds.x, orbitDirectionSeconds.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(
                orbitDirectionSeconds.x, orbitDirectionSeconds.y));
            nextOrbitDirectionTime = Time.time + Random.Range(minimum, maximum);
        }

        private void CancelAction()
        {
            performingAction = false;
            CurrentState = CrystalScorpionCombatState.Idle;
            Movement.Stop();
        }

        protected override void ResetCombatState()
        {
            performingAction = false;
            CurrentState = CrystalScorpionCombatState.Idle;
            attackPatternIndex = 0;
            strafeDirection = Random.value < 0.5f ? -1f : 1f;
            ScheduleOrbitDirection();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            performingAction = false;
            CurrentState = CrystalScorpionCombatState.Idle;
        }
    }
}
