using System.Collections;
using TopDownGame.Combat;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public enum TankCombatState
    {
        Idle,
        Pursuing,
        Circling,
        Bracing,
        Telegraphing,
        Charging,
        Recovering
    }

    /// <summary>
    /// Advances deliberately, pressures the edge of melee range and uses
    /// locked, avoidable telegraphs for its bash and periodic ground slam.
    /// </summary>
    public sealed class TankEnemy : EnemyBase
    {
        [Header("Positioning")]
        [SerializeField, Min(0.1f)] private float circleOuterDistance = 2.8f;
        [SerializeField, Min(0.1f)] private float circleSpeedMultiplier = 0.52f;
        [SerializeField] private Vector2 circleDirectionSeconds =
            new Vector2(1.6f, 2.8f);
        [SerializeField, Min(0.05f)] private float recoveryDuration = 0.38f;

        [Header("Shield Bash")]
        [SerializeField, Min(0.1f)] private float bashWindup = 0.42f;
        [SerializeField, Min(0.1f)] private float bashImpulse = 7.5f;
        [SerializeField, Min(0.05f)] private float bashDuration = 0.16f;
        [SerializeField, Min(0.1f)] private float bashRadius = 1.05f;
        [SerializeField, Min(0f)] private float bashKnockback = 4f;

        [Header("Ground Slam")]
        [SerializeField, Min(2)] private int slamEveryAttacks = 3;
        [SerializeField, Min(0.1f)] private float slamWindup = 0.78f;
        [SerializeField, Min(0.1f)] private float slamRadius = 2.05f;
        [SerializeField, Min(1f)] private float slamDamageMultiplier = 1.55f;
        [SerializeField, Min(0f)] private float slamKnockback = 5f;

        private bool performingAction;
        private int attackPatternIndex;
        private float strafeDirection = 1f;
        private float nextCircleDirectionTime;

        public TankCombatState CurrentState { get; private set; }

        protected override void Tick(float targetDistance)
        {
            if (Data == null)
            {
                return;
            }

            if (HandleSignatureAbility(targetDistance, performingAction))
            {
                return;
            }

            if (performingAction)
            {
                if (CurrentState != TankCombatState.Recovering)
                {
                    Movement.Stop();
                }
                return;
            }

            if (targetDistance > DetectionRange)
            {
                CurrentState = TankCombatState.Idle;
                PatrolOrReturn();
                return;
            }

            UpdateCircleDirection();
            if (targetDistance > circleOuterDistance)
            {
                CurrentState = TankCombatState.Pursuing;
                Movement.Pursue(Target, MoveSpeed, 0.12f);
            }
            else if (targetDistance > AttackRange)
            {
                CurrentState = TankCombatState.Circling;
                Movement.Orbit(Target.position,
                    MoveSpeed * circleSpeedMultiplier,
                    (circleOuterDistance + AttackRange) * 0.5f,
                    strafeDirection);
            }
            else
            {
                CurrentState = TankCombatState.Bracing;
                Movement.Stop();
                if (CanAttack)
                {
                    StartSelectedAttack();
                }
            }
        }

        private void StartSelectedAttack()
        {
            attackPatternIndex++;
            if (attackPatternIndex % Mathf.Max(2, slamEveryAttacks) == 0)
            {
                StartCoroutine(GroundSlamRoutine());
            }
            else
            {
                StartCoroutine(ShieldBashRoutine());
            }
        }

        private IEnumerator ShieldBashRoutine()
        {
            BeginAction();
            Vector2 direction = DirectionToTarget();
            EnemyCombatVfx.TelegraphLine(transform.position, direction,
                1.8f, 0.62f, bashWindup,
                new Color(1f, 0.42f, 0.12f, 0.74f));
            CharacterAnimator?.PlayAttackPhased(direction, bashWindup,
                bashDuration, recoveryDuration);
            yield return new WaitForSeconds(bashWindup);

            if (!Health.IsAlive)
            {
                CancelAction();
                yield break;
            }

            CurrentState = TankCombatState.Charging;
            Movement.AddImpulse(direction * bashImpulse);
            yield return new WaitForSeconds(bashDuration);

            if (!Health.IsAlive)
            {
                CancelAction();
                yield break;
            }

            Vector2 impactPosition = transform.position;
            EnemyCombatVfx.Burst(impactPosition, bashRadius,
                new Color(1f, 0.38f, 0.1f, 0.86f), 0.28f);
            DamageTargetNear(impactPosition, bashRadius, 1f,
                bashKnockback, direction);
            yield return RecoverRoutine();
        }

        private IEnumerator GroundSlamRoutine()
        {
            BeginAction();
            Vector2 lockedOrigin = transform.position;
            Vector2 direction = DirectionToTarget();
            EnemyCombatVfx.TelegraphCircle(lockedOrigin, slamRadius,
                slamWindup, new Color(1f, 0.24f, 0.06f, 0.74f));
            const float activePoseDuration = 0.12f;
            CharacterAnimator?.PlaySkillPhased(direction, slamWindup,
                activePoseDuration,
                Mathf.Max(0.05f, recoveryDuration - activePoseDuration),
                new Color(1f, 0.22f, 0.08f));
            yield return new WaitForSeconds(slamWindup);

            if (!Health.IsAlive)
            {
                CancelAction();
                yield break;
            }

            EnemyCombatVfx.Burst(lockedOrigin, slamRadius,
                new Color(1f, 0.2f, 0.04f, 0.9f), 0.4f);
            DamageTargetNear(lockedOrigin, slamRadius,
                slamDamageMultiplier, slamKnockback, direction);
            yield return RecoverRoutine();
        }

        private void BeginAction()
        {
            performingAction = true;
            CurrentState = TankCombatState.Telegraphing;
            Movement.StopImmediately();
            BeginCooldown();
        }

        private void DamageTargetNear(Vector2 position, float radius,
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

        private IEnumerator RecoverRoutine()
        {
            CurrentState = TankCombatState.Recovering;
            if (Target != null && Vector2.Distance(transform.position,
                Target.position) < AttackRange + 0.35f)
            {
                Movement.MoveAway(Target.position, MoveSpeed * 0.72f);
            }
            else if (Target != null)
            {
                Movement.Strafe(Target.position,
                    MoveSpeed * circleSpeedMultiplier, -strafeDirection);
            }

            yield return new WaitForSeconds(recoveryDuration);
            Movement.Stop();
            performingAction = false;
            CurrentState = TankCombatState.Idle;
            strafeDirection *= -1f;
            ScheduleCircleDirection();
        }

        private Vector2 DirectionToTarget()
        {
            if (Target == null)
            {
                return Vector2.right;
            }

            Vector2 direction = Target.position - transform.position;
            return direction.sqrMagnitude > 0.01f
                ? direction.normalized
                : Vector2.right;
        }

        private void UpdateCircleDirection()
        {
            if (Time.time < nextCircleDirectionTime)
            {
                return;
            }

            strafeDirection *= -1f;
            ScheduleCircleDirection();
        }

        private void ScheduleCircleDirection()
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(
                circleDirectionSeconds.x, circleDirectionSeconds.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(
                circleDirectionSeconds.x, circleDirectionSeconds.y));
            nextCircleDirectionTime = Time.time + Random.Range(minimum, maximum);
        }

        private void CancelAction()
        {
            performingAction = false;
            CurrentState = TankCombatState.Idle;
            Movement.Stop();
        }

        protected override void ResetCombatState()
        {
            performingAction = false;
            attackPatternIndex = 0;
            CurrentState = TankCombatState.Idle;
            strafeDirection = Random.value < 0.5f ? -1f : 1f;
            ScheduleCircleDirection();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            performingAction = false;
            CurrentState = TankCombatState.Idle;
        }
    }
}
