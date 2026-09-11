using TMPro;
using TopDownGame.Equipment;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>One visual language for equipment screens: inset slots, brass edges and quiet surfaces.</summary>
    public static class FantasyEquipmentUI
    {
        public static readonly Color Ink = Hex("181C29");
        public static readonly Color Panel = Hex("222634");
        public static readonly Color Edge = Hex("746047");
        public static readonly Color Gold = Hex("E5C28A");
        public static readonly Color TextColor = Hex("EEE7D8");
        public static readonly Color Muted = Hex("ABA69E");
        public static readonly Color Green = Hex("82C5A1");

        public static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            Image image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Transform Frame(Transform parent, string name, float x, float y, float w, float h, Color fill)
        {
            var outer = Box(parent, name, x, y, w, h, Hex("100F17"));
            Box(outer.transform, "Brass", 2, 2, w - 4, h - 4, Edge);
            Box(outer.transform, "Inset", 4, 4, w - 8, h - 8, fill);
            Box(outer.transform, "Highlight", 5, 5, w - 10, 1, Hex("9C8260"));
            return outer.transform;
        }

        public static TMP_Text Text(Transform parent, string name, string value,
            float x, float y, float w, float h, float size = 20,
            Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            TMP_Text text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color ?? TextColor;
            text.alignment = align;
            text.raycastTarget = false;
            EquipmentPanelLayout.StyleText(text, size);
            return text;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite,
            float x, float y, float w, float h)
        {
            Image image = Box(parent, name, x, y, w, h, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = sprite != null;
            return image;
        }

        public static Button Button(Transform parent, string name, string label,
            float x, float y, float w, float h, UnityAction action, bool primary = false)
        {
            Transform frame = Frame(parent, name, x, y, w, h, primary ? Hex("A4773D") : Hex("303443"));
            var button = frame.gameObject.AddComponent<Button>();
            Image face = frame.Find("Inset").GetComponent<Image>();
            frame.GetComponent<Image>().raycastTarget = true;
            button.targetGraphic = face;
            EquipmentPanelLayout.StyleButton(button);
            if (action != null) button.onClick.AddListener(action);
            TMP_Text text = Text(frame, "Label", label, 8, 4, w - 16, h - 8, 18,
                primary ? Hex("FFF0CB") : TextColor, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        public static Transform Window(Transform surface, string title, string eyebrow, UnityAction close)
        {
            Box(surface, "WindowShadow", 94, 76, 1736, 938, new Color(0, 0, 0, 0.6f));
            Transform frame = Frame(surface, "EquipmentWindow", 104, 80, 1712, 920, Ink);
            Box(frame, "Header", 6, 6, 1700, 100, Hex("282631"));
            Line(frame, 28, 106, 1656);
            Text(frame, "Eyebrow", eyebrow, 34, 17, 1050, 22, 15, Muted);
            TMP_Text heading = Text(frame, "Title", title, 32, 40, 950, 52, 36, Gold);
            heading.characterSpacing = 3;
            Button(frame, "Close", "×", 1626, 26, 54, 54, close);
            foreach (Vector2 point in new[] { new Vector2(12,12), new Vector2(1698,12), new Vector2(12,906), new Vector2(1698,906) })
            {
                Image stud = Box(frame, "CornerStud", point.x - 4, point.y - 4, 8, 8, Gold);
                stud.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            }
            Line(frame, 28, 862, 1656);
            Text(frame, "KeyHints", "CHUỘT  Chọn đồ     •     E  Đang dùng     •     ESC  Đóng", 32, 874, 750, 26, 16, Muted);
            return frame;
        }

        public static void Line(Transform parent, float x, float y, float w)
        {
            Box(parent, "Divider", x, y, w, 1, Edge);
        }

        public static void Medallion(Transform parent, float x, float y, float diameter, Color color)
        {
            EquipmentMedallion ring = Rect(parent, "Medallion", x, y, diameter, diameter)
                .gameObject.AddComponent<EquipmentMedallion>();
            ring.color = new Color(color.r, color.g, color.b, 0.35f);
            ring.raycastTarget = false;
        }

        public static Button ItemSlot(Transform parent, EquipmentData item, float x, float y,
            float size, bool selected, bool equipped, UnityAction select, string badge = null)
        {
            Color rarity = item != null ? item.RarityColor : Edge;
            Transform frame = Frame(parent, item != null ? item.ItemId + "Slot" : "EmptySlot", x, y, size, size, Ink);
            frame.Find("Brass").GetComponent<Image>().color = selected ? Gold : Color.Lerp(Edge, rarity, 0.45f);
            Image face = frame.Find("Inset").GetComponent<Image>();
            face.color = Color.Lerp(Ink, rarity, selected ? 0.2f : 0.055f);
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            frame.GetComponent<Image>().raycastTarget = true;
            EquipmentPanelLayout.StyleButton(button);
            if (select != null) button.onClick.AddListener(select);
            button.interactable = item != null;
            if (item != null)
            {
                Icon(frame, "Icon", item.Icon, 12, 10, size - 24, size - 24);
                Box(frame, "RarityMark", size / 2 - 12, size - 8, 24, 2, rarity);
                if (equipped)
                {
                    Box(frame, "EquippedBadge", size - 27, 4, 23, 21, Hex("37614B"));
                    Text(frame, "Equipped", "E", size - 27, 4, 23, 21, 13, TextColor, TextAlignmentOptions.Center);
                }
                if (!string.IsNullOrEmpty(badge))
                    Text(frame, "Badge", badge, 5, 3, size - 10, 20, 13, Gold);
            }
            else Text(frame, "Empty", "·", 0, 0, size, size, 28, Edge, TextAlignmentOptions.Center);
            return button;
        }

        public static ScrollRect Grid(Transform parent, string name, float x, float y,
            float w, float h, Vector2 cell, int columns, out Transform content)
        {
            RectTransform viewport = Rect(parent, name, x, y, w, h);
            viewport.gameObject.AddComponent<Image>().color = Ink;
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            RectTransform items = Rect(viewport, "Items", 0, 0, w - 12, 0);
            var grid = items.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell;
            grid.spacing = new Vector2(8, 8);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            items.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = items;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 36;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            EquipmentPanelLayout.AddScrollbar(scroll);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            content = items;
            return scroll;
        }

        public static void Clear(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        public static string SlotLabel(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => "Vũ khí", EquipmentSlot.Helmet => "Mũ",
            EquipmentSlot.Armor => "Giáp", EquipmentSlot.Pants => "Quần", _ => "Dây chuyền"
        };
    }

    /// <summary>Details are shared between bag and forge so only the selected item carries dense text.</summary>
    public sealed class EquipmentDetailView
    {
        public readonly Button Action;
        public readonly TMP_Text ActionLabel;
        private readonly TMP_Text name, rarity, description, stats, comparison, value, state;
        private readonly Image icon;

        public EquipmentDetailView(Transform parent, float x, float y, UnityAction action)
        {
            Transform root = FantasyEquipmentUI.Frame(parent, "SelectedEquipment", x, y, 514, 724,
                FantasyEquipmentUI.Hex("24232E"));
            FantasyEquipmentUI.Text(root, "Heading", "TRANG BỊ ĐÃ CHỌN", 24, 16, 460, 24, 15, FantasyEquipmentUI.Muted);
            FantasyEquipmentUI.Medallion(root, 153, 52, 208, FantasyEquipmentUI.Gold);
            icon = FantasyEquipmentUI.Icon(root, "DetailIcon", null, 169, 65, 176, 176);
            state = FantasyEquipmentUI.Text(root, "State", "", 26, 240, 462, 26, 15,
                FantasyEquipmentUI.Green, TextAlignmentOptions.Center);
            name = FantasyEquipmentUI.Text(root, "Name", "Chọn một trang bị", 24, 272, 466, 42, 28,
                FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            rarity = FantasyEquipmentUI.Text(root, "Rarity", "", 24, 315, 466, 28, 17,
                FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            description = FantasyEquipmentUI.Text(root, "Description", "", 28, 352, 458, 42, 17,
                FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Line(root, 28, 404, 458);
            stats = FantasyEquipmentUI.Text(root, "Stats", "", 32, 420, 450, 118, 19);
            stats.alignment = TextAlignmentOptions.TopLeft;
            comparison = FantasyEquipmentUI.Text(root, "Comparison", "", 32, 546, 450, 55, 17);
            comparison.alignment = TextAlignmentOptions.TopLeft;
            value = FantasyEquipmentUI.Text(root, "Value", "", 28, 606, 458, 34, 24,
                FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            Action = FantasyEquipmentUI.Button(root, "Action", "CHỌN TRANG BỊ", 28, 654, 458, 48, action, true);
            ActionLabel = Action.transform.Find("Label").GetComponent<TMP_Text>();
        }

        public void Show(EquipmentData item, EquipmentInventory inventory, bool shop, string price = null)
        {
            icon.enabled = item != null && item.Icon != null;
            icon.sprite = item != null ? item.Icon : null;
            if (item == null)
            {
                name.text = "Chọn một trang bị";
                rarity.text = stats.text = comparison.text = value.text = state.text = "";
                description.text = "Chọn ô đồ để xem chỉ số và so sánh.";
                Action.interactable = false;
                ActionLabel.text = "CHỌN TRANG BỊ";
                return;
            }
            EquipmentData current = inventory.GetEquipped(item.Slot);
            bool equipped = current == item;
            int level = inventory.GetEnhancementLevel(item);
            int power = inventory.EffectivePowerScore(item);
            int delta = power - inventory.EffectivePowerScore(current);
            name.text = item.DisplayName + (level > 0 ? "  +" + level : "");
            name.color = Color.Lerp(item.RarityColor, FantasyEquipmentUI.TextColor, 0.3f);
            rarity.text = item.RarityLabel + "  /  " + item.TypeLabel;
            rarity.color = item.RarityColor;
            description.text = item.Description;
            state.text = equipped ? "ĐANG TRANG BỊ" : "SỨC MẠNH  " + power;
            stats.text = BuildStatRows(item, current, inventory);
            comparison.text = equipped ? "Trang bị này đang được sử dụng." :
                "So với " + (current != null ? current.DisplayName : "ô đang trống") +
                "\n<color=" + (delta >= 0 ? "#82C5A1" : "#EB947F") + ">" +
                (delta >= 0 ? "+" : "") + delta + " sức mạnh</color>";
            value.text = shop ? price : "SỨC MẠNH  " + power;
            ActionLabel.text = shop ? "MUA & TRANG BỊ" : equipped ? "THÁO TRANG BỊ" : "TRANG BỊ";
            Action.interactable = true;
        }

        private static string BuildStatRows(EquipmentData item, EquipmentData current, EquipmentInventory inventory)
        {
            float multiplier = EquipmentInventory.EnhancementMultiplier(inventory.GetEnhancementLevel(item));
            float currentMultiplier = EquipmentInventory.EnhancementMultiplier(inventory.GetEnhancementLevel(current));
            var text = new System.Text.StringBuilder();
            AppendRow(text, "Sát thương", item.BonusDamage * multiplier, (current?.BonusDamage ?? 0) * currentMultiplier, current == item);
            AppendRow(text, "Giáp", item.BonusArmor * multiplier, (current?.BonusArmor ?? 0) * currentMultiplier, current == item);
            AppendRow(text, "Máu tối đa", item.BonusMaxHealth * multiplier, (current?.BonusMaxHealth ?? 0) * currentMultiplier, current == item);
            AppendRow(text, "Chí mạng", item.BonusCriticalChance * multiplier * 100, (current?.BonusCriticalChance ?? 0) * currentMultiplier * 100, current == item, "%");
            AppendRow(text, "Tốc hồi chiêu", item.BonusCooldownRate * multiplier * 100, (current?.BonusCooldownRate ?? 0) * currentMultiplier * 100, current == item, "%");
            return text.ToString();
        }

        private static void AppendRow(System.Text.StringBuilder text, string label, float value, float current, bool equipped, string unit = "")
        {
            if (text.Length > 0) text.AppendLine();
            text.Append("<color=#ABA69E>").Append(label).Append("</color><pos=52%>")
                .Append(value > 0 ? "+" : "").Append(value.ToString("0.#")).Append(unit);
            float delta = value - current;
            if (!equipped && !Mathf.Approximately(delta, 0))
                text.Append("<pos=77%><color=").Append(delta > 0 ? "#82C5A1>" : "#EB947F>")
                    .Append(delta > 0 ? "+" : "").Append(delta.ToString("0.#")).Append(unit).Append("</color>");
        }
    }
}
