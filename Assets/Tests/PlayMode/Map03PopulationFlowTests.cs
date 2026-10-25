using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Progression;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Items;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Skills;
using TopDownGame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Plays the generated skill temple as a hunting zone, from safe spawning and loot to its reward and boss exit.</summary>
    public sealed class Map03PopulationFlowTests
    {
        private const string Key = "TopDownGame.Tests.Progress.v1";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Dictionary<string, (bool exists, string value)> records = new Dictionary<string, (bool, string)>();
        private string state;
        private bool applied, pickup;
        private bool? attack;
        private ProgressLoadStatus status;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private GameObject Player => GameObject.FindWithTag("Player");
        private EnemySpawnManager Spawner => EnemySpawnManager.Instance;
        private static IEnumerator Settle() { yield return null; yield return null; yield return null; }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("Map03PopulationProbe").AddComponent<GameProgressManager>();
            state = JsonUtility.ToJson(Progress); status = Progress.LoadStatus;
            applied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private).GetValue(Progress);
            attack = PlayerAutomationState.AutoAttackOverride; pickup = PlayerAutomationState.AutoPickupEnabled;
            foreach (string suffix in new[] { "", ".Backup", ".Unreadable" })
                records[Key + suffix] = (PlayerPrefs.HasKey(Key + suffix), PlayerPrefs.GetString(Key + suffix, ""));
            Progress.BeginNewGame(); Progress.CompleteTutorial(); SceneTransition.ResetLoading();
            PlayerAutomationState.SetAutoAttackEnabled(false); PlayerAutomationState.SetAutoPickupEnabled(false);
            yield return SceneManager.LoadSceneAsync("Map03"); yield return Settle();
            Player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
        }

        private static EnemyBase[] Enemies() => Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include);
        private static void FreezeEnemies()
        {
            foreach (EnemyBase enemy in Enemies())
            {
                enemy.StopAllCoroutines(); enemy.enabled = false;
                enemy.GetComponent<EnemySignatureAbility>().enabled = false;
                var movement = enemy.GetComponent<EnemyMovement>(); movement.ResetMotion(); movement.enabled = false;
            }
        }
        private IEnumerator FillPopulation()
        {
            float deadline = Time.realtimeSinceStartup + 6;
            while (Spawner.ActiveCount < Spawner.TargetAliveCount && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Spawner.ActiveCount, Is.EqualTo(18), "The temple needs enough safe anchors to reach its authored target.");
        }
        private static bool TerrainClear(Vector2 point, float radius = 0.58f)
        {
            int mask = ~(1 << GameplayCollisionRules.PlayerLayer | 1 << GameplayCollisionRules.EnemyLayer);
            return !Physics2D.OverlapCircleAll(point, radius, mask).Any(c => c.enabled && !c.isTrigger);
        }

        [UnityTest]
        public IEnumerator AuthoredAnchorsAreReachableAndPopulationFillsItsBoundedPool()
        {
            Assert.That(Spawner.TargetAliveCount, Is.EqualTo(MapManager.Instance.Data.TargetActiveEnemies));
            Assert.That(Spawner.SpawnPoints.Count, Is.EqualTo(21));
            Assert.That(Enemies().Length, Is.EqualTo(26));
            foreach (var enemy in Enemies())
                Assert.That(enemy.GetComponentsInChildren<Canvas>(true).Count(c => c.name == "EnemyHealthBar"),
                    Is.EqualTo(1), enemy.name + " must not clone and rebuild the same health bar.");
            Assert.That(Enemies().Any(e => e.Data.Archetype == EnemyArchetype.Archer), Is.True);
            Assert.That(Enemies().Any(e => e.Data.Archetype == EnemyArchetype.Mage), Is.True);
            var blocked = new List<string>();
            foreach (Vector2 anchor in Spawner.SpawnPoints)
            {
                if (!TerrainClear(anchor)) blocked.Add("Blocked spawn anchor " + anchor);
                if (!Player.GetComponent<PlayerMovement>().TrySetClickDestination(anchor)) blocked.Add("Unreachable anchor " + anchor);
                Player.GetComponent<PlayerMovement>().CancelMovement();
            }
            Assert.That(blocked, Is.Empty, string.Join("; ", blocked));
            yield return FillPopulation(); FreezeEnemies();
            Assert.That(MapManager.Instance.RemainingEnemies, Is.EqualTo(Spawner.ActiveCount));
            foreach (var enemy in Enemies().Where(e => e.gameObject.activeInHierarchy))
            {
                Assert.That(enemy.GetComponent<EnemyHealth>().IsAlive, Is.True);
                Assert.That(enemy.Profile.Level, Is.InRange(18, 35));
                var label = enemy.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "EnemyLevelLabel");
                label.ForceMeshUpdate();
                Assert.That(label.text, Is.EqualTo("Cấp " + enemy.Profile.Level + "  " + enemy.Profile.DisplayName));
                Assert.That(label.isTextOverflowing, Is.False, label.text);
                Assert.That(TerrainClear(enemy.GetComponent<PooledEnemy>().HomePosition), Is.True);
            }
            Assert.That(Object.FindAnyObjectByType<MapDoor>().CanInteract, Is.True);
        }

        [UnityTest]
        public IEnumerator DefeatDropsOnceAndRespawnReusesPoolOutsideCameraAwayFromPlayer()
        {
            yield return FillPopulation(); FreezeEnemies();
            int poolSize = Enemies().Length;
            var victim = Enemies().First(e => e.gameObject.activeInHierarchy &&
                Spawner.IsPointOutsideCamera(e.transform.position) && Vector2.Distance(e.transform.position, Player.transform.position) > 8);
            victim.ResetForSpawn(victim.GetComponent<PooledEnemy>().HomePosition, 18, EnemyVariant.Normal);
            var health = victim.GetComponent<EnemyHealth>();
            var stats = Player.GetComponent<PlayerStats>(); int gold = stats.Gold;
            var damage = new DamageInfo(1000000, Player, victim.transform.position, Vector2.right);
            health.TakeDamage(damage); health.TakeDamage(damage);
            Assert.That(Object.FindObjectsByType<EnemyLootPickup>().Length, Is.EqualTo(1));
            var loot = Object.FindAnyObjectByType<EnemyLootPickup>(); int reward = loot.Gold;
            Assert.That(stats.Gold, Is.EqualTo(gold));
            loot.Collect(Player); int level = stats.Level, xp = stats.Experience;
            loot.Collect(Player);
            Assert.That(stats.Gold, Is.EqualTo(gold + reward)); Assert.That(reward, Is.GreaterThan(0));
            Assert.That(stats.Level, Is.EqualTo(level)); Assert.That(stats.Experience, Is.EqualTo(xp));
            Assert.That(MapManager.Instance.RemainingEnemies, Is.EqualTo(17));
            var living = new HashSet<EnemyBase>(Enemies().Where(e => e.gameObject.activeInHierarchy && e.GetComponent<EnemyHealth>().IsAlive));
            int births = 0;
            float deadline = Time.realtimeSinceStartup + 7;
            while (Spawner.ActiveCount < 18 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                foreach (var enemy in Enemies().Where(e => e.gameObject.activeInHierarchy && e.GetComponent<EnemyHealth>().IsAlive))
                    if (living.Add(enemy))
                    {
                        births++;
                        var label = enemy.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "EnemyLevelLabel");
                        Assert.That(label.text, Is.EqualTo("Cấp " + enemy.Profile.Level + "  " + enemy.Profile.DisplayName));
                        Assert.That(Spawner.IsPointOutsideCamera(enemy.transform.position), Is.True);
                        Assert.That(Vector2.Distance(enemy.transform.position, Player.transform.position), Is.GreaterThanOrEqualTo(Spawner.MinimumPlayerDistance));
                    }
            }
            Assert.That(Spawner.ActiveCount, Is.EqualTo(18));
            Assert.That(births, Is.GreaterThan(0));
            // A reserve may fill first; every newly active reserve and the recycled corpse
            // must use the same bounded pool and authored safe spawn policy.
            Assert.That(Enemies().Length, Is.EqualTo(poolSize));
            foreach (var enemy in Enemies().Where(e => e.gameObject.activeInHierarchy))
            {
                Vector2 home = enemy.GetComponent<PooledEnemy>().HomePosition;
                Assert.That(Spawner.SpawnPoints.Any(p => Vector2.Distance(p, home) <= 0.301f), Is.True);
                Assert.That(TerrainClear(home), Is.True);
            }
            Assert.That(Object.FindAnyObjectByType<MapDoor>().CanInteract, Is.True);
            Progress.CaptureActivePlayer();
            Assert.That(Progress.Gold, Is.EqualTo(stats.Gold));
        }

        private IEnumerator Walk(Vector2 point)
        {
            var movement = Player.GetComponent<PlayerMovement>();
            Assert.That(movement.TrySetClickDestination(point), Is.True, "No route to " + point);
            float deadline = Time.realtimeSinceStartup + 20;
            while (Vector2.Distance(Player.GetComponent<Rigidbody2D>().position, point) > 0.18f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(Vector2.Distance(Player.GetComponent<Rigidbody2D>().position, point), Is.LessThan(0.4f), "Blocked route to " + point);
            movement.CancelMovement(); yield return Settle();
        }

        [UnityTest]
        public IEnumerator WalkFromArrivalToSkillChestAndBossExitWithoutClearingTheMap()
        {
            yield return FillPopulation(); FreezeEnemies(); Spawner.enabled = false;
            var skills = Player.GetComponent<SkillInventory>(); var stats = Player.GetComponent<PlayerStats>();
            int goldBefore = stats.Gold;
            foreach (Vector2 point in new[] { new Vector2(-10,-3), new Vector2(0,0), new Vector2(-6.3f,8) }) yield return Walk(point);
            var chest = Object.FindAnyObjectByType<RewardChest>(); Assert.That(chest.CanInteract, Is.True);
            chest.Interact(Player.GetComponent<PlayerInteraction>());
            var rewardUI = Object.FindAnyObjectByType<SkillSelectionUI>(); Assert.That(rewardUI.IsOpen, Is.True);
            Button choice = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
            string id = choice.name.Substring("Reward_".Length); int previousLevel = skills.GetLevel(id);
            choice.onClick.Invoke(); choice.onClick.Invoke();
            Assert.That(skills.GetLevel(id), Is.EqualTo(previousLevel + 1));
            Assert.That(rewardUI.IsOpen, Is.False); Assert.That(chest.CanInteract, Is.False);
            Assert.That(stats.Gold, Is.GreaterThan(goldBefore));
            int gold = stats.Gold, level = stats.Level, xp = stats.Experience;
            var door = Object.FindAnyObjectByType<MapDoor>();
            yield return Walk((Vector2)door.transform.position + Vector2.left * 0.8f);
            Assert.That(MapManager.Instance.RemainingEnemies, Is.GreaterThan(0));
            Assert.That(door.CanInteract, Is.True); Assert.That(Progress.IsRegionUnlocked("BossMap01"), Is.False);
            door.Interact(Player.GetComponent<PlayerInteraction>()); door.Interact(Player.GetComponent<PlayerInteraction>());
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().name != "BossMap01" && Time.realtimeSinceStartup < deadline) yield return null;
            yield return Settle();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("BossMap01"));
            Assert.That(Progress.IsRegionUnlocked("BossMap01"), Is.True);
            Assert.That(Player.GetComponent<PlayerStats>().Gold, Is.EqualTo(gold));
            Assert.That(Player.GetComponent<PlayerStats>().Level, Is.EqualTo(level));
            Assert.That(Player.GetComponent<PlayerStats>().Experience, Is.EqualTo(xp));
            Assert.That(Player.GetComponent<SkillInventory>().GetLevel(id), Is.EqualTo(previousLevel + 1));
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator HuntingSkillRewardAndBossGuidanceRenderAtThreeRatios()
        {
            yield return FillPopulation(); FreezeEnemies(); Spawner.enabled = false;
            var camera = Camera.main; camera.GetComponent<CameraFollow2D>().enabled = false;
            Player.GetComponent<Rigidbody2D>().position = new Vector2(-1.5f,-3); Physics2D.SyncTransforms();
            camera.transform.position = new Vector3(0,0,-10); yield return Settle();
            yield return Render("GiaoTranh", GameObject.Find("CombatHUDRoot").transform);
            var chest = Object.FindAnyObjectByType<RewardChest>();
            Player.GetComponent<Rigidbody2D>().position = chest.transform.position + Vector3.left * 1.3f; Physics2D.SyncTransforms();
            camera.transform.position = chest.transform.position + Vector3.back * 10; yield return Settle();
            chest.Interact(Player.GetComponent<PlayerInteraction>());
            yield return Render("RuongKyNang", GameObject.Find("SkillSelectionPanel").transform);
            EventSystem.current.currentSelectedGameObject.GetComponent<Button>().onClick.Invoke();
            var door = Object.FindAnyObjectByType<MapDoor>();
            Player.GetComponent<Rigidbody2D>().position = door.transform.position + Vector3.left * 0.8f; Physics2D.SyncTransforms();
            camera.transform.position = new Vector3(10.5f,6f,-10); yield return Settle();
            Assert.That(Player.GetComponent<PlayerInteraction>().HasTarget, Is.True);
            yield return Render("CuaBoss", GameObject.Find("CombatHUDRoot").transform);
        }

        private IEnumerator Render(string stateName, Transform panel)
        {
            Canvas canvas = GameObject.Find("CombatHUDRoot").GetComponentInParent<Canvas>();
            Camera camera = Camera.main;
            RenderMode mode = canvas.renderMode; Camera oldCamera = canvas.worldCamera; int sorting = canvas.sortingOrder;
            RenderTexture target = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 30000;
                foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                {
                    target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target;
                    yield return null; yield return null; Canvas.ForceUpdateCanvases();
                    var bounds = (RectTransform)canvas.transform;
                    foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
                        float scale = text.transform.TransformVector(Vector3.right).magnitude / canvas.transform.TransformVector(Vector3.right).magnitude;
                        // Retain the existing HUD 12px gate; the skill reward modal uses 14px.
                        float minimumFont = panel.name == "CombatHUDRoot" ? 12 : 14;
                        if (!string.IsNullOrWhiteSpace(text.text))
                            Assert.That(text.fontSize * scale * size.x / bounds.rect.width, Is.GreaterThanOrEqualTo(minimumFont), text.name);
                    }
                    foreach (Button button in panel.GetComponentsInChildren<Button>())
                    {
                        var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                        foreach (Vector3 point in corners) Assert.That(bounds.rect.Contains(bounds.InverseTransformPoint(point)), Is.True, button.name);
                    }
                    camera.Render(); RenderTexture previous = RenderTexture.active;
                    var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target; texture.ReadPixels(new Rect(0,0,size.x,size.y),0,0); texture.Apply();
                        Directory.CreateDirectory("Assets/Screenshots");
                        File.WriteAllBytes("Assets/Screenshots/Map03SanQuai_" + stateName + "_" + size.x + "x" + size.y + "_Verified.png", texture.EncodeToPNG());
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(texture); }
                    camera.targetTexture = null; target.Release(); Object.Destroy(target); target = null;
                }
            }
            finally
            {
                camera.targetTexture = null; canvas.renderMode = mode; canvas.worldCamera = oldCamera; canvas.sortingOrder = sorting;
                if (target != null) { target.Release(); Object.Destroy(target); }
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            yield return null; yield return null;
            Scene scene = SceneManager.GetActiveScene(); Scene clean = SceneManager.CreateScene("Map03PopulationClean");
            SceneManager.SetActiveScene(clean); yield return SceneManager.UnloadSceneAsync(scene);
            SceneTransition.ResetLoading();
            if (Progress != null && state != null)
            {
                JsonUtility.FromJsonOverwrite(state, Progress);
                typeof(GameProgressManager).GetField("playerProgressApplied", Private).SetValue(Progress, applied);
                typeof(GameProgressManager).GetField("loadStatus", Private).SetValue(Progress, status);
            }
            typeof(PlayerAutomationState).GetMethod("ResetSession", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            if (attack.HasValue) PlayerAutomationState.SetAutoAttackEnabled(attack.Value);
            PlayerAutomationState.SetAutoPickupEnabled(pickup);
            foreach (var record in records)
                if (record.Value.exists) PlayerPrefs.SetString(record.Key, record.Value.value); else PlayerPrefs.DeleteKey(record.Key);
        }
    }
}
