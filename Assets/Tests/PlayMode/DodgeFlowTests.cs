using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TopDownGame.Combat;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Skills;
using TopDownGame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Exercises generated dodge input, collision and modal lifetimes against isolated player progress.</summary>
    public sealed class DodgeFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string original, originalState;
        private bool existed;
        private PlayerDodge dodge;
        private PlayerMovement movement;
        private PlayerHealth health;
        private PlayerStats stats;
        private PlayerCharacterAnimator animator;
        private PlayerCombat combat;
        private Rigidbody2D body;
        private GameObject modalOwner;
        private GameProgressManager Progress => GameProgressManager.Instance;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("DodgeProgressProbe").AddComponent<GameProgressManager>();
            original = PlayerPrefs.GetString(SaveKey, "");
            existed = PlayerPrefs.HasKey(SaveKey);
            originalState = JsonUtility.ToJson(Progress);
            Progress.BeginNewGame();
            Progress.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            yield return null;
            FindPlayer();
            modalOwner = new GameObject("DodgeModalProbe");
        }

        private void FindPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            dodge = player.GetComponent<PlayerDodge>();
            movement = player.GetComponent<PlayerMovement>();
            health = player.GetComponent<PlayerHealth>();
            stats = player.GetComponent<PlayerStats>();
            animator = player.GetComponent<PlayerCharacterAnimator>();
            combat = player.GetComponent<PlayerCombat>();
            body = player.GetComponent<Rigidbody2D>();
            combat.SetAutoAttackEnabled(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Assert.That(dodge.HasRequiredReferences, Is.True);
        }

        private void Arena()
        {
            movement.CancelMovement();
            body.position = new Vector2(1000,1000);
            body.transform.position = body.position;
            Physics2D.SyncTransforms();
        }

        private static DamageInfo Hit(float amount) => new DamageInfo(amount, null, Vector2.zero, Vector2.right, 0, false, true);

        [UnityTest]
        public IEnumerator DodgeTravelsEightDirectionsAndStandingUsesLastFacingWithoutTeleporting()
        {
            foreach (Vector2 direction in new[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down,
                new Vector2(1,1), new Vector2(-1,1), new Vector2(-1,-1), new Vector2(1,-1) })
            {
                Arena();
                yield return null;
                Vector2 start = body.position;
                Assert.That(dodge.TryDodge(direction), Is.True);
                Assert.That(body.position, Is.EqualTo(start), "Movement must be deferred to the physics step.");
                Assert.That(dodge.Direction, Is.EqualTo(direction.normalized));
                Assert.That(dodge.TryDodge(-direction), Is.False);
                yield return new WaitForSeconds(dodge.Duration + 0.06f);
                Vector2 displacement = body.position - start;
                Assert.That(displacement.magnitude, Is.InRange(1.7f,2.4f));
                Assert.That(Vector2.Angle(displacement,direction), Is.LessThan(1f));
                Assert.That(dodge.IsDodging, Is.False);
                yield return new WaitForSeconds(dodge.CooldownRemaining + 0.03f);
            }
            Vector2 facing = animator.FacingDirection;
            Assert.That(dodge.TryDodge(Vector2.zero), Is.True);
            Assert.That(dodge.Direction, Is.EqualTo(facing));
        }

        [UnityTest]
        public IEnumerator DodgeStopsAtThinWallsAndSlidesAlongThemWithoutDisablingColliders()
        {
            Arena();
            var wall = new GameObject("DodgeThinWall");
            wall.transform.position = new Vector2(1001,1000);
            var obstacle = wall.AddComponent<BoxCollider2D>();
            obstacle.size = new Vector2(0.06f,10f);
            Physics2D.SyncTransforms();
            try
            {
                Assert.That(dodge.TryDodge(Vector2.one), Is.True);
                yield return new WaitForSeconds(0.34f);
                Collider2D playerCollider = body.GetComponent<Collider2D>();
                Assert.That(playerCollider.enabled, Is.True);
                Assert.That(body.position.x + playerCollider.bounds.extents.x, Is.LessThanOrEqualTo(obstacle.bounds.min.x - 0.005f));
                Assert.That(body.position.y, Is.GreaterThan(1001f), "The unblocked axis should continue along the wall.");
                Assert.That(body.position.x, Is.GreaterThan(1000.2f));
            }
            finally { Object.Destroy(wall); }
        }

        [UnityTest]
        public IEnumerator OnlyTheDodgeWindowBlocksDirectDamageAndHitImmunityRemainsIndependent()
        {
            Arena();
            float hp = health.CurrentHealth;
            Assert.That(dodge.TryDodge(Vector2.right), Is.True);
            ((IDamageable)health).TakeDamage(Hit(10));
            Assert.That(health.CurrentHealth, Is.EqualTo(hp));
            Assert.That(health.IsDodgeInvulnerable, Is.True);
            Assert.That(health.TakeStatusDamage(3), Is.EqualTo(3), "Dodge does not cleanse existing poison.");
            yield return new WaitForSeconds(dodge.InvulnerabilityDuration + 0.02f);
            Assert.That(dodge.IsDodging, Is.True, "The ending of a dodge remains vulnerable.");
            Assert.That(health.IsDodgeInvulnerable, Is.False);
            ((IDamageable)health).TakeDamage(Hit(10));
            Assert.That(health.CurrentHealth, Is.EqualTo(hp - 13));
            Assert.That(dodge.IsDodging, Is.False, "Taking a hit interrupts the vulnerable tail.");
            dodge.Cancel();
            ((IDamageable)health).TakeDamage(Hit(10));
            Assert.That(health.CurrentHealth, Is.EqualTo(hp - 13), "Ending dodge immunity must not clear post-hit immunity.");
            yield return new WaitForSeconds(0.38f);
            ((IDamageable)health).TakeDamage(Hit(10));
            Assert.That(health.CurrentHealth, Is.EqualTo(hp - 23));
        }

        [UnityTest]
        public IEnumerator PauseFreezesMotionCooldownAndImmunityAndDisableRestoresThePose()
        {
            Arena();
            Transform visual = movement.VisualRenderer.transform;
            Vector3 scale = visual.localScale;
            Quaternion rotation = visual.localRotation;
            Assert.That(dodge.TryDodge(Vector2.right), Is.True);
            yield return new WaitForSeconds(0.06f);
            var bag = Object.FindAnyObjectByType<InventoryUI>();
            bag.Open();
            yield return null;
            float remaining = dodge.CooldownRemaining;
            Vector2 position = body.position;
            Assert.That(Time.timeScale, Is.Zero);
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(dodge.CooldownRemaining, Is.EqualTo(remaining).Within(0.001f));
            Assert.That(body.position, Is.EqualTo(position));
            Assert.That(health.IsDodgeInvulnerable, Is.True);
            Assert.That(dodge.TryDodge(Vector2.up), Is.False);
            bag.Close();
            Assert.That(Time.timeScale, Is.EqualTo(1));
            dodge.enabled = false;
            Assert.That(dodge.IsDodging, Is.False);
            Assert.That(health.IsDodgeInvulnerable, Is.False);
            Assert.That(movement.enabled, Is.True);
            Assert.That(visual.localScale, Is.EqualTo(scale));
            Assert.That(Quaternion.Angle(visual.localRotation,rotation), Is.LessThan(0.01f));
            Assert.That(dodge.TryDodge(Vector2.right), Is.False);
            dodge.enabled = true;
            Assert.That(dodge.TryDodge(Vector2.right), Is.False, "Disabling cannot reset cooldown.");
            yield return new WaitForSeconds(dodge.CooldownRemaining + 0.03f);
            Assert.That(dodge.TryDodge(Vector2.left), Is.True);
            health.TakeStatusDamage(health.CurrentHealth + 1);
            Assert.That(health.IsAlive, Is.False);
            Assert.That(dodge.IsDodging, Is.False);
            Assert.That(movement.enabled, Is.False);
            Assert.That(dodge.TryDodge(Vector2.up), Is.False);
        }

        [UnityTest]
        public IEnumerator KeyboardDodgeWinsSimultaneousCombatAndHoldingSpaceDoesNotRepeat()
        {
            Arena();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                float mana = stats.CurrentMana;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W,Key.D,Key.Space,Key.J,Key.Q));
                yield return null;
                yield return null;
                Assert.That(dodge.IsDodging, Is.True);
                Assert.That(dodge.Direction, Is.EqualTo(Vector2.one.normalized));
                Assert.That(combat.IsAttacking, Is.False);
                Assert.That(combat.CanRunAutoCombat, Is.False);
                Assert.That(body.GetComponent<SkillManager>().IsCasting, Is.False);
                Assert.That(stats.CurrentMana, Is.EqualTo(mana));
                yield return new WaitForSeconds(1.1f);
                Assert.That(dodge.IsDodging, Is.False, "Holding Space does not automatically dodge again.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                yield return null;
                Assert.That(movement.TrySetClickDestination(body.position + Vector2.right), Is.True);
                Vector2 start = body.position;
                yield return new WaitForSeconds(0.5f);
                Assert.That(body.position.x, Is.GreaterThan(start.x + 0.8f), "Click movement still works after dodge.");
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator GamepadSouthUsesItsPairedMoveActionAndLetsFocusedHudOwnSubmit()
        {
            Arena();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                body.GetComponent<PlayerInput>().SwitchCurrentControlScheme("Gamepad",gamepad);
                InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(-1,1) }.WithButton(GamepadButton.South));
                yield return null;
                yield return null;
                Assert.That(dodge.IsDodging, Is.True);
                Assert.That(Vector2.Angle(dodge.Direction,new Vector2(-1,1)), Is.LessThan(1));
                yield return new WaitForSeconds(1.1f);
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                yield return null;
                Button launcher = GameObject.Find("InventoryLauncher").GetComponent<Button>();
                launcher.Select();
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
                yield return null;
                yield return null;
                Assert.That(dodge.IsDodging, Is.False, "UI Submit owns South when a HUD button has focus.");
                Assert.That(Object.FindAnyObjectByType<InventoryUI>().IsOpen, Is.True);
            }
            finally { InputSystem.RemoveDevice(gamepad); }
        }

        [UnityTest]
        public IEnumerator AttackCastingModalAndTextFocusRejectDodgeWithoutSpendingItsCooldown()
        {
            Arena();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.J));
                yield return null;
                yield return null;
                Assert.That(combat.IsAttacking, Is.True);
                Assert.That(dodge.TryDodge(Vector2.up), Is.False);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                yield return new WaitForSeconds(0.5f);
                var manager = body.GetComponent<SkillManager>();
                Assert.That(manager.TryCastFromGesture(0,Vector2.right), Is.True);
                Assert.That(dodge.TryDodge(Vector2.up), Is.False);
                yield return new WaitForSeconds(0.8f);
                GameplayState.EnterModal(modalOwner);
                Assert.That(dodge.TryDodge(Vector2.up), Is.False);
                GameplayState.ExitModal(modalOwner);
                Assert.That(dodge.TryDodge(Vector2.up), Is.False, "Closing a modal cannot leak input into a dodge in the same frame.");
                yield return null;
                var input = new GameObject("DodgeTextInput",typeof(RectTransform),typeof(TMP_InputField));
                input.transform.SetParent(Object.FindAnyObjectByType<InventoryUI>().GetComponentInParent<Canvas>().transform,false);
                try
                {
                    EventSystem.current.SetSelectedGameObject(input);
                    Assert.That(dodge.TryDodge(Vector2.up), Is.False);
                    Assert.That(dodge.CooldownRemaining, Is.Zero);
                    EventSystem.current.SetSelectedGameObject(null);
                    Assert.That(dodge.TryDodge(Vector2.up), Is.True);
                }
                finally { Object.Destroy(input); }
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator DodgeHudRendersReadyActiveAndCooldownInLobbyAndCombatAtCommonAspectRatios()
        {
            foreach (string scene in new[] { "Lobby", "Map01" })
            {
                if (scene != SceneManager.GetActiveScene().name)
                {
                    yield return SceneManager.LoadSceneAsync(scene);
                    yield return null;
                    yield return null;
                    FindPlayer();
                    modalOwner = new GameObject("DodgeRenderModal");
                    // The first map frames may have accepted auto-cast before this fixture disables auto combat.
                    // Observe its real timeline instead of bypassing the dodge/cast gate to take a screenshot.
                    float deadline = Time.time + 3f;
                    while ((body.GetComponent<SkillManager>().IsCasting || combat.IsAttacking || movement.IsMovementLocked) && Time.time < deadline)
                        yield return null;
                    Assert.That(body.GetComponent<SkillManager>().IsCasting || combat.IsAttacking || movement.IsMovementLocked, Is.False);
                }
                Canvas canvas = Object.FindAnyObjectByType<DodgeHUD>().GetComponentInParent<Canvas>();
                Camera camera = Camera.main;
                RenderMode oldMode = canvas.renderMode;
                Camera oldCamera = canvas.worldCamera;
                int oldSorting = canvas.sortingOrder;
                RenderTexture target = null;
                try
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 1;
                    canvas.sortingOrder = 30000;
                    foreach (string state in new[] { "SanSang", "DangNe", "HoiChieu" })
                    {
                        if (state == "DangNe")
                        {
                            Assert.That(dodge.TryDodge(Vector2.right), Is.True,
                                scene + ": cooldown=" + dodge.CooldownRemaining + ", locked=" + movement.IsMovementLocked +
                                ", enabled=" + movement.enabled + ", modal=" + GameplayState.IsInputBlocked +
                                ", closeFrame=" + GameplayState.ModalClosedThisFrame + ", attacking=" + combat.IsAttacking +
                                ", casting=" + body.GetComponent<SkillManager>().IsCasting);
                            yield return new WaitForSeconds(0.08f);
                        }
                        if (state == "HoiChieu") dodge.Cancel();
                        GameplayState.EnterModal(modalOwner);
                        foreach (Vector2Int size in new[] { new Vector2Int(1920,1080),new Vector2Int(1280,800),new Vector2Int(1024,768) })
                        {
                            target = new RenderTexture(size.x,size.y,24);
                            camera.targetTexture = target;
                            yield return null;
                            yield return null;
                            Transform card = canvas.transform.Find("DodgeCard");
                            foreach (TMP_Text text in card.GetComponentsInChildren<TMP_Text>())
                            {
                                text.ForceMeshUpdate();
                                Assert.That(text.isTextOverflowing, Is.False, text.name);
                            }
                            string label = card.Find("DodgeStatus").GetComponent<TMP_Text>().text;
                            Assert.That(label, Does.Contain(state == "SanSang" ? "SẴN SÀNG" : state == "DangNe" ? "ĐANG NÉ" : "HỒI:"));
                            var corners = new Vector3[4];
                            ((RectTransform)card).GetWorldCorners(corners);
                            foreach (Vector3 point in corners)
                            {
                                Vector3 local = canvas.transform.InverseTransformPoint(point);
                                Assert.That(((RectTransform)canvas.transform).rect.Contains(local), Is.True);
                            }
                            Capture(camera,target,"NeDon_" + scene + "_" + state + "_" + size.x + "x" + size.y);
                            camera.targetTexture = null; target.Release(); Object.Destroy(target); target = null;
                        }
                        GameplayState.ExitModal(modalOwner);
                        yield return null;
                    }
                }
                finally
                {
                    GameplayState.ExitModal(modalOwner);
                    canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.sortingOrder = oldSorting;
                    camera.targetTexture = null;
                    if (target != null) { target.Release(); Object.Destroy(target); }
                }
            }
        }

        private static void Capture(Camera camera, RenderTexture target, string name)
        {
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                texture.Apply();
                File.WriteAllBytes("Assets/Screenshots/" + name + "_Verified.png",texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(texture); }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (modalOwner != null) GameplayState.ExitModal(modalOwner);
            Scene scene = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("DodgeClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if (originalState != null) JsonUtility.FromJsonOverwrite(originalState,Progress);
            if (existed) PlayerPrefs.SetString(SaveKey,original); else PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
