using System.Collections;
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
    /// <summary>Verifies equipment browsing and essence transactions through the generated Lobby with isolated saves.</summary>
    public sealed class EquipmentFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string original, originalState;
        private bool existed;
        private InventoryUI bag;
        private EquipmentEnhancementUI forge;
        private EquipmentInventory inventory;
        private PlayerStats stats;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private Transform BagPanel => GameObject.Find("CharacterEquipmentPanel").transform;
        private Transform ForgePanel => GameObject.Find("EquipmentEnhancementPanel").transform;
        private T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        private void Click(string name) => Button(name).onClick.Invoke();
        private EquipmentData Item(EquipmentSlot slot) => inventory.Catalog.First(x => x.Slot == slot);
        private void Own(EquipmentData item) => Progress.AddUniqueItem(item.ItemId);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("EquipmentFlowProgressProbe").AddComponent<GameProgressManager>();
            original = PlayerPrefs.GetString(SaveKey, "");
            existed = PlayerPrefs.HasKey(SaveKey);
            originalState = JsonUtility.ToJson(Progress);
            Progress.BeginNewGame();
            Progress.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            yield return null;
            bag = Object.FindAnyObjectByType<InventoryUI>();
            forge = Object.FindAnyObjectByType<EquipmentEnhancementUI>();
            inventory = GameObject.FindWithTag("Player").GetComponent<EquipmentInventory>();
            stats = inventory.GetComponent<PlayerStats>();
        }

        [UnityTest]
        public IEnumerator BagKeepsSelectedSlotsAcrossBrowsingSortAndFiltersAndReturnsFocusOnEscape()
        {
            foreach (EquipmentData item in inventory.Catalog) Own(item);
            Button background = Button("PotentialLauncher");
            background.Select();
            bag.Open();
            yield return null;
            forge.Open();
            Assert.That(forge.IsOpen, Is.False);
            Assert.That(background.IsInteractable(), Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Filter0"));
            bag.Open();
            Transform items = Field<Transform>(bag, "itemsRoot");
            EquipmentData weapon = Item(EquipmentSlot.Weapon);
            Button itemButton = items.Find(weapon.ItemId + "Slot").GetComponent<Button>();
            itemButton.Select();
            ExecuteEvents.Execute(itemButton.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(itemButton.gameObject));
            Assert.That(Field<EquipmentData>(bag, "selectedItem"), Is.EqualTo(weapon));
            ScrollRect scroll = items.GetComponentInParent<ScrollRect>();
            scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 260);
            Click("Sort");
            yield return null;
            Assert.That(items.Find(weapon.ItemId + "Slot").GetComponent<Button>(), Is.EqualTo(itemButton));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(itemButton.gameObject));
            Bounds selectedBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, itemButton.transform);
            Assert.That(selectedBounds.min.y, Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin - 1));
            Assert.That(selectedBounds.max.y, Is.LessThanOrEqualTo(scroll.viewport.rect.yMax + 1));
            Click("Filter3");
            yield return null;
            Assert.That(Field<EquipmentData>(bag, "selectedItem").Slot, Is.EqualTo(EquipmentSlot.Necklace));
            Assert.That(itemButton.gameObject.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Filter3"));
            Click("Filter0");
            inventory.Equip(weapon);
            Button worn = GameObject.Find("EquipmentWeapon").GetComponent<Button>();
            worn.Select();
            worn.onClick.Invoke();
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(worn.gameObject));
            Click("Action");
            Assert.That(inventory.GetEquipped(weapon.Slot), Is.Null);
            Assert.That(worn.interactable, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.activeInHierarchy, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().IsInteractable(), Is.True);
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(bag.IsOpen, Is.False);
                Assert.That(background.IsInteractable(), Is.True);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(background.gameObject));
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameObject.Find("PausePanel"), Is.Null);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator BagEquipUnequipPersistsAndEmptyFiltersClearDetailsWithoutAcceptingClosedActions()
        {
            EquipmentData weapon = Item(EquipmentSlot.Weapon);
            Own(weapon);
            bag.Open();
            Button action = Button("Action");
            int gold = stats.Gold;
            action.onClick.Invoke();
            Assert.That(inventory.GetEquipped(weapon.Slot), Is.EqualTo(weapon));
            Assert.That(Progress.LoadProgress(), Is.True);
            Assert.That(Progress.GetEquippedItemId(weapon.Slot), Is.EqualTo(weapon.ItemId));
            action.onClick.Invoke();
            Assert.That(inventory.GetEquipped(weapon.Slot), Is.Null);
            Assert.That(Progress.LoadProgress(), Is.True);
            Assert.That(Progress.GetEquippedItemId(weapon.Slot), Is.Empty);
            Click("Filter3");
            Assert.That(Field<EquipmentData>(bag, "selectedItem"), Is.Null);
            Assert.That(action.interactable, Is.False);
            Assert.That(BagPanel.Find("Surface/EquipmentWindow/Bag/Empty").GetComponent<TMP_Text>().text, Does.Contain("Không có đồ"));
            bag.Close();
            bag.Open();
            Assert.That(Field<EquipmentData>(bag, "selectedItem"), Is.Null);
            Assert.That(action.interactable, Is.False);
            Click("Filter0");
            bag.Close();
            action.onClick.Invoke();
            Assert.That(inventory.GetEquipped(weapon.Slot), Is.Null);
            Assert.That(stats.Gold, Is.EqualTo(gold));
            PlayerMovement movement = inventory.GetComponent<PlayerMovement>();
            movement.enabled = false;
            bag.Open();
            bag.enabled = false;
            Assert.That(movement.enabled, Is.False);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            bag.Open();
            Assert.That(bag.IsOpen, Is.False, "Disabled controllers cannot reopen orphan panels.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnhancementSlotsWorkByMouseAndNumberKeysAndModalCleanupRestoresFocus()
        {
            Button background = Button("InventoryLauncher");
            background.Select();
            forge.Open();
            forge.Open();
            bag.Open();
            Assert.That(bag.IsOpen, Is.False);
            Assert.That(background.IsInteractable(), Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("EnhancementSlotWeapon"));
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                Click("EnhancementSlot" + slot);
                Assert.That(Field<int>(forge, "selectedSlot"), Is.EqualTo((int)slot));
                Assert.That(Button("Enhance").interactable, Is.False);
            }
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit2));
                yield return null;
                yield return null;
                Assert.That(Field<int>(forge, "selectedSlot"), Is.EqualTo(1));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(forge.IsOpen, Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(background.gameObject));
                Assert.That(background.IsInteractable(), Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameObject.Find("PausePanel"), Is.Null);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            int essence = Progress.HunterEssence;
            forge.Open();
            Click("OpenInventory");
            Assert.That(forge.IsOpen, Is.False);
            Assert.That(bag.IsOpen, Is.True);
            Assert.That(Progress.HunterEssence, Is.EqualTo(essence));
            Assert.That(Time.timeScale, Is.Zero);
            bag.Close();
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator EnhancementPreviewMatchesActualStatsChargesOncePerFrameAndSavesResult()
        {
            EquipmentData weapon = Item(EquipmentSlot.Weapon);
            Own(weapon);
            inventory.Equip(weapon);
            int firstCost = GameProgressManager.EnhancementEssenceCost(0);
            int secondCost = GameProgressManager.EnhancementEssenceCost(1);
            Progress.AddHunterEssence(firstCost + secondCost);
            forge.Open();
            string preview = Field<TMP_Text>(forge, "preview").text;
            Assert.That(preview, Does.Contain((weapon.BonusDamage * EquipmentInventory.EnhancementMultiplier(1)).ToString("0.#")));
            Assert.That(Field<TMP_Text>(forge, "costText").text, Does.Contain("CHI PHÍ: " + firstCost));
            Button enhance = Button("Enhance");
            enhance.Select();
            enhance.onClick.Invoke();
            enhance.onClick.Invoke();
            Assert.That(inventory.GetEnhancementLevel(weapon), Is.EqualTo(1));
            Assert.That(Progress.HunterEssence, Is.EqualTo(secondCost));
            Assert.That(stats.BaseDamage, Is.GreaterThanOrEqualTo(weapon.BonusDamage * 1.08f));
            Assert.That(Progress.LoadProgress(), Is.True);
            Assert.That(Progress.HunterEssence, Is.EqualTo(secondCost));
            Assert.That(Progress.GetEnhancementLevel(weapon.ItemId), Is.EqualTo(1));
            yield return null;
            enhance.onClick.Invoke();
            Assert.That(inventory.GetEnhancementLevel(weapon), Is.EqualTo(2));
            Assert.That(Progress.HunterEssence, Is.Zero);
            Assert.That(enhance.interactable, Is.False);
            Assert.That(Field<TMP_Text>(forge, "status").text, Does.Contain("lên +2"));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("EnhancementSlotWeapon"));
            yield return null;
            enhance.onClick.Invoke();
            Assert.That(inventory.GetEnhancementLevel(weapon), Is.EqualTo(2));
            forge.Close();
            Progress.AddHunterEssence(50000);
            int beforeClosed = Progress.HunterEssence;
            enhance.onClick.Invoke();
            Assert.That(Progress.HunterEssence, Is.EqualTo(beforeClosed));
            Assert.That(inventory.GetEnhancementLevel(weapon), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator EnhancementCapsAtTwentyAndDisableDestroyLeaveNoModalOrOrphanPanels()
        {
            EquipmentData weapon = Item(EquipmentSlot.Weapon);
            Own(weapon);
            inventory.Equip(weapon);
            Progress.AddHunterEssence(50000);
            for (int i = 0; i < EquipmentInventory.MaximumEnhancementLevel; i++)
                Assert.That(Progress.TryEnhanceEquipment(weapon.ItemId, EquipmentInventory.MaximumEnhancementLevel, out _), Is.True);
            forge.Open();
            Assert.That(Button("Enhance").interactable, Is.False);
            int essence = Progress.HunterEssence;
            Click("Enhance");
            Assert.That(Progress.HunterEssence, Is.EqualTo(essence));
            Assert.That(Field<TMP_Text>(forge, "levelText").text, Does.Contain("ĐÃ TỐI ĐA"));
            Assert.That(Field<TMP_Text>(forge, "columns").text, Does.EndWith("TỐI ĐA"));
            forge.enabled = false;
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            forge.Open();
            Assert.That(forge.IsOpen, Is.False);
            Progress.AddHunterEssence(123);
            forge.enabled = true;
            Assert.That(Button("ForgeLauncher").GetComponentInChildren<TMP_Text>().text, Does.Contain((essence + 123).ToString()));
            forge.Open();
            Object.Destroy(forge);
            yield return null;
            yield return null;
            Assert.That(GameObject.Find("EquipmentEnhancementPanel"), Is.Null);
            Assert.That(GameObject.Find("ForgeLauncher"), Is.Null);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BagAndEnhancementRenderEmptyOwnedAndMaximumStatesAcrossAspectRatios()
        {
            Camera camera = Camera.main;
            Canvas hud = bag.GetComponentInParent<Canvas>();
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
                foreach (string state in new[] { "Trong", "DayDu" })
                {
                    if (state == "DayDu")
                    {
                        foreach (EquipmentData item in inventory.Catalog) Own(item);
                        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot))) inventory.Equip(Item(slot));
                    }
                    bag.Open();
                    foreach (Vector2Int size in Sizes())
                    {
                        target = new RenderTexture(size.x, size.y, 24);
                        camera.targetTexture = target;
                        yield return null;
                        yield return null;
                        AssertReadable(BagPanel);
                        Capture(camera, target, "HanhTrangUX_" + state + "_" + size.x + "x" + size.y);
                        if (size.x / (float)size.y < 1.5f)
                        {
                            Click("CharacterView");
                            yield return null;
                            Assert.That(BagPanel.Find("Surface/EquipmentWindow/Character").gameObject.activeSelf, Is.True);
                            AssertReadable(BagPanel);
                            Capture(camera, target, "HanhTrangUX_NhanVat_" + state + "_" + size.x + "x" + size.y);
                            Click("BagView");
                            yield return null;
                        }
                        camera.targetTexture = null;
                        target.Release();
                        Object.Destroy(target);
                        target = null;
                    }
                    bag.Close();
                }
                // Reuse the owned equipment to exercise missing essence, success and maximum UI states.
                foreach (string state in new[] { "Trong", "ThieuTinhHoa", "SanSang", "ThanhCong", "ToiDa" })
                {
                    if (state == "Trong")
                        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot))) inventory.Unequip(slot, out _);
                    if (state == "ThieuTinhHoa")
                        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot))) inventory.Equip(Item(slot));
                    if (state == "SanSang") Progress.AddHunterEssence(50000);
                    if (state == "ToiDa")
                    {
                        EquipmentData weapon = Item(EquipmentSlot.Weapon);
                        while (Progress.GetEnhancementLevel(weapon.ItemId) < EquipmentInventory.MaximumEnhancementLevel)
                            Assert.That(Progress.TryEnhanceEquipment(weapon.ItemId, EquipmentInventory.MaximumEnhancementLevel, out _), Is.True);
                    }
                    forge.Open();
                    if (state == "ThanhCong") { yield return null; Click("Enhance"); }
                    foreach (Vector2Int size in Sizes())
                    {
                        target = new RenderTexture(size.x, size.y, 24);
                        camera.targetTexture = target;
                        yield return null;
                        yield return null;
                        AssertReadable(ForgePanel);
                        Capture(camera, target, "CuongHoaUX_" + state + "_" + size.x + "x" + size.y);
                        camera.targetTexture = null;
                        target.Release();
                        Object.Destroy(target);
                        target = null;
                    }
                    forge.Close();
                }
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

        private static Vector2Int[] Sizes() => new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) };

        private static void AssertReadable(Transform panel)
        {
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>())
            {
                text.ForceMeshUpdate();
                Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text);
            }
            foreach (Button button in panel.GetComponentsInChildren<Button>())
            {
                // Cards may be clipped by their scroll viewport; the surrounding controls must fit the screen.
                if (button.GetComponentInParent<ScrollRect>() != null) continue;
                var corners = new Vector3[4];
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 point = panel.InverseTransformPoint(corner);
                    Assert.That(point.x, Is.InRange(((RectTransform)panel).rect.xMin - 1, ((RectTransform)panel).rect.xMax + 1), button.name);
                    Assert.That(point.y, Is.InRange(((RectTransform)panel).rect.yMin - 1, ((RectTransform)panel).rect.yMax + 1), button.name);
                }
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
            if (bag != null) bag.Close();
            if (forge != null) forge.Close();
            Scene scene = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("EquipmentFlowClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if (originalState != null) JsonUtility.FromJsonOverwrite(originalState, Progress);
            if (existed) PlayerPrefs.SetString(SaveKey, original); else PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
