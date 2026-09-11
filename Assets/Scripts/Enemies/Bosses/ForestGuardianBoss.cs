using System.Collections;
using TopDownGame.Combat;
using TopDownGame.Enemies;
using TopDownGame.Save;
using TopDownGame.Enemies.Spawning;
using UnityEngine;

namespace TopDownGame.Enemies.Bosses
{
    public enum BossState
    {
        Idle,
        Chase,
        BasicAttack,
        Skill,
        Summon,
        Stunned,
        PhaseChange,
        Dead
    }

    public sealed class ForestGuardianBoss : EnemyBase
    {
        [SerializeField] private string bossId = "forest_guardian";
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private Transform[] summonPoints;
        [SerializeField] private LayerMask playerLayers = ~0;
        [SerializeField, Min(0.5f)] private float slamRadius = 2.5f;
        [SerializeField, Min(1f)] private float chargeSpeed = 10f;

        private bool acting;
        private bool phaseTwo;
        private int attackIndex;

        public BossState State { get; private set; } = BossState.Idle;

        protected override void Start()
        {
            base.Start();
            Health.Died += OnBossDied;
        }

        protected override void Tick(float targetDistance)
        {
            if (HandleSignatureAbility(targetDistance, acting))
            {
                return;
            }

            if (acting || Data == null)
            {
                return;
            }

            if (!phaseTwo && Health.HealthRatio <= 0.5f)
            {
                StartCoroutine(PhaseChangeRoutine());
                return;
            }

            if (targetDistance > Data.AttackRange * 1.5f)
            {
                State = BossState.Chase;
                Movement.Pursue(Target,
                    MoveSpeed * (phaseTwo ? 1.3f : 1f), 0.15f);
                return;
            }

            Movement.Stop();
            if (CanAttack)
            {
                StartCoroutine(PerformPattern());
            }
        }

        private IEnumerator PerformPattern()
        {
            acting = true;
            BeginCooldown(phaseTwo ? 0.65f : 1f);

            switch (attackIndex++ % 4)
            {
                case 0:
                    yield return SlamRoutine();
                    break;
                case 1:
                    yield return RadialProjectilesRoutine();
                    break;
                case 2:
                    yield return ChargeRoutine();
                    break;
                default:
                    yield return SummonRoutine();
                    break;
            }

            acting = false;
        }

        private IEnumerator SlamRoutine()
        {
            State = BossState.Skill;
            EnemyCombatVfx.TelegraphCircle(transform.position, slamRadius, 0.65f,
                new Color(0.3f, 1f, 0.24f, 0.76f));
            CharacterAnimator?.PlaySkill(Vector2.down, 0.78f,
                new Color(0.35f, 1f, 0.2f));
            yield return new WaitForSeconds(0.65f);
            EnemyCombatVfx.Burst(transform.position, slamRadius,
                new Color(0.28f, 1f, 0.18f, 0.9f), 0.45f);
            DamagePlayersInRadius(transform.position, slamRadius, Damage * 1.4f);
        }

        private IEnumerator RadialProjectilesRoutine()
        {
            State = BossState.Skill;
            const float windup = 0.48f;
            EnemyCombatVfx.TelegraphCircle(transform.position, 1.35f, windup,
                new Color(0.66f, 0.2f, 1f, 0.72f));
            CharacterAnimator?.PlaySkill(Vector2.up, windup,
                new Color(0.72f, 0.24f, 1f));
            yield return new WaitForSeconds(windup);
            EnemyCombatVfx.Burst(transform.position, 1.4f,
                new Color(0.7f, 0.2f, 1f, 0.86f), 0.3f);
            const int projectileCount = 10;
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * 360f / projectileCount;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                CreateProjectile(direction);
            }
            yield return new WaitForSeconds(0.25f);
        }

