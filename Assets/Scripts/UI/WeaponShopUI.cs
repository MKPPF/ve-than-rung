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
    /// <summary>A merchant-led shop keeps browsing free of purchase actions until the player inspects an item.</summary>
    public sealed class WeaponShopUI : MonoBehaviour
    {
        [Header("Builder References")]
        [SerializeField] private EquipmentData[] catalog;
        [SerializeField] private Sprite merchantPortrait;
        private const float FeaturedDiscount = 0.2f;
        private const int RerollCost = 100;
        private readonly HashSet<string> featuredOfferIds = new HashSet<string>();
        private readonly List<Button> tabs = new List<Button>();
        private GameObject panel;
        private Transform cardsRoot;
        private ScrollRect shopScroll;
        private EquipmentDetailView details;
        private TMP_Text goldText, statusText, catalogTitle, offerText, emptyText;
        private EquipmentInventory inventory;
        private PlayerStats stats;
        private PlayerMovement movement;
        private bool movementWasEnabled;
        private EquipmentSlot selectedSlot = EquipmentSlot.Weapon;
        private EquipmentData selectedItem;
        private int offerRotation;
        private Button rerollButton;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        public void Open(PlayerInteraction player)
        {
            if (player == null || IsOpen || GameplayState.IsBlockedByOther(this) || Time.timeScale <= 0) return;
            inventory = player.GetComponent<EquipmentInventory>();
            stats = player.GetComponent<PlayerStats>();
            movement = player.GetComponent<PlayerMovement>();
            if (inventory == null || stats == null) return;
            if (!HasCatalogItems()) catalog = inventory.Catalog;
            BuildPanel();
            if (panel == null) return;
            movementWasEnabled = movement != null && movement.enabled;
            if (movement != null) movement.enabled = false;
            GameplayState.EnterModal(this);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            if (featuredOfferIds.Count == 0) RotateFeaturedOffers(false);
            if (selectedItem == null) SelectFirstItem();
            statusText.text = "Chọn món đồ  •  Xem chỉ số  •  Mua ở khung bên phải";
            RefreshCards();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            if (movement != null) movement.enabled = movementWasEnabled;
            GameplayState.ExitModal(this);
        }

        private void OnDisable() => Close();
        private void OnDestroy()
        {
            GameplayState.ExitModal(this);
            if (panel != null) Destroy(panel);
        }

        private bool HasCatalogItems()
        {
            if (catalog == null) return false;
            foreach (EquipmentData item in catalog) if (item != null) return true;
            return false;
        }

        private bool HasUnownedItems()
        {
            if (catalog == null || inventory == null) return false;
            foreach (EquipmentData item in catalog) if (item != null && !inventory.Owns(item)) return true;
            return false;
        }

        public static int DiscountedPrice(int originalPrice, float discount)
        {
            return Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0, originalPrice) * (1f - Mathf.Clamp01(discount))));
        }

        private bool IsFeatured(EquipmentData item) => item != null && featuredOfferIds.Contains(item.ItemId);
        private int PurchasePrice(EquipmentData item) => item == null ? 0 :
            IsFeatured(item) ? DiscountedPrice(item.Price, FeaturedDiscount) : item.Price;

        private void RotateFeaturedOffers(bool chargeGold)
        {
            if (stats == null || !HasUnownedItems()) return;
            if (chargeGold && !stats.TrySpendGold(RerollCost))
            {
                statusText.text = "Cần " + RerollCost + " vàng để đổi ưu đãi.";
                return;
            }
            offerRotation++;
            featuredOfferIds.Clear();
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                var candidates = new List<EquipmentData>();
                foreach (EquipmentData item in catalog)
                    if (item != null && item.Slot == slot && !inventory.Owns(item)) candidates.Add(item);
                if (candidates.Count > 0)
                    featuredOfferIds.Add(candidates[Mathf.Abs((offerRotation * 7 + (int)slot * 3) % candidates.Count)].ItemId);
            }
            if (chargeGold)
            {
                statusText.text = "Đã đổi lượt ưu đãi  •  -100 vàng";
                RefreshCards();
            }
        }

        private void SelectFirstItem()
        {
            selectedItem = null;
            if (catalog == null) return;
            foreach (EquipmentData item in catalog)
            {
                if (item == null || item.Slot != selectedSlot) continue;
                if (selectedItem == null) selectedItem = item;
                if (IsFeatured(item) && !inventory.Owns(item)) { selectedItem = item; return; }
            }
        }

        private void SelectSlot(EquipmentSlot slot)
        {
            selectedSlot = slot;
            SelectFirstItem();
            RefreshCards();
            shopScroll.verticalNormalizedPosition = 1;
        }

        private void SelectItem(EquipmentData item)
        {
            selectedItem = item;
            RefreshCards();
        }

        private void HandleItem(EquipmentData item)
        {
            if (inventory == null || item == null) return;
            inventory.TryBuyAndEquip(item, PurchasePrice(item), out string message);
            selectedItem = item;
            statusText.text = message;
            RefreshCards();
        }

        private void RefreshCards()
        {
            if (inventory == null || stats == null || cardsRoot == null) return;
            goldText.text = stats.Gold.ToString("N0") + "  VÀNG";
            F.Clear(cardsRoot);
            var visible = new List<EquipmentData>();
            if (catalog != null)
                foreach (EquipmentData item in catalog)
                    if (item != null && item.Slot == selectedSlot) visible.Add(item);
            visible.Sort((a, b) => a.Price.CompareTo(b.Price));
            foreach (EquipmentData item in visible) CreateCard(item);
            catalogTitle.text = "HÀNG HÓA  <color=#ABA69E>/  " + visible.Count + " món</color>";
            emptyText.gameObject.SetActive(visible.Count == 0);
            for (int i = 0; i < tabs.Count; i++)
            {
                bool active = i == (int)selectedSlot;
                tabs[i].transform.Find("Inset").GetComponent<Image>().color = active ? F.Hex("695439") : F.Hex("303443");
                tabs[i].transform.Find("Label").GetComponent<TMP_Text>().color = active ? F.Gold : F.Muted;
            }
            rerollButton.interactable = stats.Gold >= RerollCost && HasUnownedItems();
            offerText.text = HasUnownedItems() ? "MỖI NHÓM 1 ƯU ĐÃI  /  -20%" : "BẠN ĐÃ SỞ HỮU TOÀN BỘ TRANG BỊ";
            if (selectedItem == null) { details.Show(null, inventory, true); return; }
            bool owned = inventory.Owns(selectedItem);
            bool equipped = inventory.GetEquipped(selectedItem.Slot) == selectedItem;
            int price = PurchasePrice(selectedItem);
            string priceLabel = owned ? "ĐÃ SỞ HỮU" : IsFeatured(selectedItem) ?
                "<s><color=#ABA69E>" + selectedItem.Price + "</color></s>  " + price + " VÀNG  <color=#82C5A1>-20%</color>" :
                price + " VÀNG";
            details.Show(selectedItem, inventory, true, priceLabel);
            details.ActionLabel.text = equipped ? "ĐANG TRANG BỊ" : owned ? "TRANG BỊ" :
                stats.Gold >= price ? "MUA & TRANG BỊ" : "THIẾU " + (price - stats.Gold) + " VÀNG";
            details.Action.interactable = !equipped && (owned || stats.Gold >= price);
        }

        private void CreateCard(EquipmentData item)
        {
            bool owned = inventory.Owns(item);
            bool equipped = inventory.GetEquipped(item.Slot) == item;
            bool selected = item == selectedItem;
            Button card = F.Button(cardsRoot, item.ItemId + "Card", "", 0, 0, 162, 242, () => SelectItem(item));
            card.transform.Find("Brass").GetComponent<Image>().color = selected ? F.Gold : F.Edge;
            card.transform.Find("Inset").GetComponent<Image>().color = Color.Lerp(F.Ink, item.RarityColor, selected ? 0.17f : 0.065f);
            F.Icon(card.transform, "Icon", item.Icon, 22, 28, 120, 120);
            F.Box(card.transform, "RarityMark", 60, 152, 44, 2, item.RarityColor);
            F.Text(card.transform, "ItemName", item.DisplayName, 8, 165, 148, 32, 19,
                F.TextColor, TextAlignmentOptions.Center);
            F.Text(card.transform, "Price", owned ? "ĐÃ CÓ" : PurchasePrice(item) + " vàng", 8, 203, 148, 26, 18,
                owned ? F.Green : F.Gold, TextAlignmentOptions.Center);
            if (IsFeatured(item) && !owned)
            {
                F.Box(card.transform, "OfferRibbon", 6, 6, 58, 24, F.Hex("42624D"));
                F.Text(card.transform, "Offer", "-20%", 8, 6, 54, 24, 15, F.TextColor, TextAlignmentOptions.Center);
            }
            if (equipped) F.Text(card.transform, "Equipped", "ĐANG DÙNG", 8, 6, 148, 22, 13, F.Green, TextAlignmentOptions.Center);
        }

        private void BuildPanel()
        {
            if (panel != null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            panel = new GameObject("WeaponShopPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel);
            Transform window = F.Window(surface, "LÒ RÈN", "KHU NGHỈ CHÂN  /  THƯƠNG NHÂN", Close);
            goldText = F.Text(window, "Gold", "", 1190, 39, 392, 42, 24, F.Gold, TextAlignmentOptions.Right);

            Transform merchant = F.Frame(window, "Merchant", 24, 122, 372, 724, F.Hex("231F23"));
            Image portrait = F.Icon(merchant, "ForgeKeeper", merchantPortrait, 6, 6, 360, 712);
            portrait.preserveAspect = false;
            F.Text(merchant, "MerchantTitle", "THỢ RÈN", 24, 28, 324, 40, 27, F.Gold, TextAlignmentOptions.Center);
            F.Text(merchant, "MerchantSubtitle", "THÉP TỐT. HÀNH TRÌNH DÀI.", 18, 69, 336, 24, 14, F.Muted, TextAlignmentOptions.Center);
            F.Box(merchant, "QuoteShade", 6, 592, 360, 126, new Color(0.08f, 0.065f, 0.07f, 0.84f));
            F.Text(merchant, "Quote", "“Một thanh kiếm tốt sẽ không\nbao giờ bỏ rơi chủ nhân.”", 25, 610, 322, 64, 21,
                F.Gold, TextAlignmentOptions.Center).fontStyle = FontStyles.Italic;
            F.Text(merchant, "Signature", "TRANG BỊ CHO MỌI CHUYẾN ĐI", 20, 681, 332, 20, 12, F.Muted, TextAlignmentOptions.Center);

            Transform stock = F.Frame(window, "Stock", 420, 122, 730, 724, F.Panel);
            catalogTitle = F.Text(stock, "StockTitle", "HÀNG HÓA", 20, 19, 688, 36, 24);
            for (int i = 0; i < 5; i++)
            {
                EquipmentSlot slot = (EquipmentSlot)i;
                tabs.Add(F.Button(stock, "Tab" + slot, F.SlotLabel(slot).ToUpperInvariant(), 20 + i * 139, 72, 131, 40, () => SelectSlot(slot)));
            }
            shopScroll = F.Grid(stock, "CatalogViewport", 20, 132, 692, 506, new Vector2(162, 242), 4, out cardsRoot);
            emptyText = F.Text(stock, "Empty", "Chưa có hàng trong nhóm này.", 40, 280, 650, 90, 23, F.Gold, TextAlignmentOptions.Center);
            emptyText.gameObject.SetActive(false);
            F.Line(stock, 22, 654, 686);
            offerText = F.Text(stock, "Offers", "", 22, 668, 395, 32, 15, F.Green);
            rerollButton = F.Button(stock, "RerollOffers", "ĐỔI ƯU ĐÃI · 100 VÀNG", 438, 667, 270, 38, () => RotateFeaturedOffers(true));
            details = new EquipmentDetailView(window, 1174, 122, () => HandleItem(selectedItem));
            statusText = F.Text(window, "Status", "", 720, 874, 960, 26, 16, F.Gold, TextAlignmentOptions.Right);
            panel.SetActive(false);
        }
    }
}
