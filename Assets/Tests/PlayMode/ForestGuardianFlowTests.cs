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
using TopDownGame.Enemies.Bosses;
using TopDownGame.Enemies.Spawning;
using TopDownGame.Maps;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Exercises the authored boss's commitments and cancellation so its warnings remain a reliable promise to the player.</summary>
    public sealed class ForestGuardianFlowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Key = "TopDownGame.Tests.Progress.v1";
        private readonly Dictionary<string, (bool exists, string value)> records = new Dictionary<string, (bool, string)>();
        private string state;
        private ProgressLoadStatus status;
        private bool applied, pickup;
        private bool? attack;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private ForestGuardianBoss boss;
        private GameObject player;
        private PlayerHealth playerHealth;
        private EnemyHealth bossHealth;
        private GameObject pauseOwner;
        private GameObject detachedSource;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("BossFlowProgressProbe").AddComponent<GameProgressManager>();
            state = JsonUtility.ToJson(Progress); status = Progress.LoadStatus;
            applied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private).GetValue(Progress);
            attack = PlayerAutomationState.AutoAttackOverride; pickup = PlayerAutomationState.AutoPickupEnabled;
            foreach (string suffix in new[] { "", ".Backup", ".Unreadable" })
                records[Key + suffix] = (PlayerPrefs.HasKey(Key + suffix), PlayerPrefs.GetString(Key + suffix, ""));
            PlayerAutomationState.SetAutoAttackEnabled(false); PlayerAutomationState.SetAutoPickupEnabled(false);
            yield return LoadArena();
        }

        private IEnumerator LoadArena()
        {
            Progress.BeginNewGame(); Progress.CompleteTutorial(); SceneTransition.ResetLoading();
            yield return SceneManager.LoadSceneAsync("BossMap01"); yield return null; yield return null;
            player = GameObject.FindWithTag("Player"); playerHealth = player.GetComponent<PlayerHealth>();
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            player.GetComponent<PlayerMovement>().CancelMovement();
            boss = Object.FindAnyObjectByType<ForestGuardianBoss>(); bossHealth = boss.GetComponent<EnemyHealth>();
            boss.enabled = false; boss.StopAllCoroutines();
            boss.GetComponent<EnemySignatureAbility>().ResetForSpawn();
            typeof(EnemySignatureAbility).GetField("nextReadyTime", Private).SetValue(boss.GetComponent<EnemySignatureAbility>(), float.MaxValue);
            typeof(EnemyBase).GetProperty("NextAttackTime", Private).SetValue(boss, float.MaxValue);
            boss.GetComponent<EnemyMovement>().ResetMotion();
            MovePlayer((Vector2)boss.transform.position + Vector2.right);
            Camera.main.GetComponent<CameraFollow2D>().SetTarget(player.transform);
            yield return null;
        }

        private void MovePlayer(Vector2 position)
        {
            player.GetComponent<PlayerMovement>().CancelMovement();
            player.GetComponent<Rigidbody2D>().position = position;
            player.transform.position = position;
            Physics2D.SyncTransforms();
        }

        private void StartAction(string method)
        {
            boss.enabled = true;
            typeof(ForestGuardianBoss).GetField("acting", Private).SetValue(boss, true);
            typeof(EnemySignatureAbility).GetField("nextReadyTime", Private).SetValue(boss.GetComponent<EnemySignatureAbility>(), float.MaxValue);
            boss.StartCoroutine((IEnumerator)typeof(ForestGuardianBoss).GetMethod(method, Private).Invoke(boss, null));
        }

        private static int ActiveProjectiles() => Object.FindObjectsByType<EnemyProjectile>().Length;
        private static int ActiveEnemies() => Object.FindObjectsByType<EnemyBase>().Length;
        private static EnemyCombatVfx[] Warnings() => Object.FindObjectsByType<EnemyCombatVfx>().Where(v => v.name.Contains("Warning")).ToArray();

        private Vector2 PlaceChargeInOpenSpace()
        {
            Vector2 origin = new Vector2(1000, 1000);
            boss.GetComponent<EnemyMovement>().ResetMotion();
            boss.GetComponent<Rigidbody2D>().position = origin;
            boss.transform.position = origin;
            MovePlayer(origin + Vector2.right);
            return origin;
        }

        private IEnumerator WaitForChargeStart()
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!boss.GetComponent<EnemyMovement>().IsCharging && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(boss.GetComponent<EnemyMovement>().IsCharging, Is.True, "The healthy charge must enter its active movement window.");
        }

        private IEnumerator WaitForChargeEnd()
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (boss.GetComponent<EnemyMovement>().IsCharging && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(boss.GetComponent<EnemyMovement>().IsCharging, Is.False);
            yield return null;
        }

        private static BoxCollider2D ChargeWall(Vector2 position)
        {
            var wall = new GameObject("ChargeThinWall");
            wall.transform.position = position;
            var collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.06f, 10f);
            Physics2D.SyncTransforms();
            return collider;
        }

        [UnityTest]
        public IEnumerator DeathDuringEveryWindupCancelsDamageProjectilesSummonsAndKeepsDeadState()
        {
            foreach (string action in new[] { "SlamRoutine", "RadialProjectilesRoutine", "ChargeRoutine", "SummonRoutine", "PhaseChangeRoutine" })
            {
                yield return LoadArena();
                typeof(EnemyHealth).GetField("deathDelay", Private).SetValue(bossHealth, 2f);
                StartAction(action); yield return new WaitForSeconds(0.1f);
                Assert.That(Warnings(), Is.Not.Empty, action + " needs a visible warning before it can be cancelled.");
                float health = playerHealth.CurrentHealth;
                int projectiles = ActiveProjectiles(), enemies = ActiveEnemies();
                ((IDamageable)bossHealth).TakeDamage(new DamageInfo(1000000, player, boss.transform.position, Vector2.left, 0));
                yield return null;
                Assert.That(Warnings(), Is.Empty, action + " retained its cancelled warning.");
                yield return new WaitForSeconds(1.15f);
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(health), action + " damaged after death.");
                Assert.That(ActiveProjectiles(), Is.EqualTo(projectiles), action + " fired after death.");
                Assert.That(ActiveEnemies(), Is.EqualTo(enemies), action + " summoned after death.");
                Assert.That(boss.State, Is.EqualTo(BossState.Dead), action);
            }
        }

        [UnityTest]
        public IEnumerator DisablingDuringWindupCancelsThePendingSlamAndReenableCanStartAFreshAction()
        {
            boss.enabled = true;
            StartAction("SlamRoutine"); yield return new WaitForSeconds(0.1f);
            float before = playerHealth.CurrentHealth;
            boss.enabled = false;
            Assert.That(boss.GetComponent<EnemyCharacterAnimator>().CurrentActionPhase, Is.EqualTo(EnemyActionPhase.None));
            Assert.That(Warnings(), Is.Empty);
            yield return new WaitForSeconds(0.8f);
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(before), "Disabled boss finished an old action.");
            boss.enabled = true;
            StartAction("SlamRoutine"); yield return new WaitForSeconds(0.8f);
            Assert.That(playerHealth.CurrentHealth, Is.LessThan(before), "A fresh warning must be able to resolve after reenable.");
        }

        [UnityTest]
        public IEnumerator SlamHitsOnlyTheGroundWarnedBeforeBossDisplacement()
        {
            Vector2 warned = boss.transform.position;
            float initialPlayerX = player.GetComponent<Rigidbody2D>().position.x;
            StartAction("SlamRoutine"); yield return new WaitForSeconds(0.1f);
            boss.GetComponent<Rigidbody2D>().position = warned + Vector2.right * 4;
            Physics2D.SyncTransforms();
            float before = playerHealth.CurrentHealth;
            yield return new WaitForSeconds(0.7f);
            Assert.That(playerHealth.CurrentHealth, Is.LessThan(before), "The visible old circle must remain the damaging area.");
            Assert.That(player.GetComponent<Rigidbody2D>().position.x, Is.GreaterThan(initialPlayerX + 0.05f),
                "The slam must push away from its warned explosion center, not from the displaced caster.");

            yield return LoadArena(); warned = boss.transform.position;
            StartAction("SlamRoutine"); yield return new WaitForSeconds(0.1f);
            boss.GetComponent<Rigidbody2D>().position = warned + Vector2.right * 4;
            MovePlayer(warned + Vector2.right * 5);
            before = playerHealth.CurrentHealth;
            yield return new WaitForSeconds(0.7f);
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(before), "Displacement must not create an unannounced second danger zone.");
        }

        [UnityTest]
        public IEnumerator RadialPoseStaysInWindupUntilTheTenProjectilesAreReleased()
        {
            MovePlayer((Vector2)boss.transform.position + Vector2.right * 3);
            Vector2 warned = boss.transform.position;
            StartAction("RadialProjectilesRoutine");
            var animator = boss.GetComponent<EnemyCharacterAnimator>();
            yield return new WaitForSeconds(0.1f);
            boss.GetComponent<Rigidbody2D>().position = warned + Vector2.up * 4;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.3f);
            Assert.That(ActiveProjectiles(), Is.Zero);
            Assert.That(animator.CurrentActionPhase, Is.EqualTo(EnemyActionPhase.Windup));
            yield return new WaitForSeconds(0.12f);
            Assert.That(ActiveProjectiles(), Is.EqualTo(10));
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>())
                Assert.That(Vector2.Distance(projectile.transform.position, warned), Is.LessThan(1), "Shots must originate at the circle that was actually warned.");
            Assert.That(animator.CurrentActionPhase, Is.EqualTo(EnemyActionPhase.Active).Or.EqualTo(EnemyActionPhase.Recovery));
        }

        [UnityTest]
        public IEnumerator RealPatternKeepsItsRecoveryBeforeResumingChase()
        {
            StartAction("PerformPattern");
            yield return new WaitForSeconds(0.72f);
            Vector2 origin = boss.transform.position;
            MovePlayer(origin + Vector2.right * 4);
            yield return new WaitForSeconds(0.12f);
            Assert.That(boss.State, Is.EqualTo(BossState.Skill));
            Assert.That(boss.GetComponent<EnemyMovement>().Speed, Is.LessThan(0.01f));
            yield return new WaitForSeconds(0.3f);
            Assert.That(boss.State, Is.EqualTo(BossState.Chase));
            Assert.That(Vector2.Distance(boss.transform.position, origin), Is.GreaterThan(0.03f));
        }

        [UnityTest]
        public IEnumerator ChargeImpactNeverDamagesAnUnwarnedSidePosition()
        {
            Vector2 origin = boss.transform.position;
            StartAction("ChargeRoutine");
            yield return new WaitForSeconds(0.1f);
            Vector2 sidePosition = origin + new Vector2(1.8f, 1.6f);
            bool warned = Warnings().Any(w =>
            {
                if (w.name.Contains("Area"))
                    return Vector2.Distance(w.transform.position, sidePosition) <= w.transform.localScale.x * 0.5f;
                Vector3 local = w.transform.InverseTransformPoint(sidePosition);
                return Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.y) <= 0.5f;
            });
            MovePlayer(sidePosition);
            float before = playerHealth.CurrentHealth;
            yield return new WaitForSeconds(0.85f);
            Assert.That(playerHealth.CurrentHealth, Is.LessThan(before), "The side sample must actually be hit before checking the warning geometry.");
            Assert.That(warned, Is.True, "A player hit to the side of the route must have seen the full landing blast area before the charge.");
        }

        [UnityTest]
        public IEnumerator DisplacementDuringChargeWindupCannotRelocateItsExplosion()
        {
            // Isolate displacement from the authored arena's walls pushing either body away.
            Vector2 origin = PlaceChargeInOpenSpace();
            yield return null;
            StartAction("ChargeRoutine");
            yield return new WaitForSeconds(0.1f);
            boss.GetComponent<Rigidbody2D>().position = origin + Vector2.right * 4f;
            Physics2D.SyncTransforms();
            MovePlayer(origin + Vector2.right * 5.5f);
            float before = playerHealth.CurrentHealth;
            yield return new WaitForSeconds(0.85f);
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(before), "Displacing the caster cannot move the blast to ground outside its committed warning.");
        }

        [UnityTest]
        public IEnumerator ChargeStopsMovingWhenItsActiveWindowEnds()
        {
            Vector2 origin = boss.transform.position;
            StartAction("ChargeRoutine");
            MovePlayer(origin + Vector2.up * 4);
            yield return new WaitForSeconds(0.78f);
            Vector2 stopped = boss.GetComponent<Rigidbody2D>().position;
            Assert.That(Vector2.Distance(stopped, origin), Is.GreaterThan(1), "The healthy boss must actually charge before testing its stop.");
            Assert.That(boss.GetComponent<EnemyCharacterAnimator>().CurrentActionPhase, Is.EqualTo(EnemyActionPhase.Recovery));
            yield return new WaitForSeconds(0.1f);
            Assert.That(Vector2.Distance(boss.GetComponent<Rigidbody2D>().position, stopped), Is.LessThan(0.01f));
            Assert.That(boss.GetComponent<EnemyMovement>().Speed, Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator ChargeFollowsItsWarnedLandingWithSmoothStepsAndPauseKeepsItsWarning()
        {
            Vector2 origin = PlaceChargeInOpenSpace(); yield return null;
            StartAction("ChargeRoutine");
            var warnings = Warnings();
            var circle = warnings.Single(w => w.name.Contains("Area"));
            Vector2 landing = circle.transform.position;
            Assert.That(circle.transform.localScale.x, Is.EqualTo(boss.AttackRange * 2f).Within(0.001f));
            Assert.That(Vector2.Distance(origin, landing), Is.InRange(1.5f, 2.5f));
            MovePlayer(origin + Vector2.up * 5f);
            yield return WaitForChargeStart();
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Vector2 paused = boss.GetComponent<Rigidbody2D>().position;
            pauseOwner = new GameObject("ChargePauseProbe"); GameplayState.EnterModal(pauseOwner);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(boss.GetComponent<Rigidbody2D>().position, Is.EqualTo(paused));
            Assert.That(warnings.All(w => w.gameObject.activeSelf), Is.True);
            GameplayState.ExitModal(pauseOwner); Object.Destroy(pauseOwner); pauseOwner = null;

            Vector2 previous = paused;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (boss.GetComponent<EnemyMovement>().IsCharging && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
                Vector2 current = boss.GetComponent<Rigidbody2D>().position;
                Assert.That(current.x, Is.GreaterThanOrEqualTo(previous.x - 0.001f));
                Assert.That(current.y, Is.EqualTo(origin.y).Within(0.01f));
                Assert.That(Vector2.Distance(current, previous), Is.LessThan(0.5f), "Charge must travel through physics steps, not teleport to its marker.");
                previous = current;
            }
            Assert.That(boss.GetComponent<EnemyMovement>().IsCharging, Is.False);
            yield return null;
            Assert.That(Vector2.Distance(boss.GetComponent<Rigidbody2D>().position, landing), Is.LessThan(0.03f));
            Assert.That(Warnings(), Is.Empty);
            var independent = EnemyCombatVfx.TelegraphCircle(landing, 1f, 0.06f, Color.white);
            Assert.That(warnings.Any(w => w.gameObject == independent), Is.True, "Exercise reuse of a formerly held warning.");
            yield return new WaitForSeconds(0.12f);
            Assert.That(independent.activeSelf, Is.False, "Held lifetime cannot leak into an unrelated pooled effect.");
        }

        [UnityTest]
        public IEnumerator ExistingWallClampsBothTheWarnedLandingAndTheActualCharge()
        {
            Vector2 origin = PlaceChargeInOpenSpace(); yield return null;
            BoxCollider2D wall = ChargeWall(origin + Vector2.right * 1.8f);
            StartAction("ChargeRoutine");
            Vector2 landing = Warnings().Single(w => w.name.Contains("Area")).transform.position;
            float extent = boss.GetComponent<Collider2D>().bounds.extents.x;
            Assert.That(landing.x + extent, Is.LessThanOrEqualTo(wall.bounds.min.x - 0.02f));
            MovePlayer(origin + Vector2.up * 5f);
            yield return WaitForChargeStart(); yield return WaitForChargeEnd();
            Assert.That(Vector2.Distance(boss.GetComponent<Rigidbody2D>().position, landing), Is.LessThan(0.03f));
            Assert.That(boss.GetComponent<Collider2D>().bounds.max.x, Is.LessThanOrEqualTo(wall.bounds.min.x - 0.02f));
        }

        [UnityTest]
        public IEnumerator NewWallOnTheCommittedRouteStopsTheBossWithoutAnUnwarnedExplosion()
        {
            Vector2 origin = PlaceChargeInOpenSpace(); yield return null;
            StartAction("ChargeRoutine");
            Vector2 landing = Warnings().Single(w => w.name.Contains("Area")).transform.position;
            MovePlayer(landing + Vector2.up * 1.6f);
            float before = playerHealth.CurrentHealth;
            ChargeWall(origin + Vector2.right * 1.3f);
            yield return WaitForChargeStart(); yield return WaitForChargeEnd();
            Assert.That(boss.GetComponent<Rigidbody2D>().position.x, Is.LessThan(landing.x - 0.2f));
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(before), "A blocked charge must fizzle instead of moving its blast off the landing marker.");
            Assert.That(Warnings(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator KnockbackDuringActiveChargeInterruptsTheBlastAndKeepsTheIncomingImpulse()
        {
            Vector2 origin = PlaceChargeInOpenSpace(); yield return null;
            StartAction("ChargeRoutine");
            MovePlayer(origin + new Vector2(1.8f, 1.6f));
            float before = playerHealth.CurrentHealth;
            yield return WaitForChargeStart();
            ((IDamageable)bossHealth).TakeDamage(new DamageInfo(1f, player, boss.transform.position, Vector2.up, 4f));
            Assert.That(boss.GetComponent<EnemyMovement>().IsCharging, Is.False);
            yield return new WaitForSeconds(0.5f);
            Assert.That(playerHealth.CurrentHealth, Is.EqualTo(before));
            Assert.That(boss.GetComponent<Rigidbody2D>().position.y, Is.GreaterThan(origin.y + 0.1f), "Interrupting a charge must not erase the player's knockback.");
            Assert.That(Warnings(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator DeathOrDisableDuringActiveChargeCancelsMotionAndCannotReleaseTheOldBlast()
        {
            foreach (bool death in new[] { false, true })
            {
                yield return LoadArena();
                Vector2 origin = PlaceChargeInOpenSpace(); yield return null;
                typeof(EnemyHealth).GetField("deathDelay", Private).SetValue(bossHealth, 2f);
                StartAction("ChargeRoutine");
                MovePlayer(origin + new Vector2(1.8f, 1.6f));
                yield return WaitForChargeStart();
                yield return new WaitForFixedUpdate();
                float before = playerHealth.CurrentHealth;
                if (death) ((IDamageable)bossHealth).TakeDamage(new DamageInfo(1000000, player, boss.transform.position, Vector2.up));
                else boss.enabled = false;
                Vector2 stopped = boss.GetComponent<Rigidbody2D>().position;
                yield return new WaitForSeconds(0.5f);
                Assert.That(playerHealth.CurrentHealth, Is.EqualTo(before));
                Assert.That(boss.GetComponent<EnemyMovement>().IsCharging, Is.False);
                Assert.That(Vector2.Distance(boss.GetComponent<Rigidbody2D>().position, stopped), Is.LessThan(0.01f));
                Assert.That(Warnings(), Is.Empty);
                if (!death)
                {
                    StartAction("ChargeRoutine"); MovePlayer(origin + Vector2.up * 5f);
                    yield return WaitForChargeStart(); yield return WaitForChargeEnd();
                    Assert.That(Vector2.Distance(boss.transform.position, stopped), Is.GreaterThan(0.5f));
                }
                else Assert.That(boss.State, Is.EqualTo(BossState.Dead));
            }
        }

        [UnityTest]
        public IEnumerator HeldChargeWarningSurvivesImpactEffectPoolPressure()
        {
            Vector2 origin = PlaceChargeInOpenSpace(); yield return null;
            StartAction("ChargeRoutine");
            var held = Warnings();
            Assert.That(held.Length, Is.EqualTo(2));
            Vector2 landing = held.Single(w => w.name.Contains("Area")).transform.position;
            MovePlayer(origin + Vector2.up * 5f);
            for (int i = 0; i < 120; i++)
                EnemyCombatVfx.Burst(origin + Vector2.up * 8f, 0.2f, Color.white, 1f);
            Assert.That(held.All(w => w.gameObject.activeSelf && w.name.Contains("Warning")), Is.True,
                "Impact VFX cannot recycle an outstanding landing warning when the bounded pool fills.");
            Assert.That((Vector2)held.Single(w => w.name.Contains("Area")).transform.position, Is.EqualTo(landing));
            yield return WaitForChargeStart(); yield return WaitForChargeEnd();
            Assert.That(Warnings(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator ChargeWarningAndLandingRenderWithThePlayerCameraAtThreeRatios()
        {
            Vector2 origin = boss.transform.position;
            StartAction("ChargeRoutine");
            MovePlayer(origin + Vector2.down * 1.5f);
            yield return new WaitForSeconds(0.15f);
            pauseOwner = new GameObject("ChargeCapturePause"); GameplayState.EnterModal(pauseOwner);
            yield return Render("Lao_BaoDiemDap");
            GameplayState.ExitModal(pauseOwner);
            yield return WaitForChargeStart(); yield return WaitForChargeEnd();
            GameplayState.EnterModal(pauseOwner);
            yield return Render("Lao_DapDungVung");
            GameplayState.ExitModal(pauseOwner);
        }

        [UnityTest]
        public IEnumerator WarningKeepsItsFullBoundaryAndReusedEffectDoesNotRetainTheOldOwner()
        {
            StartAction("SlamRoutine"); yield return new WaitForSeconds(0.1f);
            var warning = Warnings().Single();
            float diameter = 2 * (float)typeof(ForestGuardianBoss).GetField("slamRadius", Private).GetValue(boss);
            Assert.That(warning.transform.localScale.x, Is.EqualTo(diameter).Within(0.001f));
            yield return new WaitForSeconds(0.25f);
            Assert.That(warning.transform.localScale.x, Is.EqualTo(diameter).Within(0.001f));
            boss.enabled = false;
            // The first warning is the only pooled VFX in this quiet arena.
            var independent = EnemyCombatVfx.TelegraphCircle(warning.transform.position, 1, 1, Color.white);
            Assert.That(independent, Is.SameAs(warning.gameObject));
            EnemyCombatVfx.CancelWarnings(boss);
            yield return null;
            Assert.That(independent.activeSelf, Is.True, "A reused effect must not be cancelled by the previous warning owner.");
        }

        [UnityTest]
        public IEnumerator ArenaUnloadRetiresItsReleasedProjectilesAndTheSamePoolCanBeReused()
        {
            MovePlayer((Vector2)boss.transform.position + Vector2.right * 3);
            StartAction("RadialProjectilesRoutine"); yield return new WaitForSeconds(0.52f);
            var released = Object.FindObjectsByType<EnemyProjectile>(); Assert.That(released.Length, Is.EqualTo(10));
            yield return SceneManager.LoadSceneAsync("Map03"); yield return null; yield return null;
            Assert.That(ActiveProjectiles(), Is.Zero, "Old arena shots must not hit the player in the destination map.");
            player = GameObject.FindWithTag("Player");
            var reused = EnemyProjectilePool.Acquire(null, player.transform.position + Vector3.right * 5, Vector2.right, Color.white, Vector3.one * 0.4f);
            reused.Launch(Vector2.right, 6, 5, player);
            Assert.That(released.Contains(reused), Is.True);
            Assert.That(reused.GetComponent<Rigidbody2D>().simulated, Is.True);
        }

        [UnityTest]
        public IEnumerator ReleasedProjectileAndHazardCanResolveAfterTheirSourceWasDestroyed()
        {
            detachedSource = new GameObject("ReleasedAttackSource");
            var shot = EnemyProjectilePool.Acquire(null, player.transform.position + Vector3.right * 5, Vector2.left, Color.white, Vector3.one * 0.4f);
            shot.Launch(Vector2.zero, 0, 10, detachedSource);
            Object.Destroy(detachedSource); yield return null;
            float before = playerHealth.CurrentHealth;
            Vector2 contact = player.GetComponent<Rigidbody2D>().position;
            shot.GetComponent<Rigidbody2D>().position = contact;
            shot.transform.position = contact;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.15f);
            Assert.That(playerHealth.CurrentHealth, Is.LessThan(before));
            Assert.That(shot.gameObject.activeSelf, Is.False);
            yield return new WaitForSeconds(0.4f);
            detachedSource = new GameObject("ReleasedHazardSource");
            EnemyHazardZone.Spawn(player.transform.position, 2, 1, 0.5f, 10, detachedSource, Color.green);
            Object.Destroy(detachedSource); yield return null;
            before = playerHealth.CurrentHealth;
            yield return new WaitForSeconds(0.2f);
            Assert.That(playerHealth.CurrentHealth, Is.LessThan(before));
        }

        [UnityTest]
        public IEnumerator DeathBetweenAncientQuakeHitsCannotCreateItsPendingEchoesOrHazard()
        {
            boss.enabled = true;
            var signature = boss.GetComponent<EnemySignatureAbility>();
            typeof(EnemySignatureAbility).GetField("target", Private).SetValue(signature, player.transform);
            bool firstHit = false;
            System.Action<float, float> defeatOnFirstHit = (current, maximum) =>
            {
                if (firstHit || current >= maximum) return;
                firstHit = true;
                ((IDamageable)bossHealth).TakeDamage(new DamageInfo(1000000, player, boss.transform.position, Vector2.left, 0));
            };
            playerHealth.HealthChanged += defeatOnFirstHit;
            try
            {
                // Use the real action wrapper so the boss cannot run its regular AI over this signature.
                signature.StartCoroutine((IEnumerator)typeof(EnemySignatureAbility).GetMethod("PerformRoutine", Private).Invoke(signature, null));
                Assert.That(signature.IsPerforming, Is.True);
                float deadline = Time.realtimeSinceStartup + 4f;
                while (!firstHit && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(firstHit, Is.True,
                    $"The primary impact must happen before testing cancellation of the pending echoes. Boss alive={bossHealth.IsAlive}, signature active={signature.IsPerforming}, player HP={playerHealth.CurrentHealth}.");
                // Wait from the observed primary hit, rather than from the start of two frame-quantized waits.
                yield return new WaitForSeconds(0.3f);
                Assert.That(boss.State, Is.EqualTo(BossState.Dead));
                Assert.That(EnemyHazardZone.ActiveCount, Is.Zero, "No new lingering hazard may appear after its caster died between impacts.");
            }
            finally { playerHealth.HealthChanged -= defeatOnFirstHit; }
        }

        [UnityTest]
        public IEnumerator SlamWarningAndCancelledDeathRenderWithThePlayerCameraAtThreeRatios()
        {
            MovePlayer((Vector2)boss.transform.position + Vector2.down * 1.5f);
            Camera.main.GetComponent<CameraFollow2D>().SetTarget(player.transform);
            StartAction("SlamRoutine"); yield return new WaitForSeconds(0.2f);
            pauseOwner = new GameObject("BossCapturePause"); GameplayState.EnterModal(pauseOwner);
            Assert.That(Warnings(), Is.Not.Empty);
            yield return Render("BaoDon");
            ((IDamageable)bossHealth).TakeDamage(new DamageInfo(1000000, player, boss.transform.position, Vector2.up, 0));
            yield return null;
            Assert.That(Warnings(), Is.Empty); Assert.That(boss.State, Is.EqualTo(BossState.Dead));
            yield return Render("HaBossHuyDon");
            GameplayState.ExitModal(pauseOwner);
        }

        private IEnumerator Render(string stateName)
        {
            var root = GameObject.Find("CombatHUDRoot").transform;
            Canvas canvas = root.GetComponentInParent<Canvas>(); Camera camera = Camera.main;
            RenderMode oldMode = canvas.renderMode; Camera oldCamera = canvas.worldCamera; int oldSorting = canvas.sortingOrder;
            RenderTexture target = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder = 30000;
                foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                {
                    target = new RenderTexture(size.x, size.y, 24); camera.targetTexture = target;
                    yield return null; yield return null; Canvas.ForceUpdateCanvases();
                    var bounds = (RectTransform)canvas.transform;
                    if (stateName == "Lao_BaoDiemDap")
                    {
                        var warning = Warnings().Single(w => w.name.Contains("Area"));
                        float radius = warning.transform.localScale.x * 0.5f;
                        Vector3 low = camera.WorldToScreenPoint(warning.transform.position - new Vector3(radius, radius));
                        Vector3 high = camera.WorldToScreenPoint(warning.transform.position + new Vector3(radius, radius));
                        Rect danger = Rect.MinMaxRect(low.x, low.y, high.x, high.y);
                        foreach (string utility in new[] { "CharacterDrawer", "InventoryLauncher", "PotentialLauncher", "SkillLauncher", "ForgeLauncher", "AutomationControls" })
                        {
                            var rect = (RectTransform)canvas.transform.Find(utility);
                            CanvasGroup group = rect.GetComponent<CanvasGroup>();
                            if (group != null && group.alpha == 0) continue;
                            var points = new Vector3[4]; rect.GetWorldCorners(points);
                            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, points[0]);
                            Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, points[2]);
                            Assert.That(danger.Overlaps(Rect.MinMaxRect(a.x, a.y, b.x, b.y)), Is.False,
                                utility + " covers the committed charge landing at " + size);
                        }
                    }
                    foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
                        float scale = text.transform.TransformVector(Vector3.right).magnitude / canvas.transform.TransformVector(Vector3.right).magnitude;
                        if (!string.IsNullOrWhiteSpace(text.text))
                            Assert.That(text.fontSize * scale * size.x / bounds.rect.width, Is.GreaterThanOrEqualTo(12), text.name);
                    }
                    foreach (Button button in root.GetComponentsInChildren<Button>())
                    {
                        var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                        foreach (Vector3 point in corners) Assert.That(bounds.rect.Contains(bounds.InverseTransformPoint(point)), Is.True, button.name);
                    }
                    camera.Render(); RenderTexture previous = RenderTexture.active;
                    var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target; texture.ReadPixels(new Rect(0,0,size.x,size.y),0,0); texture.Apply();
                        File.WriteAllBytes("Assets/Screenshots/VeThanRung_" + stateName + "_" + size.x + "x" + size.y + "_Verified.png", texture.EncodeToPNG());
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(texture); }
                    camera.targetTexture = null; target.Release(); Object.Destroy(target); target = null;
                }
            }
            finally
            {
                camera.targetTexture = null; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.sortingOrder = oldSorting;
                if (target != null) { target.Release(); Object.Destroy(target); }
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            GameplayState.CancelHitstop();
            if (pauseOwner != null) GameplayState.ExitModal(pauseOwner);
            if (detachedSource != null) Object.Destroy(detachedSource);
            Scene scene = SceneManager.GetActiveScene(); Scene clean = SceneManager.CreateScene("BossFlowClean");
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