        private IEnumerator ChargeRoutine()
        {
            State = BossState.BasicAttack;
            Vector2 direction = (Target.position - transform.position).normalized;
            const float windup = 0.42f;
            EnemyCombatVfx.TelegraphLine(transform.position, direction, 4.5f,
                0.72f, windup, new Color(1f, 0.3f, 0.08f, 0.7f));
            CharacterAnimator?.PlaySkill(direction, 0.72f,
                new Color(1f, 0.28f, 0.08f));
            yield return new WaitForSeconds(windup);
            Movement.AddImpulse(direction * chargeSpeed);
            yield return new WaitForSeconds(0.28f);
            EnemyCombatVfx.Burst(transform.position, AttackRange,
                new Color(1f, 0.3f, 0.08f, 0.8f), 0.28f);
            DamagePlayersInRadius(transform.position, AttackRange, Damage * 1.2f);
        }

        private IEnumerator SummonRoutine()
        {
            State = BossState.Summon;
            const float windup = 0.7f;
            EnemyCombatVfx.TelegraphCircle(transform.position, 2.2f, windup,
                new Color(0.2f, 0.9f, 0.36f, 0.68f));
            CharacterAnimator?.PlaySkill(Vector2.up, windup,
                new Color(0.22f, 0.95f, 0.35f));
            yield return new WaitForSeconds(windup);
            EnemyCombatVfx.Burst(transform.position, 2.2f,
                new Color(0.2f, 0.95f, 0.34f, 0.82f), 0.45f);
            if (minionPrefab != null && summonPoints != null && summonPoints.Length > 0)
            {
                foreach (Transform summonPoint in summonPoints)
                {
                    if (summonPoint != null)
                    {
                        if (EnemySpawnManager.Instance != null)
                            EnemySpawnManager.Instance.SpawnReinforcement(
                                minionPrefab, summonPoint.position);
                        else Instantiate(minionPrefab, summonPoint.position,
                            Quaternion.identity);
                    }
                }
            }
            else if (minionPrefab != null)
            {
                const int summonCount = 3;
                for (int i = 0; i < summonCount; i++)
                {
                    float angle = i * 360f / summonCount;
                    Vector2 offset = Quaternion.Euler(0f, 0f, angle) *
                        Vector2.right * 1.8f;
                    Vector2 position = (Vector2)transform.position + offset;
                    if (EnemySpawnManager.Instance != null)
                        EnemySpawnManager.Instance.SpawnReinforcement(
                            minionPrefab, position);
                    else Instantiate(minionPrefab, position, Quaternion.identity);
                }
            }

            yield return new WaitForSeconds(0.2f);
        }

        private IEnumerator PhaseChangeRoutine()
        {
            acting = true;
            phaseTwo = true;
            State = BossState.PhaseChange;
            Movement.Stop();
            EnemyCombatVfx.TelegraphCircle(transform.position, 3f, 1f,
                new Color(1f, 0.18f, 0.08f, 0.75f));
            CharacterAnimator?.PlaySkill(Vector2.up, 1f,
                new Color(1f, 0.18f, 0.08f));
            yield return new WaitForSeconds(1f);
            EnemyCombatVfx.Burst(transform.position, 3f,
                new Color(1f, 0.18f, 0.06f, 0.9f), 0.5f);
            acting = false;
        }

        private void CreateProjectile(Vector2 direction)
        {
            EnemyProjectile projectile = EnemyProjectilePool.Acquire(null,
                transform.position, direction, new Color(0.6f, 0.15f, 0.9f),
                Vector3.one * 0.4f);
            projectile.Launch(direction, 6f, Damage, gameObject);
        }

        private void DamagePlayersInRadius(Vector2 position, float radius, float damage)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, playerLayers);
            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable target = hit.GetComponentInParent<IDamageable>();
                target?.TakeDamage(new DamageInfo(damage, gameObject,
                    hit.ClosestPoint(position), hit.transform.position - transform.position, 4f));
                Profile?.ApplyLifeSteal(damage);
                break;
            }
        }

        private void OnBossDied(EnemyHealth enemyHealth)
        {
            State = BossState.Dead;
            GameProgressManager.Instance?.MarkBossDefeated(bossId);
        }

        protected override void ResetCombatState()
        {
            acting = false;
            phaseTwo = false;
            attackIndex = 0;
            State = BossState.Idle;
        }
    }
}
