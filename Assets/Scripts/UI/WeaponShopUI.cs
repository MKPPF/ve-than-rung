using System;
using System.Collections.Generic;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public static readonly int[] SalePercentages = { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
        private readonly HashSet<string> featuredOfferIds = new HashSet<string>();
        private readonly Dictionary<string, int> itemDiscounts = new Dictionary<string, int>();
        private readonly List<Button> tabs = new List<Button>();
        private readonly Dictionary<EquipmentData, Button> cards = new Dictionary<EquipmentData, Button>();
        private readonly MenuModalFocus focus = new MenuModalFocus();
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
        private Transform window;
        private bool? compactLayout;
        private static float nextStageSaleAnnouncementTime;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            if (featuredOfferIds.Count == 0)
            {
                RotateFeaturedOffers(false);
            }
            AnnounceCurrentFeaturedSaleIfDue();
        }

        private void Update()
        {
            if (IsOpen) ApplyResponsiveLayout();
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
            ApplyResponsiveLayout();
            focus.Capture(panel.transform);
            stats.GoldChanged += HandleGoldChanged;
            if (featuredOfferIds.Count == 0) RotateFeaturedOffers(false);
            if (selectedItem == null) SelectFirstItem();
            UpdateSelectionHint();
            RefreshCards();
            if (tabs.Count > (int)selectedSlot && tabs[(int)selectedSlot] != null)
            {
                tabs[(int)selectedSlot].Select();
            }
            AnnounceCurrentFeaturedSaleIfDue();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            if (stats != null) stats.GoldChanged -= HandleGoldChanged;
            if (movement != null) movement.enabled = movementWasEnabled;
            GameplayState.ExitModal(this);
            focus.Release();
        }

        private void OnDisable() => Close();
        private void OnDestroy()
        {
            if (stats != null) stats.GoldChanged -= HandleGoldChanged;
            focus.Release();
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

        private bool CanRotateOffers()
        {
            if (catalog == null || inventory == null) return false;
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                int count = 0;
                foreach (EquipmentData item in catalog)
                    if (item != null && item.Slot == slot && !inventory.Owns(item) && ++count > 1) return true;
            }
            return false;
        }

        public static int DiscountedPrice(int originalPrice, float discount)
        {
            return Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0, originalPrice) * (1f - Mathf.Clamp01(discount))));
        }

        private bool IsFeatured(EquipmentData item) => item != null && featuredOfferIds.Contains(item.ItemId);

        public int GetItemDiscountPercent(EquipmentData item)
        {
            if (item == null || !IsFeatured(item)) return 0;
            if (GamePreferences.IsAutomatedTestRun) return 20;
            if (itemDiscounts.TryGetValue(item.ItemId, out int pct) && pct > 0) return pct;
            return 20;
        }

        private int PurchasePrice(EquipmentData item)
        {
            if (item == null) return 0;
            int discount = GetItemDiscountPercent(item);
            if (discount <= 0) return item.Price;
            return DiscountedPrice(item.Price, discount / 100f);
        }

        public void RotateFeaturedOffers(bool chargeGold)
        {
            if (stats == null && inventory == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null)
                {
                    stats = p.GetComponent<PlayerStats>();
                    inventory = p.GetComponent<EquipmentInventory>();
                }
            }
            if (!HasUnownedItems()) return;
            if (chargeGold && (!IsOpen || GameplayState.IsBlockedByOther(this) || !CanRotateOffers())) return;
            if (chargeGold && stats != null && !stats.TrySpendGold(RerollCost))
            {
                if (statusText != null) statusText.text = "Cần " + RerollCost + " vàng để đổi ưu đãi.";
                return;
            }
            offerRotation++;
            var previous = new HashSet<string>(featuredOfferIds);
            featuredOfferIds.Clear();
            itemDiscounts.Clear();

            EquipmentData bestAnnouncedItem = null;
            int bestDiscount = 0;

            if (catalog != null)
            {
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
                {
                    var candidates = new List<EquipmentData>();
                    foreach (EquipmentData item in catalog)
                        if (item != null && item.Slot == slot && (inventory == null || !inventory.Owns(item))) candidates.Add(item);
                    if (candidates.Count == 0) continue;
                    int oldIndex = candidates.FindIndex(item => previous.Contains(item.ItemId));
                    int nextIndex = oldIndex >= 0 ? (oldIndex + 1) % candidates.Count :
                        Mathf.Abs((offerRotation * 7 + (int)slot * 3) % candidates.Count);
                    EquipmentData selected = candidates[nextIndex];
                    featuredOfferIds.Add(selected.ItemId);

                    int salePct = GamePreferences.IsAutomatedTestRun ? 20 :
                        SalePercentages[UnityEngine.Random.Range(0, SalePercentages.Length)];
                    itemDiscounts[selected.ItemId] = salePct;

                    if (salePct > bestDiscount)
                    {
                        bestDiscount = salePct;
                        bestAnnouncedItem = selected;
                    }
                }
            }

            if (bestAnnouncedItem != null && !GamePreferences.IsAutomatedTestRun)
            {
                string notice = "ƯU ĐÃI LÒ RÈN: " + bestAnnouncedItem.DisplayName + " ĐANG GIẢM GIÁ " + bestDiscount + "%! Hãy ghé cửa hàng ngay!";
                TopDownGame.UI.GameplayHUD.ShowNotice(notice, new Color(1f, 0.85f, 0.35f), 3.5f);
            }

            if (chargeGold)
            {
                if (statusText != null)
                {
                    statusText.text = bestAnnouncedItem != null
                        ? "Đã đổi ưu đãi mới: " + bestAnnouncedItem.DisplayName + " (-" + bestDiscount + "%)  •  -100 vàng"
                        : "Đã đổi lượt ưu đãi  •  -100 vàng";
                }
                GameProgressManager.Instance?.CaptureActivePlayer();
                RefreshCards();
            }
        }

        public void AnnounceCurrentFeaturedSaleIfDue()
        {
            if (GamePreferences.IsAutomatedTestRun) return;
            if (Time.unscaledTime < nextStageSaleAnnouncementTime) return;
            nextStageSaleAnnouncementTime = Time.unscaledTime + 180f;

            EquipmentData bestItem = null;
            int bestDiscount = 0;
            if (catalog != null)
            {
                foreach (EquipmentData item in catalog)
                {
                    if (item == null || inventory == null || inventory.Owns(item)) continue;
                    int pct = GetItemDiscountPercent(item);
                    if (pct > bestDiscount)
                    {
                        bestDiscount = pct;
                        bestItem = item;
                    }
                }
            }
            if (bestItem != null && bestDiscount > 0)
            {
                GameplayHUD.ShowNotice("ƯU ĐÃI LÒ RÈN: " + bestItem.DisplayName + " ĐANG GIẢM GIÁ " + bestDiscount + "%! Ghé thợ rèn để mua ngay!",
                    new Color(1f, 0.85f, 0.35f), 3.5f);
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
                if (IsFeatured(item) && inventory != null && !inventory.Owns(item)) { selectedItem = item; return; }
            }
        }

        private void SelectSlot(EquipmentSlot slot)
        {
            if (!IsOpen) return;
            selectedSlot = slot;
            SelectFirstItem();
            RefreshCards();
            if (shopScroll != null) shopScroll.verticalNormalizedPosition = 1;
            UpdateSelectionHint();
        }

        private void SelectItem(EquipmentData item)
        {
            if (!IsOpen) return;
            selectedItem = item;
            RefreshCards();
            UpdateSelectionHint();
        }

        private void UpdateSelectionHint()
        {
            if (statusText == null) return;
            if (selectedItem == null)
            {
                statusText.text = "Chưa có hàng trong nhóm này.";
                return;
            }
            if (inventory != null && inventory.Owns(selectedItem))
            {
                statusText.text = "Món đã sở hữu có thể trang bị lại miễn phí.";
                return;
            }
            int discount = GetItemDiscountPercent(selectedItem);
            if (discount > 0)
            {
                statusText.text = "Ưu đãi giảm giá " + discount + "% cho " + selectedItem.DisplayName + "! Mua để trang bị ngay.";
            }
            else
            {
                statusText.text = "Giá gốc: " + selectedItem.Price + " vàng. Mua sẽ trang bị ngay món đã chọn.";
            }
        }

        private void HandleItem(EquipmentData item)
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || inventory == null || item == null ||
                inventory.GetEquipped(item.Slot) == item) return;
            inventory.TryBuyAndEquip(item, PurchasePrice(item), out string message);
            selectedItem = item;
            if (statusText != null) statusText.text = message;
            RefreshCards();
        }

        private void HandleGoldChanged(int amount)
        {
            if (IsOpen) RefreshCards();
        }

        private void RefreshCards()
        {
            if (panel == null || details == null || cardsRoot == null)
            {
                BuildPanel();
            }
            if (inventory == null || stats == null || cardsRoot == null) return;
            if (goldText != null) goldText.text = stats.Gold.ToString("N0") + "  VÀNG";
            var visible = new List<EquipmentData>();
            if (catalog != null)
                foreach (EquipmentData item in catalog)
                    if (item != null && item.Slot == selectedSlot) visible.Add(item);
            visible.Sort((a, b) => a.Price.CompareTo(b.Price));
            bool rebuild = cards.Count != visible.Count;
            if (!rebuild)
            {
                foreach (EquipmentData item in visible)
                {
                    if (!cards.ContainsKey(item) || cards[item] == null)
                    {
                        rebuild = true;
                        break;
                    }
                }
            }
            if (rebuild)
            {
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                    EventSystem.current.currentSelectedGameObject.transform.IsChildOf(cardsRoot))
                {
                    if (tabs.Count > (int)selectedSlot && tabs[(int)selectedSlot] != null)
                        tabs[(int)selectedSlot].Select();
                }
                F.Clear(cardsRoot);
                cards.Clear();
                foreach (EquipmentData item in visible) CreateCard(item);
            }
            foreach (EquipmentData item in visible)
            {
                if (cards.TryGetValue(item, out Button card) && card != null)
                {
                    RefreshCard(item, card);
                }
            }
            if (catalogTitle != null) catalogTitle.text = "HÀNG HÓA  <color=#ABA69E>/  " + visible.Count + " món</color>";
            if (emptyText != null) emptyText.gameObject.SetActive(visible.Count == 0);
            for (int i = 0; i < tabs.Count; i++)
            {
                if (tabs[i] == null) continue;
                bool active = i == (int)selectedSlot;
                Transform inset = tabs[i].transform.Find("Inset");
                if (inset != null) inset.GetComponent<Image>().color = active ? F.Hex("695439") : F.Hex("303443");
                Transform label = tabs[i].transform.Find("Label");
                if (label != null) label.GetComponent<TMP_Text>().color = active ? F.Gold : F.Muted;
            }
            bool canRotate = CanRotateOffers();
            if (rerollButton != null)
            {
                rerollButton.interactable = stats.Gold >= RerollCost && canRotate;
                TMP_Text rerollText = rerollButton.GetComponentInChildren<TMP_Text>();
                if (rerollText != null)
                {
                    rerollText.text = !canRotate ? "KHÔNG CÒN ƯU ĐÃI KHÁC" :
                        stats.Gold < RerollCost ? "THIẾU " + (RerollCost - stats.Gold) + " VÀNG ĐỂ ĐỔI" :
                        GamePreferences.IsAutomatedTestRun ? "ĐỔI ƯU ĐÃI · 100 VÀNG" : "ĐỔI ƯU ĐÃI (5%-50%) · 100 VÀNG";
                }
            }
            if (offerText != null)
            {
                offerText.text = HasUnownedItems() ? (GamePreferences.IsAutomatedTestRun ? "GIẢM 20% CHO CÁC MÓN ĐÁNH DẤU" : "ƯU ĐÃI GIẢM TỪ 5% ĐẾN 50% CHO CÁC MÓN ĐÁNH DẤU") : "ĐÃ SỞ HỮU TOÀN BỘ TRANG BỊ";
            }
            if (selectedItem == null)
            {
                details?.Show(null, inventory, true);
                return;
            }
            bool owned = inventory.Owns(selectedItem);
            bool equipped = inventory.GetEquipped(selectedItem.Slot) == selectedItem;
            int discountPct = GetItemDiscountPercent(selectedItem);
            int price = PurchasePrice(selectedItem);
            string priceLabel;
            if (owned)
            {
                priceLabel = "ĐÃ SỞ HỮU";
            }
            else if (discountPct > 0)
            {
                priceLabel = "<s><color=#ABA69E>" + selectedItem.Price + "</color></s>  " + price + " VÀNG  <color=#82C5A1>-" + discountPct + "%</color>";
            }
            else
            {
                priceLabel = price + " VÀNG";
            }
            details?.Show(selectedItem, inventory, true, priceLabel);
            if (details != null && details.Action != null && details.ActionLabel != null)
            {
                details.ActionLabel.text = equipped ? "ĐANG TRANG BỊ" : owned ? "TRANG BỊ" :
                    stats.Gold >= price ? "MUA & TRANG BỊ" : "THIẾU " + (price - stats.Gold) + " VÀNG";
                details.Action.interactable = !equipped && (owned || stats.Gold >= price);
            }
            if (EventSystem.current != null)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (details != null && details.Action != null && (selected == null || selected == details.Action.gameObject && !details.Action.interactable ||
                    rerollButton != null && selected == rerollButton.gameObject && !rerollButton.interactable))
                {
                    if (tabs.Count > (int)selectedSlot && tabs[(int)selectedSlot] != null)
                        tabs[(int)selectedSlot].Select();
                }
            }
        }

        private void CreateCard(EquipmentData item)
        {
            if (item == null || cardsRoot == null) return;
            Button card = F.Button(cardsRoot, item.ItemId + "Card", "", 0, 0, 162, 242, () => SelectItem(item));
            cards[item] = card;
            card.gameObject.AddComponent<ScrollSelectionIntoView>();
            F.Icon(card.transform, "Icon", item.Icon, 22, 28, 120, 120);
            F.Box(card.transform, "RarityMark", 60, 152, 44, 2, item.RarityColor);
            F.Text(card.transform, "ItemName", item.DisplayName, 8, 158, 148, 46, 19,
                F.TextColor, TextAlignmentOptions.Center);
            F.Text(card.transform, "Price", "", 8, 211, 148, 26, 18, F.Gold, TextAlignmentOptions.Center);
            F.Box(card.transform, "OfferRibbon", 6, 6, 68, 24, F.Hex("7B2B2B"));
            F.Text(card.transform, "Offer", "-20%", 6, 6, 68, 24, 15, F.TextColor, TextAlignmentOptions.Center);
            F.Text(card.transform, "Equipped", "ĐANG DÙNG", 8, 6, 148, 22, 13, F.Green, TextAlignmentOptions.Center);
        }

        private void RefreshCard(EquipmentData item, Button card)
        {
            if (item == null || card == null || inventory == null) return;
            bool owned = inventory.Owns(item);
            bool equipped = inventory.GetEquipped(item.Slot) == item;
            bool selected = item == selectedItem;
            int discountPct = GetItemDiscountPercent(item);
            bool onSale = discountPct > 0 && !owned;

            Transform brass = card.transform.Find("Brass");
            if (brass != null) brass.GetComponent<Image>().color = selected ? F.Gold : F.Edge;

            Transform inset = card.transform.Find("Inset");
            if (inset != null) inset.GetComponent<Image>().color = Color.Lerp(F.Ink, item.RarityColor, selected ? 0.17f : 0.065f);

            Transform priceTrans = card.transform.Find("Price");
            if (priceTrans != null)
            {
                TMP_Text price = priceTrans.GetComponent<TMP_Text>();
                if (owned)
                {
                    price.text = "ĐÃ CÓ";
                    price.color = F.Green;
                }
                else if (onSale)
                {
                    price.text = "<s>" + item.Price + "</s> " + PurchasePrice(item) + " v";
                    price.color = F.Gold;
                }
                else
                {
                    price.text = item.Price + " vàng";
                    price.color = F.Gold;
                }
            }

            Transform ribbon = card.transform.Find("OfferRibbon");
            if (ribbon != null) ribbon.gameObject.SetActive(onSale);

            Transform offer = card.transform.Find("Offer");
            if (offer != null)
            {
                offer.gameObject.SetActive(onSale);
                if (onSale)
                {
                    TMP_Text offerTxt = offer.GetComponent<TMP_Text>();
                    if (offerTxt != null) offerTxt.text = "-" + discountPct + "%";
                }
            }

            Transform equippedTrans = card.transform.Find("Equipped");
            if (equippedTrans != null) equippedTrans.gameObject.SetActive(equipped);
        }

        private void BuildPanel()
        {
            if (panel != null && details != null && window != null && cardsRoot != null) return;
            if (panel != null)
            {
                Destroy(panel);
                panel = null;
                details = null;
                window = null;
                cardsRoot = null;
            }
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }
            if (canvas == null) return;
            panel = new GameObject("WeaponShopPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel);
            window = F.Window(surface, "LÒ RÈN", "KHU NGHỈ CHÂN  /  THƯƠNG NHÂN", Close);
            window.Find("KeyHints").GetComponent<TMP_Text>().text = "MŨI TÊN / ENTER  Chọn     •     CUỘN  Xem đồ     •     ESC  Đóng";
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
            tabs.Clear();
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

        private void ApplyResponsiveLayout()
        {
            if (panel == null || window == null) return;
            Rect available = ((RectTransform)panel.transform).rect;
            bool compact = available.width / Mathf.Max(1f, available.height) < 1.5f;
            if (compactLayout == compact) return;
            compactLayout = compact;
            panel.GetComponent<EquipmentPanelLayout>()?.ConfigureWindow(window, compact, goldText, statusText);
            Transform merchantTrans = window.Find("Merchant");
            if (merchantTrans != null) merchantTrans.gameObject.SetActive(!compact);
            Transform stockTrans = window.Find("Stock");
            if (stockTrans != null) SetHorizontal(stockTrans, compact ? 24 : 420, 730);
            Transform detailTrans = window.Find("SelectedEquipment");
            if (detailTrans != null) SetHorizontal(detailTrans, compact ? 778 : 1174, 514);
        }

        private static void SetHorizontal(Transform target, float x, float width)
        {
            if (target == null) return;
            var rect = (RectTransform)target;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        }
    }
}
