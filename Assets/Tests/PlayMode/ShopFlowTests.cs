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
    /// <summary>Exercises forge browsing, paid offers and saved purchases through the generated Lobby.</summary>
    public sealed class ShopFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string original, originalState;
        private bool existed;
        private WeaponShopUI shop;
        private PlayerInteraction player;
        private EquipmentInventory inventory;
        private PlayerStats stats;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private HashSet<string> Offers => (HashSet<string>)typeof(WeaponShopUI).GetField("featuredOfferIds", Private).GetValue(shop);
        private Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        private void Click(string name) => Button(name).onClick.Invoke();

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("ShopProgressProbe").AddComponent<GameProgressManager>();
            original = PlayerPrefs.GetString(SaveKey, "");
            existed = PlayerPrefs.HasKey(SaveKey);
            originalState = JsonUtility.ToJson(Progress);
            Progress.BeginNewGame();
            Progress.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            yield return null;
            shop = Object.FindAnyObjectByType<WeaponShopUI>();
            player = GameObject.FindWithTag("Player").GetComponent<PlayerInteraction>();
            inventory = player.GetComponent<EquipmentInventory>();
            stats = player.GetComponent<PlayerStats>();
        }

        [UnityTest]
        public IEnumerator BrowsingKeepsFocusDoesNotSpendAndEscapeRestoresTheLobby()
        {
            Button background = Button("InventoryLauncher");
            background.Select();
            Object.FindAnyObjectByType<WeaponShopNpc>().Interact(player);
            yield return null;
            Assert.That(shop.IsOpen, Is.True);
            Assert.That(background.IsInteractable(), Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("TabWeapon"));
            shop.Open(player);
            Assert.That(Object.FindObjectsByType<EquipmentPanelLayout>().Length, Is.EqualTo(1));
            EquipmentData item = inventory.Catalog.First(x => x.Slot == EquipmentSlot.Weapon);
            Button card = Button(item.ItemId + "Card");
            card.Select();
            int gold = stats.Gold;
            ExecuteEvents.Execute(card.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(card.gameObject));
            Assert.That(card, Is.Not.Null, "Browsing must not destroy the focused card.");
            Assert.That(stats.Gold, Is.EqualTo(gold));
            Assert.That(inventory.Owns(item), Is.False);
            ExecuteEvents.Execute(card.gameObject, new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Right }, ExecuteEvents.moveHandler);
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(GameObject.Find("WeaponShopPanel").transform), Is.True);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(shop.IsOpen, Is.False);
                Assert.That(background.IsInteractable(), Is.True);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(background.gameObject));
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameObject.Find("PausePanel"), Is.Null);
                int beforeClosedAction = stats.Gold;
                typeof(WeaponShopUI).GetMethod("HandleItem", Private).Invoke(shop, new object[] { item });
                Assert.That(stats.Gold, Is.EqualTo(beforeClosedAction));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator PurchaseDiscountEquipsOnceAndPersistsOwnershipAndGold()
        {
            shop.Open(player);
            EquipmentData featured = inventory.Catalog.First(x => x.Slot == EquipmentSlot.Weapon && Offers.Contains(x.ItemId));
            stats.AddGold(featured.Price);
            Click(featured.ItemId + "Card");
            Button action = Button("Action");
            int expected = stats.Gold - WeaponShopUI.DiscountedPrice(featured.Price, 0.2f);
            action.Select();
            action.onClick.Invoke();
            action.onClick.Invoke();
            yield return null;
            Assert.That(stats.Gold, Is.EqualTo(expected));
            Assert.That(inventory.GetEquipped(featured.Slot), Is.EqualTo(featured));
            Assert.That(action.interactable, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.EqualTo(action.gameObject));
            Assert.That(action.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("ĐANG TRANG BỊ"));
            Assert.That(Progress.LoadProgress(), Is.True);
            Assert.That(Progress.Gold, Is.EqualTo(expected));
            Assert.That(Progress.HasItem(featured.ItemId), Is.True);
            Assert.That(Progress.GetEquippedItemId(featured.Slot), Is.EqualTo(featured.ItemId));
            shop.Close();
            inventory.Unequip(featured.Slot, out _);
            shop.Open(player);
            Click(featured.ItemId + "Card");
            Assert.That(Button("Action").GetComponentInChildren<TMP_Text>().text, Is.EqualTo("TRANG BỊ"));
            Click("Action");
            Assert.That(stats.Gold, Is.EqualTo(expected), "Equipping an owned item is free.");
            stats.TrySpendGold(stats.Gold);
            EquipmentData expensive = inventory.Catalog.Last(x => x.Slot == EquipmentSlot.Weapon && !inventory.Owns(x));
            Click(expensive.ItemId + "Card");
            Assert.That(Button("Action").interactable, Is.False);
            Assert.That(Button("Action").GetComponentInChildren<TMP_Text>().text, Does.StartWith("THIẾU "));
            Click("Action");
            Assert.That(stats.Gold, Is.Zero);
            Assert.That(inventory.Owns(expensive), Is.False);
        }

        [UnityTest]
        public IEnumerator PaidRerollChangesSevenCandidateOffersAndSavesCostWithoutChargingForNoAlternative()
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                Progress.AddUniqueItem(inventory.Catalog.First(x => x.Slot == slot).ItemId);
            shop.Open(player);
            var previous = new HashSet<string>(Offers);
            int before = stats.Gold;
            Assert.That(Button("RerollOffers").interactable, Is.True);
            Click("RerollOffers");
            Assert.That(stats.Gold, Is.EqualTo(before - 100));
            Assert.That(Offers.Overlaps(previous), Is.False, "Every group with seven alternatives must change, rather than repeat modulo seven.");
            Assert.That(Progress.LoadProgress(), Is.True);
            Assert.That(Progress.Gold, Is.EqualTo(before - 100), "The reroll fee is saved immediately.");
            shop.Close();
            foreach (var group in inventory.Catalog.GroupBy(x => x.Slot))
                foreach (EquipmentData item in group.Skip(2)) Progress.AddUniqueItem(item.ItemId);
            shop.Open(player);
            Assert.That(Button("RerollOffers").interactable, Is.False);
            int noAlternativeGold = stats.Gold;
            Click("RerollOffers");
            Assert.That(stats.Gold, Is.EqualTo(noAlternativeGold));
            yield return null;
            shop.enabled = false;
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ShopStatesRenderWithoutOverflowAndKeyboardSelectionStaysVisible()
        {
            shop.Open(player);
            Camera camera = Camera.main;
            Canvas hud = shop.GetComponentInParent<Canvas>();
            RenderMode oldMode = hud.renderMode;
            Camera oldCamera = hud.worldCamera;
            int oldSorting = hud.sortingOrder;
            RenderTexture target = null;
            try
            {
                hud.renderMode = RenderMode.ScreenSpaceCamera;
                hud.worldCamera = camera;
                hud.planeDistance = 1;
                hud.sortingOrder = 30000;
                foreach (string state in new[] { "UuDai", "DaMua", "ThieuVang" })
                {
                    if (state == "DaMua")
                    {
                        EquipmentData item = inventory.Catalog.First(x => x.Slot == EquipmentSlot.Weapon && Offers.Contains(x.ItemId));
                        stats.AddGold(item.Price);
                        Click(item.ItemId + "Card");
                        Click("Action");
                    }
                    if (state == "ThieuVang")
                    {
                        stats.TrySpendGold(stats.Gold);
                        EquipmentData item = inventory.Catalog.Last(x => x.Slot == EquipmentSlot.Weapon && !inventory.Owns(x));
                        Click(item.ItemId + "Card");
                    }
                    foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                    {
                        target = new RenderTexture(size.x, size.y, 24);
                        camera.targetTexture = target;
                        yield return null;
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        Transform panel = GameObject.Find("WeaponShopPanel").transform;
                        Transform window = panel.Find("Surface/EquipmentWindow");
                        bool compact = size.x / (float)size.y < 1.5f;
                        Assert.That(window.Find("Merchant").gameObject.activeSelf, Is.EqualTo(!compact));
                        Assert.That(((RectTransform)window).rect.width, Is.EqualTo(compact ? 1336 : 1712));
                        foreach (Button button in panel.GetComponentsInChildren<Button>())
                        {
                            var corners = new Vector3[4];
                            ((RectTransform)button.transform).GetWorldCorners(corners);
                            foreach (Vector3 corner in corners)
                            {
                                Vector3 point = panel.InverseTransformPoint(corner);
                                Assert.That(point.x, Is.InRange(((RectTransform)panel).rect.xMin, ((RectTransform)panel).rect.xMax), button.name);
                                Assert.That(point.y, Is.InRange(((RectTransform)panel).rect.yMin, ((RectTransform)panel).rect.yMax), button.name);
                            }
                        }
                        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
                        {
                            text.ForceMeshUpdate();
                            Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
                        }
                        Capture(camera, target, "LoRenUX_" + state + "_" + size.x + "x" + size.y);
                        camera.targetTexture = null;
                        target.Release();
                        Object.Destroy(target);
                        target = null;
                    }
                }
                target = new RenderTexture(1024, 768, 24);
                camera.targetTexture = target;
                foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                {
                    Click("Tab" + slot);
                    yield return null;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    foreach (EquipmentData item in inventory.Catalog.Where(x => x.Slot == slot))
                    {
                        Click(item.ItemId + "Card");
                        foreach (TMP_Text text in GameObject.Find("WeaponShopPanel").GetComponentsInChildren<TMP_Text>())
                        {
                            text.ForceMeshUpdate();
                            Assert.That(text.isTextOverflowing, Is.False, slot + " / " + text.name + ": " + text.text);
                        }
                    }
                    Capture(camera, target, "LoRenUX_Nhom" + slot + "_1024x768");
                }
                Click("TabWeapon");
                yield return null;
                yield return null;
                ScrollRect scroll = GameObject.Find("CatalogViewport").GetComponent<ScrollRect>();
                scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 260);
                Canvas.ForceUpdateCanvases();
                Button last = scroll.content.GetComponentsInChildren<Button>().Last();
                last.Select();
                yield return null;
                Assert.That(scroll.content.anchoredPosition.y, Is.GreaterThan(0));
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, last.transform);
                Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin - 1));
                Assert.That(bounds.max.y, Is.LessThanOrEqualTo(scroll.viewport.rect.yMax + 1));
            }
            finally
            {
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
            if (shop != null) shop.Close();
            Scene scene = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("ShopFlowClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if (originalState != null) JsonUtility.FromJsonOverwrite(originalState, Progress);
            if (existed) PlayerPrefs.SetString(SaveKey, original); else PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
