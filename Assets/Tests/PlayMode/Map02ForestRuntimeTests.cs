#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Enemies;
using TopDownGame.Enemies.Progression;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class Map02ForestRuntimeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject probe, target;
        private EnemyData data;
        private Scene map, previous;
        private string originalProgress;
        private bool applied;
        private float oldTimeScale;

        [SetUp]
        public void Setup()
        {
            oldTimeScale = Time.timeScale;
            Time.timeScale = 1f;
        }

        private T CreateProbe<T>(EnemyArchetype archetype) where T : EnemyBase
        {
            data = ScriptableObject.CreateInstance<EnemyData>();
            typeof(EnemyData).GetField("archetype", Private).SetValue(data, archetype);
            probe = new GameObject("Map02CombatProbe");
            probe.transform.position = new Vector3(500, 500);
            probe.AddComponent<CircleCollider2D>().radius = 0.55f;
            T enemy = probe.AddComponent<T>();
            enemy.enabled = false;
            typeof(EnemyBase).GetField("data", Private).SetValue(enemy, data);
            probe.GetComponent<Rigidbody2D>().gravityScale = 0;
            probe.GetComponent<EnemyMovement>().enabled = false;
            probe.GetComponent<EnemySignatureAbility>().enabled = false;
            enemy.ResetForSpawn(probe.transform.position, 1, EnemyVariant.Normal);
            target = new GameObject("Map02TargetProbe");
            target.transform.position = probe.transform.position + Vector3.right;
            target.AddComponent<PlayerHealth>().enabled = false;
            typeof(EnemyBase).GetField("<Target>k__BackingField", Private).SetValue(enemy, target.transform);
            return enemy;
        }

        [UnityTest]
        public IEnumerator ArcherKilledDuringWindupCannotReleaseAnArrow()
        {
            var archer = CreateProbe<ForestArcherEnemy>(EnemyArchetype.Archer);
            typeof(EnemyHealth).GetField("deathDelay", Private).SetValue(probe.GetComponent<EnemyHealth>(), 2f);
            var routine = (IEnumerator)typeof(ForestArcherEnemy).GetMethod("AttackRoutine", Private).Invoke(archer, null);
            archer.StartCoroutine(routine);
            yield return new WaitForSeconds(0.1f);
            probe.GetComponent<EnemyHealth>().TakeDamage(new DamageInfo(100000f, target, probe.transform.position, Vector2.left, 0));
            yield return new WaitForSeconds(0.8f);
            Assert.That(Object.FindObjectsByType<EnemyProjectile>().Count(p =>
                p.gameObject.activeInHierarchy && p.transform.position.x > 400), Is.Zero,
                "A dead archer must not release its pending shot.");
        }

        [UnityTest]
        public IEnumerator WarningUsesThinGroundArcWithoutCrossingSpokes()
        {
            var warning = ForestAttackVisual.ShowRain(new Vector2(500,500), 1.45f, 0.95f);
            try
            {
                var lines = warning.GetComponentsInChildren<LineRenderer>();
                Assert.That(lines.Length, Is.EqualTo(1));
                var inner = lines.Single();
                Assert.That(inner.sortingOrder, Is.LessThan(5), "Warning stays under enemy and player sprites.");
                Assert.That(inner.widthMultiplier, Is.LessThanOrEqualTo(0.035f));
                Assert.That(inner.startColor.a, Is.InRange(0.3f,0.75f));
                for (int i = 0; i < inner.positionCount; i++)
                {
                    Assert.That(inner.GetPosition(i).magnitude, Is.EqualTo(1.45f).Within(0.001f));
                }
                Assert.That(warning.GetComponentsInChildren<Collider2D>(), Is.Empty);
                yield return null;
            }
            finally { if (warning != null) Object.Destroy(warning.gameObject); }
            var sector = ForestAttackVisual.ShowSector(new Vector2(500,500),Vector2.right,1.5f,0.4f);
            try
            {
                var line=sector.GetComponent<LineRenderer>();
                Assert.That(line.loop,Is.False);
                for(int i=0;i<line.positionCount;i++)
                    Assert.That(line.GetPosition(i).magnitude,Is.EqualTo(1.5f).Within(0.001f),"No spokes to centre.");
                yield return null;
            }
            finally { if(sector!=null)Object.Destroy(sector.gameObject); }
        }

        [UnityTest]
        public IEnumerator DodgingBehindGoblinDuringWindupAvoidsTheLockedSlash()
        {
            var goblin = CreateProbe<ForestGoblinEnemy>(EnemyArchetype.Goblin);
            var health = target.GetComponent<PlayerHealth>();
            float before = health.CurrentHealth;
            var routine = (IEnumerator)typeof(ForestGoblinEnemy).GetMethod("SlashRoutine", Private).Invoke(goblin, null);
            goblin.StartCoroutine(routine);
            yield return new WaitForSeconds(0.1f);
            target.transform.position = probe.transform.position + Vector3.left * 0.8f;
            yield return new WaitForSeconds(0.3f);
            Assert.That(health.CurrentHealth, Is.EqualTo(before));
            goblin.StopAllCoroutines();
            target.transform.position = probe.transform.position + Vector3.right * 0.8f;
            routine = (IEnumerator)typeof(ForestGoblinEnemy).GetMethod("SlashRoutine", Private).Invoke(goblin, null);
            goblin.StartCoroutine(routine);
            yield return new WaitForSeconds(0.35f);
            Assert.That(health.CurrentHealth, Is.LessThan(before), "Staying in the locked sector must still take damage.");
        }

        [UnityTest]
        public IEnumerator ArrowRainLocksItsLandingZonePausesAndCanBeDodged()
        {
            var archer = CreateProbe<ForestArcherEnemy>(EnemyArchetype.Archer);
            var health = target.GetComponent<PlayerHealth>();
            float before = health.CurrentHealth;
            var routine = (IEnumerator)typeof(ForestArcherEnemy).GetMethod("ArrowRainRoutine", Private).Invoke(archer, null);
            archer.StartCoroutine(routine);
            Vector2 locked = archer.LockedRainCenter;
            yield return new WaitForSeconds(0.4f);
            Assert.That(health.CurrentHealth, Is.EqualTo(before), "Warning is harmless.");
            var visual = Object.FindObjectsByType<ForestAttackVisual>().Single(v => v.name == "Forest Arrow Rain");
            float progress = visual.Progress;
            Time.timeScale = 0;
            yield return null; yield return null;
            Assert.That(visual.Progress, Is.EqualTo(progress));
            Time.timeScale = 1;
            target.transform.position += Vector3.right * 3f;
            yield return new WaitForSeconds(0.7f);
            Assert.That(archer.LockedRainCenter, Is.EqualTo(locked));
            Assert.That(health.CurrentHealth, Is.EqualTo(before), "Escape the locked circle to dodge rain.");
            Assert.That(archer.ArrowRainCasts, Is.EqualTo(1));
            archer.StopAllCoroutines();
            routine = (IEnumerator)typeof(ForestArcherEnemy).GetMethod("ArrowRainRoutine", Private).Invoke(archer, null);
            archer.StartCoroutine(routine);
            yield return new WaitForSeconds(1.1f);
            Assert.That(health.CurrentHealth, Is.LessThan(before), "Staying in the warning must take exactly the authored impact.");
        }

        [UnityTest]
        public IEnumerator DyingCancelsRainAndRespawnResetsTacticalCounters()
        {
            var archer = CreateProbe<ForestArcherEnemy>(EnemyArchetype.Archer);
            typeof(EnemyHealth).GetField("deathDelay", Private).SetValue(probe.GetComponent<EnemyHealth>(), 3f);
            var routine = (IEnumerator)typeof(ForestArcherEnemy).GetMethod("ArrowRainRoutine", Private).Invoke(archer, null);
            archer.StartCoroutine(routine);
            float before = target.GetComponent<PlayerHealth>().CurrentHealth;
            yield return new WaitForSeconds(0.2f);
            probe.GetComponent<EnemyHealth>().TakeDamage(new DamageInfo(100000, target, probe.transform.position, Vector2.left, 0));
            yield return new WaitForSeconds(1.1f);
            Assert.That(target.GetComponent<PlayerHealth>().CurrentHealth, Is.EqualTo(before));
            Assert.That(archer.ArrowRainCasts, Is.Zero);
            Assert.That(Object.FindObjectsByType<ForestAttackVisual>().Any(v => v.name == "Forest Arrow Rain"), Is.False);
            archer.ResetForSpawn(new Vector2(500, 500), 1, EnemyVariant.Normal);
            Assert.That(archer.EvasiveSteps, Is.Zero);
            Assert.That(archer.ArrowRainCasts, Is.Zero);
            Assert.That(archer.CurrentForestState, Is.EqualTo(ForestArcherCombatState.Idle));
        }

        [UnityTest]
        public IEnumerator GoblinAmbushTravelsAndArcherEvadesUsingPhysics()
        {
            var goblin = CreateProbe<ForestGoblinEnemy>(EnemyArchetype.Goblin);
            probe.GetComponent<EnemyMovement>().enabled = true;
            target.transform.position = probe.transform.position + Vector3.right * 2.6f;
            Vector2 origin = probe.transform.position;
            var routine = (IEnumerator)typeof(ForestGoblinEnemy).GetMethod("LeafDashRoutine", Private).Invoke(goblin, null);
            goblin.StartCoroutine(routine);
            yield return new WaitForSeconds(0.4f);
            Assert.That(Vector2.Distance(probe.transform.position, origin), Is.LessThan(0.1f));
            yield return new WaitForSeconds(0.45f);
            Assert.That(Vector2.Distance(probe.transform.position, origin), Is.GreaterThan(1.3f));
            Assert.That(goblin.LeafDashCasts, Is.EqualTo(1));
            Object.Destroy(probe); Object.Destroy(target); Object.Destroy(data);
            yield return null;
            var archer = CreateProbe<ForestArcherEnemy>(EnemyArchetype.Archer);
            probe.GetComponent<EnemyMovement>().enabled = true;
            float before = Vector2.Distance(probe.transform.position, target.transform.position);
            routine = (IEnumerator)typeof(ForestArcherEnemy).GetMethod("EvasiveStepRoutine", Private).Invoke(archer, null);
            archer.StartCoroutine(routine);
            yield return new WaitForSeconds(0.5f);
            Assert.That(archer.EvasiveSteps, Is.EqualTo(1));
            Assert.That(Vector2.Distance(probe.transform.position, target.transform.position), Is.GreaterThan(before + 0.6f));
        }

        [UnityTest]
        public IEnumerator GeneratedForestCanBeWalkedToRewardAndExitAndCaptured()
        {
            Assert.That((bool)typeof(GameProgressManager).GetProperty("IsAutomatedTestRun",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.True);
            if (GameProgressManager.Instance == null)
                new GameObject("Map02TestProgress").AddComponent<GameProgressManager>();
            var progress = GameProgressManager.Instance;
            originalProgress = JsonUtility.ToJson(progress);
            applied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private).GetValue(progress);
            typeof(GameProgressManager).GetField("hasSafeRespawn", Private).SetValue(progress, false);
            previous = SceneManager.GetActiveScene();
            yield return SceneManager.LoadSceneAsync("Map02", LoadSceneMode.Additive);
            map = SceneManager.GetSceneByName("Map02");
            SceneManager.SetActiveScene(map);
            yield return null;
            var roots = map.GetRootGameObjects();
            var school = roots.Select(r=>r.GetComponent<ForestFishSchool>()).Single(f=>f!=null);
            Assert.That(school.FishCount,Is.EqualTo(20));
            Assert.That(school.PondFishCount,Is.EqualTo(8));
            Assert.That(school.RiverFishCount,Is.EqualTo(12));
            Assert.That(school.FishStayInWater,Is.True);
            Assert.That(school.transform.Find("ForestFish_Visuals").GetComponentsInChildren<Collider2D>(),Is.Empty);
            float fishTime=school.AnimationTime;
            Time.timeScale=0;
            yield return null; yield return null;
            Assert.That(school.AnimationTime,Is.EqualTo(fishTime));
            Time.timeScale=1;
            var player = roots.Single(r => r.CompareTag("Player"));
            player.GetComponent<PlayerCombat>().enabled = false;
            var movement = player.GetComponent<PlayerMovement>();
            var spawner = roots.Select(r => r.GetComponent<EnemySpawnManager>()).First(s => s != null);
            Assert.That(spawner.ActiveCount, Is.InRange(12, 16));
            Assert.That(spawner.TargetAliveCount, Is.EqualTo(16));
            Assert.That(spawner.SpawnPoints.Count, Is.EqualTo(12), "Forest respawns must stay on validated anchors.");
            foreach (var enemy in roots.SelectMany(r => r.GetComponentsInChildren<EnemyBase>()))
            {
                Assert.That(enemy.Data, Is.Not.Null);
                Assert.That(enemy.GetComponent<EnemyHealth>().IsAlive, Is.True);
                enemy.enabled = false;
                enemy.GetComponent<EnemySignatureAbility>().enabled = false;
                enemy.GetComponent<EnemyMovement>().ResetMotion();
                enemy.GetComponent<EnemyMovement>().enabled = false;
            }
            spawner.enabled = false;
            // Verify actual movement against real collision, with combat paused separately.
            foreach (var door in roots.SelectMany(r => r.GetComponentsInChildren<MapDoor>())) door.enabled = false;
            foreach (var point in new[] { ArtPoint(349, 440), ArtPoint(410, 680),
                ArtPoint(640, 535), ArtPoint(715, 268), ArtPoint(972, 400),
                ArtPoint(1178, 220), ArtPoint(1320, 790) })
            {
                Assert.That(movement.TrySetClickDestination(point), Is.True, "No route to " + point);
                float deadline = Time.time + 20f;
                while (Vector2.Distance(player.transform.position, point) > 0.15f && Time.time < deadline)
                    yield return null;
                Assert.That(Vector2.Distance(player.transform.position, point), Is.LessThan(0.4f), "Blocked route to " + point);
                if (point == ArtPoint(1178, 220))
                {
                    // Chests are solid scenery: stand beside the chest, not inside it.
                    var chest = roots.Select(r => r.GetComponent<RewardChest>()).First(c => c != null);
                    yield return null;
                    Assert.That(player.GetComponent<PlayerInteraction>().HasTarget, Is.True,
                        "Chest approach: " + player.transform.position + " chest=" + chest.transform.position);
                    Assert.That(chest.CanInteract, Is.True);
                }
            }
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).First();
            camera.GetComponent<CameraFollow2D>().enabled = false;
            camera.transform.position = new Vector3(6, 2, -10);
            camera.orthographicSize = 5.2f;
            player.GetComponent<Rigidbody2D>().position = new Vector2(3.3f, 1.1f);
            var canvas = roots.SelectMany(r => r.GetComponentsInChildren<Canvas>()).First();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.sortingOrder = 1000;
            yield return null;
            var render = new RenderTexture(1920, 1080, 24);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            float oldCaptureDelta = Time.captureDeltaTime;
            try
            {
                camera.targetTexture = render;
                yield return null;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = render;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texture.Apply();
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/Map02_V4_Runtime_Verified.png", texture.EncodeToPNG());
                // Controlled two-enemy encounter, running the real tactical AI.
                // This also records motion instead of treating a still as proof.
                var goblin = roots.SelectMany(r => r.GetComponentsInChildren<ForestGoblinEnemy>()).OrderBy(e => Vector2.Distance(e.transform.position, player.transform.position)).First();
                var archer = roots.SelectMany(r => r.GetComponentsInChildren<ForestArcherEnemy>()).OrderBy(e => Vector2.Distance(e.transform.position, new Vector2(6, 3))).First();
                goblin.ResetForSpawn(new Vector2(6, 1.1f), 8, EnemyVariant.Normal);
                archer.ResetForSpawn(new Vector2(6.2f, 3.6f), 8, EnemyVariant.Normal);
                goblin.GetComponent<EnemyMovement>().enabled = true;
                archer.GetComponent<EnemyMovement>().enabled = true;
                goblin.enabled = archer.enabled = true;
                typeof(ForestGoblinEnemy).GetField("nextAmbushTime", Private).SetValue(goblin, 0f);
                typeof(ForestArcherEnemy).GetField("nextRainTime", Private).SetValue(archer, 0f);
                typeof(EnemyBase).GetProperty("NextAttackTime", Private).SetValue(goblin, 0f);
                typeof(EnemyBase).GetProperty("NextAttackTime", Private).SetValue(archer, 0f);
                Vector2 goblinStart = goblin.transform.position;
                float largestTravel = 0;
                Directory.CreateDirectory("Captures/Map02V4");
                Time.captureDeltaTime = 1f / 12f;
                for (int frame = 0; frame < 96; frame++)
                {
                    if (frame == 7) movement.TrySetClickDestination(new Vector2(1.8f, 0.1f));
                    if (frame == 40) movement.TrySetClickDestination(new Vector2(4.2f, 1.4f));
                    if (frame == 70) movement.TrySetClickDestination(new Vector2(3f, 1f));
                    yield return null;
                    largestTravel = Mathf.Max(largestTravel, Vector2.Distance(goblin.transform.position, goblinStart));
                    Assert.That(school.FishStayInWater,Is.True,"Fish silhouette crossed its water habitat.");
                    camera.Render();
                    RenderTexture.active = render;
                    texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes("Captures/Map02V4/Frame_" + frame.ToString("000") + ".jpg", texture.EncodeToJPG(85));
                    if (frame == 5 || frame == 10 || frame == 30)
                        File.WriteAllBytes("Assets/Screenshots/Map02_V4_Combat_" + frame + ".png", texture.EncodeToPNG());
                }
                Assert.That(largestTravel, Is.GreaterThan(1.2f), "Goblin must flank and chase in the real map.");
                Assert.That(goblin.LeafDashCasts, Is.GreaterThan(0));
                Assert.That(archer.ArrowRainCasts, Is.GreaterThan(0));
                Assert.That(player.GetComponent<PlayerHealth>().IsAlive, Is.True);
                Assert.That(school.AnimationTime,Is.GreaterThan(fishTime+5f));
                canvas.enabled=false;
                foreach(var view in new[] {("HoCa",new Vector3(11.7f,1,-10)),("SongCa",new Vector3(0,-8.5f,-10))})
                {
                    camera.transform.position=view.Item2;
                    camera.orthographicSize=3.5f;
                    yield return null;
                    camera.Render(); RenderTexture.active=render;
                    texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                    File.WriteAllBytes("Assets/Screenshots/Map02_V6_"+view.Item1+"_Verified.png",texture.EncodeToPNG());
                }
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                render.Release();
                Object.Destroy(render);
                Object.Destroy(texture);
                Time.captureDeltaTime = oldCaptureDelta;
            }
        }

        private static Vector2 ArtPoint(float x, float y) => new Vector2(x * 36f / 1536f - 18f, 12f - y * 24f / 1024f);

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = oldTimeScale;
            if (probe != null) Object.Destroy(probe);
            if (target != null) Object.Destroy(target);
            if (data != null) Object.Destroy(data);
            foreach (var effect in Object.FindObjectsByType<EnemyCombatVfx>())
                if (effect.transform.position.x > 400) Object.Destroy(effect.gameObject);
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>())
                if (projectile.transform.position.x > 400) Object.Destroy(projectile.gameObject);
            foreach (var effect in Object.FindObjectsByType<ForestAttackVisual>())
                if (effect.transform.position.x > 400) Object.Destroy(effect.gameObject);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (map.IsValid() && map.isLoaded) yield return SceneManager.UnloadSceneAsync(map);
            if (originalProgress != null && GameProgressManager.Instance != null)
            {
                JsonUtility.FromJsonOverwrite(originalProgress, GameProgressManager.Instance);
                typeof(GameProgressManager).GetField("playerProgressApplied", Private)
                    .SetValue(GameProgressManager.Instance, applied);
            }
            yield return null;
        }
    }
}
#endif
