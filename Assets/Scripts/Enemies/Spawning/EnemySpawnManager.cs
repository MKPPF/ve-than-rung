using System.Collections.Generic;
using TopDownGame.Enemies.Progression;
using TopDownGame.Maps;
using TopDownGame.Player;
using UnityEngine;

namespace TopDownGame.Enemies.Spawning
{
    /// <summary>Reuses a bounded enemy pool and authored safe anchors so hunting stays populated without spawning in view or across maze walls.</summary>
    [DisallowMultipleComponent]
    public sealed class EnemySpawnManager : MonoBehaviour
    {
        public static EnemySpawnManager Instance { get; private set; }
        [Header("Population")]
        [SerializeField, Min(1)] private int poolHardLimit = 28;
        [SerializeField, Min(0)] private int reservePoolSize = 8;
        [SerializeField, Min(0.05f)] private float evaluationInterval = 0.35f;
        [SerializeField] private bool expandSpawnAnchors = true;
        [SerializeField] private Vector2[] additionalSpawnPoints = System.Array.Empty<Vector2>();

        [Header("Safe Respawn")]
        [SerializeField, Min(2f)] private float minimumPlayerDistance = 7f;
        [SerializeField, Min(0.1f)] private float spawnOverlapRadius = 0.58f;
        [SerializeField, Range(0f, 0.4f)] private float cameraMargin = 0.08f;
        [SerializeField] private LayerMask spawnBlockingLayers = ~0;

        [Header("Respawn Timers")]
        [SerializeField] private Vector2 normalRespawnSeconds = new Vector2(2f, 4f);
        [SerializeField] private Vector2 strongRespawnSeconds = new Vector2(8f, 15f);
        [SerializeField] private Vector2 eliteRespawnSeconds = new Vector2(30f, 90f);
        [SerializeField] private Vector2 bossRespawnSeconds = new Vector2(180f, 600f);

        private readonly List<PooledEnemy> pool = new List<PooledEnemy>(32);
        private readonly List<Vector2> spawnPoints = new List<Vector2>(32);
        private readonly Collider2D[] overlapResults = new Collider2D[12];
        private ContactFilter2D spawnFilter;
        private MapManager mapManager;
        private MapData mapData;
        private Transform player;
        private Camera gameplayCamera;
        private float nextEvaluation;
        private int spawnCursor;
        private bool initialized;

