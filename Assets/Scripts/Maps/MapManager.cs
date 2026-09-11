using System.Collections.Generic;
using TMPro;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Spawning;
using TopDownGame.World;
using TopDownGame.Save;
using UnityEngine;

namespace TopDownGame.Maps
{
    public sealed class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [SerializeField] private MapData mapData;
        [SerializeField] private MapDoor[] doors;
        [SerializeField] private RewardChest rewardChest;
        [SerializeField] private TMP_Text completionMessage;

        private readonly HashSet<EnemyHealth> aliveEnemies = new HashSet<EnemyHealth>();
        private EnemySpawnManager spawnManager;

        public int RemainingEnemies => aliveEnemies.Count;
        public int TargetEnemyCount => spawnManager != null
            ? spawnManager.TargetAliveCount
            : mapData != null ? mapData.TargetActiveEnemies : 0;
        public bool IsRespawning => spawnManager != null && spawnManager.IsRespawning;
        // Kept for backwards compatibility. Hunting zones never require a clear.
        public bool IsComplete => true;
        public MapData Data => mapData;
        public string ObjectiveText => "Mục tiêu: săn quái, nhặt đồ và lên cấp";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            spawnManager = GetComponent<EnemySpawnManager>();
            if (spawnManager == null)
            {
                spawnManager = gameObject.AddComponent<EnemySpawnManager>();
            }

            if (GetComponent<HuntingZoneScenery>() == null)
            {
                gameObject.AddComponent<HuntingZoneScenery>();
            }
        }

        private void Start()
        {
            if (doors == null || doors.Length == 0)
            {
                doors = FindObjectsByType<MapDoor>();
            }

            if (rewardChest == null)
            {
                rewardChest = FindAnyObjectByType<RewardChest>();
            }

            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>();
            foreach (EnemyHealth enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    RegisterEnemy(enemy);
                }
            }

            SetDoorsLocked(false);
            rewardChest?.Unlock();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Transform safeSpawn = GameObject.Find("PlayerSpawnPoint")?.transform;
            if (player != null)
            {
                GameProgressManager.Instance?.SetSafeRespawn(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    safeSpawn != null ? safeSpawn.position : player.transform.position);
            }

            if (completionMessage != null)
            {
                completionMessage.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void NotifyEnemyDefeated(EnemyHealth enemy)
        {
            if (enemy != null)
            {
                aliveEnemies.Remove(enemy);
            }

        }

        public void RegisterEnemy(EnemyHealth enemy)
        {
            if (enemy != null && enemy.IsAlive)
            {
                aliveEnemies.Add(enemy);
                enemy.Died -= NotifyEnemyDefeated;
                enemy.Died += NotifyEnemyDefeated;
                spawnManager?.RegisterExisting(enemy);
            }
        }

        public bool CanLeaveMap()
        {
            return true;
        }

        private void SetDoorsLocked(bool locked)
        {
            foreach (MapDoor door in doors)
            {
                door?.SetLocked(locked);
            }
        }
    }
}
