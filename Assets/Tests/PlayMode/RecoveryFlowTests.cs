using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Equipment;
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
    /// <summary>Exercises real death and recovery transitions so the UI promise is backed by persisted player progress.</summary>
    public sealed class RecoveryFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string original, originalState;
        private bool existed, originalApplied, originalPickup;
        private bool? originalAttack;
        private PlayerStats stats;
        private PlayerHealth health;
        private SkillInventory skills;
        private GameOverUI death;
        private GameProgressManager Progress => GameProgressManager.Instance;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("RecoveryProgressProbe").AddComponent<GameProgressManager>();
            original = PlayerPrefs.GetString(SaveKey, "");
            existed = PlayerPrefs.HasKey(SaveKey);
            originalState = JsonUtility.ToJson(Progress);
            originalApplied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private).GetValue(Progress);
            originalAttack = PlayerAutomationState.AutoAttackOverride;
            originalPickup = PlayerAutomationState.AutoPickupEnabled;
            Progress.BeginNewGame();
            Progress.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Map01");
            yield return null; yield return null;
            BindPlayer();
        }

        private void BindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            stats = player.GetComponent<PlayerStats>();
            health = player.GetComponent<PlayerHealth>();
            skills = player.GetComponent<SkillInventory>();
            death = Object.FindAnyObjectByType<GameOverUI>();
        }

        private void AwardUnsavedRunProgress()
        {
            stats.AddGold(375);
            stats.AddExperience(stats.ExperienceToNextLevel + 11);
            stats.SpendMana(stats.CurrentMana - 15);
            skills.AddOrUpgrade(SkillId.Shield.ToString());
            Assert.That(skills.TryEquip(SkillId.Shield.ToString(), 3), Is.True);
            Progress.AddHunterEssence(17);
            Progress.MarkBossDefeated("recovery_probe_guardian");
            Assert.That(Progress.PlayerLevel, Is.LessThan(stats.Level), "This run must contain progress newer than the last player snapshot.");
        }

        private IEnumerator Kill()
        {
            health.TakeStatusDamage(stats.MaxHealth * 2);
            yield return null;
            Assert.That(health.IsAlive, Is.False);
            Assert.That(GameObject.Find("GameOverPanel"), Is.Not.Null);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameplayState.IsInputBlocked, Is.True);
        }

        [UnityTest]
        public IEnumerator DeathSnapshotsNewExperienceAndSkillsBeforeRetryAndRestoresFullResources()
        {
            AwardUnsavedRunProgress();
            int level = stats.Level, experience = stats.Experience, gold = stats.Gold;
            yield return Kill();
            Assert.That(Progress.PlayerLevel, Is.EqualTo(level), "Dying must capture the current run before offering recovery.");
            Assert.That(Progress.CurrentHealth, Is.GreaterThan(0));
            Assert.That(Progress.OwnedSkills.Any(x => x.skillId == SkillId.Shield.ToString()), Is.True);
            var sceneHandle = SceneManager.GetActiveScene().handle;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("RestartButton"));
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().handle == sceneHandle && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().handle, Is.Not.EqualTo(sceneHandle));
            yield return null; yield return null;
            BindPlayer();
            Assert.That(stats.Level, Is.EqualTo(level));
            Assert.That(stats.Experience, Is.EqualTo(experience));
            Assert.That(stats.Gold, Is.EqualTo(gold));
            Assert.That(stats.CurrentMana, Is.EqualTo(stats.MaxMana));
            Assert.That(health.CurrentHealth, Is.EqualTo(stats.MaxHealth));
            Assert.That(health.IsAlive, Is.True);
            Assert.That(skills.Slots[3].skillId, Is.EqualTo(SkillId.Shield.ToString()));
            Assert.That(Progress.IsBossDefeated("recovery_probe_guardian"), Is.True);
            Assert.That(Progress.HunterEssence, Is.EqualTo(17));
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator ClosedDeathCallbacksCannotSaveReloadOrLeaveLiveGameplay()
        {
            string saved = PlayerPrefs.GetString(SaveKey);
            var sceneHandle = SceneManager.GetActiveScene().handle;
            Progress.CaptureDefeatedPlayer(stats, health, skills);
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(saved), "Recovery capture must reject a living player.");
            death.Restart();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(sceneHandle));
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(saved));
            death.ReturnToMenu();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(sceneHandle));
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(saved));
            death.ReturnToLobby();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(sceneHandle));
            Assert.That(health.IsAlive, Is.True);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator DirectLobbyKeepsLoadoutChestAndNewProgressAndRejectsOtherModalOrDuplicateActions()
        {
            EquipmentInventory equipment = stats.GetComponent<EquipmentInventory>();
            EquipmentData weapon = equipment.Catalog.First(x => x.Slot == EquipmentSlot.Weapon);
            EquipmentData stored = equipment.Catalog.First(x => x.Slot == EquipmentSlot.Helmet);
            Progress.AddUniqueItem(weapon.ItemId);
            Assert.That(equipment.Equip(weapon), Is.True);
            Progress.InitializeChest(null);
            Progress.AddUniqueItem(stored.ItemId);
            Assert.That(Progress.MoveItemToChest(stored.ItemId), Is.True);
            AwardUnsavedRunProgress();
            int level = stats.Level, experience = stats.Experience, gold = stats.Gold;
            yield return Kill();
            var blocker = new GameObject("RecoveryOtherModalProbe");
            try
            {
                GameplayState.EnterModal(blocker);
                death.Restart(); death.ReturnToMenu(); death.ReturnToLobby();
                yield return null;
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Map01"));
                Assert.That(death.IsOpen, Is.True);
            }
            finally { GameplayState.ExitModal(blocker); Object.Destroy(blocker); }
            int lobbyLoads = 0;
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> loaded = (scene, mode) => { if (scene.name == "Lobby") lobbyLoads++; };
            SceneManager.sceneLoaded += loaded;
            try
            {
                var button = GameObject.Find("GameOverLobbyButton");
                ExecuteEvents.Execute(button, new PointerEventData(EventSystem.current)
                    { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                death.ReturnToLobby(); // The same-frame duplicate must not schedule another load.
                yield return WaitForScene("Lobby");
                Assert.That(lobbyLoads, Is.EqualTo(1));
                BindPlayer();
                Assert.That(stats.Level, Is.EqualTo(level));
                Assert.That(stats.Experience, Is.EqualTo(experience));
                Assert.That(stats.Gold, Is.EqualTo(gold));
                Assert.That(health.CurrentHealth, Is.EqualTo(stats.MaxHealth));
                Assert.That(stats.CurrentMana, Is.EqualTo(stats.MaxMana));
                Assert.That(skills.Slots[3].skillId, Is.EqualTo(SkillId.Shield.ToString()));
                Assert.That(stats.GetComponent<EquipmentInventory>().GetEquipped(EquipmentSlot.Weapon).ItemId, Is.EqualTo(weapon.ItemId));
                Assert.That(Progress.ChestItems, Does.Contain(stored.ItemId));
                Assert.That(Progress.Items, Does.Not.Contain(stored.ItemId));
                Assert.That(Progress.IsRegionUnlocked("Map01"), Is.True);
                Assert.That(Progress.TutorialCompleted, Is.True);
                Assert.That(GameplayState.IsInputBlocked, Is.False);
            }
            finally { SceneManager.sceneLoaded -= loaded; }
        }

        [UnityTest]
        public IEnumerator DeathToMenuThenManagerReloadAndContinueKeepEarnedProgress()
        {
            AwardUnsavedRunProgress();
            int level = stats.Level, experience = stats.Experience, gold = stats.Gold;
            yield return Kill();
            Button menuButton = GameObject.Find("GameOverMenuButton").GetComponent<Button>();
            menuButton.Select();
            ExecuteEvents.Execute(menuButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return WaitForScene("MainMenu");
            Assert.That(GameObject.Find("SaveSummary").GetComponent<TMP_Text>().text, Does.Contain("Cấp " + level));
            string checkpoint = PlayerPrefs.GetString(SaveKey);
            Object.Destroy(Progress.gameObject);
            yield return null;
            new GameObject("RecoveryReloadedProgressProbe").AddComponent<GameProgressManager>();
            Assert.That(Progress.HasSavedGame, Is.True);
            Assert.That(Progress.PlayerLevel, Is.EqualTo(level));
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(checkpoint));
            Object.FindAnyObjectByType<MainMenuController>().ContinueGame();
            yield return WaitForScene("Lobby");
            BindPlayer();
            Assert.That(stats.Level, Is.EqualTo(level));
            Assert.That(stats.Experience, Is.EqualTo(experience));
            Assert.That(stats.Gold, Is.EqualTo(gold));
            Assert.That(skills.Slots[3].skillId, Is.EqualTo(SkillId.Shield.ToString()));
            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(stats.MaxHealth));
            Assert.That(stats.CurrentMana, Is.EqualTo(stats.MaxMana));
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator RepeatedShowDisableAndReenableKeepDeathOwnershipAndDoNotReviveCurrentAvatar()
        {
            AwardUnsavedRunProgress();
            yield return Kill();
            string checkpoint = PlayerPrefs.GetString(SaveKey);
            typeof(GameOverUI).GetMethod("Show", Private).Invoke(death, null);
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(checkpoint));
            Assert.That(health.IsAlive, Is.False, "Only the next scene's player may recover.");
            Assert.That(stats.GetComponent<PlayerMovement>().enabled, Is.False);
            death.enabled = false;
            Assert.That(death.IsOpen, Is.False);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            death.ReturnToMenu(); death.ReturnToLobby(); death.Restart();
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Map01"));
            death.enabled = true;
            yield return null;
            Assert.That(death.IsOpen, Is.True);
            Assert.That(GameplayState.IsInputBlocked, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(health.IsAlive, Is.False);
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(checkpoint));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("RestartButton"));
            Object.FindAnyObjectByType<PauseMenu>().Toggle();
            Assert.That(GameObject.Find("PausePanel"), Is.Null);
        }

        [UnityTest]
        public IEnumerator DeathChoicesRenderAndNavigateWithinModalAtThreeRatiosInNormalAndBossMaps()
        {
            foreach (string scene in new[] { "Map01", "BossMap01" })
            {
                if (SceneManager.GetActiveScene().name != scene)
                {
                    yield return SceneManager.LoadSceneAsync(scene);
                    yield return null; yield return null;
                    BindPlayer();
                }
                yield return Kill();
                Canvas canvas = death.GetComponentInParent<Canvas>();
                Transform panel = canvas.transform.Find("GameOverPanel");
                Assert.That(panel.GetComponentsInChildren<Button>().Length, Is.EqualTo(3));
                Assert.That(panel.Find("Surface/GameOverWindow/Eyebrow").GetComponent<TMP_Text>().text, Is.EqualTo(MapManager.Instance.Data.DisplayName.ToUpperInvariant()));
                foreach (MoveDirection direction in new[] { MoveDirection.Down, MoveDirection.Down, MoveDirection.Up, MoveDirection.Up })
                {
                    ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, new AxisEventData(EventSystem.current)
                        { moveDir = direction }, ExecuteEvents.moveHandler);
                    Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(panel), Is.True);
                }
                yield return Render(canvas, panel, scene);
            }
        }

        private IEnumerator WaitForScene(string scene)
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
            yield return null; yield return null;
        }

        private IEnumerator Render(Canvas canvas, Transform panel, string scene)
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
                    var bounds = (RectTransform)canvas.transform;
                    foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
                        float scale = text.transform.TransformVector(Vector3.right).magnitude /
                            canvas.transform.TransformVector(Vector3.right).magnitude;
                        Assert.That(text.fontSize * scale * size.x / bounds.rect.width, Is.GreaterThanOrEqualTo(14), text.name);
                    }
                    foreach (Button button in panel.GetComponentsInChildren<Button>())
                    {
                        var corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                        foreach (Vector3 point in corners) Assert.That(bounds.rect.Contains(bounds.InverseTransformPoint(point)), Is.True, button.name);
                    }
                    camera.Render();
                    RenderTexture previous = RenderTexture.active;
                    var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    try
                    {
                        RenderTexture.active = target;
                        image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                        Directory.CreateDirectory("Assets/Screenshots");
                        File.WriteAllBytes("Assets/Screenshots/HoiSinh_" + scene + "_" + size.x + "x" + size.y + "_Verified.png", image.EncodeToPNG());
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

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            // A failed assertion may have observed a scene load queued by a callback.
            yield return null; yield return null;
            Scene scene = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("RecoveryClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if (Progress != null && originalState != null)
            {
                JsonUtility.FromJsonOverwrite(originalState, Progress);
                typeof(GameProgressManager).GetField("playerProgressApplied", Private).SetValue(Progress, originalApplied);
            }
            typeof(PlayerAutomationState).GetMethod("ResetSession", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            if (originalAttack.HasValue) PlayerAutomationState.SetAutoAttackEnabled(originalAttack.Value);
            PlayerAutomationState.SetAutoPickupEnabled(originalPickup);
            if (existed) PlayerPrefs.SetString(SaveKey, original); else PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
