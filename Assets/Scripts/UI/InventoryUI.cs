using System;
using System.Collections.Generic;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using F = TopDownGame.UI.FantasyEquipmentUI;

namespace TopDownGame.UI
{
    /// <summary>A compact RPG equipment desk separates item browsing from inspection and actions.</summary>
    public sealed class InventoryUI : MonoBehaviour
    {
        private GameObject panel;
        private Button launcherButton;
        private Transform equipmentRoot, itemsRoot;
        private ScrollRect inventoryScroll;
        private EquipmentDetailView details;
        private TMP_Text inventoryTitle, goldText, powerText, levelText, statsText, statusText, emptyText;
        private Image avatar;
        private readonly List<Button> filters = new List<Button>();
        private EquipmentInventory inventory;
        private PlayerStats stats;
        private PlayerMovement movement;
        private EquipmentData selectedItem;
        private int filterMode;
        private bool sortByPower, movementWasEnabled;
        private Button sortButton;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            if (panel == null) BuildPanel();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.iKey.wasPressedThisFrame)
            {
                if (IsOpen) Close(); else Open();
            }
            else if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open()
        {
            if (IsOpen || GameplayState.IsBlockedByOther(this) || Time.timeScale <= 0) return;
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) return;
            inventory = player.GetComponent<EquipmentInventory>();
            stats = player.GetComponent<PlayerStats>();
            movement = player.GetComponent<PlayerMovement>();
            if (inventory == null || stats == null) return;
            BuildPanel();
            if (panel == null) return;
            movementWasEnabled = movement != null && movement.enabled;
            if (movement != null) movement.enabled = false;
            GameplayState.EnterModal(this);
            SpriteRenderer renderer = player.GetComponentInChildren<SpriteRenderer>();
            avatar.sprite = renderer != null ? renderer.sprite : null;
            avatar.enabled = avatar.sprite != null;
            if (selectedItem == null || !inventory.Owns(selectedItem))
            {
                selectedItem = inventory.CurrentEquipment;
                if (selectedItem == null)
                    foreach (EquipmentData item in inventory.OwnedItems()) { selectedItem = item; break; }
            }
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            launcherButton.gameObject.SetActive(false);
            statusText.text = "Chuẩn bị cho chuyến phiêu lưu tiếp theo.";
            RefreshAll();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            if (launcherButton != null) launcherButton.gameObject.SetActive(true);
            if (movement != null) movement.enabled = movementWasEnabled;
            GameplayState.ExitModal(this);
        }

        private void OnDisable() => Close();
        private void OnDestroy()
        {
            GameplayState.ExitModal(this);
            if (panel != null) Destroy(panel);
            if (launcherButton != null) Destroy(launcherButton.gameObject);
        }

        private void SelectItem(EquipmentData item)
        {
            selectedItem = item;
            RefreshInventory();
            RefreshEquipment();
            details.Show(selectedItem, inventory, false);
        }

        private void HandleAction()
        {
            if (selectedItem == null || inventory == null) return;
            if (inventory.GetEquipped(selectedItem.Slot) == selectedItem)
            {
                if (inventory.Unequip(selectedItem.Slot, out EquipmentData removed))
                    statusText.text = "Đã tháo " + removed.DisplayName + ".";
            }
            else if (inventory.Equip(selectedItem)) statusText.text = "Đã trang bị " + selectedItem.DisplayName + ".";
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshInventory();
            RefreshEquipment();
            details.Show(selectedItem, inventory, false);
            int power = 0;
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                power += inventory.EffectivePowerScore(inventory.GetEquipped(slot));
            powerText.text = power.ToString("N0");
            levelText.text = "CHIẾN BINH  /  CẤP " + stats.Level;
            goldText.text = stats.Gold.ToString("N0") + "  VÀNG";
            statsText.text =
                "Máu tối đa<pos=64%><color=#EEE7D8>" + stats.MaxHealth.ToString("N0") + "</color>\n" +
                "Sát thương<pos=64%><color=#EEE7D8>" + stats.BaseDamage.ToString("0.#") + "</color>\n" +
                "Giáp<pos=64%><color=#EEE7D8>" + stats.Armor.ToString("0.#") + "</color>\n" +
                "Chí mạng<pos=64%><color=#EEE7D8>" + (stats.CriticalChance * 100).ToString("0.#") + "%</color>\n" +
                "Tốc chạy<pos=64%><color=#EEE7D8>" + stats.MoveSpeed.ToString("0.#") + "</color>\n" +
                "Tốc hồi chiêu<pos=64%><color=#EEE7D8>" + (stats.CooldownRate * 100).ToString("0.#") + "%</color>";
        }

        private void RefreshEquipment()
        {
            F.Clear(equipmentRoot);
            EquipmentSlot[] slots = { EquipmentSlot.Weapon, EquipmentSlot.Helmet, EquipmentSlot.Armor, EquipmentSlot.Pants, EquipmentSlot.Necklace };
            Vector2[] positions = { new Vector2(18, 120), new Vector2(278, 120), new Vector2(278, 232), new Vector2(278, 344), new Vector2(18, 344) };
            for (int i = 0; i < slots.Length; i++)
            {
                EquipmentSlot slot = slots[i];
                EquipmentData item = inventory.GetEquipped(slot);
                Vector2 position = positions[i];
                F.ItemSlot(equipmentRoot, item, position.x, position.y, 76,
                    item != null && item == selectedItem, false, () => SelectItem(item));
                F.Text(equipmentRoot, slot + "Label", F.SlotLabel(slot), position.x - 5,
                    position.y + 79, 86, 24, 14, F.Muted, TextAlignmentOptions.Center);
            }
        }

        private void RefreshInventory()
        {
            F.Clear(itemsRoot);
            var owned = new List<EquipmentData>(inventory.OwnedItems());
            owned.Sort((a, b) =>
            {
                int order = sortByPower ? inventory.EffectivePowerScore(b).CompareTo(inventory.EffectivePowerScore(a)) : a.Slot.CompareTo(b.Slot);
                if (order == 0) order = a.Price.CompareTo(b.Price);
                return order != 0 ? order : string.CompareOrdinal(a.ItemId, b.ItemId);
            });
            int visible = 0;
            foreach (EquipmentData item in owned)
            {
                if (!MatchesFilter(item)) continue;
                visible++;
                int level = inventory.GetEnhancementLevel(item);
                F.ItemSlot(itemsRoot, item, 0, 0, 89, item == selectedItem,
                    inventory.GetEquipped(item.Slot) == item, () => SelectItem(item), level > 0 ? "+" + level : null);
            }
            for (int i = visible; i < 42; i++) F.ItemSlot(itemsRoot, null, 0, 0, 89, false, false, null);
            inventoryTitle.text = "TÚI ĐỒ  <color=#ABA69E>" + visible + " / " + owned.Count + "</color>";
            emptyText.gameObject.SetActive(visible == 0);
            emptyText.text = owned.Count == 0 ? "Túi đồ còn trống\nGhé lò rèn để mua trang bị." : "Không có đồ trong nhóm này.";
            for (int i = 0; i < filters.Count; i++)
            {
                filters[i].transform.Find("Inset").GetComponent<Image>().color = i == filterMode ? F.Hex("695439") : F.Hex("303443");
                filters[i].transform.Find("Label").GetComponent<TMP_Text>().color = i == filterMode ? F.Gold : F.Muted;
            }
        }

        private bool MatchesFilter(EquipmentData item) => filterMode switch
        {
            1 => item.Slot == EquipmentSlot.Weapon,
            2 => item.Slot == EquipmentSlot.Helmet || item.Slot == EquipmentSlot.Armor || item.Slot == EquipmentSlot.Pants,
            3 => item.Slot == EquipmentSlot.Necklace,
            _ => true
        };

        private void SetFilter(int mode)
        {
            filterMode = mode;
            RefreshInventory();
            inventoryScroll.verticalNormalizedPosition = 1;
        }

        private void ToggleSort()
        {
            sortByPower = !sortByPower;
            sortButton.transform.Find("Label").GetComponent<TMP_Text>().text = sortByPower ? "SỨC MẠNH ↓" : "THEO LOẠI";
            RefreshInventory();
            inventoryScroll.verticalNormalizedPosition = 1;
        }

        private void BuildPanel()
        {
            if (panel != null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            launcherButton = F.Button(canvas.transform, "InventoryLauncher", "HÀNH TRANG  [I]", 0, 0, 250, 58, Open);
            RectTransform launchRect = launcherButton.GetComponent<RectTransform>();
            launchRect.anchorMin = launchRect.anchorMax = launchRect.pivot = Vector2.one;
            launchRect.anchoredPosition = new Vector2(-30, -30);
            panel = new GameObject("CharacterEquipmentPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel);
            Transform window = F.Window(surface, "HÀNH TRANG", "TRANG BỊ  /  NHÂN VẬT", Close);
            goldText = F.Text(window, "Gold", "", 1190, 39, 392, 42, 24, F.Gold, TextAlignmentOptions.Right);

            Transform character = F.Frame(window, "Character", 24, 122, 372, 724, F.Panel);
            levelText = F.Text(character, "Level", "", 20, 20, 332, 26, 17, F.Muted, TextAlignmentOptions.Center);
            F.Text(character, "PowerLabel", "SỨC MẠNH", 22, 54, 328, 22, 14, F.Gold, TextAlignmentOptions.Center);
            powerText = F.Text(character, "Power", "0", 22, 75, 328, 48, 36, F.Gold, TextAlignmentOptions.Center);
            F.Medallion(character, 55, 158, 262, F.Gold);
            avatar = F.Icon(character, "Avatar", null, 91, 133, 190, 300);
            equipmentRoot = F.Rect(character, "EquipmentSlots", 0, 0, 372, 470);
            F.Line(character, 24, 482, 324);
            F.Text(character, "StatsHeading", "CHỈ SỐ NHÂN VẬT", 24, 496, 324, 26, 14, F.Gold);
            statsText = F.Text(character, "CharacterStats", "", 28, 533, 320, 166, 19, F.Muted);
            statsText.lineSpacing = 14;
            statsText.alignment = TextAlignmentOptions.TopLeft;

            Transform bag = F.Frame(window, "Bag", 420, 122, 730, 724, F.Panel);
            inventoryTitle = F.Text(bag, "Count", "TÚI ĐỒ", 20, 19, 480, 36, 24, F.TextColor);
            sortButton = F.Button(bag, "Sort", "THEO LOẠI", 538, 20, 170, 36, ToggleSort);
            string[] labels = { "TẤT CẢ", "VŨ KHÍ", "GIÁP", "PHỤ KIỆN" };
            for (int i = 0; i < labels.Length; i++)
            {
                int mode = i;
                filters.Add(F.Button(bag, "Filter" + i, labels[i], 20 + i * 173, 72, 165, 40, () => SetFilter(mode)));
            }
            inventoryScroll = F.Grid(bag, "InventoryViewport", 20, 120, 692, 586, new Vector2(89, 89), 7, out itemsRoot);
            emptyText = F.Text(bag, "Empty", "", 40, 290, 650, 100, 23, F.Gold, TextAlignmentOptions.Center);
            emptyText.gameObject.SetActive(false);
            details = new EquipmentDetailView(window, 1174, 122, HandleAction);
            statusText = F.Text(window, "Status", "", 740, 874, 940, 26, 16, F.Gold, TextAlignmentOptions.Right);
            panel.SetActive(false);
        }
    }
}
