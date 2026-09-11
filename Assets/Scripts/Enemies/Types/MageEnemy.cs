using System.Collections;
using TopDownGame.Combat;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public enum CaveMageCombatState
    {
        Idle,
        Pursuing,
        Orbiting,
        Retreating,
        Telegraphing,
        Recovering
    }

    /// <summary>
    /// Keeps a readable casting ring around the player and commits spells to
    /// positions locked at the beginning of their telegraphs.
    /// </summary>
    public sealed class MageEnemy : RangedEnemy
    {
        [Header("Positioning")]
        [SerializeField, Min(0.1f)] private float combatPreferredDistance = 5.6f;
        [SerializeField, Min(0.1f)] private float combatRetreatDistance = 3.2f;
        [SerializeField, Min(0.1f)] private float orbitSpeedMultiplier = 0.68f;
        [SerializeField] private Vector2 orbitDirectionSeconds =
            new Vector2(1.35f, 2.45f);
        [SerializeField, Min(0.05f)] private float recoveryDuration = 0.3f;

        [Header("Rune Burst")]
        [SerializeField, Min(0.1f)] private float castTime = 0.65f;
        [SerializeField, Min(0.1f)] private float areaRadius = 1.5f;

        [Header("Greater Rune")]
        [SerializeField, Min(2)] private int greaterRuneEveryCasts = 3;
        [SerializeField, Min(1f)] private float greaterRuneWindupMultiplier = 1.25f;
        [SerializeField, Min(1f)] private float greaterRuneRadiusMultiplier = 1.38f;
        [SerializeField, Min(1f)] private float greaterRuneDamageMultiplier = 1.35f;

        private int castPatternIndex;
        private float strafeDirection = 1f;
        private float nextOrbitDirectionTime;

        public CaveMageCombatState CurrentState { get; private set; }

        protected override void Tick(float targetDistance)
        {
            if (Data == null)
            {
                return;
            }

            if (HandleSignatureAbility(targetDistance, IsAttacking))
            {
                return;
            }

            if (IsAttacking)
            {
                if (CurrentState != CaveMageCombatState.Recovering)
                {
                    Movement.Stop();
                }
                return;
            }

            if (targetDistance > DetectionRange)
            {
                CurrentState = CaveMageCombatState.Idle;
                PatrolOrReturn();
                return;
            }

            UpdateOrbitDirection();
            if (targetDistance < combatRetreatDistance)
            {
                CurrentState = CaveMageCombatState.Retreating;
                Movement.MoveAway(Target.position, MoveSpeed);
            }
            else if (targetDistance > combatPreferredDistance)
            {
                CurrentState = CaveMageCombatState.Pursuing;
                Movement.Pursue(Target, MoveSpeed, 0.22f);
            }
            else
            {
                CurrentState = CaveMageCombatState.Orbiting;
                Movement.Orbit(Target.position,
                    MoveSpeed * orbitSpeedMultiplier,
                    combatPreferredDistance, strafeDirection);
            }

            if (targetDistance <= AttackRange && CanAttack)
            {
                BeginCooldown();
                StartCoroutine(AttackRoutine());
            }
        }

        protected override IEnumerator AttackRoutine()
        {
            IsAttacking = true;
            CurrentState = CaveMageCombatState.Telegraphing;
            Movement.StopImmediately();

            int patternLength = Mathf.Max(2, greaterRuneEveryCasts);
            bool greaterRune = ++castPatternIndex % patternLength == 0;
            Vector2 lockedPosition = Target != null
                ? (Vector2)Target.position
                : (Vector2)transform.position;
            Vector2 direction = lockedPosition - (Vector2)transform.position;
            float windup = castTime * (greaterRune
                ? greaterRuneWindupMultiplier
                : 1f);
            float radius = areaRadius * (greaterRune
                ? greaterRuneRadiusMultiplier
                : 1f);
            Color runeColor = greaterRune
                ? new Color(0.88f, 0.28f, 1f, 0.82f)
                : new Color(0.62f, 0.2f, 1f, 0.72f);

            EnemyCombatVfx.TelegraphCircle(lockedPosition, radius, windup,
                runeColor);
            const float activePoseDuration = 0.1f;
            CharacterAnimator?.PlaySkillPhased(direction, windup,
                activePoseDuration,
                Mathf.Max(0.05f, recoveryDuration - activePoseDuration),
                greaterRune ? new Color(0.92f, 0.34f, 1f) :
                    new Color(0.68f, 0.24f, 1f));
            yield return new WaitForSeconds(windup);

            if (!Health.IsAlive)
            {
                CancelAction();
                yield break;
            }

            EnemyCombatVfx.Burst(lockedPosition, radius,
                greaterRune ? new Color(0.92f, 0.28f, 1f, 0.94f) :
                    new Color(0.7f, 0.22f, 1f, 0.88f),
                greaterRune ? 0.52f : 0.42f);
            DamageTargetAt(lockedPosition, radius,
                greaterRune ? greaterRuneDamageMultiplier : 1f);
            yield return RecoverRoutine();
        }

        private void DamageTargetAt(Vector2 lockedPosition, float radius,
            float damageMultiplier)
        {
            if (!Health.IsAlive || Target == null ||
                Vector2.Distance(lockedPosition, Target.position) > radius)
            {
                return;
            }

            IDamageable damageable = Target.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            Vector2 direction = (Vector2)Target.position -
                (Vector2)transform.position;
            float dealtDamage = Damage * damageMultiplier;
            damageable.TakeDamage(new DamageInfo(dealtDamage, gameObject,
                Target.position, direction));
            Profile?.ApplyLifeSteal(dealtDamage);
        }

        private IEnumerator RecoverRoutine()
        {
            CurrentState = CaveMageCombatState.Recovering;
            if (Target != null && Vector2.Distance(transform.position,
                Target.position) < combatRetreatDistance + 0.4f)
            {
                Movement.MoveAway(Target.position, MoveSpeed * 0.9f);
            }
            else if (Target != null)
            {
                Movement.Strafe(Target.position,
                    MoveSpeed * orbitSpeedMultiplier, -strafeDirection);
            }

            yield return new WaitForSeconds(recoveryDuration);
            Movement.Stop();
            IsAttacking = false;
            CurrentState = CaveMageCombatState.Idle;
            strafeDirection *= -1f;
            ScheduleOrbitDirection();
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
            IsAttacking = false;
            CurrentState = CaveMageCombatState.Idle;
            Movement.Stop();
        }

        protected override void ResetCombatState()
        {
            base.ResetCombatState();
            CurrentState = CaveMageCombatState.Idle;
            castPatternIndex = 0;
            strafeDirection = Random.value < 0.5f ? -1f : 1f;
            ScheduleOrbitDirection();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            IsAttacking = false;
            CurrentState = CaveMageCombatState.Idle;
        }
    }
}
