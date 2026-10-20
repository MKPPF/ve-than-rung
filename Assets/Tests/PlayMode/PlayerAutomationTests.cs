using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Items;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TopDownGame.Tests.PlayMode
{
    public sealed class PlayerAutomationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string TestSaveKey = "TopDownGame.Tests.Progress.v1";
        private GameObject player, canvas;
        private EnemyLootPickup loot;
        private ItemPickup itemPickup;
        private ItemData item;
        private bool? originalAttack;
        private bool originalPickup, hadSave, originalApplied;
        private string originalSave, originalProgress;
        private float originalTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalAttack = PlayerAutomationState.AutoAttackOverride;
            originalPickup = PlayerAutomationState.AutoPickupEnabled;
            ResetAutomation();
            originalTimeScale = Time.timeScale;
            Time.timeScale = 1;
            Assert.That(typeof(GameProgressManager).GetProperty("ActiveSaveKey",
                BindingFlags.Static | BindingFlags.NonPublic).GetValue(null), Is.EqualTo(TestSaveKey));
            if (GameProgressManager.Instance == null)
                new GameObject(nameof(GameProgressManager)).AddComponent<GameProgressManager>();
            originalProgress = JsonUtility.ToJson(GameProgressManager.Instance);
            originalApplied = (bool)typeof(GameProgressManager).GetField("playerProgressApplied", Private)
                .GetValue(GameProgressManager.Instance);
            hadSave = PlayerPrefs.HasKey(TestSaveKey);
            originalSave = PlayerPrefs.GetString(TestSaveKey, "");
            player = new GameObject("AutomationPlayerProbe", typeof(PlayerStats),
                typeof(PlayerCombat), typeof(PlayerInteraction), typeof(CircleCollider2D));
            player.tag = "Player";
            player.transform.position = new Vector3(1000, 1000, 0);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameplayState.ExitModal(player);
            Time.timeScale = originalTimeScale;
            if (canvas != null) Object.Destroy(canvas);
            if (loot != null) Object.Destroy(loot.gameObject);
            if (itemPickup != null) Object.Destroy(itemPickup.gameObject);
            if (item != null) Object.Destroy(item);
            if (player != null) Object.Destroy(player);
            yield return null;
            ResetAutomation();
            if (originalAttack.HasValue) PlayerAutomationState.SetAutoAttackEnabled(originalAttack.Value);
            PlayerAutomationState.SetAutoPickupEnabled(originalPickup);
            if (GameProgressManager.Instance != null && originalProgress != null)
            {
                JsonUtility.FromJsonOverwrite(originalProgress, GameProgressManager.Instance);
                typeof(GameProgressManager).GetField("playerProgressApplied", Private)
                    .SetValue(GameProgressManager.Instance, originalApplied);
            }
            if (hadSave) PlayerPrefs.SetString(TestSaveKey, originalSave);
            else PlayerPrefs.DeleteKey(TestSaveKey);
        }

        [UnityTest]
        public IEnumerator HudButtonsToggleIndependentlyAndBlockDuringModal()
        {
            canvas = new GameObject("AutomationCanvasProbe", typeof(Canvas), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            PlayerAutomationUI.Ensure(canvas);
            PlayerAutomationUI.Ensure(canvas);
            yield return null;
            Assert.That(canvas.GetComponents<PlayerAutomationUI>().Length, Is.EqualTo(1));
            Transform root = canvas.transform.Find("AutomationControls");
            var attack = root.Find("AutoAttackToggle").GetComponent<Button>();
            var pickup = root.Find("AutoPickupToggle").GetComponent<Button>();
            attack.onClick.Invoke();
            Assert.That(player.GetComponent<PlayerCombat>().AutoAttackEnabled, Is.False);
            Assert.That(PlayerAutomationState.AutoPickupEnabled, Is.True);
            Assert.That(attack.GetComponentInChildren<TMP_Text>().text, Does.Contain("TẮT"));
            pickup.onClick.Invoke();
            Assert.That(PlayerAutomationState.AutoPickupEnabled, Is.False);
            GameplayState.EnterModal(player);
            attack.onClick.Invoke();
            pickup.onClick.Invoke();
            yield return null;
            Assert.That(attack.interactable, Is.False);
            Assert.That(PlayerAutomationState.AutoPickupEnabled, Is.False);
            Assert.That(player.GetComponent<PlayerCombat>().AutoAttackEnabled, Is.False);
            GameplayState.ExitModal(player);
            Object.Destroy(player);
            yield return null;
            player = new GameObject("AutomationReplacementProbe", typeof(PlayerStats), typeof(PlayerCombat));
            player.tag = "Player";
            player.transform.position = new Vector3(1000, 1000, 0);
            Assert.That(player.GetComponent<PlayerCombat>().AutoAttackEnabled, Is.False,
                "A new scene's player must inherit the session choice.");
            Assert.That(PlayerAutomationState.AutoPickupEnabled, Is.False);
        }

        [UnityTest]
        public IEnumerator LootStaysOnGroundWhenDisabledAndManualPickupRewardsOnce()
        {
            PlayerAutomationState.SetAutoPickupEnabled(false);
            loot = EnemyLootPickup.Spawn(player.transform.position + Vector3.right * 0.1f, 7, 11, null);
            PlayerStats stats = player.GetComponent<PlayerStats>();
            int goldBefore = stats.Gold;
            loot.SendMessage("Update");
            loot.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>());
            yield return null;
            Assert.That(loot.IsCollected, Is.False);
            Assert.That(stats.Gold, Is.EqualTo(goldBefore));
            Assert.That(loot.CanInteract, Is.True);
            GameplayState.EnterModal(player);
            loot.Interact(player.GetComponent<PlayerInteraction>());
            Assert.That(loot.IsCollected, Is.False);
            GameplayState.ExitModal(player);
            loot.Interact(player.GetComponent<PlayerInteraction>());
            loot.Interact(player.GetComponent<PlayerInteraction>());
            Assert.That(stats.Gold, Is.EqualTo(goldBefore + 11));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReenablingPickupCollectsLootAlreadyNearPlayer()
        {
            PlayerAutomationState.SetAutoPickupEnabled(false);
            loot = EnemyLootPickup.Spawn(player.transform.position + Vector3.right, 0, 3, null);
            float startX = loot.transform.position.x;
            yield return new WaitForSeconds(0.08f);
            Assert.That(loot.transform.position.x, Is.EqualTo(startX).Within(0.001f));
            PlayerAutomationState.SetAutoPickupEnabled(true);
            yield return new WaitForSeconds(0.4f);
            Assert.That(loot == null || loot.IsCollected, Is.True);
        }

        [UnityTest]
        public IEnumerator ItemTriggerRespectsToggleAndManualInteraction()
        {
            PlayerAutomationState.SetAutoPickupEnabled(false);
            item = ScriptableObject.CreateInstance<ItemData>();
            typeof(ItemData).GetField("itemId", Private).SetValue(item, "automation_item_probe");
            itemPickup = new GameObject("AutomationItemProbe", typeof(CircleCollider2D))
                .AddComponent<ItemPickup>();
            itemPickup.transform.position = player.transform.position;
            typeof(ItemPickup).GetField("item", Private).SetValue(itemPickup, item);
            itemPickup.SendMessage("OnTriggerEnter2D", player.GetComponent<Collider2D>());
            Assert.That(GameProgressManager.Instance.HasItem(item.ItemId), Is.False);
            itemPickup.Interact(player.GetComponent<PlayerInteraction>());
            Assert.That(GameProgressManager.Instance.HasItem(item.ItemId), Is.True);
            yield return null;

            itemPickup = new GameObject("AutomationItemStayProbe", typeof(CircleCollider2D))
                .AddComponent<ItemPickup>();
            typeof(ItemData).GetField("itemId", Private).SetValue(item, "automation_item_stay_probe");
            typeof(ItemPickup).GetField("item", Private).SetValue(itemPickup, item);
            itemPickup.transform.position = player.transform.position;
            PlayerAutomationState.SetAutoPickupEnabled(true);
            itemPickup.SendMessage("OnTriggerStay2D", player.GetComponent<Collider2D>());
            Assert.That(GameProgressManager.Instance.HasItem(item.ItemId), Is.True);
            yield return null;
        }

        private static void ResetAutomation()
        {
            typeof(PlayerAutomationState).GetMethod("ResetSession", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);
        }
    }
}
