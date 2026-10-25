using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Save;
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
    /// <summary>Verifies actual launcher callbacks, modal focus and physical readability against generated scenes with isolated progress.</summary>
    public sealed class HudUtilityFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private string original, originalState;
        private bool existed;
        private bool? originalAttack;
        private bool originalPickup;
        private Canvas canvas;
        private GameObject owner;
        private GestureInputReceiver savedReceiver;
        private bool savedGestureEnabled;
        private int savedGesturePort;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private static readonly string[] Paths = { "InventoryLauncher", "PotentialLauncher", "SkillLauncher", "ForgeLauncher",
            "AutomationControls/AutoAttackToggle", "AutomationControls/AutoPickupToggle", "AutomationControls/GestureToggle" };
        private Button Button(string path) => canvas.transform.Find(path).GetComponent<Button>();
        private IEnumerator OpenDrawer()
        {
            if (!Object.FindAnyObjectByType<HudUtilityLayout>().IsExpanded)
                ExecuteEvents.Execute(Button("CharacterDrawer/Toggle").gameObject, new PointerEventData(EventSystem.current)
                    { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(Object.FindAnyObjectByType<HudUtilityLayout>().IsExpanded, Is.True);
        }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("HudUtilityProgressProbe").AddComponent<GameProgressManager>();
            original = PlayerPrefs.GetString(SaveKey, "");
            existed = PlayerPrefs.HasKey(SaveKey);
            originalState = JsonUtility.ToJson(Progress);
            originalAttack = PlayerAutomationState.AutoAttackOverride;
            originalPickup = PlayerAutomationState.AutoPickupEnabled;
            Progress.BeginNewGame();
            Progress.CompleteTutorial();
            yield return Load("Lobby");
            savedReceiver = Object.FindAnyObjectByType<GestureInputReceiver>();
            savedGestureEnabled = savedReceiver.ControlEnabled;
            savedGesturePort = (int)typeof(GestureInputReceiver).GetField("port", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(savedReceiver);
            savedReceiver.SetControlEnabled(false);
        }

        private IEnumerator Load(string scene)
        {
            if (owner != null) GameplayState.ExitModal(owner);
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            yield return null;
            canvas = Object.FindAnyObjectByType<HudUtilityLayout>().GetComponentInParent<Canvas>();
            GameObject.FindWithTag("Player").GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            owner = new GameObject("HudUtilityModalProbe");
        }

        [UnityTest]
        public IEnumerator LauncherMouseSubmitAndModalReturnKeepCallbacksCountsAndNavigation()
        {
            yield return OpenDrawer();
            PlayerStats stats = GameObject.FindWithTag("Player").GetComponent<PlayerStats>();
            stats.AddExperience(stats.ExperienceToNextLevel * 3);
            Progress.AddHunterEssence(123);
            yield return null;
            Assert.That(Button("PotentialLauncher").GetComponentInChildren<TMP_Text>().text, Does.Contain("+"));
            Assert.That(Button("ForgeLauncher").GetComponentInChildren<TMP_Text>().text, Does.Contain("CƯỜNG HÓA").And.Contain("123"));
            foreach (string path in Paths.Take(4))
            {
                TMP_Text label = Button(path).GetComponentInChildren<TMP_Text>();
                label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, label.text);
            }
            for (int i = 0; i < 4; i++)
            {
                Button launcher = Button(Paths[i]);
                launcher.Select();
                if (i % 2 == 0)
                    ExecuteEvents.Execute(launcher.gameObject, new PointerEventData(EventSystem.current)
                        { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                else ExecuteEvents.Execute(launcher.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                yield return null;
                Assert.That(GameplayState.IsInputBlocked, Is.True, Paths[i]);
                Assert.That(Time.timeScale, Is.EqualTo(0));
                Assert.That(Button("AutomationControls/AutoPickupToggle").IsInteractable(), Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.EqualTo(launcher.gameObject));
                // Real close methods must return the previous selection after suspension.
                if (i == 0) Object.FindAnyObjectByType<InventoryUI>().Close();
                if (i == 1) Object.FindAnyObjectByType<PotentialUpgradeUI>().Close();
                if (i == 2) Object.FindAnyObjectByType<SkillEquipmentUI>().Close();
                if (i == 3) Object.FindAnyObjectByType<EquipmentEnhancementUI>().Close();
                yield return null;
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(launcher.gameObject));
                Assert.That(launcher.IsInteractable(), Is.True);
            }
            Button first = Button(Paths[0]);
            EventSystem.current.SetSelectedGameObject(null);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
                yield return null; yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Button("CharacterDrawer/Toggle").gameObject));
                first.Select();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                Vector3 position = stats.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow));
                yield return new WaitForSecondsRealtime(0.12f);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Button(Paths[1]).gameObject));
                Assert.That(Vector3.Distance(position, stats.transform.position), Is.LessThan(0.01f), "HUD arrows must not also walk the player.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                first.Select();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                yield return new WaitForSecondsRealtime(0.12f);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Button(Paths[1]).gameObject));
                Assert.That(Vector3.Distance(position, stats.transform.position), Is.LessThan(0.01f), "HUD WASD bindings must not also walk the player.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return null; yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Button(Paths[2]).gameObject));
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                yield return null;
                Button pickup = Button(Paths[5]);
                pickup.Select();
                bool previousPickup = PlayerAutomationState.AutoPickupEnabled;
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
                yield return null; yield return null;
                Assert.That(PlayerAutomationState.AutoPickupEnabled, Is.EqualTo(!previousPickup));
                Assert.That(stats.GetComponent<PlayerDodge>().IsDodging, Is.False, "HUD Submit must not also dodge.");
                Assert.That(stats.GetComponent<PlayerDodge>().CooldownRemaining, Is.Zero);
                InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.Select));
                yield return null; yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null, "View releases HUD selection.");
                Assert.That(GameplayState.IsInputBlocked, Is.False, "Navigation must not leave a blocked world frame behind.");
                InputSystem.QueueStateEvent(gamepad, new GamepadState());
                position = stats.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.DownArrow));
                yield return new WaitForSecondsRealtime(0.12f);
                Assert.That(Vector3.Distance(position, stats.transform.position), Is.GreaterThan(0.05f), "Arrows resume world movement after leaving HUD.");
            }
            finally { InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad); }
            yield return OpenDrawer();
            for (int i = 0; i < Paths.Length - 1; i++)
                Assert.That(Button(Paths[i]).FindSelectableOnDown(), Is.EqualTo(Button(Paths[i + 1])));
        }

        [UnityTest]
        public IEnumerator DrawerCancelShortcutsAndHiddenRaycastsDoNotLeakWorldActions()
        {
            var layout = Object.FindAnyObjectByType<HudUtilityLayout>();
            Assert.That(layout.IsExpanded, Is.False);
            var hidden = (RectTransform)Button(Paths[5]).transform;
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, hidden.TransformPoint(hidden.rect.center))
            }, hits);
            Assert.That(hits.Any(h => h.gameObject == hidden.gameObject || h.gameObject.transform.IsChildOf(hidden)), Is.False);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.I));
                yield return null; yield return null;
                Assert.That(Object.FindAnyObjectByType<InventoryUI>().IsOpen, Is.True, "Inventory shortcut works from the compact HUD.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                Object.FindAnyObjectByType<InventoryUI>().Close(); yield return null;
                Assert.That(layout.IsExpanded, Is.False);
                Assert.That(Button(Paths[0]).IsInteractable(), Is.False, "Closing a shortcut panel must not expose invisible launchers.");
                yield return OpenDrawer();
                Button("CharacterDrawer/Toggle").Select();
                ExecuteEvents.Execute(Button("CharacterDrawer/Toggle").gameObject, new PointerEventData(EventSystem.current)
                    { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null, "Mouse collapse releases navigation so WASD can resume.");
                yield return OpenDrawer();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null; yield return null;
                Assert.That(layout.IsExpanded, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1), "Escape closes the drawer without also pausing.");
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Select));
                yield return null; yield return null;
                Assert.That(layout.IsExpanded, Is.True);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Button("CharacterDrawer/Toggle").gameObject));
                InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
                yield return null; yield return null;
                Assert.That(layout.IsExpanded, Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(pad); }
        }

        [UnityTest]
        public IEnumerator AutomationLabelsAndGestureHintsRemainIndependentDuringPause()
        {
            yield return OpenDrawer();
            var combat = GameObject.FindWithTag("Player").GetComponent<PlayerCombat>();
            Button attack = Button(Paths[4]), pickup = Button(Paths[5]);
            bool pickupBefore = PlayerAutomationState.AutoPickupEnabled;
            attack.onClick.Invoke();
            Assert.That(combat.AutoAttackEnabled, Is.True);
            Assert.That(PlayerAutomationState.AutoPickupEnabled, Is.EqualTo(pickupBefore));
            pickup.onClick.Invoke();
            Assert.That(PlayerAutomationState.AutoPickupEnabled, Is.EqualTo(!pickupBefore));
            GameplayState.EnterModal(owner);
            yield return null;
            attack.onClick.Invoke(); pickup.onClick.Invoke();
            Assert.That(combat.AutoAttackEnabled, Is.True);
            Assert.That(PlayerAutomationState.AutoPickupEnabled, Is.EqualTo(!pickupBefore));
            GameplayState.ExitModal(owner);
            pickup.onClick.Invoke();
            combat.SetAutoAttackEnabled(false);
            var receiver = Object.FindAnyObjectByType<GestureInputReceiver>();
            Assert.That(receiver, Is.Not.Null);
            Assert.That(receiver.ControlEnabled, Is.False);
            Assert.That(receiver.StatusText, Does.Contain("đang tắt"));
            yield return null;
            Assert.That(canvas.transform.Find("AutomationControls/GestureStatus").GetComponent<TMP_Text>().text, Is.EqualTo(receiver.StatusText));
        }

        [UnityTest]
        public IEnumerator GestureWaitingUsesReadableHudWithoutDuplicatingLegacyOverlay()
        {
            GestureInputReceiver receiver = Object.FindAnyObjectByType<GestureInputReceiver>();
            Assert.That(receiver, Is.Not.Null);
            var portField = typeof(GestureInputReceiver).GetField("port", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            int originalPort = (int)portField.GetValue(receiver);
            int testPort;
            using (var probe = new System.Net.Sockets.UdpClient(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0)))
                testPort = ((System.Net.IPEndPoint)probe.Client.LocalEndPoint).Port;
            try
            {
                portField.SetValue(receiver, testPort);
                receiver.SetControlEnabled(true);
                Assert.That(receiver.ControlEnabled, Is.True);
                Assert.That(receiver.HasStatusDisplay, Is.True);
                Assert.That(receiver.HasRecentPacket, Is.False);
                yield return null; yield return null;
                Assert.That(receiver.StatusText, Does.Contain("Chờ camera").And.Contain("mục 8"));
                Assert.That(Button(Paths[6]).GetComponentInChildren<TMP_Text>().text, Does.Contain("BẬT"));
                yield return Render("Lobby_CuChiBat_ThuGon");
                Assert.That(canvas.transform.Find("CharacterDrawer/GestureSummary").GetComponent<TMP_Text>().text, Is.EqualTo(receiver.StatusText));
                yield return OpenDrawer();
                yield return Render("Lobby_CuChiBat_Mo");
            }
            finally
            {
                receiver.SetControlEnabled(false);
                portField.SetValue(receiver, originalPort);
            }
        }

        [UnityTest]
        public IEnumerator UtilityPanelsRemainReadableAndSeparateAtThreeRatiosAcrossScenes()
        {
            foreach (string scene in new[] { "Lobby", "Map01", "BossMap01", "RiftMap01" })
            {
                if (SceneManager.GetActiveScene().name != scene) yield return Load(scene);
                yield return Render(scene + "_ThuGon");
                yield return OpenDrawer();
                yield return Render(scene + "_Mo");
            }
        }

        private IEnumerator Render(string scene)
        {
            Camera camera = Camera.main;
            RenderMode mode = canvas.renderMode;
            Camera oldCamera = canvas.worldCamera;
            int sorting = canvas.sortingOrder;
            RenderTexture target = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.sortingOrder = 30000;
                foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                {
                    target = new RenderTexture(size.x, size.y, 24);
                    camera.targetTexture = target;
                    yield return null; yield return null;
                    Canvas.ForceUpdateCanvases();
                    bool expanded = Object.FindAnyObjectByType<HudUtilityLayout>().IsExpanded;
                    var panels = new List<RectTransform> { (RectTransform)canvas.transform.Find("CharacterDrawer") };
                    if (expanded)
                    {
                        panels.AddRange(Paths.Take(4).Select(path => (RectTransform)canvas.transform.Find(path)));
                        panels.Add((RectTransform)canvas.transform.Find("AutomationControls"));
                    }
                    else
                    {
                        foreach (string path in Paths)
                            Assert.That(Button(path).IsInteractable(), Is.False, path + " must not receive hidden UI input.");
                        Rect compact = Bounds(panels[0]);
                        Assert.That(compact.height / ((RectTransform)canvas.transform).rect.height,
                            Is.LessThan(scene.Contains("CuChiBat") ? 0.32f : 0.2f), "Collapsed HUD must release the encounter space at " + size);
                    }
                    MiniMapController map = canvas.GetComponent<MiniMapController>();
                    panels.Add(map.Panel);
                    panels.Add((RectTransform)canvas.transform.Find("DodgeCard"));
                    if (canvas.transform.Find("LobbyHUDRoot") is Transform lobby)
                    {
                        panels.Add((RectTransform)lobby.Find("LocationCard"));
                        panels.Add((RectTransform)lobby.Find("Prompt"));
                    }
                    Assert.That(map.Panel.GetComponentsInChildren<Graphic>(), Has.All.Matches<Graphic>(g => !g.raycastTarget));
                    Assert.That(map.Panel.GetComponentsInChildren<TMP_Text>().Count(t => t.name.StartsWith("Legend_")), Is.EqualTo(4));
                    if (scene == "Lobby") Assert.That(map.Panel.Find("Title").GetComponent<TMP_Text>().text, Does.Contain("TRẠM NGHỈ TÁN RỪNG"));
                    foreach (RectTransform panel in panels)
                    {
                        Rect bounds = Bounds(panel);
                        Assert.That(((RectTransform)canvas.transform).rect.Contains(bounds.min), Is.True, panel.name);
                        Assert.That(((RectTransform)canvas.transform).rect.Contains(bounds.max), Is.True, panel.name);
                        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
                        {
                            text.ForceMeshUpdate();
                            Assert.That(text.isTextOverflowing, Is.False, scene + "/" + text.name + ": " + text.text);
                            float physical = text.fontSize * Bounds(text.rectTransform).width / text.rectTransform.rect.width *
                                size.x / ((RectTransform)canvas.transform).rect.width;
                            Assert.That(physical, Is.GreaterThanOrEqualTo(14.5f), text.name + " physical font size");
                        }
                    }
                    Transform combat = canvas.transform.Find("CombatHUDRoot");
                    if (combat != null)
                        foreach (string name in new[] { "PlayerStatus", "SkillStrip", "ControlHints", "BossStatus" })
                        {
                            var panel = (RectTransform)combat.Find(name);
                            if (panel.gameObject.activeInHierarchy) panels.Add(panel);
                        }
                    for (int a = 0; a < panels.Count; a++)
                        for (int b = a + 1; b < panels.Count; b++)
                            Assert.That(Bounds(panels[a]).Overlaps(Bounds(panels[b])), Is.False, panels[a].name + " overlaps " + panels[b].name);
                    camera.Render();
                    RenderTexture previous = RenderTexture.active;
                    var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                        Directory.CreateDirectory("Assets/Screenshots");
                        File.WriteAllBytes("Assets/Screenshots/HUDTienIch_" + scene + "_" + size.x + "x" + size.y + "_Verified.png", image.EncodeToPNG());
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                    camera.targetTexture = null; target.Release(); Object.Destroy(target); target = null;
                }
            }
            finally
            {
                camera.targetTexture = null; canvas.renderMode = mode; canvas.worldCamera = oldCamera; canvas.sortingOrder = sorting;
                if (target != null) { target.Release(); Object.Destroy(target); }
            }
        }

        private Rect Bounds(RectTransform rect)
        {
            var points = new Vector3[4]; rect.GetWorldCorners(points);
            Vector3 min = canvas.transform.InverseTransformPoint(points[0]);
            Vector3 max = canvas.transform.InverseTransformPoint(points[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (owner != null) GameplayState.ExitModal(owner);
            // Nested render enumerators may fail before their parent finally is disposed.
            // Teardown must always restore the persistent UDP receiver as well.
            if (savedReceiver != null)
            {
                savedReceiver.SetControlEnabled(false);
                typeof(GestureInputReceiver).GetField("port", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(savedReceiver, savedGesturePort);
                if (savedGestureEnabled) savedReceiver.SetControlEnabled(true);
            }
            Scene scene = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("HudUtilityClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            typeof(PlayerAutomationState).GetMethod("ResetSession", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            if (originalAttack.HasValue) PlayerAutomationState.SetAutoAttackEnabled(originalAttack.Value);
            PlayerAutomationState.SetAutoPickupEnabled(originalPickup);
            if (originalState != null) JsonUtility.FromJsonOverwrite(originalState, Progress);
            if (existed) PlayerPrefs.SetString(SaveKey, original); else PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
