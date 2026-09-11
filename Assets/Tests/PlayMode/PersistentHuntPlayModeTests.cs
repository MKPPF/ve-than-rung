using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Combat;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Equipment;
using TopDownGame.Items;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class HuntTestEnemy : EnemyBase
    {
        protected override void Tick(float targetDistance)
        {
            PatrolOrReturn();
        }
    }

    public sealed class PersistentHuntPlayModeTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance |
            BindingFlags.NonPublic;
        private string originalProgress;
        private string originalSave;
        private bool hadSave;
        private bool originalApplied;
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";

        [UnitySetUp]
        public IEnumerator SaveTestContext()
        {
            Assert.That(typeof(GameProgressManager).GetProperty("ActiveSaveKey",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.EqualTo(SaveKey));
            if (GameProgressManager.Instance == null)
                new GameObject(nameof(GameProgressManager)).AddComponent<GameProgressManager>();
            originalProgress = JsonUtility.ToJson(GameProgressManager.Instance);
            originalApplied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", PrivateInstance).GetValue(GameProgressManager.Instance);
            hadSave = PlayerPrefs.HasKey(SaveKey);
            originalSave = PlayerPrefs.GetString(SaveKey, "");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreTestContext()
        {
            foreach (HuntTestEnemy enemy in Object.FindObjectsByType<HuntTestEnemy>(FindObjectsInactive.Include))
                Object.Destroy(enemy.gameObject);
            foreach (string name in new[] { "HuntMapManagerProbe", "HuntPlayerProbe", "HuntCameraProbe",
                "DuplicateLootPlayerProbe", "DuplicateLootEnemyProbe", "PassThroughPlayerProbe", "PassThroughEnemyProbe" })
            {
                GameObject probe = GameObject.Find(name);
                if (probe != null) Object.Destroy(probe);
            }
            yield return null;
            if (originalProgress != null && GameProgressManager.Instance != null)
            {
                JsonUtility.FromJsonOverwrite(originalProgress, GameProgressManager.Instance);
                typeof(GameProgressManager).GetField("playerProgressApplied", PrivateInstance).SetValue(GameProgressManager.Instance, originalApplied);
                if (hadSave) PlayerPrefs.SetString(SaveKey, originalSave);
                else PlayerPrefs.DeleteKey(SaveKey);
            }
        }

        [UnityTest]
        public IEnumerator PlayerAndEnemyBodiesPassThroughEachOther()
        {
            GameplayCollisionRules.Apply();
            GameObject player = new GameObject("PassThroughPlayerProbe");
            player.layer = GameplayCollisionRules.PlayerLayer;
            CircleCollider2D playerCollider = player.AddComponent<CircleCollider2D>();
            Rigidbody2D playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.bodyType = RigidbodyType2D.Kinematic;
            playerBody.useFullKinematicContacts = true;

            GameObject enemy = new GameObject("PassThroughEnemyProbe");
            enemy.layer = GameplayCollisionRules.EnemyLayer;
            CircleCollider2D enemyCollider = enemy.AddComponent<CircleCollider2D>();
            Rigidbody2D enemyBody = enemy.AddComponent<Rigidbody2D>();
            enemyBody.gravityScale = 0f;
            player.transform.position = Vector3.zero;
            enemy.transform.position = Vector3.zero;

            yield return new WaitForFixedUpdate();
            Assert.That(Physics2D.GetIgnoreLayerCollision(player.layer, enemy.layer),
                Is.True);
            Assert.That(playerCollider.IsTouching(enemyCollider), Is.False,
                "Overlapping player and enemy bodies must not create a blocking contact.");

            Object.Destroy(player);
            Object.Destroy(enemy);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HunterEssenceEnhancementIsSpentAndPersisted()
        {
            yield return null;
            GameProgressManager progress = GameProgressManager.Instance;
            Assert.That(progress, Is.Not.Null);
            const string ItemId = "automated_enhancement_probe";
            progress.AddUniqueItem(ItemId);
            int currentLevel = progress.GetEnhancementLevel(ItemId);
            int expectedCost = GameProgressManager.EnhancementEssenceCost(currentLevel);
            int essenceBefore = progress.HunterEssence;
            progress.AddHunterEssence(expectedCost);

            Assert.That(progress.TryEnhanceEquipment(ItemId,
                currentLevel + 1, out int spent), Is.True);
            Assert.That(spent, Is.EqualTo(expectedCost));
            Assert.That(progress.GetEnhancementLevel(ItemId),
                Is.EqualTo(currentLevel + 1));
            Assert.That(progress.HunterEssence, Is.EqualTo(essenceBefore));
            Assert.That(progress.LoadProgress(), Is.True);
            Assert.That(progress.GetEnhancementLevel(ItemId),
                Is.EqualTo(currentLevel + 1),
                "Enhancement level must survive a save reload.");
        }

        [UnityTest]
        public IEnumerator DuplicateEquipmentDropBecomesHunterEssence()
        {
            yield return null;
            GameProgressManager progress = GameProgressManager.Instance;
            Assert.That(progress, Is.Not.Null);

            EquipmentData duplicate = ScriptableObject.CreateInstance<EquipmentData>();
            Set(duplicate, "itemId", "duplicate_loot_probe");
            Set(duplicate, "displayName", "Duplicate Loot Probe");
            Set(duplicate, "price", 650);
            progress.AddUniqueItem(duplicate.ItemId);

            GameObject player = new GameObject("DuplicateLootPlayerProbe");
            player.tag = "Player";
            PlayerStats stats = player.AddComponent<PlayerStats>();
            EquipmentInventory equipment = player.AddComponent<EquipmentInventory>();
            Set(equipment, "catalog", new[] { duplicate });

            EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
            Set(enemyData, "maxHealth", 1f);
            Set(enemyData, "goldReward", 1);
            Set(enemyData, "experienceReward", 1);
            Set(enemyData, "itemDropChance", 1f);
            GameObject enemy = new GameObject("DuplicateLootEnemyProbe");
            EnemyHealth health = enemy.AddComponent<EnemyHealth>();
            health.Initialize(enemyData);

            int essenceBefore = progress.HunterEssence;
            health.TakeDamage(new DamageInfo(999f, player,
                enemy.transform.position, Vector2.right));
            EnemyLootPickup pickup = Object.FindAnyObjectByType<EnemyLootPickup>();
            Assert.That(pickup, Is.Not.Null,
                "Defeated enemies must create a world loot pickup.");
            Assert.That(progress.HunterEssence, Is.EqualTo(essenceBefore),
                "Loot must not be granted before it is collected.");
            pickup.Collect(player);
            Assert.That(progress.HunterEssence - essenceBefore,
                Is.EqualTo(duplicate.DuplicateEssenceValue));

            Object.Destroy(enemy);
            Object.Destroy(player);
            Object.Destroy(enemyData);
            Object.Destroy(duplicate);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PooledEnemyRewardsOnceAndRespawnsSafely()
        {
            Time.timeScale = 1f;
            GameObject player = new GameObject("HuntPlayerProbe");
            player.tag = "Player";
            PlayerStats stats = player.AddComponent<PlayerStats>();
            player.transform.position = Vector3.zero;

            GameObject cameraObject = new GameObject("HuntCameraProbe");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            Set(data, "maxHealth", 12f);
            Set(data, "damage", 1f);
            Set(data, "moveSpeed", 0.2f);
            Set(data, "goldReward", 3);
            Set(data, "experienceReward", 9);

            GameObject enemyObject = new GameObject("PooledHuntEnemyProbe");
            enemyObject.transform.position = new Vector3(10f, 0f, 0f);
            enemyObject.AddComponent<SpriteRenderer>();
            enemyObject.AddComponent<CircleCollider2D>();
            Rigidbody2D body = enemyObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            EnemyMovement movement = enemyObject.AddComponent<EnemyMovement>();
            EnemyHealth health = enemyObject.AddComponent<EnemyHealth>();
            HuntTestEnemy controller = enemyObject.AddComponent<HuntTestEnemy>();
            typeof(EnemyBase).GetField("data", PrivateInstance)
                .SetValue(controller, data);

            MapData mapData = ScriptableObject.CreateInstance<MapData>();
            Set(mapData, "targetActiveEnemies", 1);
            Set(mapData, "recommendedMinLevel", 1);
            Set(mapData, "recommendedMaxLevel", 1);
            GameObject mapObject = new GameObject("HuntMapManagerProbe");
            MapManager map = mapObject.AddComponent<MapManager>();
            typeof(MapManager).GetField("mapData", PrivateInstance)
                .SetValue(map, mapData);
            EnemySpawnManager spawn = mapObject.GetComponent<EnemySpawnManager>();
            Set(spawn, "reservePoolSize", 1);
            Set(spawn, "evaluationInterval", 0.04f);
            Set(spawn, "minimumPlayerDistance", 2f);
            Set(spawn, "normalRespawnSeconds", new Vector2(0.18f, 0.24f));

            yield return null;
            yield return null;
            EnemyHealth[] beforePool = Object.FindObjectsByType<EnemyHealth>(
                FindObjectsInactive.Include);
            int poolSize = beforePool.Length;
            int goldBefore = stats.Gold;
            int experienceBefore = stats.Experience;
            health.TakeDamage(new DamageInfo(999f, player,
                enemyObject.transform.position, Vector2.right));
            EnemyLootPickup rewardPickup = Object.FindAnyObjectByType<EnemyLootPickup>();
            Assert.That(rewardPickup, Is.Not.Null);
            Assert.That(stats.Gold, Is.EqualTo(goldBefore));
            Assert.That(stats.Experience, Is.EqualTo(experienceBefore));
            rewardPickup.Collect(player);
            int goldDelta = stats.Gold - goldBefore;
            int experienceDelta = stats.Experience - experienceBefore;
            health.TakeDamage(new DamageInfo(999f, player,
                enemyObject.transform.position, Vector2.right));
            Assert.That(stats.Gold - goldBefore, Is.EqualTo(goldDelta));
            Assert.That(stats.Experience - experienceBefore,
                Is.EqualTo(experienceDelta));
            Assert.That(goldDelta, Is.GreaterThan(0));
            Assert.That(experienceDelta, Is.GreaterThan(0));
            Assert.That(map.CanLeaveMap(), Is.True,
                "A hunting zone must never require clearing all enemies.");

            yield return new WaitForSeconds(0.9f);
            EnemyHealth[] afterPool = Object.FindObjectsByType<EnemyHealth>(
                FindObjectsInactive.Include);
            Assert.That(afterPool.Length, Is.EqualTo(poolSize),
                "Respawn must reuse the prewarmed pool, not instantiate per death.");
            EnemyHealth respawned = null;
            foreach (EnemyHealth candidate in afterPool)
            {
                if (candidate.gameObject.activeInHierarchy && candidate.IsAlive)
                {
                    respawned = candidate;
                    break;
                }
            }
            Assert.That(respawned, Is.Not.Null);
            Assert.That(Vector2.Distance(respawned.transform.position,
                player.transform.position), Is.GreaterThanOrEqualTo(2f));
            Assert.That(spawn.IsPointOutsideCamera(respawned.transform.position),
                Is.True);

            PlayerCombat combat = player.AddComponent<PlayerCombat>();
            Set(combat, "attackRange", 20f);
            Set(combat, "autoSkillSearchRange", 20f);
            Set(combat, "attackCooldown", 0.05f);
            float healthBeforeAutoAttack = respawned.CurrentHealth;
            yield return new WaitForSeconds(0.35f);
            Assert.That(respawned.CurrentHealth, Is.LessThan(healthBeforeAutoAttack),
                "Auto Attack must reacquire pooled enemies after respawn.");

            Object.Destroy(mapObject);
            Object.Destroy(player);
            Object.Destroy(cameraObject);
            Object.Destroy(data);
            Object.Destroy(mapData);
            yield return null;
        }

        private static void Set(Object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                PrivateInstance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
