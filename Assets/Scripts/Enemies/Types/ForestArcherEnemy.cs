using System.Collections;
using TopDownGame.Combat;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public enum ForestArcherCombatState
    {
        Idle,
        Repositioning,
        CrossStepping,
        Retreating,
        Aiming,
        LeafVolley,
        EvasiveStep,
        ArrowRain,
        HoldingPost
    }

    /// <summary>
    /// Deep Forest Archer controller: keeps a mobile firing lane, cross-steps
    /// around the player and uses arrowhead-shaped aim markers rather than a
    /// stretched laser line.
    /// </summary>
    public sealed class ForestArcherEnemy : RangedEnemy
    {
        [Header("Hunter Footwork")]
        [SerializeField, Min(0.5f)] private float forestPreferredDistance = 5.25f;
        [SerializeField, Min(0.5f)] private float forestRetreatDistance = 2.85f;
        [SerializeField, Range(0.2f, 1.5f)] private float crossStepSpeedMultiplier = 0.72f;
        [SerializeField] private Vector2 crossStepDirectionSeconds =
            new Vector2(0.72f, 1.28f);

        [Header("Leaf Volley")]
        [SerializeField, Min(2)] private int leafVolleyEveryShots = 3;
        [SerializeField, Min(0.05f)] private float quickShotWindup = 0.4f;
        [SerializeField, Min(0.05f)] private float volleyWindup = 0.65f;
        [SerializeField, Min(0f)] private float volleyBackstepImpulse = 4.8f;
        [Header("Hunter Tactics")]
        [SerializeField, Min(0.1f)] private float evasionCooldown = 4f;
        [SerializeField, Min(0.5f)] private float arrowRainCooldown = 7.5f;
        [SerializeField, Min(0.4f)] private float arrowRainWindup = 0.95f;
        [SerializeField, Min(0.5f)] private float arrowRainRadius = 1.45f;
        [SerializeField, Min(0.1f)] private float postHoldSeconds = 1.1f;
        private readonly ForestPathAgent pathAgent = new ForestPathAgent();
        private Vector2 firingPost, home, lastSeen;
        private float nextPostTime, nextEvasionTime, nextRainTime, seenUntil;
        private bool hasHome;
        private ForestAttackVisual rainWarning;

        private int shotPatternIndex;
        private float crossStepDirection = 1f;
        private float nextCrossStepTime;

        public ForestArcherCombatState CurrentForestState { get; private set; }
        public int LeafVolleyCasts { get; private set; }
        public int ArrowRainCasts { get; private set; }
        public int EvasiveSteps { get; private set; }
        public Vector2 LockedRainCenter { get; private set; }
        public string MovementName => "Đổi Chốt Né Bắn";
        public string SkillName => "Vũ Tiễn Phong Tỏa";
        public bool UsesLinearCombatVfx => false;
        public bool UsesCircularCombatVfx => true;

        protected override void Tick(float targetDistance)
        {
            if (Data == null) return;
            if (!hasHome) { home = transform.position; hasHome = true; }
            if (IsAttacking) return;
            bool visible = targetDistance <= DetectionRange &&
                ForestCombatGeometry.HasClearPath(transform.position, Target.position);
            if (visible) { lastSeen = Target.position; seenUntil = Time.time + 1.7f; }
            if (Vector2.Distance(transform.position, home) > 12f ||
                (!visible && Time.time > seenUntil))
            {
                CurrentForestState = ForestArcherCombatState.Idle;
                if (Vector2.Distance(transform.position, home) > 0.6f)
                    pathAgent.Move(Movement, transform.position, home, MoveSpeed * 0.65f);
                else Movement.Stop();
                return;
            }
            if (!visible)
            {
                CurrentForestState = ForestArcherCombatState.Repositioning;
                pathAgent.Move(Movement, transform.position, lastSeen, MoveSpeed);
                return;
            }
            UpdateCrossStepDirection();
            if (targetDistance < forestRetreatDistance && Time.time >= nextEvasionTime)
            {
                StartCoroutine(EvasiveStepRoutine());
                return;
            }
            if (targetDistance > 2.8f && targetDistance <= AttackRange &&
                Time.time >= nextRainTime && CanAttack)
            {
                StartCoroutine(ArrowRainRoutine());
                return;
            }
            if (Time.time >= nextPostTime)
            {
                nextPostTime = Time.time + postHoldSeconds;
                ChooseFiringPost();
            }
            if (targetDistance < forestRetreatDistance)
            {
                CurrentForestState = ForestArcherCombatState.Retreating;
                pathAgent.Move(Movement, transform.position, firingPost, MoveSpeed * 1.18f);
            }
            else if (Vector2.Distance(transform.position, firingPost) > 0.55f)
            {
                CurrentForestState = ForestArcherCombatState.Repositioning;
                pathAgent.Move(Movement, transform.position, firingPost, MoveSpeed * crossStepSpeedMultiplier);
            }
            else
            {
                CurrentForestState = ForestArcherCombatState.HoldingPost;
                Movement.Stop();
            }

            if (targetDistance <= AttackRange && CanAttack)
            {
                shotPatternIndex++;
                StartCoroutine(AttackRoutine());
                BeginCooldown(0.68f);
            }
        }

        protected override IEnumerator AttackRoutine()
        {
            IsAttacking = true;
            bool volley = shotPatternIndex % Mathf.Max(2,
                leafVolleyEveryShots) == 0;
            CurrentForestState = volley
                ? ForestArcherCombatState.LeafVolley
                : ForestArcherCombatState.Aiming;
            Movement.StopImmediately();
            Vector2 direction = DirectionToTarget();
            float distance = Target != null
                ? Vector2.Distance(transform.position, Target.position)
                : AttackRange;
            float windup = volley ? volleyWindup : quickShotWindup;
            Color color = volley
                ? new Color(0.64f, 1f, 0.16f, 0.82f)
                : new Color(1f, 0.82f, 0.24f, 0.76f);
            EnemyCombatVfx.ForestArrowAim(transform.position, direction,
                Mathf.Min(AttackRange, distance), windup, color);
            CharacterAnimator?.PlaySkillPhased(direction, windup,
                volley ? 0.16f : 0.1f, 0.14f, color);
            yield return new WaitForSeconds(windup);

            if (Target == null || Health == null || !Health.IsAlive ||
                !ForestCombatGeometry.HasClearPath(transform.position, Target.position))
            {
                IsAttacking = false;
                yield break;
            }

            if (volley)
            {
                LeafVolleyCasts++;
                Movement.AddImpulse(-direction * volleyBackstepImpulse);
                LaunchFan(direction, 5, 26f, 1.06f, 0.52f,
                    new Color(0.66f, 1f, 0.2f));
                EnemyCombatVfx.ForestLeafBurst(transform.position, 0.72f,
                    color, 0.24f);
            }
            else
            {
                CreateProjectile(direction, 1.32f, 0.92f,
                    new Color(1f, 0.78f, 0.18f));
            }

            yield return new WaitForSeconds(volley ? 0.16f : 0.1f);
            IsAttacking = false;
            CurrentForestState = ForestArcherCombatState.CrossStepping;
            nextPostTime = 0f;
        }

        private void ChooseFiringPost()
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)Target.position).normalized;
            firingPost = transform.position;
            for (int i = 0; i < 6; i++)
            {
                float angle = (i % 2 == 0 ? 1 : -1) * crossStepDirection * (20f + (i / 2) * 23f);
                Vector2 direction = Quaternion.Euler(0, 0, angle) * away;
                Vector2 candidate = (Vector2)Target.position + direction * forestPreferredDistance;
                var navigation = ForestNavigation.Instance;
                if (navigation != null && !navigation.CanStandAt(candidate)) continue;
                if (!ForestCombatGeometry.HasClearPath(candidate, Target.position)) continue;
                firingPost = candidate;
                return;
            }
        }

        private IEnumerator EvasiveStepRoutine()
        {
            IsAttacking = true;
            CurrentForestState = ForestArcherCombatState.EvasiveStep;
            nextEvasionTime = Time.time + evasionCooldown;
            Vector2 away = -DirectionToTarget();
            Vector2 sideways = new Vector2(-away.y, away.x) * crossStepDirection;
            Vector2 direction = (away + sideways * 0.85f).normalized;
            var navigation = ForestNavigation.Instance;
            if (navigation != null && !navigation.CanTravelDirectly(transform.position,
                (Vector2)transform.position + direction * 1.6f))
                direction = (away - sideways * 0.85f).normalized;
            Movement.StopImmediately();
            EnemyCombatVfx.ForestDashFootprints(transform.position, direction, 1.6f, 0.18f, new Color(1, 0.75f, 0.3f, 0.8f));
            CharacterAnimator?.PlaySkillPhased(-away, 0.18f, 0.25f, 0.25f, new Color(1, 0.8f, 0.3f));
            yield return new WaitForSeconds(0.18f);
            if (!CanContinueAction()) { IsAttacking = false; yield break; }
            EvasiveSteps++;
            Movement.AddImpulse(direction * 8.5f);
            yield return new WaitForSeconds(0.25f);
            Movement.StopImmediately();
            if (CanContinueAction())
                EnemyCombatVfx.ForestLeafBurst(transform.position, 0.6f, new Color(0.8f, 1, 0.4f, 0.7f));
            yield return new WaitForSeconds(0.25f);
            IsAttacking = false;
            nextPostTime = 0;
        }

        private IEnumerator ArrowRainRoutine()
        {
            IsAttacking = true;
            CurrentForestState = ForestArcherCombatState.ArrowRain;
            nextRainTime = Time.time + arrowRainCooldown;
            BeginCooldown();
            Movement.StopImmediately();
            LockedRainCenter = Target.position;
            rainWarning = ForestAttackVisual.ShowRain(LockedRainCenter, arrowRainRadius, arrowRainWindup);
            CharacterAnimator?.PlaySkillPhased(DirectionToTarget(), arrowRainWindup, 0.18f, 0.4f, new Color(1f, 0.66f, 0.16f));
            float elapsed = 0;
            while (elapsed < arrowRainWindup)
            {
                if (!CanContinueAction())
                {
                    if (rainWarning != null) Destroy(rainWarning.gameObject);
                    IsAttacking = false;
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (CanContinueAction())
            {
                ArrowRainCasts++;
                IDamageable target = Target.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive &&
                    Vector2.Distance(Target.position, LockedRainCenter) <= arrowRainRadius &&
                    ForestCombatGeometry.HasClearPath(LockedRainCenter, Target.position))
                {
                    float damage = Damage * 1.35f;
                    target.TakeDamage(new DamageInfo(damage, gameObject, Target.position, DirectionToTarget(), 1.6f));
                    Profile?.ApplyLifeSteal(damage);
                }
                EnemyCombatVfx.ForestLeafBurst(LockedRainCenter, arrowRainRadius, new Color(1f, 0.65f, 0.17f, 0.9f), 0.3f);
            }
            yield return new WaitForSeconds(0.4f);
            IsAttacking = false;
            nextPostTime = 0;
        }

        private bool CanContinueAction() => Health != null && Health.IsAlive &&
            Target != null && gameObject.activeInHierarchy;

        private void LaunchFan(Vector2 forward, int count, float totalAngle,
            float speedMultiplier, float damageMultiplier, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (count - 1f);
                float angle = Mathf.Lerp(-totalAngle * 0.5f,
                    totalAngle * 0.5f, t);
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) *
                    forward;
                CreateProjectile(direction, speedMultiplier,
                    damageMultiplier, Color.Lerp(color,
                        new Color(1f, 0.88f, 0.28f),
                        Mathf.Abs(t - 0.5f) * 0.5f));
            }
        }

        private Vector2 DirectionToTarget()
        {
            if (Target == null) return Vector2.right;
            Vector2 direction = Target.position - transform.position;
            return direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
        }

        private void UpdateCrossStepDirection()
        {
            if (Time.time < nextCrossStepTime) return;
            crossStepDirection *= -1f;
            float minimum = Mathf.Max(0.1f, Mathf.Min(
                crossStepDirectionSeconds.x, crossStepDirectionSeconds.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(
                crossStepDirectionSeconds.x, crossStepDirectionSeconds.y));
            nextCrossStepTime = Time.time + Random.Range(minimum, maximum);
        }

        protected override void ResetCombatState()
        {
            base.ResetCombatState();
            if (rainWarning != null) Destroy(rainWarning.gameObject);
            shotPatternIndex = 0;
            crossStepDirection = Random.value < 0.5f ? -1f : 1f;
            nextCrossStepTime = Time.time + Random.Range(0.2f, 0.55f);
            LeafVolleyCasts = 0;
            ArrowRainCasts = EvasiveSteps = 0;
            home = transform.position;
            hasHome = true;
            firingPost = home;
            seenUntil = nextPostTime = nextEvasionTime = 0;
            nextRainTime = Time.time + Random.Range(2.8f, 4.2f);
            pathAgent.Reset();
            CurrentForestState = ForestArcherCombatState.Idle;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAllCoroutines();
            if (rainWarning != null) Destroy(rainWarning.gameObject);
            IsAttacking = false;
            CurrentForestState = ForestArcherCombatState.Idle;
        }
    }
}
