#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Exercises real UI components and catalog on an isolated test save, with reproducible renders.</summary>
    public sealed class EquipmentInterfacePlayModeTests
    {
        private GameObject player;
        private GameObject canvasObject;
        private GameObject cameraObject;
        private InventoryUI bag;
        private WeaponShopUI shop;
        private RenderTexture target;
        private Camera camera;
        private GameProgressManager testProgress;
        private string originalProgress;
        private string originalSave;
        private bool originalSaveExists;
        private bool originalGoldGrantExists;
        private int originalGoldGrant;
        private bool originalPlayerProgressApplied;
        private readonly List<EquipmentData> catalog = new List<EquipmentData>();
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator InventoryAndShopRemainExclusiveAndRenderAcrossAspectRatios()
        {
            Assert.That((bool)typeof(GameProgressManager).GetProperty("IsAutomatedTestRun",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.True,
                "Never run this fixture against the player's save.");
            if (GameProgressManager.Instance == null)
                new GameObject("EquipmentTestProgress").AddComponent<GameProgressManager>();
            var progress = GameProgressManager.Instance;
            testProgress = progress;
            originalProgress = JsonUtility.ToJson(progress);
            originalSaveExists = PlayerPrefs.HasKey("TopDownGame.Tests.Progress.v1");
            originalSave = PlayerPrefs.GetString("TopDownGame.Tests.Progress.v1", "");
            originalGoldGrantExists = PlayerPrefs.HasKey("TopDownGame.Tests.Progress.GoldGrant.v1");
            originalGoldGrant = PlayerPrefs.GetInt("TopDownGame.Tests.Progress.GoldGrant.v1", 0);
            originalPlayerProgressApplied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private).GetValue(progress);
            typeof(GameProgressManager).GetField("items", Private).SetValue(progress, new List<string>());

            foreach (string guid in AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/ScriptableObjects/Equipment" }))
                catalog.Add(AssetDatabase.LoadAssetAtPath<EquipmentData>(AssetDatabase.GUIDToAssetPath(guid)));
            catalog.Sort((a, b) => a.Price.CompareTo(b.Price));
            player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            player.name = "EquipmentInterfacePlayerProbe";
            var inventory = player.GetComponent<EquipmentInventory>();
            var stats = player.GetComponent<PlayerStats>();
            var movement = player.GetComponent<PlayerMovement>();
            var interaction = player.GetComponent<PlayerInteraction>();

            cameraObject = new GameObject("EquipmentInterfaceCameraProbe", typeof(Camera));
            camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.02f, 0.03f);
            camera.cullingMask = 1 << 5;
            canvasObject = new GameObject("EquipmentInterfaceCanvasProbe", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 300;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            bag = canvasObject.AddComponent<InventoryUI>();
            shop = canvasObject.AddComponent<WeaponShopUI>();
            typeof(WeaponShopUI).GetField("catalog", Private).SetValue(shop, catalog.ToArray());
            typeof(WeaponShopUI).GetField("merchantPortrait", Private).SetValue(shop,
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/UI/ForgeKeeperV2.png"));
            yield return null;

            stats.AddGold(5000);
            shop.Open(interaction);
            Assert.That(shop.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            bag.Open();
            Assert.That(bag.IsOpen, Is.False);

            var offers = (HashSet<string>)typeof(WeaponShopUI).GetField("featuredOfferIds", Private).GetValue(shop);
            var featured = catalog.Find(item => item.Slot == EquipmentSlot.Weapon && offers.Contains(item.ItemId));
            int before = stats.Gold;
            FindButton(featured.ItemId + "Card").onClick.Invoke();
            Assert.That(stats.Gold, Is.EqualTo(before), "Selecting merchandise must not purchase it.");
            Assert.That(typeof(WeaponShopUI).GetField("selectedItem", Private).GetValue(shop), Is.SameAs(featured));
            typeof(WeaponShopUI).GetMethod("HandleItem", Private).Invoke(shop, new object[] { featured });
            Assert.That(stats.Gold, Is.EqualTo(before - WeaponShopUI.DiscountedPrice(featured.Price, 0.2f)));
            Assert.That(inventory.GetEquipped(featured.Slot), Is.SameAs(featured));
            int after = stats.Gold;
            typeof(WeaponShopUI).GetMethod("HandleItem", Private).Invoke(shop, new object[] { featured });
            Assert.That(stats.Gold, Is.EqualTo(after), "Owned equipment must not be charged twice.");

            foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
            {
                Resize(size);
                yield return null;
                yield return null;
                Capture("CuaHang_" + size.x + "x" + size.y);
                AssertSurfaceFits("WeaponShopPanel");
            }
            stats.TrySpendGold(stats.Gold);
            typeof(WeaponShopUI).GetMethod("RefreshCards", Private).Invoke(shop, null);
            Assert.That(FindButton("RerollOffers").interactable, Is.False);
            Assert.That(FindButton("Action").interactable, Is.False);
            shop.Close();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(GameplayState.ModalClosedThisFrame, Is.True);
            var pauseObject = new GameObject("EquipmentPauseProbe");
            var pause = pauseObject.AddComponent<PauseMenu>();
            pause.Toggle();
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Esc close must not also open Pause.");
            Object.Destroy(pauseObject);

            foreach (var item in catalog) progress.AddUniqueItem(item.ItemId);
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                inventory.Equip(catalog.Find(item => item.Slot == slot));
            bag.Open();
            Assert.That(bag.IsOpen, Is.True);
            shop.Open(interaction);
            Assert.That(shop.IsOpen, Is.False);
            Assert.That(movement.enabled, Is.False);
            FindButton("Filter1").onClick.Invoke();
            var bagItems = (Transform)typeof(InventoryUI).GetField("itemsRoot", Private).GetValue(bag);
            int selectableWeapons = 0;
            foreach (Button button in bagItems.GetComponentsInChildren<Button>())
                if (button.interactable) selectableWeapons++;
            Assert.That(selectableWeapons, Is.EqualTo(catalog.FindAll(item => item.Slot == EquipmentSlot.Weapon).Count));
            FindButton("Filter0").onClick.Invoke();
            FindButton("Sort").onClick.Invoke();
            Assert.That((bool)typeof(InventoryUI).GetField("sortByPower", Private).GetValue(bag), Is.True);
            FindButton("Sort").onClick.Invoke();
            foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
            {
                Resize(size);
                yield return null;
                yield return null;
                Capture("HanhTrang_" + size.x + "x" + size.y);
                AssertSurfaceFits("CharacterEquipmentPanel");
            }
            FindButton("Action").onClick.Invoke();
            Assert.That(inventory.CurrentEquipment, Is.Null, "Selected worn item should unequip.");
            FindButton("Action").onClick.Invoke();
            Assert.That(inventory.CurrentEquipment, Is.Not.Null);
            bag.enabled = false;
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(movement.enabled, Is.True);
            movement.enabled = false;
            shop.Open(interaction);
            shop.Close();
            Assert.That(movement.enabled, Is.False, "Closing must preserve an already-disabled movement component.");
        }

        private Button FindButton(string name)
        {
            foreach (Button button in canvasObject.GetComponentsInChildren<Button>())
                if (button.name == name) return button;
            Assert.Fail("Missing button " + name);
            return null;
        }

        private void Resize(Vector2Int size)
        {
            if (target != null) { camera.targetTexture = null; target.Release(); Object.Destroy(target); }
            target = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = target;
        }

        private void AssertSurfaceFits(string panelName)
        {
            var panel = (RectTransform)canvasObject.transform.Find(panelName);
            var surface = (RectTransform)panel.Find("Surface");
            Assert.That(surface.rect.width * surface.localScale.x, Is.LessThanOrEqualTo(panel.rect.width + 1));
            Assert.That(surface.rect.height * surface.localScale.y, Is.LessThanOrEqualTo(panel.rect.height + 1));
        }

        private void Capture(string name)
        {
            foreach (Transform child in canvasObject.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            Canvas.ForceUpdateCanvases();
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
            bag?.Close();
            shop?.Close();
            if (canvasObject != null) Object.Destroy(canvasObject);
            if (player != null) Object.Destroy(player);
            if (cameraObject != null) Object.Destroy(cameraObject);
            if (target != null) { target.Release(); Object.Destroy(target); }
            // Other fixtures share the runner's persistent manager. Restore its
            // test-only state instead of leaving a destroyed singleton reference.
            if (testProgress != null && originalProgress != null)
            {
                JsonUtility.FromJsonOverwrite(originalProgress, testProgress);
                typeof(GameProgressManager).GetField("playerProgressApplied", Private).SetValue(testProgress, originalPlayerProgressApplied);
                if (originalSaveExists) PlayerPrefs.SetString("TopDownGame.Tests.Progress.v1", originalSave);
                else PlayerPrefs.DeleteKey("TopDownGame.Tests.Progress.v1");
                if (originalGoldGrantExists) PlayerPrefs.SetInt("TopDownGame.Tests.Progress.GoldGrant.v1", originalGoldGrant);
                else PlayerPrefs.DeleteKey("TopDownGame.Tests.Progress.GoldGrant.v1");
            }
            Time.timeScale = 1f;
            catalog.Clear();
            yield return null;
            Assert.That(GameObject.FindGameObjectsWithTag("Player"), Is.Empty,
                "The UI fixture must not leave a player for later reward or targeting tests.");
        }
    }
}
#endif
