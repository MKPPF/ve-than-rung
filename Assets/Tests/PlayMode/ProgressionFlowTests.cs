using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TopDownGame.Core;
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
    /// <summary>Checks progression menu focus, spending and save behavior in the generated Lobby using isolated progress.</summary>
    public sealed class ProgressionFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string original, originalState;
        private bool existed;
        private SkillEquipmentUI skills;
        private PotentialUpgradeUI potential;
        private SkillInventory inventory;
        private SkillManager manager;
        private PlayerStats stats;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
        private static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
        private static void Click(string name) => Button(name).onClick.Invoke();
        private Transform SkillPanel => GameObject.Find("SkillEquipmentPanel").transform;
        private Transform PotentialPanel => GameObject.Find("PotentialPanel").transform;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("ProgressionFlowProbe").AddComponent<GameProgressManager>();
            original = PlayerPrefs.GetString(SaveKey, "");
            existed = PlayerPrefs.HasKey(SaveKey);
            originalState = JsonUtility.ToJson(Progress);
            Progress.BeginNewGame();
            Progress.CompleteTutorial();
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
            yield return null;
            skills = Object.FindAnyObjectByType<SkillEquipmentUI>();
            potential = Object.FindAnyObjectByType<PotentialUpgradeUI>();
            var player = GameObject.FindWithTag("Player");
            inventory = player.GetComponent<SkillInventory>();
            manager = player.GetComponent<SkillManager>();
            stats = player.GetComponent<PlayerStats>();
        }

        private void LoadSkills(int level = 1, bool empty = false)
        {
            var owned = new List<GameProgressManager.SkillProgress>();
            if (!empty)
                foreach (SkillId id in System.Enum.GetValues(typeof(SkillId)))
                    owned.Add(new GameProgressManager.SkillProgress { skillId = id.ToString(), level = level });
            inventory.LoadProgress(owned, new string[4]);
        }

        [UnityTest]
        public IEnumerator SkillNavigationRetainsCardsAndEscapeRestoresBackgroundFocus()
        {
            LoadSkills();
            Button background = Button("InventoryLauncher");
            background.Select();
            skills.Open();
            potential.Open();
            Assert.That(potential.IsOpen, Is.False);
            Assert.That(background.IsInteractable(), Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Does.StartWith("Skill"));
            var cards = Field<List<Button>>(skills, "skillButtons").ToArray();
            Button card = cards.Last();
            card.Select();
            ExecuteEvents.Execute(card.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return null;
            Assert.That(inventory.Slots.All(slot => slot.IsEmpty), Is.True, "Submit on a library card only selects it.");
            int index = Field<int>(skills, "selectedSkill");
            string id = Field<List<string>>(skills, "owned")[index];
            inventory.TryUpgrade(id, manager.GetSkillData(id).MaxLevel);
            yield return null;
            Assert.That(Field<List<Button>>(skills, "skillButtons")[index], Is.EqualTo(card));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(card.gameObject));
            ScrollRect scroll = card.GetComponentInParent<ScrollRect>();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, card.transform);
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(scroll.viewport.rect.yMin - 1));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(scroll.viewport.rect.yMax + 1));
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Digit4));
                yield return null;
                yield return null;
                Assert.That(Field<int>(skills, "selectedSlot"), Is.EqualTo(3));
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Slot3"));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
                yield return null;
                yield return null;
                Assert.That(inventory.Slots.All(slot => slot.IsEmpty), Is.True, "Enter selects the focused slot without equipping a skill.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(skills.IsOpen, Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(background.gameObject));
                Assert.That(background.IsInteractable(), Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameObject.Find("PausePanel"), Is.Null);
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator SkillEquipRemoveAndUpgradePersistWithoutRepeatedSpendingOrLostFocus()
        {
            LoadSkills();
            stats.AddGold(10000);
            skills.Open();
            var owned = Field<List<string>>(skills, "owned");
            string id = owned[Field<int>(skills, "selectedSkill")];
            Click("Equip");
            Assert.That(inventory.Slots[0].skillId, Is.EqualTo(id));
            Click("Slot1");
            Assert.That(Button("Equip").interactable, Is.False);
            Click("Equip");
            Assert.That(inventory.Slots[1].IsEmpty, Is.True);
            Assert.That(Field<TMP_Text>(skills, "status").text, Does.Contain("ô khác"));
            Click("Slot0");
            Button remove = Button("Remove");
            remove.Select();
            remove.onClick.Invoke();
            Assert.That(inventory.Slots[0].IsEmpty, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().IsInteractable(), Is.True);
            Assert.That(Progress.LoadProgress(), Is.True);
            Assert.That(PlayerPrefs.GetString(SaveKey), Does.Contain("\"equippedSkills\":[\"\",\"\",\"\",\"\"]"));
            Button upgrade = Button("Upgrade");
            upgrade.Select();
            int gold = stats.Gold, level = inventory.GetLevel(id);
            upgrade.onClick.Invoke();
            upgrade.onClick.Invoke();
            Assert.That(inventory.GetLevel(id), Is.EqualTo(level + 1));
            Assert.That(stats.Gold, Is.EqualTo(gold - SkillInventory.UpgradeGoldCost(level)));
            Assert.That(Field<TMP_Text>(skills, "status").text, Does.Contain("Đã nâng"));
            Assert.That(PlayerPrefs.GetString(SaveKey), Does.Contain("\"skillId\":\"" + id + "\",\"level\":" + (level + 1)));
            yield return null;
            stats.TrySpendGold(stats.Gold);
            Assert.That(upgrade.interactable, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().IsInteractable(), Is.True);
            upgrade.onClick.Invoke();
            Assert.That(inventory.GetLevel(id), Is.EqualTo(level + 1));
            stats.AddGold(100000);
            while (inventory.GetLevel(id) < manager.GetSkillData(id).MaxLevel)
                inventory.TryUpgrade(id, manager.GetSkillData(id).MaxLevel);
            Assert.That(upgrade.interactable, Is.False);
            Assert.That(upgrade.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("ĐÃ TỐI ĐA"));
            skills.Close();
            int maximum = inventory.GetLevel(id);
            upgrade.onClick.Invoke();
            ClickClosedEquip(skills);
            Assert.That(inventory.GetLevel(id), Is.EqualTo(maximum));
            Assert.That(inventory.Slots[0].IsEmpty, Is.True);
        }

        private void ClickClosedEquip(SkillEquipmentUI owner) => Field<Button>(owner, "equipButton").onClick.Invoke();

        [UnityTest]
        public IEnumerator PotentialSpendingUsesOnePointPerFrameAndKeepsFocusAfterLastPoint()
        {
            stats.LoadPotentialProgress(5,0,0,0,0);
            Button background = Button("SkillLauncher");
            background.Select();
            potential.Open();
            skills.Open();
            Assert.That(skills.IsOpen, Is.False);
            Assert.That(background.IsInteractable(), Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Spend0"));
            float health = stats.MaxHealth;
            Button spend = Button("Spend0");
            spend.onClick.Invoke();
            spend.onClick.Invoke();
            Click("Spend1");
            Assert.That(stats.AvailablePotentialPoints, Is.EqualTo(4));
            Assert.That(stats.VitalityPoints, Is.EqualTo(1));
            Assert.That(stats.PowerPoints, Is.Zero);
            Assert.That(stats.MaxHealth, Is.EqualTo(health + PlayerStats.VitalityHealthPerPoint));
            for (int i = 0; i < 4; i++) { yield return null; Click("Spend" + i); }
            Assert.That(stats.AvailablePotentialPoints, Is.Zero);
            Assert.That(new[] { stats.VitalityPoints, stats.PowerPoints, stats.GuardPoints, stats.HastePoints }, Is.EqualTo(new[] { 2,1,1,1 }));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Close"));
            Assert.That(Field<TMP_Text>(potential, "status").text, Does.Contain("Đã lưu"));
            Assert.That(Progress.LoadProgress(), Is.True);
            Assert.That(Progress.PotentialPoints, Is.Zero);
            yield return null;
            spend.onClick.Invoke();
            Assert.That(stats.VitalityPoints, Is.EqualTo(2));
            potential.Close();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(background.gameObject));
            stats.LoadPotentialProgress(1,2,1,1,1);
            yield return null;
            spend.onClick.Invoke();
            Assert.That(stats.AvailablePotentialPoints, Is.EqualTo(1), "Closed actions must not spend points.");
        }

        [UnityTest]
        public IEnumerator DisabledAndDestroyedProgressionControllersReleaseModalAndCleanUpPanels()
        {
            LoadSkills();
            skills.Open();
            skills.enabled = false;
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            skills.Open();
            Assert.That(skills.IsOpen, Is.False);
            stats.AddGold(1000);
            skills.enabled = true;
            skills.Open();
            Object.Destroy(skills);
            yield return null;
            yield return null;
            Assert.That(GameObject.Find("SkillEquipmentPanel"), Is.Null);
            Assert.That(GameObject.Find("SkillLauncher"), Is.Null);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            potential.Open();
            potential.enabled = false;
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            potential.Open();
            Assert.That(potential.IsOpen, Is.False);
            stats.LoadPotentialProgress(2,0,0,0,0);
            potential.enabled = true;
            Assert.That(Button("PotentialLauncher").GetComponentInChildren<TMP_Text>().text, Does.Contain("+2"));
            potential.Open();
            Assert.That(Button("Spend0").interactable, Is.True);
            Object.Destroy(potential);
            yield return null;
            yield return null;
            Assert.That(GameObject.Find("PotentialPanel"), Is.Null);
            Assert.That(GameObject.Find("PotentialLauncher"), Is.Null);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SkillAndPotentialStatesFitAndRenderAtThreeAspectRatios()
        {
            Camera camera = Camera.main;
            Canvas canvas = skills.GetComponentInParent<Canvas>();
            RenderMode oldMode = canvas.renderMode;
            Camera oldCamera = canvas.worldCamera;
            int oldSorting = canvas.sortingOrder;
            RenderTexture target = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.sortingOrder = 30000;
                foreach (string state in new[] { "Trong", "DayDu", "ToiDa" })
                {
                    LoadSkills(state == "ToiDa" ? 5 : 1, state == "Trong");
                    if (state != "Trong")
                    {
                        string[] ids = inventory.OwnedSkills.Keys.Take(4).ToArray();
                        for (int i = 0; i < ids.Length; i++) inventory.TryEquip(ids[i],i);
                    }
                    skills.Open();
                    if (state == "Trong") Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("Close"));
                    foreach (Vector2Int size in Sizes())
                    {
                        target = new RenderTexture(size.x,size.y,24);
                        camera.targetTexture = target;
                        yield return null;
                        yield return null;
                        if (state != "Trong")
                        {
                            foreach (Button card in Field<List<Button>>(skills,"skillButtons"))
                            {
                                card.onClick.Invoke();
                                AssertReadable(SkillPanel);
                            }
                            Field<List<Button>>(skills,"skillButtons")[0].onClick.Invoke();
                        }
                        AssertReadable(SkillPanel);
                        Capture(camera,target,"KyNangUX_" + state + "_" + size.x + "x" + size.y);
                        camera.targetTexture = null;
                        target.Release(); Object.Destroy(target); target = null;
                    }
                    skills.Close();
                }
                foreach (int points in new[] { 0, 12 })
                {
                    stats.LoadPotentialProgress(points,15,12,7,10);
                    potential.Open();
                    foreach (Vector2Int size in Sizes())
                    {
                        target = new RenderTexture(size.x,size.y,24);
                        camera.targetTexture = target;
                        yield return null;
                        yield return null;
                        AssertReadable(PotentialPanel);
                        Capture(camera,target,"TiemNangUX_" + points + "Diem_" + size.x + "x" + size.y);
                        camera.targetTexture = null;
                        target.Release(); Object.Destroy(target); target = null;
                    }
                    potential.Close();
                }
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.sortingOrder = oldSorting;
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
                if (button.GetComponentInParent<ScrollRect>() != null) continue;
                var corners = new Vector3[4];
                ((RectTransform)button.transform).GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 point = panel.InverseTransformPoint(corner);
                    Rect rect = ((RectTransform)panel).rect;
                    Assert.That(point.x, Is.InRange(rect.xMin - 1, rect.xMax + 1), button.name);
                    Assert.That(point.y, Is.InRange(rect.yMin - 1, rect.yMax + 1), button.name);
                }
            }
        }

        private static void Capture(Camera camera, RenderTexture target, string name)
        {
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                texture.Apply();
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/" + name + "_Verified.png",texture.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(texture); }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (skills != null) skills.Close();
            if (potential != null) potential.Close();
            Scene scene = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("ProgressionFlowClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if (originalState != null) JsonUtility.FromJsonOverwrite(originalState, Progress);
            if (existed) PlayerPrefs.SetString(SaveKey,original); else PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
