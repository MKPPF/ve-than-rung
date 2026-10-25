using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using TopDownGame.Core;
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
    /// <summary>Exercises real generated onboarding with isolated saves, scene transitions and keyboard input.</summary>
    public sealed class TutorialFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private string original;
        private bool existed, isolated;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(typeof(GameProgressManager).GetProperty("ActiveSaveKey", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.EqualTo(SaveKey));
            isolated = true;
            existed = PlayerPrefs.HasKey(SaveKey);
            original = PlayerPrefs.GetString(SaveKey, "");
            if (GameProgressManager.Instance != null) Object.Destroy(GameProgressManager.Instance.gameObject);
            yield return null;
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.DeleteKey(SaveKey + ".Backup");
            new GameObject("TutorialProgressProbe").AddComponent<GameProgressManager>();
            yield return SceneManager.LoadSceneAsync("MainMenu");
        }

        private static MenuDialog TutorialDialog() => Object.FindAnyObjectByType<LobbyTutorialUI>().GetComponent<MenuDialog>();
        private static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        private static IEnumerator Settle() { yield return null; yield return null; yield return null; }

        [UnityTest]
        public IEnumerator NewGameOpensOnceEscapePersistsAndNextNewGameResets()
        {
            Object.FindAnyObjectByType<MainMenuController>().StartNewGame();
            yield return Settle();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
            MenuDialog dialog = TutorialDialog();
            Assert.That(dialog.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(GameProgressManager.Instance.ShouldShowTutorial, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("NextTutorial"));
            Object.FindAnyObjectByType<PauseMenu>().Toggle();
            Assert.That(GameObject.Find("PausePanel"), Is.Null);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return Settle();
                Assert.That(dialog.IsOpen, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameObject.Find("PausePanel"), Is.Null);
                Assert.That(GameProgressManager.Instance.TutorialCompleted, Is.True);
                Assert.That(PlayerPrefs.GetString(SaveKey), Does.Contain("\"tutorialCompleted\":true"));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            Object.Destroy(GameProgressManager.Instance.gameObject);
            yield return null;
            new GameObject("TutorialReloadProbe").AddComponent<GameProgressManager>();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Object.FindAnyObjectByType<MainMenuController>().ContinueGame();
            yield return Settle();
            Assert.That(TutorialDialog().IsOpen, Is.False);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Object.FindAnyObjectByType<MainMenuController>().StartNewGame();
            Button("ConfirmDialog").onClick.Invoke();
            yield return Settle();
            Assert.That(TutorialDialog().IsOpen, Is.True);
            Assert.That(GameProgressManager.Instance.TutorialCompleted, Is.False);
            Button("CloseDialog").onClick.Invoke();
            Assert.That(GameProgressManager.Instance.TutorialCompleted, Is.True);
        }

        [UnityTest]
        public IEnumerator LegacySaveMigratesWithoutChangingProgressOrForcingTutorial()
        {
            var progress = GameProgressManager.Instance;
            progress.BeginNewGame();
            progress.AddGold(772);
            progress.AddUniqueItem("MigrationProbeItem");
            progress.SaveProgress();
            string legacy = Regex.Replace(PlayerPrefs.GetString(SaveKey), ",?\"tutorial(Initialized|Completed)\":(true|false)", "");
            PlayerPrefs.SetString(SaveKey, legacy);
            Assert.That(progress.LoadProgress(), Is.True);
            Assert.That(progress.Gold, Is.EqualTo(3272));
            Assert.That(progress.Items, Does.Contain("MigrationProbeItem"));
            Assert.That(progress.TutorialCompleted, Is.True);
            Assert.That(progress.ShouldShowTutorial, Is.False);
            string migrated = PlayerPrefs.GetString(SaveKey);
            Assert.That(progress.LoadProgress(), Is.True);
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(migrated));
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return Settle();
            Assert.That(TutorialDialog().IsOpen, Is.False);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator SceneInterruptionDoesNotCountAsSkipAndNoSaveDoesNotOpenTutorial()
        {
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return Settle();
            Assert.That(TutorialDialog().IsOpen, Is.False);
            GameProgressManager.Instance.BeginNewGame();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return Settle();
            Assert.That(TutorialDialog().IsOpen, Is.True);
            yield return SceneManager.LoadSceneAsync("MainMenu");
            Assert.That(GameProgressManager.Instance.TutorialCompleted, Is.False, "Scene teardown is not a player dismissal.");
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return Settle();
            Assert.That(TutorialDialog().IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator ReplayFromHelpPreservesSaveAndReturnsFocusToPause()
        {
            GameProgressManager.Instance.BeginNewGame();
            GameProgressManager.Instance.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return Settle();
            string before = PlayerPrefs.GetString(SaveKey);
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            pause.Toggle();
            Button("HelpButton").Select();
            var dialog = pause.GetComponent<MenuDialog>();
            dialog.ShowHelp();
            Button("ReplayTutorial").onClick.Invoke();
            yield return Settle();
            Assert.That(dialog.IsOpen, Is.True);
            Assert.That(GameObject.Find("TutorialStep"), Is.Not.Null);
            Button("CloseDialog").onClick.Invoke();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("HelpButton"));
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(before));
            yield return null;
            pause.Resume();
            Assert.That(Time.timeScale, Is.EqualTo(1));
            // The same replay entry is also available before starting a game.
            yield return SceneManager.LoadSceneAsync("MainMenu");
            dialog = Object.FindAnyObjectByType<MainMenuController>().GetComponent<MenuDialog>();
            dialog.ShowHelp();
            Button("ReplayTutorial").onClick.Invoke();
            yield return Settle();
            Button("CloseDialog").onClick.Invoke();
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator TutorialWaitsForExistingModalAndReleasesAfterExplicitSkip()
        {
            GameProgressManager.Instance.BeginNewGame();
            var owner = new GameObject("TutorialBlockingProbe");
            Object.DontDestroyOnLoad(owner);
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> block = (scene, mode) =>
            {
                if (scene.name == "Lobby") GameplayState.EnterModal(owner);
            };
            SceneManager.sceneLoaded += block;
            try
            {
                yield return SceneManager.LoadSceneAsync("Lobby");
                yield return Settle();
                Assert.That(TutorialDialog().IsOpen, Is.False);
            }
            finally
            {
                SceneManager.sceneLoaded -= block;
                GameplayState.ExitModal(owner);
                Object.Destroy(owner);
            }
            yield return Settle();
            Assert.That(TutorialDialog().IsOpen, Is.True);
            Button("CloseDialog").onClick.Invoke();
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EveryStepFitsCommonResolutionsAndFinalActionSavesImmediately()
        {
            GameProgressManager.Instance.BeginNewGame();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return Settle();
            MenuDialog dialog = TutorialDialog();
            Canvas canvas = dialog.GetComponentInParent<Canvas>();
            Camera camera = Camera.main;
            RenderMode oldMode = canvas.renderMode;
            int oldSorting = canvas.sortingOrder;
            Camera oldCamera = canvas.worldCamera;
            RenderTexture target = null;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.sortingOrder = 30000;
            try
            {
                int resolutionIndex = 0;
                foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                {
                    if (resolutionIndex++ > 0) TutorialGuide.Show(dialog);
                    target = new RenderTexture(size.x, size.y, 24);
                    camera.targetTexture = target;
                    yield return Settle();
                    Assert.That(Button("PreviousTutorial").interactable, Is.False);
                    for (int step = 1; step <= 6; step++)
                    {
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        Transform panel = canvas.transform.Find("MenuDialogPanel");
                        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
                        {
                            text.ForceMeshUpdate();
                            Assert.That(text.isTextOverflowing, Is.False, text.name + " step " + step);
                        }
                        var surface = (RectTransform)panel.Find("Surface");
                        var window = (RectTransform)surface.Find("DialogWindow");
                        Assert.That(window.rect.width * surface.localScale.x, Is.LessThanOrEqualTo(((RectTransform)panel).rect.width + 1));
                        Assert.That(window.rect.height * surface.localScale.y, Is.LessThanOrEqualTo(((RectTransform)panel).rect.height + 1));
                        Assert.That(GameObject.Find("TutorialStep").GetComponent<TMP_Text>().text, Does.Contain(step + " / 6"));
                        Capture(camera, target, "HuongDanLanDau_Buoc" + step + "_" + size.x + "x" + size.y);
                        if (step == 2)
                        {
                            Button("PreviousTutorial").onClick.Invoke();
                            Assert.That(GameObject.Find("TutorialStep").GetComponent<TMP_Text>().text, Does.Contain("1 / 6"));
                            Button("NextTutorial").onClick.Invoke();
                        }
                        Button("NextTutorial").onClick.Invoke();
                    }
                    Assert.That(dialog.IsOpen, Is.False);
                    Assert.That(GameProgressManager.Instance.TutorialCompleted, Is.True);
                    Assert.That(PlayerPrefs.GetString(SaveKey), Does.Contain("\"tutorialCompleted\":true"));
                    camera.targetTexture = null;
                    target.Release();
                    Object.Destroy(target);
                    target = null;
                    yield return null;
                }
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.sortingOrder = oldSorting;
                camera.targetTexture = null;
                if (target != null) { target.Release(); Object.Destroy(target); }
            }
        }

        private static void Capture(Camera camera, RenderTexture target, string name)
        {
            camera.Render();
            RenderTexture previous = RenderTexture.active;
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

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Scene scene = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("TutorialFlowClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if (GameProgressManager.Instance != null) Object.Destroy(GameProgressManager.Instance.gameObject);
            yield return null;
            if (isolated)
            {
                if (existed) PlayerPrefs.SetString(SaveKey, original); else PlayerPrefs.DeleteKey(SaveKey);
                new GameObject("RestoredTutorialProgress").AddComponent<GameProgressManager>();
            }
        }
    }
}