        public int TargetAliveCount { get; private set; }
        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < pool.Count; i++)
                {
                    PooledEnemy entry = pool[i];
                    if (entry != null && entry.PopulationMember &&
                        entry.gameObject.activeInHierarchy &&
                        entry.Health != null && entry.Health.IsAlive) count++;
                }
                return count;
            }
        }
        public bool IsRespawning => ActiveCount < TargetAliveCount;
        public float MinimumPlayerDistance => minimumPlayerDistance;
        public Vector2 NormalRespawnSeconds => normalRespawnSeconds;
        public IReadOnlyList<Vector2> SpawnPoints => spawnPoints;

        private void Awake()
        {
            Instance = this;
            mapManager = GetComponent<MapManager>();
            spawnFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = spawnBlockingLayers,
                useTriggers = false
            };
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            InitializePopulation();
        }

        private void Update()
        {
            if (!initialized || Time.time < nextEvaluation) return;
            nextEvaluation = Time.time + evaluationInterval;
            ResolveSceneReferences();
            ReplenishPopulation();
        }

        public void RegisterExisting(EnemyHealth health)
        {
            if (health == null || Contains(health)) return;
            if (pool.Count >= poolHardLimit) return;
            Adopt(health, health.transform.position);
            if (initialized)
            {
                TargetAliveCount = Mathf.Max(TargetAliveCount,
                    Mathf.Min(poolHardLimit - reservePoolSize, ActiveCount));
            }
        }

        public void NotifyReturned(PooledEnemy enemy)
        {
            if (enemy == null) return;
            if (enemy.ReadyAt <= 0f)
            {
                enemy.Schedule(Time.time + RollRespawnDelay(enemy.Health));
            }
        }

        public GameObject SpawnReinforcement(GameObject prefab, Vector2 position)
        {
            if (prefab == null) return null;
            EnemyBase prefabEnemy = prefab.GetComponent<EnemyBase>();
            if (prefabEnemy == null) return Instantiate(prefab, position,
                Quaternion.identity);

            for (int i = 0; i < pool.Count; i++)
            {
                PooledEnemy entry = pool[i];
                EnemyBase candidate = entry != null
                    ? entry.GetComponent<EnemyBase>()
                    : null;
                if (entry == null || entry.PopulationMember ||
                    entry.gameObject.activeSelf || candidate == null ||
                    candidate.Data != prefabEnemy.Data) continue;
                entry.Spawn(position, RollEnemyLevel(), RollVariant());
                mapManager?.RegisterEnemy(entry.Health);
                return entry.gameObject;
            }

            if (pool.Count >= poolHardLimit) return null;
            GameObject clone = Instantiate(prefab, position, Quaternion.identity,
                transform);
            EnemyHealth health = clone.GetComponent<EnemyHealth>();
            PooledEnemy pooled = clone.GetComponent<PooledEnemy>();
            if (pooled == null) pooled = clone.AddComponent<PooledEnemy>();
            pooled.Configure(this, position, false);
            health.Died -= OnEnemyDied;
            health.Died += OnEnemyDied;
            pool.Add(pooled);
            pooled.Spawn(position, RollEnemyLevel(), RollVariant());
            mapManager?.RegisterEnemy(health);
            return clone;
        }

        public bool IsPointOutsideCamera(Vector2 point)
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            if (gameplayCamera == null) return true;
            Vector3 viewport = gameplayCamera.WorldToViewportPoint(point);
            return viewport.z <= 0f || viewport.x < -cameraMargin ||
                viewport.x > 1f + cameraMargin || viewport.y < -cameraMargin ||
                viewport.y > 1f + cameraMargin;
        }

        private void InitializePopulation()
        {
            mapData = mapManager != null ? mapManager.Data : null;
            ResolveSceneReferences();
            EnemyHealth[] sceneEnemies = FindObjectsByType<EnemyHealth>(
                FindObjectsInactive.Exclude);
            for (int i = 0; i < sceneEnemies.Length; i++)
            {
                RegisterExisting(sceneEnemies[i]);
                EnemyBase enemy = sceneEnemies[i].GetComponent<EnemyBase>();
                EnemyVariant variant = enemy != null && enemy.Data != null &&
                    enemy.Data.Archetype == EnemyArchetype.Boss
                    ? EnemyVariant.Normal
                    : RollVariant();
                enemy?.ResetForSpawn(sceneEnemies[i].transform.position,
                    RollEnemyLevel(), variant);
                mapManager?.RegisterEnemy(sceneEnemies[i]);
                if (sceneEnemies[i].Profile != null &&
                    sceneEnemies[i].Profile.IsElite)
                {
                    HuntRewardEvents.PublishElite(sceneEnemies[i].Profile);
                }
            }

            int configuredTarget = mapData != null
                ? mapData.TargetActiveEnemies
                : Mathf.Max(1, sceneEnemies.Length);
            TargetAliveCount = Mathf.Clamp(Mathf.Max(configuredTarget,
                ActiveCount), 1, Mathf.Max(1, poolHardLimit - reservePoolSize));
            // Extra authored anchors leave free positions for replenishment even
            // when every initial enemy holds its original post.
            if (additionalSpawnPoints != null)
                foreach (Vector2 point in additionalSpawnPoints) AddSpawnPoint(point);
            if (expandSpawnAnchors) ExpandSpawnPoints(TargetAliveCount + reservePoolSize);
            int reserve = mapData != null && mapData.IsBossMap ? 0 : reservePoolSize;
            PrewarmPool(Mathf.Min(poolHardLimit, TargetAliveCount + reserve));
            initialized = true;
            nextEvaluation = Time.time + 0.15f;
        }

        private void ResolveSceneReferences()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject != null ? playerObject.transform : null;
            }
            if (gameplayCamera == null) gameplayCamera = Camera.main;
        }

        private void Adopt(EnemyHealth health, Vector2 home)
        {
            PooledEnemy pooled = health.GetComponent<PooledEnemy>();
            if (pooled == null) pooled = health.gameObject.AddComponent<PooledEnemy>();
            pooled.Configure(this, home);
            pool.Add(pooled);
            AddSpawnPoint(home);
            health.Died -= OnEnemyDied;
            health.Died += OnEnemyDied;
        }

        private void PrewarmPool(int desiredSize)
        {
            if (pool.Count == 0) return;
            int templateCount = pool.Count;
            Transform poolRoot = new GameObject("EnemyPool").transform;
            poolRoot.SetParent(transform, false);
            while (pool.Count < desiredSize)
            {
                PooledEnemy template = pool[pool.Count % templateCount];
                GameObject clone = Instantiate(template.gameObject,
                    template.transform.position, template.transform.rotation, poolRoot);
                clone.name = template.gameObject.name + "_Pool" + pool.Count;
                PooledEnemy entry = clone.GetComponent<PooledEnemy>();
                if (entry == null) entry = clone.AddComponent<PooledEnemy>();
                entry.Configure(this, template.HomePosition);
                entry.Health.Died -= OnEnemyDied;
                entry.Health.Died += OnEnemyDied;
                pool.Add(entry);
                entry.Schedule(Time.time + Random.Range(0.15f, 0.75f));
                clone.SetActive(false);
            }
        }

        private void ReplenishPopulation()
        {
            int missing = TargetAliveCount - ActiveCount;
            if (missing <= 0) return;
            for (int i = 0; i < pool.Count && missing > 0; i++)
            {
                PooledEnemy entry = pool[(spawnCursor + i) % pool.Count];
                if (entry == null || !entry.PopulationMember ||
                    entry.gameObject.activeSelf ||
                    Time.time < entry.ReadyAt) continue;
                if (!TryFindSafeSpawn(out Vector2 spawn)) continue;

                EnemyVariant variant = RollVariant();
                int level = RollEnemyLevel();
                entry.Spawn(spawn, level, variant);
                mapManager?.RegisterEnemy(entry.Health);
                if (entry.Health.Profile != null && entry.Health.Profile.IsElite)
                {
                    HuntRewardEvents.PublishElite(entry.Health.Profile);
                }
                missing--;
                spawnCursor = (spawnCursor + i + 1) % pool.Count;
            }
        }

        private bool TryFindSafeSpawn(out Vector2 result)
        {
            result = default;
            if (spawnPoints.Count == 0) return false;
            int attempts = Mathf.Min(spawnPoints.Count, 24);
            for (int offset = 0; offset < attempts; offset++)
            {
                int index = (spawnCursor + offset) % spawnPoints.Count;
                Vector2 candidate = spawnPoints[index] + Random.insideUnitCircle * 0.3f;
                if (player != null && Vector2.Distance(candidate, player.position) <
                    minimumPlayerDistance) continue;
                if (!IsPointOutsideCamera(candidate)) continue;
                int hitCount = Physics2D.OverlapCircle(candidate,
                    spawnOverlapRadius, spawnFilter, overlapResults);
                bool blocked = false;
                for (int i = 0; i < hitCount; i++)
                {
                    Collider2D hit = overlapResults[i];
                    if (hit != null && hit.enabled && !hit.isTrigger)
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked) continue;
                result = candidate;
                return true;
            }
            return false;
        }

        private void OnEnemyDied(EnemyHealth health)
        {
            if (health == null) return;
            PooledEnemy pooled = health.GetComponent<PooledEnemy>();
            if (pooled != null)
            {
                pooled.Schedule(Time.time + RollRespawnDelay(health));
            }
        }

        private float RollRespawnDelay(EnemyHealth health)
        {
            EnemyRuntimeProfile profile = health != null ? health.Profile : null;
            Vector2 range = profile != null && profile.IsBoss
                ? bossRespawnSeconds
                : profile != null && profile.IsElite
                    ? eliteRespawnSeconds
                    : profile != null && profile.Variant != EnemyVariant.Normal
                        ? strongRespawnSeconds
                        : normalRespawnSeconds;
            float minimum = Mathf.Max(0.1f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            return Random.Range(minimum, maximum);
        }

        private int RollEnemyLevel()
        {
            int minimum = mapData != null ? mapData.RecommendedMinLevel : 1;
            int maximum = mapData != null ? mapData.RecommendedMaxLevel : minimum + 4;
            int playerLevel = player != null
                ? player.GetComponent<PlayerStats>()?.Level ?? minimum
                : minimum;
            if (Random.value < 0.72f)
            {
                return Mathf.Clamp(playerLevel + Random.Range(-2, 3), minimum, maximum);
            }
            return Random.Range(minimum, maximum + 1);
        }

        private static EnemyVariant RollVariant()
        {
            float roll = Random.value;
            if (roll < 0.035f) return EnemyVariant.Elite;
            if (roll < 0.10f) return EnemyVariant.Explosive;
            if (roll < 0.18f) return EnemyVariant.Vampiric;
            if (roll < 0.29f) return EnemyVariant.Armored;
            if (roll < 0.42f) return EnemyVariant.Swift;
            return EnemyVariant.Normal;
        }

        private void ExpandSpawnPoints(int desired)
        {
            if (spawnPoints.Count == 0) return;
            int originalCount = spawnPoints.Count;
            for (int i = spawnPoints.Count; i < desired; i++)
            {
                Vector2 anchor = spawnPoints[i % originalCount];
                float angle = (i * 137.508f) * Mathf.Deg2Rad;
                float distance = 1.25f + (i % 3) * 0.85f;
                AddSpawnPoint(anchor + new Vector2(Mathf.Cos(angle),
                    Mathf.Sin(angle)) * distance);
            }
        }

        private void AddSpawnPoint(Vector2 point)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (Vector2.SqrMagnitude(spawnPoints[i] - point) < 0.2f) return;
            }
            spawnPoints.Add(point);
        }

        private bool Contains(EnemyHealth health)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && pool[i].Health == health) return true;
            }
            return false;
        }
    }
}
