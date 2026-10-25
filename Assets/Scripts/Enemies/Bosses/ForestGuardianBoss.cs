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

    /// <summary>Commits each attack to its warning and cancels pending work when the boss dies, leaves play, or returns to the pool.</summary>
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
        private const float ActivePoseSeconds = 0.1f;
        private const float RecoverySeconds = 0.25f;

        public BossState State { get; private set; } = BossState.Idle;

        protected override void Awake()
        {
            base.Awake();
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
            State = BossState.Idle;
        }

        private IEnumerator SlamRoutine()
        {
            State = BossState.Skill;
            Movement.StopImmediately();
            Vector2 lockedPosition = transform.position;
            const float windup = 0.65f;
            EnemyCombatVfx.TelegraphCircle(lockedPosition, slamRadius, windup,
                new Color(0.3f, 1f, 0.24f, 0.76f), this);
            CharacterAnimator?.PlaySkillPhased(Vector2.down, windup, ActivePoseSeconds, RecoverySeconds,
                new Color(0.35f, 1f, 0.2f));
            yield return new WaitForSeconds(windup);
            if (!Health.IsAlive) yield break;
            EnemyCombatVfx.Burst(lockedPosition, slamRadius,
                new Color(0.28f, 1f, 0.18f, 0.9f), 0.45f);
            TopDownGame.Core.CombatFeedback.WorldImpact(lockedPosition,0.14f);
            DamagePlayersInRadius(lockedPosition, slamRadius, Damage * 1.4f);
            yield return new WaitForSeconds(ActivePoseSeconds + RecoverySeconds);
        }

        private IEnumerator RadialProjectilesRoutine()
        {
            State = BossState.Skill;
            Movement.StopImmediately();
            Vector2 lockedPosition = transform.position;
            const float windup = 0.48f;
            EnemyCombatVfx.TelegraphCircle(lockedPosition, 1.35f, windup,
                new Color(0.66f, 0.2f, 1f, 0.72f), this);
            CharacterAnimator?.PlaySkillPhased(Vector2.up, windup, ActivePoseSeconds, RecoverySeconds,
                new Color(0.72f, 0.24f, 1f));
            yield return new WaitForSeconds(windup);
            if (!Health.IsAlive) yield break;
            EnemyCombatVfx.Burst(lockedPosition, 1.4f,
                new Color(0.7f, 0.2f, 1f, 0.86f), 0.3f);
            const int projectileCount = 10;
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * 360f / projectileCount;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                CreateProjectile(lockedPosition, direction);
            }
            yield return new WaitForSeconds(ActivePoseSeconds + RecoverySeconds);
        }

        private IEnumerator ChargeRoutine()
        {
            State = BossState.BasicAttack;
            Movement.StopImmediately();
            Vector2 origin = Movement.Position;
            Vector2 direction = Target != null ? ((Vector2)Target.position - origin).normalized : Vector2.right;
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.right;
            const float windup = 0.42f;
            const float active = 0.28f;
            Vector2 landing = Movement.PlanChargeDestination(direction * chargeSpeed, active);
            EnemyCombatVfx.TelegraphLine(origin, direction, Vector2.Distance(origin, landing),
                0.3f, windup, new Color(1f, 0.7f, 0.32f, 0.24f), this, true);
            EnemyCombatVfx.TelegraphCircle(landing, AttackRange, windup,
                new Color(1f, 0.3f, 0.08f, 0.8f), this, true);
            CharacterAnimator?.PlaySkillPhased(direction, windup, active, RecoverySeconds,
                new Color(1f, 0.28f, 0.08f));
            yield return new WaitForSeconds(windup);
            if (!Health.IsAlive) yield break;
            bool arrived = false;
            if (Vector2.Distance(Movement.Position, origin) <= 0.12f && Movement.isActiveAndEnabled)
            {
                Movement.BeginCharge(landing, active);
                while (Movement.IsCharging && Movement.isActiveAndEnabled) yield return null;
                arrived = Movement.HasReachedChargeDestination && Vector2.Distance(Movement.Position, landing) <= 0.08f;
            }
            EnemyCombatVfx.CancelWarnings(this);
            Movement.StopImmediately(false);
            if (!Health.IsAlive) yield break;
            if (arrived)
            {
                EnemyCombatVfx.Burst(landing, AttackRange,
                    new Color(1f, 0.3f, 0.08f, 0.8f), 0.28f);
                TopDownGame.Core.CombatFeedback.WorldImpact(landing, 0.1f);
                DamagePlayersInRadius(landing, AttackRange, Damage * 1.2f);
            }
            else CharacterAnimator?.CancelAction();
            yield return new WaitForSeconds(RecoverySeconds);
        }

        private IEnumerator SummonRoutine()
        {
            State = BossState.Summon;
            Movement.StopImmediately();
            Vector2 lockedPosition = transform.position;
            const float windup = 0.7f;
            EnemyCombatVfx.TelegraphCircle(lockedPosition, 2.2f, windup,
                new Color(0.2f, 0.9f, 0.36f, 0.68f), this);
            CharacterAnimator?.PlaySkillPhased(Vector2.up, windup, ActivePoseSeconds, RecoverySeconds,
                new Color(0.22f, 0.95f, 0.35f));
            yield return new WaitForSeconds(windup);
            if (!Health.IsAlive) yield break;
            EnemyCombatVfx.Burst(lockedPosition, 2.2f,
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

            yield return new WaitForSeconds(ActivePoseSeconds + RecoverySeconds);
        }

        private IEnumerator PhaseChangeRoutine()
        {
            acting = true;
            phaseTwo = true;
            State = BossState.PhaseChange;
            Movement.StopImmediately();
            Vector2 lockedPosition = transform.position;
            EnemyCombatVfx.TelegraphCircle(transform.position, 3f, 1f,
                new Color(1f, 0.18f, 0.08f, 0.75f), this);
            CharacterAnimator?.PlaySkillPhased(Vector2.up, 1f, ActivePoseSeconds, RecoverySeconds,
                new Color(1f, 0.18f, 0.08f));
            yield return new WaitForSeconds(1f);
            if (!Health.IsAlive) yield break;
            EnemyCombatVfx.Burst(lockedPosition, 3f,
                new Color(1f, 0.18f, 0.06f, 0.9f), 0.5f);
            yield return new WaitForSeconds(ActivePoseSeconds + RecoverySeconds);
            acting = false;
            State = BossState.Idle;
        }

        private void CreateProjectile(Vector2 origin, Vector2 direction)
        {
            EnemyProjectile projectile = EnemyProjectilePool.Acquire(null,
                origin, direction, new Color(0.6f, 0.15f, 0.9f),
                Vector3.one * 0.4f);
            projectile.Launch(direction, 6f, Damage, gameObject);
        }

        private void DamagePlayersInRadius(Vector2 position, float radius, float damage)
        {
            if (!Health.IsAlive) return;
            Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, playerLayers);
            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Player"))
                {
                    continue;
                }

                IDamageable target = hit.GetComponentInParent<IDamageable>();
                target?.TakeDamage(new DamageInfo(damage, gameObject,
                    hit.ClosestPoint(position), (Vector2)hit.transform.position - position, 4f));
                Profile?.ApplyLifeSteal(damage);
                break;
            }
        }

        private void OnBossDied(EnemyHealth enemyHealth)
        {
            CancelCurrentAction();
            State = BossState.Dead;
            GameProgressManager.Instance?.MarkBossDefeated(bossId);
        }

        protected override void ResetCombatState()
        {
            CancelCurrentAction();
            phaseTwo = false;
            attackIndex = 0;
            State = BossState.Idle;
        }

        private void CancelCurrentAction()
        {
            StopAllCoroutines();
            acting = false;
            Movement?.ResetMotion();
            CharacterAnimator?.CancelAction();
            SignatureAbility?.ResetForSpawn();
            EnemyCombatVfx.CancelWarnings(this);
        }

        protected override void OnDisable()
        {
            CancelCurrentAction();
            base.OnDisable();
            State = Health != null && !Health.IsAlive ? BossState.Dead : BossState.Idle;
        }

        private void OnDestroy()
        {
            if (Health != null) Health.Died -= OnBossDied;
        }
    }
}
