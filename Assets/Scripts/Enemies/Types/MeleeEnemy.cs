using System.Collections;
using TopDownGame.Combat;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public class MeleeEnemy : EnemyBase
    {
        [SerializeField, Min(0f)] private float knockback = 2f;
        [SerializeField, Range(0f, 1f)] private float dodgeChance = 0.2f;
        [SerializeField, Min(2)] private int skillEveryAttacks = 4;
        [SerializeField, Min(0.1f)] private float approachSlowingDistance = 1.35f;
        [SerializeField, Min(0.05f)] private float basicAttackWindup = 0.15f;
        [SerializeField, Min(0.05f)] private float basicAttackActive = 0.08f;
        [SerializeField, Min(0.05f)] private float basicAttackRecovery = 0.16f;
        private bool attacking;
        private int attackCount;

        protected override void Tick(float targetDistance)
        {
            if (Data == null)
            {
                return;
            }

            if (HandleSignatureAbility(targetDistance, attacking))
            {
                return;
            }

            if (targetDistance > DetectionRange)
            {
                PatrolOrReturn();
                return;
            }

            if (attacking)
            {
                Movement.Stop();
                return;
            }

            if (targetDistance > AttackRange)
            {
                if (Data.Archetype == EnemyArchetype.Goblin &&
                    Random.value < dodgeChance * Time.deltaTime)
                {
                    Movement.Strafe(Target.position, MoveSpeed * 1.5f,
                        Random.value < 0.5f ? -1f : 1f);
                }
                else
                {
                    Movement.Arrive(Target.position, MoveSpeed, AttackRange,
                        approachSlowingDistance);
                }

                return;
            }

            Movement.Stop();
            if (CanAttack)
            {
                StartCoroutine(AttackRoutine());
            }
        }

        private IEnumerator AttackRoutine()
        {
            attacking = true;
            bool useSkill = ++attackCount % Mathf.Max(2, skillEveryAttacks) == 0;
            BeginCooldown(useSkill ? 1.35f : 1f);
            Vector2 attackDirection = Target != null
                ? (Target.position - transform.position).normalized
                : Vector2.right;

            if (useSkill)
            {
                Movement.StopImmediately();
                yield return SkillRoutine(attackDirection);
                attacking = false;
                yield break;
            }

            Movement.StopImmediately();
            CharacterAnimator?.PlayAttackPhased(attackDirection,
                basicAttackWindup, basicAttackActive, basicAttackRecovery);
            yield return new WaitForSeconds(basicAttackWindup);

            if (Target != null &&
                Vector2.Distance(transform.position, Target.position) <= AttackRange + 0.25f)
            {
                IDamageable damageable = Target.GetComponent<IDamageable>();
                Vector2 direction = Target.position - transform.position;
                damageable?.TakeDamage(new DamageInfo(Damage, gameObject,
                    Target.position, direction, knockback));
                Profile?.ApplyLifeSteal(Damage);
            }

            attacking = false;
        }

        private IEnumerator SkillRoutine(Vector2 direction)
        {
            Movement.Stop();
            switch (Data.Archetype)
            {
                case EnemyArchetype.Slime:
                    yield return LeapSkill(direction);
                    break;

                case EnemyArchetype.Goblin:
                    yield return DashSlashSkill(direction);
                    break;

                case EnemyArchetype.Tank:
                    yield return GroundSlamSkill(direction);
                    break;

                default:
                    yield return HeavyStrikeSkill(direction);
                    break;
            }
        }

        private IEnumerator LeapSkill(Vector2 direction)
        {
            Vector2 landingPoint = Target != null
                ? (Vector2)Target.position
                : (Vector2)transform.position + direction * 1.4f;
            const float radius = 0.85f;
            const float windup = 0.48f;
            EnemyCombatVfx.TelegraphCircle(landingPoint, radius, windup,
                new Color(0.35f, 1f, 0.32f, 0.72f));
            float launchWindup = windup * 0.62f;
            float flightDuration = windup * 0.38f;
            CharacterAnimator?.PlaySkillPhased(direction, launchWindup,
                flightDuration, 0.18f, new Color(0.38f, 1f, 0.32f));
            yield return new WaitForSeconds(launchWindup);
            Movement.AddImpulse(direction * Mathf.Max(5f, MoveSpeed * 2.2f));
            yield return new WaitForSeconds(flightDuration);
            EnemyCombatVfx.Burst(landingPoint, radius,
                new Color(0.32f, 1f, 0.28f, 0.8f));
            DamagePlayerInRadius(landingPoint, radius, Damage * 1.35f, 2.8f);
        }

        private IEnumerator DashSlashSkill(Vector2 direction)
        {
            const float windup = 0.3f;
            EnemyCombatVfx.TelegraphLine(transform.position, direction, 2.8f,
                0.34f, windup, new Color(1f, 0.72f, 0.16f, 0.62f));
            CharacterAnimator?.PlaySkillPhased(direction, windup,
                0.16f, 0.18f, new Color(1f, 0.64f, 0.12f));
            yield return new WaitForSeconds(windup);
            Movement.AddImpulse(direction * 8f);
            yield return new WaitForSeconds(0.16f);
            EnemyCombatVfx.Burst(transform.position, 0.95f,
                new Color(1f, 0.66f, 0.16f, 0.75f), 0.22f);
            DamagePlayerInRadius(transform.position, 1.05f,
                Damage * 1.45f, 3.2f);
        }

        private IEnumerator GroundSlamSkill(Vector2 direction)
        {
            const float radius = 2.05f;
            const float windup = 0.78f;
            EnemyCombatVfx.TelegraphCircle(transform.position, radius, windup,
                new Color(1f, 0.26f, 0.08f, 0.72f));
            CharacterAnimator?.PlaySkillPhased(direction, windup,
                0.12f, 0.22f, new Color(1f, 0.22f, 0.08f));
            yield return new WaitForSeconds(windup);
            EnemyCombatVfx.Burst(transform.position, radius,
                new Color(1f, 0.24f, 0.06f, 0.86f), 0.38f);
            DamagePlayerInRadius(transform.position, radius,
                Damage * 1.55f, 5f);
        }

        private IEnumerator HeavyStrikeSkill(Vector2 direction)
        {
            const float windup = 0.5f;
            EnemyCombatVfx.TelegraphCircle(transform.position, 1.35f, windup,
                new Color(1f, 0.35f, 0.12f, 0.68f));
            CharacterAnimator?.PlaySkillPhased(direction, windup,
                0.1f, 0.18f, new Color(1f, 0.3f, 0.1f));
            yield return new WaitForSeconds(windup);
            EnemyCombatVfx.Burst(transform.position, 1.35f,
                new Color(1f, 0.28f, 0.08f, 0.8f));
            DamagePlayerInRadius(transform.position, 1.35f,
                Damage * 1.4f, 3.5f);
        }

        private void DamagePlayerInRadius(Vector2 position, float radius,
            float damage, float skillKnockback)
        {
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, radius))
            {
                if (!hit.CompareTag("Player")) continue;
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                Vector2 direction = (Vector2)hit.transform.position - position;
                target?.TakeDamage(new DamageInfo(damage, gameObject,
                    hit.ClosestPoint(position), direction, skillKnockback));
                Profile?.ApplyLifeSteal(damage);
                break;
            }
        }

        protected override void ResetCombatState()
        {
            attacking = false;
            attackCount = 0;
        }
    }
}
