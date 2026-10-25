using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Maps;
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
    /// <summary>Checks real portal entry, cross-canvas modal focus and the generated destination catalog.</summary>
    public sealed class TravelFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private string original, originalState;
        private bool existed;
        private LobbyPortal portal;
        private PlayerInteraction player;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (GameProgressManager.Instance == null) new GameObject("TravelProgressProbe").AddComponent<GameProgressManager>();
            original = PlayerPrefs.GetString(SaveKey, "");
            existed = PlayerPrefs.HasKey(SaveKey);
            originalState = JsonUtility.ToJson(GameProgressManager.Instance);
            GameProgressManager.Instance.BeginNewGame();
            GameProgressManager.Instance.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            yield return null;
            portal = Object.FindAnyObjectByType<LobbyPortal>();
            player = GameObject.FindWithTag("Player").GetComponent<PlayerInteraction>();
            typeof(LobbyPortal).GetField("chargeDuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(portal, 0.2f);
        }

        private IEnumerator Open()
        {
            player.GetComponent<Rigidbody2D>().position = portal.transform.position;
            Physics2D.SyncTransforms();
            portal.Interact(player);
            float deadline = Time.realtimeSinceStartup + 3;
            while (!portal.IsChoosing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(portal.IsChoosing, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TravelOwnsFocusAcrossCanvasesAndEscapeReturnsToGameplay()
        {
            Button background = GameObject.Find("InventoryLauncher").GetComponent<Button>();
            background.Select();
            yield return Open();
            Assert.That(background.IsInteractable(), Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Cancel"));
            var ui = portal.GetComponent<LobbyTravelUI>();
            ui.Open(portal);
            Assert.That(portal.GetComponentsInChildren<Canvas>().Length, Is.EqualTo(1));
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,
                new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Up }, ExecuteEvents.moveHandler);
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(portal.transform), Is.True);
            Object.FindAnyObjectByType<PauseMenu>().Toggle();
            Assert.That(GameObject.Find("PausePanel"), Is.Null);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(portal.IsChoosing, Is.False);
                Assert.That(background.IsInteractable(), Is.True);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(background.gameObject));
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameObject.Find("PausePanel"), Is.Null);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator DestinationsUseMapNamesLevelsAndReadableLockedStatesAtCommonResolutions()
        {
            Camera camera = Camera.main;
            Canvas hud = Object.FindAnyObjectByType<LobbyHUD>().GetComponentInParent<Canvas>();
            RenderMode oldMode = hud.renderMode;
            Camera oldCamera = hud.worldCamera;
            int oldSorting = hud.sortingOrder;
            RenderTexture target = null;
            try
            {
                hud.renderMode = RenderMode.ScreenSpaceCamera;
                hud.worldCamera = camera;
                hud.planeDistance = 1;
                hud.sortingOrder = 29000;
                foreach (bool unlockAll in new[] { false, true })
                {
                    if (unlockAll) foreach (string scene in LobbyPortal.Destinations) GameProgressManager.Instance.UnlockRegion(scene);
                    yield return Open();
                    Canvas canvas = portal.GetComponentInChildren<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = 0.5f;
                    canvas.sortingOrder = 30000;
                    foreach (string scene in LobbyPortal.Destinations)
                    {
                        MapData data = portal.GetDestinationData(scene);
                        Assert.That(data, Is.Not.Null, scene + " catalog reference");
                        Button button = GameObject.Find("Travel_" + scene).GetComponent<Button>();
                        string label = button.GetComponentInChildren<TMP_Text>().text;
                        Assert.That(label, Does.Contain(data.DisplayName));
                        Assert.That(label, Does.Contain("Cấp đề nghị " + data.RecommendedMinLevel + "–" + data.RecommendedMaxLevel));
                        Assert.That(button.interactable, Is.EqualTo(unlockAll || scene == "Map01"));
                        Assert.That(label, Does.Contain(button.interactable ? "ĐÃ MỞ" : "CHƯA MỞ"));
                    }
                    foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                    {
                        target = new RenderTexture(size.x, size.y, 24);
                        camera.targetTexture = target;
                        yield return null;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        RectTransform bounds = (RectTransform)canvas.transform;
                        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>())
                        {
                            text.ForceMeshUpdate();
                            Assert.That(text.isTextOverflowing, Is.False, text.text);
                            if (text.transform.parent.name.StartsWith("Travel_"))
                                Assert.That(text.textInfo.lineCount, Is.EqualTo(3), "Name, level and status should each stay on one line.");
                        }
                        foreach (Button button in canvas.GetComponentsInChildren<Button>())
                        {
                            var corners = new Vector3[4];
                            ((RectTransform)button.transform).GetWorldCorners(corners);
                            foreach (Vector3 corner in corners)
                            {
                                Vector3 point = bounds.InverseTransformPoint(corner);
                                Assert.That(point.x, Is.InRange(bounds.rect.xMin, bounds.rect.xMax));
                                Assert.That(point.y, Is.InRange(bounds.rect.yMin, bounds.rect.yMax));
                            }
                        }
                        Capture(camera, target, "ChonVungUX_" + (unlockAll ? "DaMo" : "Khoa") + "_" + size.x + "x" + size.y);
                        camera.targetTexture = null;
                        target.Release();
                        Object.Destroy(target);
                        target = null;
                    }
                    portal.CancelChoice();
                    yield return null;
                }
            }
            finally
            {
                portal.CancelChoice();
                hud.renderMode = oldMode;
                hud.worldCamera = oldCamera;
                hud.sortingOrder = oldSorting;
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
                texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                texture.Apply();
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/" + name + "_Verified.png", texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(texture); }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (portal != null) portal.CancelChoice();
            Scene scene = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("TravelFlowClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if (originalState != null) JsonUtility.FromJsonOverwrite(originalState, GameProgressManager.Instance);
            if (existed) PlayerPrefs.SetString(SaveKey, original); else PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
