using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Equipment;
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
    /// <summary>Exercises unreadable saves through the generated menu and real player, with every preference isolated from the player's campaign.</summary>
    public sealed class SaveRecoveryFlowTests
    {
        private const string Key = "TopDownGame.Tests.Progress.v1";
        private const string Broken = "{\"gold\":3,\"items\":[\"InterruptedWrite\"],";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Dictionary<string, (bool exists, string value)> records = new Dictionary<string, (bool, string)>();
        private string state;
        private bool applied, pickup;
        private bool? attack;
        private ProgressLoadStatus status;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        private static MainMenuController Menu => Object.FindAnyObjectByType<MainMenuController>();
        private static MenuDialog Dialog => Menu.GetComponent<MenuDialog>();
        private static IEnumerator Settle() { yield return null; yield return null; yield return null; }

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("SaveRecoveryProbe").AddComponent<GameProgressManager>();
            state = JsonUtility.ToJson(Progress); status = Progress.LoadStatus;
            applied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private).GetValue(Progress);
            attack = PlayerAutomationState.AutoAttackOverride; pickup = PlayerAutomationState.AutoPickupEnabled;
            foreach (string suffix in new[] { "", ".Backup", ".Unreadable" })
                records[Key + suffix] = (PlayerPrefs.HasKey(Key + suffix), PlayerPrefs.GetString(Key + suffix, ""));
            Progress.BeginNewGame(); Progress.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return Settle();
        }

        private void Corrupt(string raw = Broken)
        {
            PlayerPrefs.SetString(Key, raw);
            Assert.That(Progress.LoadProgress(), Is.False);
            Assert.That(Progress.HasSavedGame, Is.False);
        }

        private IEnumerator RecoveryMenu()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return Settle();
            Assert.That(Dialog.IsOpen, Is.True);
            Assert.That(GameplayState.IsInputBlocked, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CloseDialog"));
        }

        [UnityTest]
        public IEnumerator EmptyExistingSaveIsRetainedForAnExplicitPlayerDecision()
        {
            PlayerPrefs.DeleteKey(Key + ".Backup");
            Corrupt("");
            Progress.SaveProgress(); Progress.CaptureActivePlayer();
            Assert.That(Progress.LoadStatus, Is.EqualTo(ProgressLoadStatus.Unreadable));
            Assert.That(PlayerPrefs.HasKey(Key), Is.True);
            Assert.That(PlayerPrefs.GetString(Key), Is.Empty);
            yield return RecoveryMenu();
            Assert.That(GameObject.Find("ConfirmDialog"), Is.Null);
            Button("CloseDialog").onClick.Invoke();
            Assert.That(PlayerPrefs.HasKey(Key), Is.True);
            Assert.That(PlayerPrefs.GetString(Key), Is.Empty);
        }

        [UnityTest]
        public IEnumerator MalformedSaveIsRetainedForAnExplicitPlayerDecision()
        {
            string live = JsonUtility.ToJson(Progress);
            foreach (string raw in new[] { Broken, "{}", "{\"gold\":1}", "null", PlayerPrefs.GetString(Key).Replace("\"gold\":2500", "\"gold\":-5") })
            {
                Corrupt(raw);
                Assert.That(JsonUtility.ToJson(Progress), Is.EqualTo(live), "Parsing must never partially mutate the live manager.");
                string backup = PlayerPrefs.GetString(Key + ".Backup");
                Progress.SaveProgress(); Progress.CaptureActivePlayer();
                Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(raw));
                Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(backup));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DistinctSaveRotatesBackupAndMissingPrimaryWaitsForExplicitRestore()
        {
            string first = PlayerPrefs.GetString(Key);
            bool hadArchive = PlayerPrefs.HasKey(Key + ".Unreadable");
            string archive = PlayerPrefs.GetString(Key + ".Unreadable", "");
            Progress.AddGold(412); Progress.SaveProgress();
            string second = PlayerPrefs.GetString(Key);
            Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(first));
            Progress.SaveProgress();
            Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(first), "An identical autosave must not replace the useful previous revision.");
            PlayerPrefs.SetString(Key + ".Backup", "bad backup");
            Progress.SaveProgress();
            Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(second), "A valid primary can repair an unusable backup.");
            PlayerPrefs.DeleteKey(Key);
            Object.Destroy(Progress.gameObject); yield return null;
            new GameObject("MissingPrimaryProbe").AddComponent<GameProgressManager>();
            Assert.That(Progress.LoadStatus, Is.EqualTo(ProgressLoadStatus.RecoveryAvailable));
            Progress.SaveProgress();
            Assert.That(PlayerPrefs.HasKey(Key), Is.False);
            Assert.That(Progress.TryRestoreBackup(), Is.True);
            Assert.That(Progress.Gold, Is.EqualTo(2912));
            Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(second));
            Assert.That(PlayerPrefs.HasKey(Key + ".Unreadable"), Is.EqualTo(hadArchive));
            Assert.That(PlayerPrefs.GetString(Key + ".Unreadable", ""), Is.EqualTo(archive));
        }

        [UnityTest]
        public IEnumerator DeferralEscapeNavigationCancelAndStaleCallbacksPreserveBothRecords()
        {
            Corrupt(); string backup = PlayerPrefs.GetString(Key + ".Backup");
            yield return RecoveryMenu();
            var staleConfirm = Button("ConfirmDialog").onClick;
            var staleClose = Button("CloseDialog").onClick;
            var blocker = new GameObject("RecoveryBlockingProbe");
            GameplayState.EnterModal(blocker);
            try
            {
                staleConfirm.Invoke();
                Assert.That(Dialog.IsOpen, Is.True);
                Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(Broken));
            }
            finally { GameplayState.ExitModal(blocker); Object.Destroy(blocker); }
            Menu.ContinueGame(); Menu.StartNewGame();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            foreach (var direction in new[] { MoveDirection.Right, MoveDirection.Left, MoveDirection.Down })
            {
                ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, new AxisEventData(EventSystem.current) { moveDir = direction }, ExecuteEvents.moveHandler);
                Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(GameObject.Find("MenuDialogPanel").transform), Is.True);
            }
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return Settle();
                Assert.That(Dialog.IsOpen, Is.False);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("ContinueButton"));
            Menu.StartNewGame();
            Assert.That(Dialog.IsOpen, Is.True);
            staleConfirm.Invoke(); staleClose.Invoke();
            Assert.That(Dialog.IsOpen, Is.True, "Callbacks from a closed recovery prompt cannot act on a newer new-game dialog.");
            Button("CloseDialog").onClick.Invoke();
            Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(Broken));
            Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(backup));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
        }

        [UnityTest]
        public IEnumerator RestoreRealRunThenReloadAndContinueKeepsEarlierCampaignExactlyOnce()
        {
            yield return SceneManager.LoadSceneAsync("Map01"); yield return Settle();
            var player = GameObject.FindWithTag("Player");
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            var stats = player.GetComponent<PlayerStats>(); var health = player.GetComponent<PlayerHealth>();
            var skills = player.GetComponent<SkillInventory>(); var equipment = player.GetComponent<EquipmentInventory>();
            stats.AddExperience(stats.ExperienceToNextLevel + 23); stats.AddGold(375);
            skills.AddOrUpgrade(SkillId.Shield.ToString()); Assert.That(skills.TryEquip(SkillId.Shield.ToString(), 3), Is.True);
            EquipmentData weapon = equipment.Catalog.First(x => x != null && x.Slot == EquipmentSlot.Weapon);
            Progress.AddUniqueItem(weapon.ItemId); Assert.That(equipment.Equip(weapon), Is.True);
            Progress.InitializeChest(new[] { "save_recovery_chest" }); Progress.AddHunterEssence(27);
            Assert.That(Progress.TryEnhanceEquipment(weapon.ItemId, 20, out int spent), Is.True);
            Progress.MarkBossDefeated("save_recovery_guardian"); Progress.UnlockRegion("Map02");
            Progress.CapturePlayer(stats, health, skills);
            string earlier = PlayerPrefs.GetString(Key);
            Assert.That(ProgressSnapshot.TryDeserialize(earlier, out var expected), Is.True);
            stats.AddGold(900); Progress.CapturePlayer(stats, health, skills);
            Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(earlier));
            Corrupt(); yield return RecoveryMenu();
            Assert.That(GameObject.Find("Body").GetComponent<TMP_Text>().text, Does.Contain(expected.gold.ToString("N0")));
            var confirm = Button("ConfirmDialog").onClick;
            confirm.Invoke(); confirm.Invoke();
            Assert.That(Dialog.IsOpen, Is.False);
            Assert.That(Progress.LoadStatus, Is.EqualTo(ProgressLoadStatus.Recovered));
            Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(earlier));
            Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(earlier));
            Assert.That(PlayerPrefs.GetString(Key + ".Unreadable"), Is.EqualTo(Broken));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Object.Destroy(Progress.gameObject); yield return null;
            new GameObject("RestoredRunProbe").AddComponent<GameProgressManager>();
            int loaded = 0;
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> count = (s, m) => { if (s.name == "Lobby") loaded++; };
            SceneManager.sceneLoaded += count;
            try
            {
                Menu.ContinueGame(); Menu.ContinueGame(); yield return Settle();
                Assert.That(loaded, Is.EqualTo(1));
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
                player = GameObject.FindWithTag("Player"); stats = player.GetComponent<PlayerStats>();
                Assert.That(stats.Level, Is.EqualTo(expected.playerLevel)); Assert.That(stats.Experience, Is.EqualTo(expected.experience));
                Assert.That(stats.Gold, Is.EqualTo(expected.gold));
                Assert.That(player.GetComponent<SkillInventory>().Slots[3].skillId, Is.EqualTo("Shield"));
                Assert.That(player.GetComponent<EquipmentInventory>().GetEquipped(EquipmentSlot.Weapon).ItemId, Is.EqualTo(weapon.ItemId));
                Assert.That(Progress.GetEnhancementLevel(weapon.ItemId), Is.EqualTo(1));
                Assert.That(Progress.ChestItems, Is.EquivalentTo(expected.chestItems));
                Assert.That(Progress.HunterEssence, Is.EqualTo(27 - spent));
                Assert.That(Progress.IsBossDefeated("save_recovery_guardian"), Is.True);
                Assert.That(Progress.IsRegionUnlocked("Map02"), Is.True);
                Assert.That(Object.FindAnyObjectByType<LobbyTutorialUI>().GetComponent<MenuDialog>().IsOpen, Is.False);
                Assert.That(GameplayState.IsInputBlocked, Is.False);
            }
            finally { SceneManager.sceneLoaded -= count; }
        }

        [UnityTest]
        public IEnumerator NoUsableBackupRequiresExplicitNewGameAndRetainsUnreadableBytes()
        {
            PlayerPrefs.SetString(Key + ".Backup", "{}"); Corrupt();
            yield return RecoveryMenu();
            Assert.That(GameObject.Find("ConfirmDialog"), Is.Null);
            Button("CloseDialog").onClick.Invoke(); Menu.StartNewGame();
            Button("CloseDialog").onClick.Invoke();
            Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(Broken)); Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo("{}"));
            Menu.StartNewGame(); Button("ConfirmDialog").onClick.Invoke(); yield return Settle();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
            Assert.That(Progress.PlayerLevel, Is.EqualTo(1)); Assert.That(Progress.Gold, Is.EqualTo(2500));
            Assert.That(Progress.Items, Is.Empty); Assert.That(Progress.HunterEssence, Is.Zero);
            Assert.That(PlayerPrefs.GetString(Key + ".Unreadable"), Is.EqualTo(Broken));
            Assert.That(ProgressSnapshot.TryDeserialize(PlayerPrefs.GetString(Key + ".Backup"), out var backup), Is.True);
            Assert.That(backup.playerLevel, Is.EqualTo(1)); Assert.That(backup.gold, Is.EqualTo(2500));
            Assert.That(Object.FindAnyObjectByType<LobbyTutorialUI>().GetComponent<MenuDialog>().IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator ChangedBackupOrNewlyValidPrimaryCannotBeReplacedByStaleConfirmation()
        {
            string valid = PlayerPrefs.GetString(Key); Corrupt(); yield return RecoveryMenu();
            var oldConfirm = Button("ConfirmDialog").onClick;
            ProgressSnapshot.TryDeserialize(valid, out var changed); changed.gold = 9876;
            string newer = JsonUtility.ToJson(changed); PlayerPrefs.SetString(Key + ".Backup", newer);
            oldConfirm.Invoke();
            Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(Broken));
            Assert.That(Dialog.IsOpen, Is.True, "A changed backup must be previewed again before the player confirms it.");
            Assert.That(GameObject.Find("Body").GetComponent<TMP_Text>().text, Does.Contain(9876.ToString("N0")));
            PlayerPrefs.SetString(Key, valid);
            Button("ConfirmDialog").onClick.Invoke();
            Assert.That(Dialog.IsOpen, Is.False); Assert.That(Progress.Gold, Is.EqualTo(2500));
            Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(valid)); Assert.That(PlayerPrefs.GetString(Key + ".Backup"), Is.EqualTo(newer));
            Assert.That(Progress.HasSavedGame, Is.True);
        }

        [UnityTest]
        public IEnumerator RecoveryDecisionsRenderWithoutClippingAtThreeRatios()
        {
            Corrupt(); yield return RecoveryMenu();
            yield return Render("CoDuPhong", GameObject.Find("MenuDialogPanel").transform);
            Button("CloseDialog").onClick.Invoke();
            yield return Render("DeSau", Menu.transform.Find("MenuPanel"));
            Menu.StartNewGame();
            yield return Render("XacNhanChoiMoi", GameObject.Find("MenuDialogPanel").transform);
            Button("CloseDialog").onClick.Invoke();
            string backup = PlayerPrefs.GetString(Key + ".Backup");
            PlayerPrefs.SetString(Key + ".Backup", "{}"); Progress.LoadProgress(); Menu.ContinueGame();
            yield return Render("KhongDuPhong", GameObject.Find("MenuDialogPanel").transform);
            Button("CloseDialog").onClick.Invoke();
            PlayerPrefs.SetString(Key + ".Backup", backup); Progress.LoadProgress(); Menu.ContinueGame();
            Button("ConfirmDialog").onClick.Invoke();
            yield return Render("DaKhoiPhuc", Menu.transform.Find("MenuPanel"));
        }

        private IEnumerator Render(string stateName, Transform panel)
        {
            Canvas canvas = Menu.GetComponentInParent<Canvas>();
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
                        Assert.That(text.fontSize * scale * size.x / bounds.rect.width, Is.GreaterThanOrEqualTo(14), text.name);
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
                        File.WriteAllBytes("Assets/Screenshots/KhoiPhucSave_" + stateName + "_" + size.x + "x" + size.y + "_Verified.png", texture.EncodeToPNG());
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
            Scene scene = SceneManager.GetActiveScene(); Scene clean = SceneManager.CreateScene("SaveRecoveryClean");
            SceneManager.SetActiveScene(clean); yield return SceneManager.UnloadSceneAsync(scene);
            if (Progress == null) new GameObject("SaveRecoveryRestoredProbe").AddComponent<GameProgressManager>();
            JsonUtility.FromJsonOverwrite(state, Progress);
            typeof(GameProgressManager).GetField("playerProgressApplied", Private).SetValue(Progress, applied);
            typeof(GameProgressManager).GetField("loadStatus", Private).SetValue(Progress, status);
            typeof(PlayerAutomationState).GetMethod("ResetSession", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            if (attack.HasValue) PlayerAutomationState.SetAutoAttackEnabled(attack.Value);
            PlayerAutomationState.SetAutoPickupEnabled(pickup);
            foreach (var record in records)
                if (record.Value.exists) PlayerPrefs.SetString(record.Key, record.Value.value); else PlayerPrefs.DeleteKey(record.Key);
        }
    }
}
