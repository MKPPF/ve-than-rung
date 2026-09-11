using System.Collections;
using System.Reflection;
using System.IO;
using TMPro;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Save;
using TopDownGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
                    Assert.That(surface.rect.width * surface.localScale.x, Is.LessThanOrEqualTo(panel.rect.width + 1));
                    Assert.That(surface.rect.height * surface.localScale.y, Is.LessThanOrEqualTo(panel.rect.height + 1));
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
