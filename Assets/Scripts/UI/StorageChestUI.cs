using System.Collections.Generic;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Keeps storage transfers readable and pauses combat for the entire transaction.</summary>
    public sealed class StorageChestUI : MonoBehaviour
    {
        private GameObject panel;
        private Transform playerItemsRoot, chestItemsRoot;
        private TMP_Text statusText;
        private EquipmentInventory inventory;
        private Button closeButton;
        private readonly MenuModalFocus focus = new MenuModalFocus();
        public bool IsOpen => panel != null && panel.activeSelf;

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open(PlayerInteraction player)
        {
            if (player == null || IsOpen || GameplayState.IsBlockedByOther(this)) return;
            inventory = player.GetComponent<EquipmentInventory>();
            if (inventory == null) return;
            BuildPanel();
            if (panel == null) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            GameplayState.EnterModal(this);
            focus.Capture(panel.transform);
            statusText.text = "Chọn đồ bên trái để cất, bên phải để lấy. Cất món đang mặc sẽ tháo trang bị đó.";
            Refresh();
            closeButton.Select();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            GameplayState.ExitModal(this);
            focus.Release();
        }

        private void Refresh()
        {
            FantasyEquipmentUI.Clear(playerItemsRoot);
            FantasyEquipmentUI.Clear(chestItemsRoot);
            GameProgressManager progress = GameProgressManager.Instance;
            if (progress == null || inventory == null) return;
            var owned = new List<EquipmentData>(inventory.OwnedItems());
            foreach (EquipmentData item in owned)
            {
                EquipmentData captured = item;
                bool equipped = inventory.GetEquipped(item.Slot) == item;
                CreateTransferCard(playerItemsRoot, item, equipped ? "THÁO VÀ CẤT VÀO RƯƠNG" : "CẤT VÀO RƯƠNG", () =>
                {
                    inventory.UnequipIfMatching(captured.ItemId);
                    if (!progress.MoveItemToChest(captured.ItemId)) return;
                    progress.CaptureActivePlayer();
                    statusText.text = "Đã cất " + captured.DisplayName + ".";
                    Refresh();
                });
            }
            foreach (string itemId in progress.ChestItems)
            {
                EquipmentData item = inventory.FindById(itemId);
                if (item == null) continue;
                EquipmentData captured = item;
                CreateTransferCard(chestItemsRoot, item, "LẤY RA HÀNH TRANG", () =>
                {
                    if (!progress.MoveItemFromChest(captured.ItemId)) return;
                    progress.CaptureActivePlayer();
                    statusText.text = "Đã lấy " + captured.DisplayName + ".";
                    Refresh();
                });
            }
            if (owned.Count == 0) CreateEmptyMessage(playerItemsRoot, "Hành trang trống");
            if (progress.ChestItems.Count == 0) CreateEmptyMessage(chestItemsRoot, "Rương đang trống");
        }

        private void BuildPanel()
        {
            if (panel != null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            panel = new GameObject("StorageChestPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel);
            Transform frame = FantasyEquipmentUI.Window(surface, "KHO TRANG BỊ", "CẤT GIỮ VÀ CHUẨN BỊ CHUYẾN ĐI", Close);
            closeButton = frame.Find("Close").GetComponent<Button>();
            frame.Find("KeyHints").GetComponent<TMP_Text>().text = "CHUỘT  Cất / Lấy đồ     •     CUỘN  Xem danh sách     •     ESC  Đóng";
            playerItemsRoot = CreateSide(frame, "PlayerItems", "HÀNH TRANG", 30);
            chestItemsRoot = CreateSide(frame, "ChestItems", "TRONG RƯƠNG", 872);
            statusText = FantasyEquipmentUI.Text(frame, "Status", "", 32, 803, 1648, 50, 22, FantasyEquipmentUI.Gold);
        }

        private static Transform CreateSide(Transform parent, string name, string title, float x)
        {
            Transform side = FantasyEquipmentUI.Frame(parent, name, x, 128, 810, 660, FantasyEquipmentUI.Panel);
            FantasyEquipmentUI.Text(side, "Heading", title, 24, 16, 762, 40, 25, FantasyEquipmentUI.Gold);
            FantasyEquipmentUI.Grid(side, "Viewport", 22, 76, 766, 560, new Vector2(360, 250), 2, out Transform content);
            return content;
        }

        private void CreateTransferCard(Transform parent, EquipmentData item, string actionLabel, UnityEngine.Events.UnityAction action)
        {
            Transform frame = FantasyEquipmentUI.Frame(parent, item.ItemId, 0, 0, 360, 250,
                Color.Lerp(FantasyEquipmentUI.Ink, item.RarityColor, 0.08f));
            Button button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame.Find("Inset").GetComponent<Image>();
            frame.GetComponent<Image>().raycastTarget = true;
            EquipmentPanelLayout.StyleButton(button);
            // Rebuilding a scrolled grid cannot leave keyboard focus on a destroyed card.
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                if (!IsOpen || GameplayState.IsBlockedByOther(this)) return;
                action.Invoke();
                closeButton.Select();
            });
            int enhancement = inventory.GetEnhancementLevel(item);
            FantasyEquipmentUI.Text(frame, "Name", item.DisplayName + (enhancement > 0 ? "  +" + enhancement : ""), 16, 12, 328, 34, 23, item.RarityColor);
            FantasyEquipmentUI.Text(frame, "Rarity", item.RarityLabel + "  •  Sức mạnh " + inventory.EffectivePowerScore(item),
                16, 48, 328, 26, 17, FantasyEquipmentUI.Muted);
            FantasyEquipmentUI.Icon(frame, "Icon", item.Icon, 18, 90, 86, 86);
            FantasyEquipmentUI.Text(frame, "Stats", item.BuildDetailedStatSummary(EquipmentInventory.EnhancementMultiplier(enhancement)), 118, 84, 226, 104, 17,
                null, TextAlignmentOptions.TopLeft);
            FantasyEquipmentUI.Line(frame, 16, 197, 328);
            FantasyEquipmentUI.Text(frame, "Action", actionLabel, 16, 208, 328, 28, 19,
                FantasyEquipmentUI.Green, TextAlignmentOptions.Center);
        }

        private static void CreateEmptyMessage(Transform parent, string message) =>
            FantasyEquipmentUI.Text(parent, "Empty", message, 0, 0, 360, 100, 23, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);

        private void OnDisable() => Close();
        private void OnDestroy()
        {
            Close();
            if (panel != null) Destroy(panel);
        }
    }
}
