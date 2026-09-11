using UnityEngine;
using TopDownGame.Maps;
using TopDownGame.Enemies.Progression;
using TopDownGame.Core;

namespace TopDownGame.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [SerializeField] private EnemyData data;

        protected Transform Target { get; private set; }
        protected EnemyMovement Movement { get; private set; }
        protected EnemyHealth Health { get; private set; }
        protected EnemyCharacterAnimator CharacterAnimator { get; private set; }
        protected EnemySignatureAbility SignatureAbility { get; private set; }
        protected float NextAttackTime { get; set; }

        private Vector2 homePosition;
        private Vector2 patrolPoint;
        private float nextPatrolDecision;
        private float nextLogicUpdate;
        private float nextTargetSearch;
        private const float PatrolRadius = 2.4f;
        private const float LeashRadius = 5.5f;

        public EnemyData Data => data;
        public EnemyRuntimeProfile Profile { get; private set; }
        public float Damage => Profile != null ? Profile.Damage : data != null ? data.Damage : 0f;
        public float MoveSpeed => Profile != null ? Profile.MoveSpeed : data != null ? data.MoveSpeed : 0f;
        public float AttackRange => data != null ? data.AttackRange : 1f;
        public float DetectionRange => data != null ? data.DetectionRange : 0f;
        public float AttackCooldown => data != null ? data.AttackCooldown : 1f;

        protected virtual void Awake()
        {
            GameplayCollisionRules.ConfigureEnemy(gameObject);
            Movement = GetComponent<EnemyMovement>();
            Health = GetComponent<EnemyHealth>();
            CharacterAnimator = GetComponent<EnemyCharacterAnimator>();
            Profile = GetComponent<EnemyRuntimeProfile>();
            if (Profile == null)
            {
                Profile = gameObject.AddComponent<EnemyRuntimeProfile>();
            }
            if (CharacterAnimator == null)
            {
                CharacterAnimator = gameObject.AddComponent<EnemyCharacterAnimator>();
            }
            SignatureAbility = GetComponent<EnemySignatureAbility>();
            if (SignatureAbility == null)
            {
                SignatureAbility = gameObject.AddComponent<EnemySignatureAbility>();
            }
            SignatureAbility.Configure(this);
            ConfigureMovementPresentation();
        }

        protected virtual void Start()
        {
            if (!Health.IsInitialized)
            {
                Profile.Configure(data, 1, EnemyVariant.Normal);
                Health.Initialize(data, Profile);
            }
            MapManager.Instance?.RegisterEnemy(Health);
            ResolveTarget();
            homePosition = transform.position;
            patrolPoint = homePosition;
            ConfigureMovementPresentation();
        }

        protected virtual void Update()
        {
            if (data == null || !Health.IsAlive)
            {
                Movement.Stop();
                return;
            }

            if (Target == null)
            {
                if (Time.time >= nextTargetSearch)
                {
                    nextTargetSearch = Time.time + 1f;
                    ResolveTarget();
                }
                PatrolOrReturn();
                return;
            }

            float targetDistance = Vector2.Distance(transform.position,
                Target.position);
            bool farFromPlayer = targetDistance > DetectionRange * 1.6f;
            if (farFromPlayer && Time.time < nextLogicUpdate) return;
            nextLogicUpdate = farFromPlayer ? Time.time + 0.18f : Time.time;
            Tick(targetDistance);
        }

        protected abstract void Tick(float targetDistance);

        protected bool CanAttack => Time.time >= NextAttackTime;

        protected void BeginCooldown(float multiplier = 1f)
        {
            NextAttackTime = Time.time + AttackCooldown * Mathf.Max(0.1f, multiplier);
        }

        public void ResetForSpawn(Vector2 spawnPosition, int enemyLevel,
            EnemyVariant variant)
        {
            StopAllCoroutines();
            transform.position = spawnPosition;
            homePosition = spawnPosition;
            patrolPoint = spawnPosition;
            nextPatrolDecision = Time.time + Random.Range(0.35f, 1.1f);
            nextLogicUpdate = 0f;
            nextTargetSearch = 0f;
            NextAttackTime = Time.time + Random.Range(0.15f, 0.65f);
            Movement.ResetMotion();
            Profile.Configure(data, enemyLevel, variant);
            Health.ResetForSpawn(data, Profile);
            ConfigureMovementPresentation();
            CharacterAnimator?.ResetForSpawn(Profile.VisualTint,
                Profile.VisualScale);
            SignatureAbility?.Configure(this);
            SignatureAbility?.ResetForSpawn();
            ResetCombatState();
            ResolveTarget();
        }

        protected void PatrolOrReturn()
        {
            Vector2 position = transform.position;
            float distanceHome = Vector2.Distance(position, homePosition);
            if (distanceHome > LeashRadius)
            {
                Movement.MoveTowards(homePosition, MoveSpeed * 0.75f);
                return;
            }

            if (Time.time >= nextPatrolDecision ||
                Vector2.Distance(position, patrolPoint) < 0.25f)
            {
                nextPatrolDecision = Time.time + Random.Range(1.2f, 2.8f);
                patrolPoint = homePosition + Random.insideUnitCircle * PatrolRadius;
            }

            Movement.MoveTowards(patrolPoint, MoveSpeed * 0.38f);
        }

        protected virtual void ResetCombatState()
        {
        }

        protected bool HandleSignatureAbility(float targetDistance,
            bool localActionBusy)
        {
            return SignatureAbility != null && SignatureAbility.HandleCombatTick(
                Target, targetDistance, localActionBusy);
        }

        protected virtual void OnDisable()
        {
            Movement?.ResetMotion();
        }

        private void ResolveTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Target = player != null ? player.transform : null;
        }

        private void ConfigureMovementPresentation()
        {
            if (data == null)
            {
                return;
            }

            Movement?.ConfigureArchetype(data.Archetype);
            CharacterAnimator?.ConfigureArchetype(data.Archetype);
        }
    }
}
