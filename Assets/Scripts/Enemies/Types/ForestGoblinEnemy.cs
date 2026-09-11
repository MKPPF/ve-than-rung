using System.Collections;
using TopDownGame.Combat;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public enum ForestGoblinCombatState
    {
        Idle,
        Stalking,
        Zigzagging,
        Slashing,
        LeafDashing,
        Recovering,
        Flanking,
        Retreating,
        Tracking
    }

    /// <summary>
    /// Deep Forest Goblin controller: advances in low, alternating arcs and
    /// attacks with short leaf-blade dashes instead of generic straight-line
    /// telegraphs.
    /// </summary>
    public sealed class ForestGoblinEnemy : MeleeEnemy
    {
        [Header("Forest Footwork")]
        [SerializeField, Min(0.2f)] private float stalkingDistance = 2.45f;
        [SerializeField, Range(0.5f, 2f)] private float scurrySpeedMultiplier = 1.18f;
        [SerializeField] private Vector2 zigzagDirectionSeconds =
            new Vector2(1.15f, 1.8f);

        [Header("Leaf Blades")]
        [SerializeField, Min(2)] private int leafDashEveryAttacks = 3;
        [SerializeField, Min(0.05f)] private float slashWindup = 0.3f;
        [SerializeField, Min(0.05f)] private float slashActive = 0.08f;
        [SerializeField, Min(0.05f)] private float slashRecovery = 0.24f;
        [SerializeField, Min(0.1f)] private float leafDashDistance = 2.7f;
        [SerializeField, Min(0.05f)] private float leafDashWindup = 0.48f;
        [SerializeField, Min(0.05f)] private float leafDashActive = 0.28f;
        [SerializeField, Min(0f)] private float leafStrikeKnockback = 2.4f;
        [Header("Ambush Tactics")]
        [SerializeField, Min(0.5f)] private float ambushCooldown = 5.5f;
        [SerializeField, Min(0.1f)] private float retreatDuration = 0.7f;
        [SerializeField, Min(0.1f)] private float secondSlashWindup = 0.38f;
        [SerializeField, Min(2f)] private float chaseLeash = 12f;
        private readonly ForestPathAgent pathAgent = new ForestPathAgent();
        private Vector2 home, lastSeen;
        private float seenUntil, nextAmbushTime, retreatUntil;
        private bool hasHome;

        private bool performingAction;
        private int attackPatternIndex;
        private float zigzagDirection = 1f;
        private float nextZigzagTime;

        public ForestGoblinCombatState CurrentForestState { get; private set; }
        public int LeafDashCasts { get; private set; }
        public string MovementName => "Vòng Sườn Truy Kích";
        public string SkillName => "Ảnh Bộ Song Trảm";
        public bool UsesLinearCombatVfx => false;
        public bool UsesCircularCombatVfx => false;

        protected override void Tick(float targetDistance)
        {
            if (Data == null) return;
            if (!hasHome) { home = transform.position; hasHome = true; }
            // This controller owns the entire pattern; the old generic signature
            // is deliberately not invoked on top of the new ambush state machine.
            if (performingAction) return;
            bool visible = targetDistance <= DetectionRange &&
                ForestCombatGeometry.HasClearPath(transform.position, Target.position);
            if (visible) { lastSeen = Target.position; seenUntil = Time.time + 2.2f; }
            if (Vector2.Distance(transform.position, home) > chaseLeash ||
                (!visible && Time.time > seenUntil))
            {
                CurrentForestState = ForestGoblinCombatState.Idle;
                if (Vector2.Distance(transform.position, home) > 0.6f)
                    pathAgent.Move(Movement, transform.position, home, MoveSpeed * 0.65f);
                else Movement.Stop();
                return;
            }
            if (!visible)
            {
                CurrentForestState = ForestGoblinCombatState.Tracking;
                pathAgent.Move(Movement, transform.position, lastSeen, MoveSpeed);
                return;
            }
            if (Time.time < retreatUntil)
            {
                CurrentForestState = ForestGoblinCombatState.Retreating;
                Vector2 away = ((Vector2)transform.position - (Vector2)Target.position).normalized;
                Vector2 tangent = new Vector2(-away.y, away.x) * zigzagDirection;
                pathAgent.Move(Movement, transform.position,
                    (Vector2)transform.position + (away + tangent * 0.65f).normalized * 1.7f,
                    MoveSpeed * 1.12f);
                return;
            }
            UpdateZigzagDirection();
            if (CanAttack && Time.time >= nextAmbushTime &&
                targetDistance > AttackRange + 0.4f && targetDistance < leafDashDistance + 0.7f)
            {
                StartCoroutine(LeafDashRoutine());
                return;
            }
            if (targetDistance > AttackRange + 0.16f)
            {
                CurrentForestState = ForestGoblinCombatState.Flanking;
                Vector2 fromTarget = ((Vector2)transform.position - (Vector2)Target.position).normalized;
                Vector2 flank = Quaternion.Euler(0, 0, zigzagDirection * 38f) * fromTarget;
                float standOff = targetDistance > stalkingDistance ? 1.7f : AttackRange * 0.68f;
                pathAgent.Move(Movement, transform.position,
                    (Vector2)Target.position + flank * standOff, MoveSpeed * scurrySpeedMultiplier);
                return;
            }

            Movement.Stop();
            if (CanAttack)
            {
                attackPatternIndex++;
                StartCoroutine(attackPatternIndex % Mathf.Max(2,
                    leafDashEveryAttacks) == 0
                    ? LeafDashRoutine()
                    : SlashRoutine());
            }
        }

        private IEnumerator SlashRoutine()
        {
            performingAction = true;
            CurrentForestState = ForestGoblinCombatState.Slashing;
            BeginCooldown(0.72f);
            Movement.StopImmediately();
            Vector2 direction = DirectionToTarget();
            ForestAttackVisual.ShowSector(transform.position, direction,
                AttackRange + 0.26f, slashWindup);
            CharacterAnimator?.PlayAttackPhased(direction, slashWindup,
                slashActive, slashRecovery);
            yield return new WaitForSeconds(slashWindup);
            if (CanContinueAction())
            {
                EnemyCombatVfx.ForestSlashArc(transform.position, direction,
                    AttackRange + 0.26f, VariantColor(0.82f), 0.2f);
                DamageTargetNear(AttackRange + 0.26f, 1f, leafStrikeKnockback,
                    direction);
            }
            yield return new WaitForSeconds(slashActive + slashRecovery);
            FinishAction();
        }

        private IEnumerator LeafDashRoutine()
        {
            performingAction = true;
            CurrentForestState = ForestGoblinCombatState.LeafDashing;
            BeginCooldown(0.86f);
            Movement.StopImmediately();
            Vector2 direction = DirectionToTarget();
            Color color = VariantColor(0.78f);
            nextAmbushTime = Time.time + ambushCooldown;
            float travel = Mathf.Min(leafDashDistance,
                Vector2.Distance(transform.position, Target.position));
            EnemyCombatVfx.ForestDashFootprints(transform.position,
                direction, travel, leafDashWindup, color);
            CharacterAnimator?.PlaySkillPhased(direction, leafDashWindup,
                leafDashActive, slashRecovery, color);
            yield return new WaitForSeconds(leafDashWindup);
            if (!CanContinueAction())
            {
                FinishAction();
                yield break;
            }

            LeafDashCasts++;
            // Existing movement owns sweep/slide collision; this impulse never
            // teleports across the grove or the creek.
            Movement.AddImpulse(direction * Mathf.Min(13f,
                travel / Mathf.Max(0.1f, leafDashActive) + 10f * leafDashActive));
            yield return new WaitForSeconds(leafDashActive);
            Movement.StopImmediately();
            if (CanContinueAction())
            {
                EnemyCombatVfx.ForestSlashArc(transform.position, direction,
                    1.3f, color, 0.24f);
                DamageTargetNear(1.3f, 0.9f, leafStrikeKnockback,
                    direction);
            }
            if (CanContinueAction())
            {
                // The second blade gets its own newly locked warning. Dodging
                // the first strike does not let the follow-up silently auto-hit.
                direction = DirectionToTarget();
                ForestAttackVisual.ShowSector(transform.position, direction, 1.5f, secondSlashWindup);
                CharacterAnimator?.PlayAttackPhased(direction, secondSlashWindup, slashActive, slashRecovery);
                yield return new WaitForSeconds(secondSlashWindup);
                if (CanContinueAction())
                {
                    EnemyCombatVfx.ForestSlashArc(transform.position, direction, 1.5f, color, 0.25f);
                    DamageTargetNear(1.5f, 0.75f, leafStrikeKnockback, direction);
                }
            }
            yield return new WaitForSeconds(slashRecovery);
            FinishAction();
        }

        private void DamageTargetNear(float radius, float multiplier,
            float skillKnockback, Vector2 direction)
        {
            if (!CanContinueAction() || !ForestCombatGeometry.IsInsideStrike(
                transform.position, Target.position, direction, radius, 65f))
            {
                return;
            }

            IDamageable target = Target.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;
            float damage = Damage * multiplier;
            target.TakeDamage(new DamageInfo(damage, gameObject,
                Target.position, direction, skillKnockback));
            Profile?.ApplyLifeSteal(damage);
        }

        private Vector2 DirectionToTarget()
        {
            if (Target == null) return Vector2.right;
            Vector2 direction = Target.position - transform.position;
            return direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : Vector2.right;
        }

        private Color VariantColor(float alpha)
        {
            Color profileColor = Profile != null
                ? Profile.VisualTint
                : Color.white;
            Color baseColor = Color.Lerp(new Color(0.22f, 0.95f, 0.18f),
                new Color(1f, 0.68f, 0.12f),
                Profile != null && Profile.IsElite ? 0.72f : 0.2f);
            Color color = Color.Lerp(baseColor, profileColor, 0.28f);
            color.a = alpha;
            return color;
        }

        private bool CanContinueAction()
        {
            return Health != null && Health.IsAlive && Target != null;
        }

        private void FinishAction()
        {
            Movement.Stop();
            performingAction = false;
            retreatUntil = Time.time + retreatDuration;
            CurrentForestState = ForestGoblinCombatState.Recovering;
        }

        private void UpdateZigzagDirection()
        {
            if (Time.time < nextZigzagTime) return;
            zigzagDirection *= -1f;
            float minimum = Mathf.Max(0.1f, Mathf.Min(
                zigzagDirectionSeconds.x, zigzagDirectionSeconds.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(
                zigzagDirectionSeconds.x, zigzagDirectionSeconds.y));
            nextZigzagTime = Time.time + Random.Range(minimum, maximum);
        }

        protected override void ResetCombatState()
        {
            performingAction = false;
            attackPatternIndex = 0;
            zigzagDirection = Random.value < 0.5f ? -1f : 1f;
            nextZigzagTime = Time.time + Random.Range(0.15f, 0.45f);
            LeafDashCasts = 0;
            home = transform.position;
            hasHome = true;
            seenUntil = retreatUntil = 0;
            nextAmbushTime = Time.time + Random.Range(1.8f, 3.2f);
            pathAgent.Reset();
            CurrentForestState = ForestGoblinCombatState.Idle;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAllCoroutines();
            performingAction = false;
            CurrentForestState = ForestGoblinCombatState.Idle;
        }
    }
}
