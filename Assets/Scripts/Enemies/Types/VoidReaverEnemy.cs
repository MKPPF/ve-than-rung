using System.Collections;
using TopDownGame.Combat;
using UnityEngine;

namespace TopDownGame.Enemies
{
    /// <summary>
    /// Elite melee hunter with a readable two-hit combo and a long telegraphed
    /// rift dash, giving the eclipse zone a distinct combat rhythm.
    /// </summary>
    public sealed class VoidReaverEnemy : EnemyBase
    {
        [SerializeField, Min(2)] private int riftDashEveryAttacks = 3;
        [SerializeField, Min(0.1f)] private float comboWindup = 0.22f;
        [SerializeField, Min(0.1f)] private float comboInterval = 0.16f;
        [SerializeField, Min(0.1f)] private float dashWindup = 0.65f;
        [SerializeField, Min(1f)] private float dashDistance = 4.8f;
        [SerializeField, Min(0f)] private float knockback = 3.8f;

        private bool attacking;
        private int attackCount;

        protected override void Tick(float targetDistance)
        {
            if (Data == null) return;
            if (HandleSignatureAbility(targetDistance, attacking)) return;
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
                Vector2 predictedTarget = Target.position;
                Rigidbody2D targetBody = Target.GetComponent<Rigidbody2D>();
                if (targetBody != null)
                {
                    predictedTarget += targetBody.linearVelocity * 0.22f;
                }
                Movement.Arrive(predictedTarget, MoveSpeed,
                    AttackRange * 0.88f, 1.9f);
                return;
            }

            Movement.Stop();
            if (CanAttack) StartCoroutine(AttackRoutine());
        }

        private IEnumerator AttackRoutine()
        {
            attacking = true;
            attackCount++;
            bool useDash = attackCount % Mathf.Max(2, riftDashEveryAttacks) == 0;
            BeginCooldown(useDash ? 1.25f : 0.9f);
            if (useDash)
            {
                yield return RiftDashRoutine();
            }
            else
            {
                yield return TwinSlashRoutine();
            }
            attacking = false;
        }

        private IEnumerator TwinSlashRoutine()
        {
            for (int strike = 0; strike < 2; strike++)
            {
                Vector2 direction = DirectionToTarget();
                CharacterAnimator?.PlayAttackPhased(direction, comboWindup,
                    0.08f, comboInterval);
                yield return new WaitForSeconds(comboWindup);
                EnemyCombatVfx.Burst(transform.position, AttackRange * 0.85f,
                    strike == 0
                        ? new Color(0.48f, 0.18f, 1f, 0.72f)
                        : new Color(0.1f, 0.82f, 1f, 0.72f), 0.18f);
                TryDamagePlayer(Damage * (strike == 0 ? 0.72f : 0.9f),
                    AttackRange + 0.28f, knockback * 0.65f);
                yield return new WaitForSeconds(comboInterval);
            }
        }

        private IEnumerator RiftDashRoutine()
        {
            Vector2 direction = DirectionToTarget();
            float distance = Mathf.Min(dashDistance, Target != null
                ? Vector2.Distance(transform.position, Target.position) + 0.8f
                : dashDistance);
            EnemyCombatVfx.TelegraphLine(transform.position, direction, distance,
                0.52f, dashWindup, new Color(0.58f, 0.12f, 1f, 0.76f));
            CharacterAnimator?.PlaySkillPhased(direction, dashWindup,
                0.18f, 0.26f, new Color(0.62f, 0.18f, 1f));
            yield return new WaitForSeconds(dashWindup);
            Movement.StopImmediately();
            Movement.AddImpulse(direction * Mathf.Max(9f, distance * 2.2f));
            yield return new WaitForSeconds(0.18f);
            EnemyCombatVfx.Burst(transform.position, 1.35f,
                new Color(0.5f, 0.12f, 1f, 0.9f), 0.3f);
            TryDamagePlayer(Damage * 1.65f, 1.45f, knockback);
        }

        private Vector2 DirectionToTarget()
        {
            return Target != null
                ? ((Vector2)Target.position - (Vector2)transform.position).normalized
                : Vector2.right;
        }

        private void TryDamagePlayer(float damage, float radius,
            float hitKnockback)
        {
            if (Target == null || Vector2.Distance(transform.position,
                Target.position) > radius) return;
            IDamageable target = Target.GetComponent<IDamageable>();
            Vector2 direction = Target.position - transform.position;
            target?.TakeDamage(new DamageInfo(damage, gameObject,
                Target.position, direction, hitKnockback));
            Profile?.ApplyLifeSteal(damage);
        }

        protected override void ResetCombatState()
        {
            attacking = false;
            attackCount = 0;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            attacking = false;
        }
    }
}
