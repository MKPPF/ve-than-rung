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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Travels through generated exits with isolated progress and checks rejection, cancellation and readable destination guidance.</summary>
    public sealed class MapTransitionFlowTests
    {
        private const string SaveKey = "TopDownGame.Tests.Progress.v1";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string originalSave, originalState;
        private bool existed, originalApplied, originalPickup;
        private bool? originalAttack;
        private PlayerInteraction player;
        private GameProgressManager Progress => GameProgressManager.Instance;
        private MapDoor Door => Object.FindAnyObjectByType<MapDoor>();
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.That(GamePreferences.IsAutomatedTestRun, Is.True);
            if (Progress == null) new GameObject("MapTransitionProgressProbe").AddComponent<GameProgressManager>();
            existed = PlayerPrefs.HasKey(SaveKey);
            originalSave = PlayerPrefs.GetString(SaveKey, "");
            originalState = JsonUtility.ToJson(Progress);
            originalApplied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private).GetValue(Progress);
            originalAttack = PlayerAutomationState.AutoAttackOverride;
            originalPickup = PlayerAutomationState.AutoPickupEnabled;
            Progress.BeginNewGame(); Progress.CompleteTutorial();
            SceneTransition.ResetLoading();
            yield return SceneManager.LoadSceneAsync("Map01");
            yield return null; yield return null;
            Bind();
        }

        private void Bind()
        {
            player = GameObject.FindWithTag("Player").GetComponent<PlayerInteraction>();
            player.GetComponent<PlayerCombat>().SetAutoAttackEnabled(false);
            PlayerAutomationState.SetAutoPickupEnabled(false);
        }

        private void Approach()
        {
            player.GetComponent<PlayerMovement>().CancelMovement();
            player.GetComponent<Rigidbody2D>().position = Door.transform.position + Vector3.left * 0.7f;
            Physics2D.SyncTransforms();
        }

        private void AssertNoTravelOrSave(string saved)
        {
            Assert.That(SceneTransition.IsLoading, Is.False);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Map01"));
            Assert.That(Progress.IsRegionUnlocked("Map02"), Is.False);
            Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(saved));
        }

        [UnityTest]
        public IEnumerator InvalidDisabledLockedDistantModalAndDeadCallbacksHaveNoSideEffects()
        {
            MapDoor door = Door;
            string saved = PlayerPrefs.GetString(SaveKey);
            door.Interact(player); AssertNoTravelOrSave(saved); // Spawn is far from the exit.
            Approach();
            door.Interact(null); AssertNoTravelOrSave(saved);
            player.enabled = false; door.Interact(player); player.enabled = true; AssertNoTravelOrSave(saved);
            door.enabled = false; door.Interact(player); door.enabled = true; AssertNoTravelOrSave(saved);
            door.GetComponent<Collider2D>().enabled = false; door.Interact(player);
            door.GetComponent<Collider2D>().enabled = true; AssertNoTravelOrSave(saved);
            door.SetLocked(true); Assert.That(door.InteractionPrompt, Is.EqualTo("Cổng đang khóa"));
            door.Interact(player); door.SetLocked(false); AssertNoTravelOrSave(saved);
            var blocker = new GameObject("MapTravelModalProbe");
            try
            {
                GameplayState.EnterModal(blocker);
                door.Interact(player);
                Assert.That(SceneTransition.TryLoadScene("Map02", player), Is.False);
                AssertNoTravelOrSave(saved);
            }
            finally { GameplayState.ExitModal(blocker); Object.Destroy(blocker); }
            MapData target = door.DestinationMap;
            Set(door, "destinationMap", null); door.Interact(player); AssertNoTravelOrSave(saved);
            Set(door, "destinationMap", target); Set(door, "nextScene", "MissingMapProbe");
            door.Interact(player); AssertNoTravelOrSave(saved);
            Assert.That(SceneTransition.TryLoadScene("MissingMapProbe", player), Is.False);
            Assert.That(SceneTransition.TryLoadScene("", player), Is.False);
            Assert.That(SceneTransition.TryLoadScene("Map02", null), Is.False);
            Set(door, "nextScene", "Map02");
            Object.FindAnyObjectByType<GameOverUI>().enabled = false;
            player.GetComponent<PlayerHealth>().TakeStatusDamage(1000000);
            Assert.That(player.GetComponent<PlayerHealth>().IsAlive, Is.False);
            saved = PlayerPrefs.GetString(SaveKey);
            door.Interact(player); Assert.That(SceneTransition.TryLoadScene("Map02", player), Is.False);
            yield return null;
            AssertNoTravelOrSave(saved);
        }

        [UnityTest]
        public IEnumerator FadeBlocksGameplayRejectsDuplicatesAndCancellationAllowsRetryWithoutEarlyUnlock()
        {
            Approach();
            var fade = Object.FindAnyObjectByType<FadeTransition>();
            Set(fade, "duration", 0.7f);
            int arrivals = 0;
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> onLoaded = (s,m) => { if (s.name == "Map02") arrivals++; };
            SceneManager.sceneLoaded += onLoaded;
            try
            {
                player.GetComponent<PlayerStats>().AddGold(73);
                Door.Interact(player);
                Assert.That(fade.IsTransitioning, Is.True);
                Assert.That(GameplayState.IsInputBlocked, Is.True);
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(Progress.IsRegionUnlocked("Map02"), Is.False, "Unlock only after arrival, not when a fade may still be cancelled.");
                string acceptedSave = PlayerPrefs.GetString(SaveKey);
                Door.Interact(player);
                Assert.That(SceneTransition.TryLoadScene("Map03", player), Is.False);
                Assert.That(PlayerPrefs.GetString(SaveKey), Is.EqualTo(acceptedSave));
                Object.FindAnyObjectByType<PauseMenu>().Toggle();
                Object.FindAnyObjectByType<InventoryUI>().Open();
                Assert.That(GameObject.Find("PausePanel"), Is.Null);
                Assert.That(Object.FindAnyObjectByType<InventoryUI>().IsOpen, Is.False);
                Vector2 position = player.GetComponent<Rigidbody2D>().position;
                yield return new WaitForSecondsRealtime(0.12f);
                Assert.That(player.GetComponent<Rigidbody2D>().position, Is.EqualTo(position));
                fade.enabled = false;
                Assert.That(fade.IsTransitioning, Is.False);
                Assert.That(SceneTransition.IsLoading, Is.False);
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(fade.GetComponent<CanvasGroup>().alpha, Is.Zero);
                yield return new WaitForSecondsRealtime(0.75f);
                Assert.That(arrivals, Is.Zero);
                Assert.That(Progress.IsRegionUnlocked("Map02"), Is.False);
                fade.enabled = true; Set(fade, "duration", 0.1f);
                Door.Interact(player); Door.Interact(player);
                yield return WaitForScene("Map02");
                Assert.That(arrivals, Is.EqualTo(1));
                Assert.That(SceneTransition.IsLoading, Is.False);
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(Progress.IsRegionUnlocked("Map02"), Is.True);
            }
            finally { SceneManager.sceneLoaded -= onLoaded; }
        }

        [UnityTest]
        public IEnumerator EntireHuntingChainKeepsEarnedProgressLoadoutAndChestAndReturnsToLobby()
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            EquipmentInventory equipment = player.GetComponent<EquipmentInventory>();
            EquipmentData weapon = equipment.Catalog.First(x => x.Slot == EquipmentSlot.Weapon);
            EquipmentData stored = equipment.Catalog.First(x => x.Slot == EquipmentSlot.Helmet);
            Progress.AddUniqueItem(weapon.ItemId); Assert.That(equipment.Equip(weapon), Is.True);
            Progress.InitializeChest(null); Progress.AddUniqueItem(stored.ItemId);
            Assert.That(Progress.MoveItemToChest(stored.ItemId), Is.True);
            stats.AddExperience(stats.ExperienceToNextLevel + 13);
            SkillInventory skills = player.GetComponent<SkillInventory>();
            skills.AddOrUpgrade(SkillId.Shield.ToString()); Assert.That(skills.TryEquip(SkillId.Shield.ToString(), 3), Is.True);
            Progress.AddHunterEssence(19);
            int level = stats.Level, xp = stats.Experience, gold = stats.Gold;
            foreach (string next in new[] { "Map02", "Map03", "BossMap01", "CaveMap01", "RiftMap01", "Lobby" })
            {
                Assert.That(Door.DestinationScene, Is.EqualTo(next));
                Assert.That(Door.CanInteract, Is.True);
                Assert.That(MapManager.Instance.RemainingEnemies, Is.GreaterThan(0), "Hunting exits must not require defeating the zone.");
                Assert.That(Progress.IsRegionUnlocked(next), Is.EqualTo(next == "Lobby"));
                stats.AddGold(11); gold += 11;
                stats.SpendMana(7);
                float mana = stats.CurrentMana;
                Approach();
                Door.Interact(player);
                Assert.That(Progress.Gold, Is.EqualTo(gold));
                Assert.That(Progress.CurrentMana, Is.EqualTo(mana));
                Assert.That(SceneTransition.IsLoading, Is.True);
                yield return WaitForScene(next); Bind(); stats = player.GetComponent<PlayerStats>();
                Assert.That(stats.Gold, Is.EqualTo(gold));
                Assert.That(stats.Level, Is.EqualTo(level)); Assert.That(stats.Experience, Is.EqualTo(xp));
                Assert.That(player.GetComponent<SkillInventory>().Slots[3].skillId, Is.EqualTo(SkillId.Shield.ToString()));
                Assert.That(player.GetComponent<EquipmentInventory>().GetEquipped(EquipmentSlot.Weapon).ItemId, Is.EqualTo(weapon.ItemId));
                Assert.That(Progress.HasItem(stored.ItemId), Is.False);
                Assert.That(Progress.ChestItems, Does.Contain(stored.ItemId));
                Assert.That(Progress.HunterEssence, Is.EqualTo(19));
                Assert.That(Progress.IsRegionUnlocked(next), Is.True);
                Assert.That(GameplayState.IsInputBlocked, Is.False);
                Transform spawn = GameObject.Find("PlayerSpawnPoint")?.transform;
                if (spawn != null) Assert.That(Vector2.Distance(player.transform.position, spawn.position), Is.LessThan(0.25f));
            }
            Assert.That(PlayerPrefs.GetString(SaveKey), Does.Contain(stored.ItemId));
            Assert.That(SceneTransition.IsLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator TravelWithoutFadeStillLoadsOnceAndReleasesLoadingState()
        {
            Object.Destroy(Object.FindAnyObjectByType<FadeTransition>().gameObject);
            yield return null;
            Approach();
            Door.Interact(player); Door.Interact(player);
            yield return WaitForScene("Map02");
            Assert.That(SceneTransition.IsLoading, Is.False);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(Progress.IsRegionUnlocked("Map02"), Is.True);
        }

        [UnityTest]
        public IEnumerator LobbyDepartureTransfersInputToFadeAndRecoversWhenFadeIsDisabled()
        {
            yield return SceneManager.LoadSceneAsync("Lobby"); yield return null; yield return null; Bind();
            var portal = Object.FindAnyObjectByType<LobbyPortal>();
            Set(portal, "chargeDuration", 0.01f); Set(portal, "departureDuration", 0.05f);
            player.GetComponent<Rigidbody2D>().position = portal.transform.position; Physics2D.SyncTransforms();
            float deadline = Time.realtimeSinceStartup + 4;
            while (!portal.IsChoosing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(portal.ChooseDestination("Map01"), Is.True);
            var fade = Object.FindAnyObjectByType<FadeTransition>(); Set(fade, "duration", 2f);
            while (!fade.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(fade.IsTransitioning, Is.True);
            fade.enabled = false; yield return null; yield return null;
            Assert.That(portal.IsTransporting, Is.False);
            Assert.That(GameplayState.IsInputBlocked, Is.False);
            Assert.That(SceneTransition.IsLoading, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Lobby"));
        }

        [UnityTest]
        public IEnumerator GeneratedExitPromptsAndWorldLabelsUseDestinationDataAtThreeResolutions()
        {
            var initialHud = Object.FindAnyObjectByType<GameplayHUD>();
            Assert.That(initialHud.GetComponentInParent<Canvas>().GetComponentsInChildren<Transform>(true).Single(t => t.name == "InteractionHint").gameObject.activeSelf, Is.False);
            foreach (string scene in new[] { "Map01", "Map02", "Map03", "BossMap01", "CaveMap01", "RiftMap01" })
            {
                if (SceneManager.GetActiveScene().name != scene)
                { yield return SceneManager.LoadSceneAsync(scene); yield return null; yield return null; Bind(); }
                Approach(); yield return null; yield return null;
                float deadline = Time.realtimeSinceStartup + 1f;
                while (!player.HasTarget && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(player.HasTarget, Is.True, scene + " player=" + player.transform.position +
                    " body=" + player.GetComponent<Rigidbody2D>().position + " door=" + Door.transform.position +
                    " interact=" + Door.CanInteract + " blocked=" + GameplayState.IsInputBlocked + " scale=" + Time.timeScale);
                Assert.That(player.CurrentPrompt, Is.EqualTo(Door.InteractionPrompt));
                var hint = Object.FindAnyObjectByType<GameplayHUD>().GetComponentInParent<Canvas>().GetComponentsInChildren<Transform>(true).Single(t => t.name == "InteractionHint");
                Assert.That(hint.gameObject.activeSelf, Is.True);
                Assert.That(hint.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), Is.True);
                Assert.That(Door.InteractionPrompt, Does.Contain(Door.DestinationName));
                Assert.That(Door.InteractionPrompt, Does.Not.Contain(Door.DestinationScene));
                TextMesh world = GameObject.Find("Guidance_ExitLabel").GetComponent<TextMesh>();
                Assert.That(world.text, Does.Contain(Door.DestinationName.ToUpperInvariant()));
                if (Door.DestinationMap != null)
                    Assert.That(world.text, Does.Contain(Door.DestinationMap.RecommendedMinLevel + "-" + Door.DestinationMap.RecommendedMaxLevel));
                yield return RenderExit(scene);
            }
        }

        private IEnumerator RenderExit(string scene)
        {
            Camera camera = Camera.main;
            Canvas canvas = Object.FindAnyObjectByType<GameplayHUD>().GetComponentInParent<Canvas>();
            var prompt = canvas.GetComponentsInChildren<TMP_Text>().Single(t => t.name == "Prompt");
            RenderMode mode = canvas.renderMode; Camera oldCamera = canvas.worldCamera;
            int oldSorting = canvas.sortingOrder;
            RenderTexture target = null;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                canvas.sortingOrder = 30000;
                foreach (Vector2Int size in new[] { new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768) })
                {
                    target = new RenderTexture(size.x,size.y,24); camera.targetTexture = target;
                    yield return null; yield return null; Canvas.ForceUpdateCanvases(); prompt.ForceMeshUpdate();
                    Assert.That(prompt.isTextOverflowing, Is.False, scene + " " + size);
                    Assert.That(prompt.text, Is.EqualTo(Door.InteractionPrompt));
                    var bounds = (RectTransform)canvas.transform;
                    var corners = new Vector3[4]; prompt.rectTransform.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners) Assert.That(bounds.rect.Contains(bounds.InverseTransformPoint(corner)), Is.True);
                    float scale = prompt.transform.TransformVector(Vector3.right).magnitude / canvas.transform.TransformVector(Vector3.right).magnitude;
                    Assert.That(prompt.fontSize * scale * size.x / bounds.rect.width, Is.GreaterThanOrEqualTo(14));
                    camera.Render(); var previous = RenderTexture.active;
                    var image = new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                    try
                    {
                        RenderTexture.active = target; image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply();
                        Directory.CreateDirectory("Assets/Screenshots");
                        File.WriteAllBytes("Assets/Screenshots/ChuyenVung_" + scene + "_" + size.x + "x" + size.y + "_Verified.png", image.EncodeToPNG());
                    }
                    finally { RenderTexture.active = previous; Object.Destroy(image); }
                    camera.targetTexture = null; target.Release(); Object.Destroy(target); target = null;
                }
            }
            finally
            {
                camera.targetTexture = null; canvas.renderMode = mode; canvas.worldCamera = oldCamera;
                canvas.sortingOrder = oldSorting;
                if (target != null) { target.Release(); Object.Destroy(target); }
            }
        }

        private IEnumerator WaitForScene(string scene)
        {
            float deadline = Time.realtimeSinceStartup + 5;
            while (SceneManager.GetActiveScene().name != scene && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(scene));
            yield return null; yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            var fade = Object.FindAnyObjectByType<FadeTransition>(); if (fade != null) fade.enabled = false;
            yield return null; yield return null;
            Scene scene = SceneManager.GetActiveScene(); Scene clean = SceneManager.CreateScene("MapTransitionClean");
            SceneManager.SetActiveScene(clean); yield return SceneManager.UnloadSceneAsync(scene);
            if (Progress != null && originalState != null)
            {
                JsonUtility.FromJsonOverwrite(originalState, Progress);
                Set(Progress,"playerProgressApplied",originalApplied);
            }
            typeof(PlayerAutomationState).GetMethod("ResetSession", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,null);
            if (originalAttack.HasValue) PlayerAutomationState.SetAutoAttackEnabled(originalAttack.Value);
            PlayerAutomationState.SetAutoPickupEnabled(originalPickup);
            if (existed) PlayerPrefs.SetString(SaveKey,originalSave); else PlayerPrefs.DeleteKey(SaveKey);
            SceneTransition.ResetLoading();
        }
    }
}
