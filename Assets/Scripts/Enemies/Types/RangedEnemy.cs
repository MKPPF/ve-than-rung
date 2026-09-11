using System.Collections;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies.Spawning;
using UnityEngine;

namespace TopDownGame.Enemies
{
    public class RangedEnemy : EnemyBase
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 7f;
        [SerializeField, Min(0.1f)] private float preferredDistance = 5f;
        [SerializeField, Min(0.1f)] private float retreatDistance = 3f;
        [SerializeField, Min(0.1f)] private float shotWindup = 0.34f;
        [SerializeField, Range(0.1f, 1f)] private float strafeSpeedMultiplier = 0.48f;
        [SerializeField] private Vector2 strafeDirectionSeconds =
            new Vector2(1.1f, 2.2f);

        protected bool IsAttacking { get; set; }
        private int shotsFired;
        private float strafeDirection = 1f;
        private float nextStrafeDirectionTime;

        protected override void Tick(float targetDistance)
        {
            if (Data == null || targetDistance > DetectionRange)
            {
                PatrolOrReturn();
                return;
            }

            if (HandleSignatureAbility(targetDistance, IsAttacking))
            {
                return;
            }

            if (IsAttacking)
            {
                Movement.Stop();
                return;
            }

            if (targetDistance < retreatDistance)
            {
                Movement.MoveAway(Target.position, MoveSpeed);
            }
            else if (targetDistance > preferredDistance)
            {
                Movement.Pursue(Target, MoveSpeed);
            }
            else
            {
                UpdateStrafeDirection();
                Movement.Orbit(Target.position,
                    MoveSpeed * strafeSpeedMultiplier, preferredDistance,
                    strafeDirection);
            }

            if (targetDistance <= AttackRange && CanAttack)
            {
                StartCoroutine(AttackRoutine());
                BeginCooldown();
            }
        }

        protected virtual IEnumerator AttackRoutine()
        {
            IsAttacking = true;
            Vector2 direction = (Target.position - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, Target.position);
            EnemyCombatVfx.TelegraphLine(transform.position, direction,
                Mathf.Min(AttackRange, distance), 0.1f, shotWindup,
                new Color(1f, 0.72f, 0.18f, 0.58f));
            CharacterAnimator?.PlayAttack(direction, shotWindup + 0.12f);
            yield return new WaitForSeconds(shotWindup);

            bool spreadShot = Data.Archetype == EnemyArchetype.Archer &&
                ++shotsFired % 3 == 0;
            if (spreadShot)
            {
                CreateProjectile(Quaternion.Euler(0f, 0f, -9f) * direction,
                    1.08f, 0.78f, new Color(1f, 0.72f, 0.18f));
                CreateProjectile(direction, 1.08f, 1f,
                    new Color(1f, 0.84f, 0.28f));
                CreateProjectile(Quaternion.Euler(0f, 0f, 9f) * direction,
                    1.08f, 0.78f, new Color(1f, 0.72f, 0.18f));
            }
            else
            {
                CreateProjectile(direction, 1f, 1f,
                    new Color(1f, 0.48f, 0.14f));
            }

            IsAttacking = false;
        }

        protected EnemyProjectile CreateProjectile(Vector2 direction,
            float speedMultiplier, float damageMultiplier, Color color)
        {
            EnemyProjectile projectile = EnemyProjectilePool.Acquire(
                projectilePrefab, transform.position, direction, color,
                new Vector3(0.34f, 0.13f, 1f));

            projectile.Launch(direction, projectileSpeed * speedMultiplier,
                Damage * damageMultiplier, gameObject);
            return projectile;
        }

        protected override void ResetCombatState()
        {
            IsAttacking = false;
            shotsFired = 0;
            strafeDirection = Random.value < 0.5f ? -1f : 1f;
            ScheduleStrafeDirection();
        }

        private void UpdateStrafeDirection()
        {
            if (Time.time < nextStrafeDirectionTime)
            {
                return;
            }

            strafeDirection *= -1f;
            ScheduleStrafeDirection();
        }

        private void ScheduleStrafeDirection()
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(
                strafeDirectionSeconds.x, strafeDirectionSeconds.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(
                strafeDirectionSeconds.x, strafeDirectionSeconds.y));
            nextStrafeDirectionTime = Time.time + Random.Range(minimum, maximum);
        }
    }
}
