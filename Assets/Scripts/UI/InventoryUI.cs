using System;
using System.Collections.Generic;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private readonly Dictionary<EquipmentData, Button> itemButtons = new Dictionary<EquipmentData, Button>();
        private readonly List<Button> emptySlots = new List<Button>();
        private readonly Dictionary<EquipmentSlot, Button> equippedButtons = new Dictionary<EquipmentSlot, Button>();
        private Transform window, character, bag;
        private Button bagViewButton, characterViewButton;
        private bool? compactLayout;
        private bool showCharacter;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            if (panel == null) BuildPanel();
        }

        private void Update()
        {
            if (IsOpen) ApplyResponsiveLayout();
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
            if (!isActiveAndEnabled || IsOpen || GameplayState.IsBlockedByOther(this) || Time.timeScale <= 0) return;
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
            if (selectedItem == null || !inventory.Owns(selectedItem) || !MatchesFilter(selectedItem))
            {
                selectedItem = inventory.CurrentEquipment;
                if (selectedItem != null && !MatchesFilter(selectedItem)) selectedItem = null;
                if (selectedItem == null)
                    foreach (EquipmentData item in inventory.OwnedItems())
                        if (MatchesFilter(item)) { selectedItem = item; break; }
            }
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            focus.Capture(panel.transform);
            launcherButton.gameObject.SetActive(false);
            inventory.EquipmentChanged += HandleEquipmentChanged;
            stats.GoldChanged += HandleGoldChanged;
            ApplyResponsiveLayout();
            statusText.text = "Chuẩn bị cho chuyến phiêu lưu tiếp theo.";
            RefreshAll();
            (compactLayout == true && showCharacter ? characterViewButton : filters[filterMode]).Select();
        }

        public void Close()
        {
            bool wasOpen = IsOpen;
            if (panel != null) panel.SetActive(false);
            if (launcherButton != null) launcherButton.gameObject.SetActive(isActiveAndEnabled);
            if (wasOpen && movement != null) movement.enabled = movementWasEnabled;
            if (inventory != null) inventory.EquipmentChanged -= HandleEquipmentChanged;
            if (stats != null) stats.GoldChanged -= HandleGoldChanged;
            GameplayState.ExitModal(this);
            focus.Release();
        }

        private void OnEnable() { if (launcherButton != null) launcherButton.gameObject.SetActive(true); }
        private void OnDisable() => Close();
        private void OnDestroy()
        {
            Close();
            if (panel != null) Destroy(panel);
            if (launcherButton != null) Destroy(launcherButton.gameObject);
        }

        private void SelectItem(EquipmentData item)
        {
            if (!IsOpen || item == null || !inventory.Owns(item)) return;
            selectedItem = item;
            RefreshInventory();
            RefreshEquipment();
            details.Show(selectedItem, inventory, false);
            EnsureFocus();
        }

        private void HandleAction()
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || selectedItem == null || inventory == null) return;
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
            EnsureFocus();
        }

        private void HandleEquipmentChanged(EquipmentData item) { if (IsOpen) RefreshAll(); }
        private void HandleGoldChanged(int gold) { if (IsOpen) goldText.text = gold.ToString("N0") + "  VÀNG"; }

        private void RefreshEquipment()
        {
            foreach (var entry in equippedButtons)
            {
                EquipmentSlot slot = entry.Key;
                EquipmentData item = inventory.GetEquipped(slot);
                int level = inventory.GetEnhancementLevel(item);
                F.RefreshItemSlot(entry.Value, item, item != null && item == selectedItem, false, level > 0 ? "+" + level : null);
            }
        }

        private void RefreshInventory()
        {
            var visibleItems = new HashSet<EquipmentData>();
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
                visibleItems.Add(item);
                visible++;
                int level = inventory.GetEnhancementLevel(item);
                if (!itemButtons.TryGetValue(item, out Button button))
                {
                    button = F.ItemSlot(itemsRoot, item, 0, 0, 89, false, false, () => SelectItem(item));
                    button.gameObject.AddComponent<ScrollSelectionIntoView>();
                    itemButtons.Add(item, button);
                }
                button.gameObject.SetActive(true);
                button.transform.SetAsLastSibling();
                F.RefreshItemSlot(button, item, item == selectedItem,
                    inventory.GetEquipped(item.Slot) == item, level > 0 ? "+" + level : null);
            }
            foreach (var entry in itemButtons)
                if (!visibleItems.Contains(entry.Key)) entry.Value.gameObject.SetActive(false);
            int emptyCount = Mathf.Max(0, 42 - visible);
            while (emptySlots.Count < emptyCount) emptySlots.Add(F.ItemSlot(itemsRoot, null, 0, 0, 89, false, false, null));
            for (int i = 0; i < emptySlots.Count; i++)
            {
                emptySlots[i].gameObject.SetActive(i < emptyCount);
                if (i < emptyCount) emptySlots[i].transform.SetAsLastSibling();
            }
            inventoryTitle.text = "TÚI ĐỒ  <color=#ABA69E>" + visible + " / " + owned.Count + "</color>";
            emptyText.gameObject.SetActive(visible == 0);
            emptyText.text = owned.Count == 0 ? "Túi đồ còn trống\nGhé lò rèn để mua trang bị." : "Không có đồ trong nhóm này.";
            for (int i = 0; i < filters.Count; i++)
            {
                filters[i].transform.Find("Inset").GetComponent<Image>().color = i == filterMode ? F.Hex("695439") : F.Hex("303443");
                filters[i].transform.Find("Label").GetComponent<TMP_Text>().color = i == filterMode ? F.Gold : F.Muted;
            }
            EnsureFocus();
            if (EventSystem.current != null)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected != null && selected.transform.IsChildOf(itemsRoot))
                    selected.GetComponent<ScrollSelectionIntoView>()?.Reveal();
            }
        }

        private void EnsureFocus()
        {
            if (!IsOpen || EventSystem.current == null) return;
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected != null && !selected.transform.IsChildOf(panel.transform)) return;
            Selectable selectable = selected != null ? selected.GetComponent<Selectable>() : null;
            if (selected == null || !selected.activeInHierarchy || selectable != null && !selectable.IsInteractable())
                (compactLayout == true && showCharacter ? characterViewButton : filters[filterMode]).Select();
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
            if (!IsOpen) return;
            filterMode = mode;
            if (selectedItem == null || !MatchesFilter(selectedItem))
            {
                selectedItem = null;
                foreach (EquipmentData item in inventory.OwnedItems())
                    if (MatchesFilter(item)) { selectedItem = item; break; }
            }
            RefreshAll();
            inventoryScroll.verticalNormalizedPosition = 1;
        }

        private void ToggleSort()
        {
            if (!IsOpen) return;
            sortByPower = !sortByPower;
            sortButton.transform.Find("Label").GetComponent<TMP_Text>().text = sortByPower ? "SỨC MẠNH ↓" : "THEO LOẠI";
            inventoryScroll.verticalNormalizedPosition = 1;
            RefreshInventory();
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
            window = F.Window(surface, "HÀNH TRANG", "TRANG BỊ  /  NHÂN VẬT", Close);
            window.Find("KeyHints").GetComponent<TMP_Text>().text = "MŨI TÊN / ENTER  Chọn  •  CUỘN  Xem đồ  •  E = Đang mặc  •  I / ESC  Đóng";
            goldText = F.Text(window, "Gold", "", 1190, 39, 392, 42, 24, F.Gold, TextAlignmentOptions.Right);

            character = F.Frame(window, "Character", 24, 122, 372, 724, F.Panel);
            levelText = F.Text(character, "Level", "", 20, 20, 332, 26, 17, F.Muted, TextAlignmentOptions.Center);
            F.Text(character, "PowerLabel", "SỨC MẠNH", 22, 54, 328, 22, 14, F.Gold, TextAlignmentOptions.Center);
            powerText = F.Text(character, "Power", "0", 22, 75, 328, 48, 36, F.Gold, TextAlignmentOptions.Center);
            F.Medallion(character, 55, 158, 262, F.Gold);
            avatar = F.Icon(character, "Avatar", null, 91, 133, 190, 300);
            equipmentRoot = F.Rect(character, "EquipmentSlots", 0, 0, 372, 470);
            EquipmentSlot[] slots = { EquipmentSlot.Weapon, EquipmentSlot.Helmet, EquipmentSlot.Armor, EquipmentSlot.Pants, EquipmentSlot.Necklace };
            Vector2[] positions = { new Vector2(18, 120), new Vector2(278, 120), new Vector2(278, 232), new Vector2(278, 344), new Vector2(18, 344) };
            for (int i = 0; i < slots.Length; i++)
            {
                EquipmentSlot slot = slots[i];
                Vector2 position = positions[i];
                Button button = F.ItemSlot(equipmentRoot, null, position.x, position.y, 76, false, false,
                    () => SelectItem(inventory.GetEquipped(slot)));
                button.name = "Equipment" + slot;
                equippedButtons.Add(slot, button);
                F.Text(equipmentRoot, slot + "Label", F.SlotLabel(slot), position.x - 5, position.y + 79, 86, 24, 14, F.Muted, TextAlignmentOptions.Center);
            }
            F.Line(character, 24, 482, 324);
            F.Text(character, "StatsHeading", "CHỈ SỐ NHÂN VẬT", 24, 496, 324, 26, 14, F.Gold);
            statsText = F.Text(character, "CharacterStats", "", 28, 533, 320, 166, 19, F.Muted);
            statsText.lineSpacing = 2;
            statsText.alignment = TextAlignmentOptions.TopLeft;

            bag = F.Frame(window, "Bag", 420, 122, 730, 724, F.Panel);
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
            bagViewButton = F.Button(window, "BagView", "TÚI ĐỒ", 24, 122, 360, 40, () => ShowCompactView(false));
            characterViewButton = F.Button(window, "CharacterView", "NHÂN VẬT & CHỈ SỐ", 394, 122, 360, 40, () => ShowCompactView(true));
            panel.SetActive(false);
        }

        private void ShowCompactView(bool characterView)
        {
            if (!IsOpen) return;
            showCharacter = characterView;
            ApplyCompactView();
        }

        private void ApplyCompactView()
        {
            bool compact = compactLayout == true;
            character.gameObject.SetActive(!compact || showCharacter);
            bag.gameObject.SetActive(!compact || !showCharacter);
            bagViewButton.gameObject.SetActive(compact);
            characterViewButton.gameObject.SetActive(compact);
            bagViewButton.transform.Find("Inset").GetComponent<Image>().color = !showCharacter ? F.Hex("695439") : F.Hex("303443");
            characterViewButton.transform.Find("Inset").GetComponent<Image>().color = showCharacter ? F.Hex("695439") : F.Hex("303443");
            EnsureFocus();
        }

        private void ApplyResponsiveLayout()
        {
            Rect available = ((RectTransform)panel.transform).rect;
            bool compact = available.width / Mathf.Max(1, available.height) < 1.5f;
            if (compactLayout == compact) return;
            compactLayout = compact;
            panel.GetComponent<EquipmentPanelLayout>().ConfigureWindow(window, compact, goldText, statusText);
            ((RectTransform)bag).anchoredPosition = new Vector2(compact ? 24 : 420, compact ? -172 : -122);
            ((RectTransform)bag).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, compact ? 674 : 724);
            foreach (string name in new[] { "Brass", "Inset" })
            {
                var rect = (RectTransform)bag.Find(name);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, (compact ? 674 : 724) - (name == "Brass" ? 4 : 8));
            }
            inventoryScroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, compact ? 536 : 586);
            ((RectTransform)character).anchoredPosition = new Vector2(compact ? 218 : 24, compact ? -172 : -122);
            character.localScale = Vector3.one * (compact ? 0.92f : 1f);
            var detail = (RectTransform)window.Find("SelectedEquipment");
            detail.anchoredPosition = new Vector2(compact ? 778 : 1174, -122);
            ApplyCompactView();
        }
    }
}
