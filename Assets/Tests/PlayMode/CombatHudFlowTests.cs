using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using TopDownGame.Core;
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
    /// <summary>Checks live combat telemetry, skill states and readable layouts against generated scenes with isolated progress.</summary>
    public sealed class CombatHudFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private string original, originalState;
        private bool existed;
        private GameObject owner;
        private Canvas canvas;
        private Transform root;
        private PlayerStats stats;
        private PlayerHealth health;
        private SkillManager skills;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private TMP_Text Text(string path) => root.Find(path).GetComponent<TMP_Text>();
        private Slider Bar(string name) => root.Find("PlayerStatus/" + name).GetComponent<Slider>();

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("CombatHudProgressProbe").AddComponent<GameProgressManager>();
            original = PlayerPrefs.GetString(SaveKey, "");
            existed = PlayerPrefs.HasKey(SaveKey);
            originalState = JsonUtility.ToJson(Progress);
            Progress.BeginNewGame();
            Progress.CompleteTutorial();
            owner = new GameObject("CombatHudModalProbe");
            yield return Load("Map01");
        }

        private IEnumerator Load(string scene)
        {
            if (owner != null) GameplayState.ExitModal(owner);
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            yield return null;
            owner = new GameObject("CombatHudModalProbe");
            GameObject player = GameObject.FindWithTag("Player");
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            stats = player.GetComponent<PlayerStats>();
            health = player.GetComponent<PlayerHealth>();
            skills = player.GetComponent<SkillManager>();
            float until = Time.realtimeSinceStartup + 10;
            while ((skills.IsCasting || player.GetComponent<PlayerCombat>().IsAttacking ||
                Enumerable.Range(0,4).Any(i => skills.GetCooldownRemaining(i) > 0f)) && Time.realtimeSinceStartup < until)
                yield return null;
            Assert.That(Enumerable.Range(0,4).All(i => skills.GetCooldownRemaining(i) == 0f), Is.True, "Initial auto-cast must finish before ready-state captures.");
            canvas = Object.FindAnyObjectByType<GameplayHUD>().GetComponentInParent<Canvas>();
            root = canvas.transform.Find("CombatHUDRoot");
            Assert.That(root, Is.Not.Null);
            GameplayState.EnterModal(owner);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResourceEventsUpdateNumbersBarsLevelAndCurrencyWithoutChangingSaveSchema()
        {
            health.LoadProgress(12345);
            Assert.That(stats.SpendMana(stats.CurrentMana - 5432), Is.True);
            stats.AddGold(4321);
            Progress.AddHunterEssence(17);
            stats.AddExperience(10);
            yield return null;
            Assert.That(Text("PlayerStatus/HealthBar/Value").text, Does.Contain(12345.ToString("N0")));
            Assert.That(Text("PlayerStatus/ManaBar/Value").text, Does.Contain(5432.ToString("N0")));
            Assert.That(Bar("HealthBar").value, Is.EqualTo(health.CurrentHealth));
            Assert.That(Bar("HealthBar").maxValue, Is.EqualTo(stats.MaxHealth));
            Assert.That(Bar("ManaBar").value, Is.EqualTo(stats.CurrentMana));
            Assert.That(Bar("ExperienceBar").value, Is.EqualTo(stats.Experience));
            Assert.That(Text("PlayerStatus/Gold").text, Does.Contain(stats.Gold.ToString("N0")).And.Contain("17 tinh hoa"));
            Assert.That(Text("PlayerStatus/Level").text, Does.Contain("Cấp 1").And.Contain("10 / 100"));
            health.Heal(stats.MaxHealth);
            stats.RestoreMana(stats.MaxMana);
            stats.AddExperience(stats.ExperienceToNextLevel);
            yield return null;
            Assert.That(Text("PlayerStatus/Level").text, Does.Contain("Cấp 2"));
            Assert.That(Bar("HealthBar").value, Is.EqualTo(stats.MaxHealth));
            Assert.That(Bar("ManaBar").value, Is.EqualTo(stats.MaxMana));
        }

        [UnityTest]
        public IEnumerator SkillSlotsRefreshNamesEmptyManaCooldownAndDeathWithoutRebuildingCards()
        {
            Transform[] cards = Enumerable.Range(0,4).Select(i => root.Find("SkillStrip/Skill" + i + "Box")).ToArray();
            SkillData data = skills.GetSkillData(0);
            Assert.That(data, Is.Not.Null);
            skills.Inventory.RemoveFromSlot(3);
            yield return null;
            Assert.That(Text("SkillStrip/Skill3Box/Name").text, Is.EqualTo("Ô trống"));
            Assert.That(Text("SkillStrip/Skill3Box/State").text, Does.Contain("K:"));
            Assert.That(root.Find("SkillStrip/Skill3Box/Icon").GetComponent<Image>().enabled, Is.False);
            stats.SpendMana(stats.CurrentMana);
            yield return null;
            Assert.That(Text("SkillStrip/Skill0Box/State").text, Is.EqualTo("THIẾU MANA"));
            stats.RestoreMana(stats.MaxMana);
            skills.Cooldowns.Start(data.Id, 0.5f,1);
            yield return null;
            Assert.That(Text("SkillStrip/Skill0Box/State").text, Does.StartWith("HỒI "));
            float remaining = skills.GetCooldownRemaining(0);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(skills.GetCooldownRemaining(0), Is.EqualTo(remaining).Within(0.001f));
            GameplayState.ExitModal(owner);
            yield return new WaitForSeconds(0.65f);
            GameplayState.EnterModal(owner);
            yield return null;
            Assert.That(Text("SkillStrip/Skill0Box/State").text, Is.EqualTo("SẴN SÀNG"));
            for (int i=0;i<4;i++) Assert.That(root.Find("SkillStrip/Skill" + i + "Box"), Is.SameAs(cards[i]));
            health.TakeStatusDamage(stats.MaxHealth * 2);
            yield return null;
            Assert.That(Text("SkillStrip/Skill0Box/State").text, Is.EqualTo("ĐÃ GỤC"));
        }

        [UnityTest]
        public IEnumerator DisplayGraphicsDoNotInterceptWorldClicksAndModalStillOwnsFocus()
        {
            GameplayState.ExitModal(owner);
            yield return null;
            foreach (string path in new[] { "PlayerStatus", "SkillStrip/Skill0Box", "ControlHints" })
            {
                RectTransform rect = (RectTransform)root.Find(path);
                var point = RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, results);
                Assert.That(results.Any(x => x.gameObject.transform.IsChildOf(root)), Is.False, path);
            }
            var bag = Object.FindAnyObjectByType<InventoryUI>();
            bag.Open();
            yield return null;
            Assert.That(GameplayState.IsInputBlocked, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject.transform.IsChildOf(root), Is.False);
            bag.Close();
            yield return null;
            Assert.That(GameplayState.IsInputBlocked, Is.False);
        }

        [UnityTest]
        public IEnumerator CombatPanelsRenderAcrossSixMapsAndThreeAspectRatiosWithReadableBounds()
        {
            foreach (string scene in new[] { "Map01","Map02","Map03","BossMap01","CaveMap01","RiftMap01" })
            {
                if (SceneManager.GetActiveScene().name != scene) yield return Load(scene);
                Assert.That(Text("PlayerStatus/Map").text, Is.EqualTo(MapManager.Instance.Data.DisplayName));
                Assert.That(Text("PlayerStatus/RecommendedLevel").text, Does.Contain(MapManager.Instance.Data.RecommendedMinLevel.ToString()));
                yield return Render("HUDChienDau_" + scene + "_SanSang");
            }
            yield return Load("Map01");
            stats.SpendMana(stats.CurrentMana);
            health.LoadProgress(stats.MaxHealth * 0.21f);
            skills.Inventory.RemoveFromSlot(3);
            yield return null;
            yield return Render("HUDChienDau_Map01_ThieuMana");
            stats.RestoreMana(stats.MaxMana);
            skills.Cooldowns.Start(skills.GetSkillData(0).Id,3,1);
            yield return null;
            yield return Render("HUDChienDau_Map01_HoiChieu");
            for (int i=0;i<4;i++) skills.Inventory.RemoveFromSlot(i);
            int index = 0;
            foreach (SkillId id in System.Enum.GetValues(typeof(SkillId)))
            {
                skills.Inventory.AddOrUpgrade(id.ToString());
                int slot = index % 4;
                Assert.That(skills.Inventory.TryEquip(id.ToString(),slot), Is.True);
                yield return null;
                TMP_Text name = Text("SkillStrip/Skill" + slot + "Box/Name");
                name.ForceMeshUpdate();
                Assert.That(name.isTextOverflowing, Is.False, id.ToString());
                if (index == 7 || index == 8) yield return Render("HUDChienDau_Map01_Loadout" + (index == 7 ? "2" : "3"));
                index++;
            }
        }

        private IEnumerator Render(string name)
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
                foreach (Vector2Int size in new[] { new Vector2Int(1920,1080),new Vector2Int(1280,800),new Vector2Int(1024,768) })
                {
                    target = new RenderTexture(size.x,size.y,24);
                    camera.targetTexture = target;
                    yield return null;
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>())
                    {
                        text.ForceMeshUpdate();
                        Assert.That(text.isTextOverflowing, Is.False,text.name + ": " + text.text);
                        Assert.That(text.fontSize * size.x / ((RectTransform)canvas.transform).rect.width, Is.GreaterThanOrEqualTo(12f), text.name + " physical font size");
                    }
                    foreach (string path in new[] { "PlayerStatus","SkillStrip","ControlHints","BossStatus" })
                    {
                        RectTransform rect = (RectTransform)root.Find(path);
                        if (!rect.gameObject.activeInHierarchy) continue;
                        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                        foreach (Vector3 point in corners)
                            Assert.That(((RectTransform)canvas.transform).rect.Contains(canvas.transform.InverseTransformPoint(point)), Is.True,path);
                    }
                    RectTransform strip = (RectTransform)root.Find("SkillStrip");
                    Assert.That(strip.rect.width, Is.LessThan(((RectTransform)canvas.transform).rect.width * 0.45f));
                    Assert.That(strip.rect.height, Is.LessThan(((RectTransform)canvas.transform).rect.height * 0.2f));
                    var panels = new List<RectTransform> {
                        (RectTransform)root.Find("PlayerStatus"), strip, (RectTransform)root.Find("ControlHints"),
                        (RectTransform)canvas.transform.Find("DodgeCard"), canvas.GetComponent<MiniMapController>().Panel
                    };
                    if (root.Find("BossStatus").gameObject.activeInHierarchy) panels.Add((RectTransform)root.Find("BossStatus"));
                    for (int a=0;a<panels.Count;a++)
                        for (int b=a+1;b<panels.Count;b++)
                            Assert.That(Bounds(panels[a]).Overlaps(Bounds(panels[b])), Is.False, panels[a].name + " overlaps " + panels[b].name);
                    Capture(camera,target,name + "_" + size.x + "x" + size.y);
                    camera.targetTexture = null; target.Release(); Object.Destroy(target); target=null;
                }
            }
            finally
            {
                camera.targetTexture=null;
                canvas.renderMode=mode; canvas.worldCamera=oldCamera; canvas.sortingOrder=sorting;
                if (target!=null) {target.Release();Object.Destroy(target);}
            }
        }

        private Rect Bounds(RectTransform rect)
        {
            var points = new Vector3[4]; rect.GetWorldCorners(points);
            Vector3 min = canvas.transform.InverseTransformPoint(points[0]);
            Vector3 max = canvas.transform.InverseTransformPoint(points[2]);
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        private static void Capture(Camera camera, RenderTexture target, string name)
        {
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                image.Apply();
                Directory.CreateDirectory("Assets/Screenshots");
                File.WriteAllBytes("Assets/Screenshots/" + name + "_Verified.png",image.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;Object.Destroy(image);}
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (owner!=null) GameplayState.ExitModal(owner);
            Scene scene=SceneManager.GetActiveScene();
            Scene clean=SceneManager.CreateScene("CombatHudClean");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(scene);
            if(originalState!=null) JsonUtility.FromJsonOverwrite(originalState,Progress);
            if(existed) PlayerPrefs.SetString(SaveKey,original); else PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
