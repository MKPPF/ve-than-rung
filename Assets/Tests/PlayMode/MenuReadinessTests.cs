using System.Collections;
using System.Reflection;
using System.IO;
using TMPro;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Save;
using TopDownGame.UI;
using TopDownGame.Audio;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Checks menu safety without ever mutating the real player save.</summary>
    public sealed class MenuReadinessTests
    {
        private const string Key = "TopDownGame.Tests.Progress.v1";
        private string saved;
        private bool existed;
        private bool isolated;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(typeof(GameProgressManager).GetProperty("ActiveSaveKey", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.EqualTo(Key));
            isolated = true;
            existed = PlayerPrefs.HasKey(Key);
            saved = PlayerPrefs.GetString(Key, "");
            // These fixtures exercise menus/storage/rewards after onboarding.
            GameProgressManager.Instance.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("MainMenu");
        }

        [UnityTest]
        public IEnumerator NewGameRequiresConfirmationAndCancelPreservesSave()
        {
            GameProgressManager.Instance.BeginNewGame();
            string before = PlayerPrefs.GetString(Key);
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            menu.StartNewGame();
            yield return null;
            var dialog = menu.GetComponent<MenuDialog>();
            Assert.That(dialog.IsOpen, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(before));
            menu.StartNewGame();
            Assert.That(menu.transform.Find("MenuDialogPanel"), Is.Not.Null);
            dialog.Close();
            Assert.That(PlayerPrefs.GetString(Key), Is.EqualTo(before));
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstLaunchDoesNotCreateSaveOrGrantDebugGold()
        {
            Object.Destroy(GameProgressManager.Instance.gameObject);
            yield return null;
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(Key + ".Backup");
            var progress = new GameObject("FreshProgressProbe").AddComponent<GameProgressManager>();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Assert.That(progress.HasSavedGame, Is.False);
            Assert.That(progress.Gold, Is.EqualTo(2500));
            Assert.That(GameObject.Find("ContinueButton").GetComponent<Button>().interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator ExistingGoldSurvivesManagerReload()
        {
            GameProgressManager.Instance.BeginNewGame();
            GameProgressManager.Instance.AddGold(123);
            typeof(GameProgressManager).GetMethod("SaveProgress", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(GameProgressManager.Instance, null);
            int gold = GameProgressManager.Instance.Gold;
            Object.Destroy(GameProgressManager.Instance.gameObject);
            yield return null;
            var restored = new GameObject("RestoredProgressProbe").AddComponent<GameProgressManager>();
            Assert.That(restored.Gold, Is.EqualTo(gold));
        }

        [UnityTest]
        public IEnumerator DeathScreenBlocksOpeningPauseMenu()
        {
            var root = new GameObject("DeathModalProbe");
            try
            {
                var gameOver = root.AddComponent<GameOverUI>();
                var pause = root.AddComponent<PauseMenu>();
                yield return null;
                typeof(GameOverUI).GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gameOver, null);
                pause.Toggle();
                Assert.That(GameplayState.IsBlockedByOther(pause), Is.True);
                Assert.That(Time.timeScale, Is.Zero);
            }
            finally { Object.Destroy(root); }
            yield return null;
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DialogsFitCommonResolutionsAndRenderScreenshots()
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            var canvas = menu.GetComponent<Canvas>();
            var camera = Camera.main;
            var originalMode = canvas.renderMode;
            var originalCamera = canvas.worldCamera;
            RenderTexture target = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                GameProgressManager.Instance.BeginNewGame();
                foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                {
                    target = new RenderTexture(size.x, size.y, 24);
                    camera.targetTexture = target;
                    menu.GetComponent<MenuDialog>().ShowHelp();
                    yield return null;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    var panel = (RectTransform)canvas.transform.Find("MenuDialogPanel");
                    var surface = (RectTransform)panel.Find("Surface");
                    var window = (RectTransform)surface.Find("DialogWindow");
                    Assert.That(window.rect.width * surface.localScale.x, Is.LessThanOrEqualTo(panel.rect.width + 1));
                    Assert.That(window.rect.height * surface.localScale.y, Is.LessThanOrEqualTo(panel.rect.height + 1));
                    foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False, text.name + " must remain readable.");
                    }
                    Capture(camera, target, "HuongDan_" + size.x + "x" + size.y);
                    menu.GetComponent<MenuDialog>().Close();
                    yield return null;
                    menu.StartNewGame();
                    yield return null;
                    Capture(camera, target, "XacNhanChoiMoi_" + size.x + "x" + size.y);
                    menu.GetComponent<MenuDialog>().Close();
                    yield return null;
                    Capture(camera, target, "MenuDoAn_" + size.x + "x" + size.y);
                    camera.targetTexture = null;
                    target.Release();
                    Object.Destroy(target);
                    target = null;
                }
            }
            finally
            {
                menu.GetComponent<MenuDialog>().Close();
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
                camera.targetTexture = null;
                if (target != null) { target.Release(); Object.Destroy(target); }
            }
        }

        private static void Capture(Camera camera, RenderTexture target, string name)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/" + name + "_Verified.png", texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(texture); }
        }

        [UnityTest]
        public IEnumerator SettingsSuspendUnderlyingControlsAndRestoreKeyboardFocus()
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            var button = GameObject.Find("SettingsButton").GetComponent<Button>();
            button.Select();
            var dialog = menu.GetComponent<MenuDialog>();
            var settings = menu.GetComponent<SettingsPanel>();
            settings.Open(dialog);
            yield return null;
            Assert.That(dialog.IsOpen, Is.True);
            Assert.That(button.IsInteractable(), Is.False, "Keyboard navigation must not reach underlying buttons.");
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CloseDialog"));
            settings.Open(dialog);
            dialog.ShowHelp();
            Assert.That(menu.GetComponentsInChildren<Slider>().Length, Is.EqualTo(4), "Repeated opens must not stack dialogs.");
            dialog.Close();
            Assert.That(button.IsInteractable(), Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(button.gameObject));
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator DisplayPreviewRevertsOnCloseAndTimeoutWithoutSaving()
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            var dialog = menu.GetComponent<MenuDialog>();
            var settings = menu.GetComponent<SettingsPanel>();
            string widthKey = GamePreferences.Key("TopDownGame.Display.Width");
            int storedWidth = PlayerPrefs.GetInt(widthKey, -1);
            settings.Open(dialog);
            settings.ApplyDisplay();
            Assert.That(settings.PendingConfirmation, Is.True);
            Assert.That(PlayerPrefs.GetInt(widthKey, -1), Is.EqualTo(storedWidth));
            dialog.Close();
            Assert.That(settings.PendingConfirmation, Is.False);
            yield return null;
            settings.Open(dialog);
            settings.ApplyDisplay();
            typeof(SettingsPanel).GetField("confirmationDeadline", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(settings, Time.unscaledTime - 1);
            yield return null;
            Assert.That(settings.PendingConfirmation, Is.False, "Countdown must finish even while the modal pauses gameplay.");
            Assert.That(PlayerPrefs.GetInt(widthKey, -1), Is.EqualTo(storedWidth));
            Assert.That(Time.timeScale, Is.Zero);
            dialog.Close();
        }

        [UnityTest]
        public IEnumerator AudioSettingsPersistIndependentlyOfNewGameAndUseTestKeys()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            var dialog = menu.GetComponent<MenuDialog>();
            var settings = menu.GetComponent<SettingsPanel>();
            var audio = GameAudioManager.EnsureExists();
            float original = audio.MasterVolume;
            string key = GamePreferences.Key("TopDownGame.Audio.Master");
            Assert.That(key, Does.StartWith("TopDownGame.Tests."));
            bool existed = PlayerPrefs.HasKey(key);
            float realVolume = PlayerPrefs.GetFloat("TopDownGame.Audio.Master", -1);
            try
            {
                settings.Open(dialog);
                GameObject.Find("MasterVolume").GetComponent<Slider>().value = 0.37f;
                Assert.That(audio.MasterVolume, Is.EqualTo(0.37f).Within(0.001f));
                Assert.That(GameObject.Find("MasterValue").GetComponent<TMP_Text>().text, Is.EqualTo("37%"));
                GameProgressManager.Instance.BeginNewGame();
                Assert.That(PlayerPrefs.GetFloat(key), Is.EqualTo(0.37f).Within(0.001f));
                Assert.That(PlayerPrefs.GetFloat("TopDownGame.Audio.Master", -1), Is.EqualTo(realVolume));
                dialog.Close();
                Object.Destroy(audio.gameObject);
                yield return null;
                audio = GameAudioManager.EnsureExists();
                Assert.That(audio.MasterVolume, Is.EqualTo(0.37f).Within(0.001f));
            }
            finally
            {
                audio.SetMasterVolume(original);
                if (!existed) PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                dialog.Close();
            }
        }

        [UnityTest]
        public IEnumerator SettingsFromPauseKeepGamePausedAndBlockMenuActions()
        {
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            pause.Toggle();
            var dialog = pause.GetComponent<MenuDialog>();
            pause.GetComponent<SettingsPanel>().Open(dialog);
            pause.ReturnToMenu();
            pause.Resume();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
            Assert.That(Time.timeScale, Is.Zero);
            dialog.Close();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameObject.Find("ResumeButton").GetComponent<Button>().IsInteractable(), Is.True);
            yield return null;
            pause.Resume();
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EscapeClosesOnlyNestedSettingsAndNextPressResumes()
        {
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                pause.Toggle();
                var dialog = pause.GetComponent<MenuDialog>();
                pause.GetComponent<SettingsPanel>().Open(dialog);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return null;
                yield return null;
                Assert.That(dialog.IsOpen, Is.False);
                Assert.That(Time.timeScale, Is.Zero, "The same Escape press must not close pause too.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return null;
                yield return null;
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator ConfirmedDisplaySettingsSurviveNewGameAndResolutionListIsUnique()
        {
            string[] keys = { GamePreferences.Key("TopDownGame.Display.Width"), GamePreferences.Key("TopDownGame.Display.Height"), GamePreferences.Key("TopDownGame.Display.Fullscreen") };
            int[] previous = new int[3];
            bool[] existed = new bool[3];
            for (int i = 0; i < 3; i++) { existed[i] = PlayerPrefs.HasKey(keys[i]); previous[i] = PlayerPrefs.GetInt(keys[i]); }
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            var dialog = menu.GetComponent<MenuDialog>();
            var settings = menu.GetComponent<SettingsPanel>();
            try
            {
                var resolutions = DisplayPreferences.AvailableResolutions();
                Assert.That(new System.Collections.Generic.HashSet<Vector2Int>(resolutions).Count, Is.EqualTo(resolutions.Count));
                settings.Open(dialog);
                settings.ApplyDisplay();
                settings.ConfirmDisplay();
                int width = PlayerPrefs.GetInt(keys[0]);
                int height = PlayerPrefs.GetInt(keys[1]);
                Assert.That(resolutions, Does.Contain(new Vector2Int(width, height)));
                GameProgressManager.Instance.BeginNewGame();
                Assert.That(PlayerPrefs.GetInt(keys[0]), Is.EqualTo(width));
                Assert.That(PlayerPrefs.GetInt(keys[1]), Is.EqualTo(height));
                Assert.That(settings.PendingConfirmation, Is.False);
                dialog.Close();
                yield return null;
            }
            finally
            {
                for (int i = 0; i < 3; i++)
                    if (existed[i]) PlayerPrefs.SetInt(keys[i], previous[i]); else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();
                dialog.Close();
            }
        }

        [UnityTest]
        public IEnumerator StorageTransfersPersistAndEscapeDoesNotOpenPause()
        {
            GameProgressManager.Instance.BeginNewGame();
            GameProgressManager.Instance.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            var storage = Object.FindAnyObjectByType<StorageChestUI>();
            var player = GameObject.FindWithTag("Player").GetComponent<PlayerInteraction>();
            var inventory = player.GetComponent<EquipmentInventory>();
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            storage.Open(player);
            Assert.That(storage.IsOpen, Is.True);
            Assert.That(GameplayState.IsInputBlocked, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            pause.Toggle();
            Assert.That(GameObject.Find("PausePanel"), Is.Null);
            string id = GameProgressManager.Instance.ChestItems[0];
            Transform panel = storage.GetComponentInParent<Canvas>().transform.Find("StorageChestPanel");
            FindTransfer(panel, id).onClick.Invoke();
            yield return null;
            Assert.That(GameProgressManager.Instance.ChestItems, Does.Not.Contain(id));
            Assert.That(inventory.Owns(inventory.FindById(id)), Is.True);
            Assert.That(inventory.Equip(inventory.FindById(id)), Is.True);
            storage.Close();
            yield return null;
            storage.Open(player);
            FindTransfer(panel, id).onClick.Invoke();
            yield return null;
            Assert.That(GameProgressManager.Instance.ChestItems, Does.Contain(id));
            Assert.That(inventory.GetEquipped(inventory.FindById(id).Slot), Is.Null, "Storing equipped gear must remove its bonuses.");
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(UnityEngine.InputSystem.Key.Escape));
                yield return null;
                yield return null;
                Assert.That(storage.IsOpen, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameObject.Find("PausePanel"), Is.Null);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            Assert.That(GameProgressManager.Instance.ChestItems, Does.Contain(id));
        }

        private static Button FindTransfer(Transform panel, string id)
        {
            foreach (Button button in panel.GetComponentsInChildren<Button>())
                if (button.name == id) return button;
            Assert.Fail("Missing storage transfer for " + id);
            return null;
        }

        [UnityTest]
        public IEnumerator SkillRewardBlocksOtherScreensSavesChoiceAndIgnoresDoubleSubmit()
        {
            GameProgressManager.Instance.BeginNewGame();
            GameProgressManager.Instance.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            var player = GameObject.FindWithTag("Player");
            var inventory = player.GetComponent<SkillInventory>();
            var rewards = Object.FindAnyObjectByType<SkillSelectionUI>();
            rewards.Show(inventory);
            Assert.That(rewards.IsOpen, Is.True);
            Object.FindAnyObjectByType<PauseMenu>().Toggle();
            Object.FindAnyObjectByType<StorageChestUI>().Open(player.GetComponent<PlayerInteraction>());
            Assert.That(GameObject.Find("PausePanel"), Is.Null);
            Assert.That(Object.FindAnyObjectByType<StorageChestUI>().IsOpen, Is.False);
            Transform panel = rewards.GetComponentInParent<Canvas>().transform.Find("SkillSelectionPanel");
            var buttons = panel.GetComponentsInChildren<Button>();
            Assert.That(buttons.Length, Is.EqualTo(3));
            string id = buttons[0].name.Substring("Reward_".Length);
            int before = inventory.GetLevel(id);
            buttons[0].onClick.Invoke();
            buttons[0].onClick.Invoke();
            Assert.That(inventory.GetLevel(id), Is.EqualTo(before + 1));
            Assert.That(rewards.IsOpen, Is.False);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(PlayerPrefs.GetString(Key), Does.Contain(id));
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            Assert.That(GameObject.FindWithTag("Player").GetComponent<SkillInventory>().GetLevel(id), Is.EqualTo(before + 1));
        }

        [UnityTest]
        public IEnumerator FullyUpgradedSkillCatalogOffersExitInsteadOfWastedReward()
        {
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            var rewards = Object.FindAnyObjectByType<SkillSelectionUI>();
            var inventory = GameObject.FindWithTag("Player").GetComponent<SkillInventory>();
            var data = (SkillData[])typeof(SkillSelectionUI).GetField("skillDatabase", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(rewards);
            foreach (SkillData skill in data)
                for (int i = 0; i < skill.MaxLevel; i++) inventory.AddOrUpgrade(skill.Id.ToString(), skill.MaxLevel);
            rewards.Show(inventory);
            Assert.That(GameObject.Find("CloseRewards"), Is.Not.Null);
            GameObject.Find("CloseRewards").GetComponent<Button>().onClick.Invoke();
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SettingsPauseAndDeathScreensFitCommonResolutions()
        {
            GameProgressManager.Instance.BeginNewGame();
            GameProgressManager.Instance.CompleteTutorial();
            foreach (string sceneName in new[] { "MainMenu", "Lobby" })
            {
                yield return SceneManager.LoadSceneAsync(sceneName);
                yield return null;
                yield return null;
                var menu = Object.FindAnyObjectByType<MainMenuController>();
                var pause = Object.FindAnyObjectByType<PauseMenu>();
                Canvas canvas = menu != null ? menu.GetComponent<Canvas>() : pause.GetComponentInParent<Canvas>();
                Camera camera = Camera.main;
                var mode = canvas.renderMode;
                int sortingOrder = canvas.sortingOrder;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.sortingOrder = 30000;
                RenderTexture target = null;
                try
                {
                    foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                    {
                        target = new RenderTexture(size.x, size.y, 24);
                        camera.targetTexture = target;
                        if (pause != null) pause.Toggle();
                        yield return null;
                        yield return null;
                        Transform basePanel = canvas.transform.Find(menu != null ? "MenuPanel" : "PausePanel");
                        AssertFits(canvas, basePanel);
                        Capture(camera, target, (menu != null ? "MenuUX" : "TamDungUX") + "_" + size.x + "x" + size.y);
                        var dialog = menu != null ? menu.GetComponent<MenuDialog>() : pause.GetComponent<MenuDialog>();
                        var settings = menu != null ? menu.GetComponent<SettingsPanel>() : pause.GetComponent<SettingsPanel>();
                        settings.Open(dialog);
                        yield return null;
                        yield return null;
                        AssertFits(canvas, canvas.transform.Find("MenuDialogPanel"));
                        Capture(camera, target, "CaiDatUX_" + sceneName + "_" + size.x + "x" + size.y);
                        settings.ApplyDisplay();
                        yield return null;
                        AssertFits(canvas, canvas.transform.Find("MenuDialogPanel"));
                        Capture(camera, target, "XacNhanHienThiUX_" + sceneName + "_" + size.x + "x" + size.y);
                        dialog.Close();
                        if (pause != null)
                        {
                            pause.Resume();
                            var death = Object.FindAnyObjectByType<GameOverUI>();
                            typeof(GameOverUI).GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(death, null);
                            yield return null;
                            yield return null;
                            Transform deathPanel = canvas.transform.Find("GameOverPanel");
                            AssertFits(canvas, deathPanel);
                            Capture(camera, target, "ThatBaiUX_" + size.x + "x" + size.y);
                            // This layout probe shows death without killing the avatar.
                            // Use lifecycle cleanup so the controller's open state and focus agree.
                            death.enabled = false;
                            death.enabled = true;
                            yield return null;
                            var player = GameObject.FindWithTag("Player");
                            var storage = Object.FindAnyObjectByType<StorageChestUI>();
                            storage.Open(player.GetComponent<PlayerInteraction>());
                            yield return null;
                            yield return null;
                            Transform storagePanel = canvas.transform.Find("StorageChestPanel");
                            AssertFits(canvas, storagePanel);
                            Capture(camera, target, "KhoDoUX_" + size.x + "x" + size.y);
                            storage.Close();
                            yield return null;
                            var rewards = Object.FindAnyObjectByType<SkillSelectionUI>();
                            rewards.Show(player.GetComponent<SkillInventory>());
                            yield return null;
                            yield return null;
                            Transform rewardsPanel = canvas.transform.Find("SkillSelectionPanel");
                            AssertFits(canvas, rewardsPanel);
                            Capture(camera, target, "PhanThuongKyNangUX_" + size.x + "x" + size.y);
                            rewardsPanel.GetComponentsInChildren<Button>()[0].onClick.Invoke();
                        }
                        yield return null;
                        camera.targetTexture = null;
                        target.Release();
                        Object.Destroy(target);
                        target = null;
                    }
                }
                finally
                {
                    canvas.renderMode = mode;
                    canvas.sortingOrder = sortingOrder;
                    camera.targetTexture = null;
                    if (target != null) { target.Release(); Object.Destroy(target); }
                }
            }
        }

        private static void AssertFits(Canvas canvas, Transform panel)
        {
            Canvas.ForceUpdateCanvases();
            var bounds = (RectTransform)canvas.transform;
            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing, Is.False, text.name + " text overflow");
            }
            foreach (Button button in panel.GetComponentsInChildren<Button>())
            {
                var corners = new Vector3[4];
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 point = bounds.InverseTransformPoint(corner);
                    Assert.That(point.x, Is.InRange(bounds.rect.xMin - 1, bounds.rect.xMax + 1), button.name + " horizontal bounds");
                    Assert.That(point.y, Is.InRange(bounds.rect.yMin - 1, bounds.rect.yMax + 1), button.name + " vertical bounds");
                }
            }
            foreach (Slider slider in panel.GetComponentsInChildren<Slider>())
            {
                // Slider-driven anchors must not retain the original fixed width.
                Assert.That(slider.fillRect.rect.width, Is.LessThanOrEqualTo(((RectTransform)slider.transform).rect.width), slider.name + " fill leaks into adjacent controls");
                Assert.That(slider.handleRect.rect.width, Is.GreaterThan(0));
                Assert.That(slider.handleRect.rect.height, Is.LessThanOrEqualTo(((RectTransform)slider.transform).rect.height), slider.name + " handle exceeds track height");
            }
        }

        [UnityTest]
        public IEnumerator ClosingNestedHelpKeepsPauseUntilResume()
        {
            var root = new GameObject("PauseProbe", typeof(RectTransform), typeof(Canvas));
            try
            {
                var pause = root.AddComponent<PauseMenu>();
                yield return null;
                pause.Toggle();
                Assert.That(Time.timeScale, Is.Zero);
                var help = root.GetComponent<MenuDialog>();
                help.ShowHelp();
                pause.Resume();
                Assert.That(Time.timeScale, Is.Zero);
                help.Close();
                Assert.That(Time.timeScale, Is.Zero, "Closing help must not resume the underlying pause menu.");
                yield return null;
                pause.Resume();
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameplayState.IsInputBlocked, Is.False);
            }
            finally { Object.Destroy(root); }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (GameProgressManager.Instance != null) Object.Destroy(GameProgressManager.Instance.gameObject);
            yield return null;
            if (isolated)
            {
                if (existed) PlayerPrefs.SetString(Key, saved);
                else PlayerPrefs.DeleteKey(Key);
                new GameObject(nameof(GameProgressManager)).AddComponent<GameProgressManager>();
            }
            // Other combat fixtures create their own MainCamera. Do not leak the
            // menu's camera/canvas into their scene-wide object searches.
            Scene menuScene = SceneManager.GetActiveScene();
            Scene cleanScene = SceneManager.CreateScene("MenuReadinessClean");
            SceneManager.SetActiveScene(cleanScene);
            yield return SceneManager.UnloadSceneAsync(menuScene);
        }
    }
}
