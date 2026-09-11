using System.Collections.Generic;
using TMPro;
using TopDownGame.Equipment;
using TopDownGame.Player;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    public sealed class StorageChestUI : MonoBehaviour
    {
        private GameObject panel;
        private Transform playerItemsRoot;
        private Transform chestItemsRoot;
        private TMP_Text statusText;
        private EquipmentInventory inventory;
        private PlayerMovement movement;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Update()
        {
            if (IsOpen && Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Open(PlayerInteraction player)
        {
            if (player == null)
            {
                return;
            }

            BuildPanel();
            inventory = player.GetComponent<EquipmentInventory>();
            movement = player.GetComponent<PlayerMovement>();
            if (panel == null || inventory == null)
            {
                return;
            }

            if (movement != null)
            {
                movement.enabled = false;
            }

            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            statusText.text = "Chọn đồ bên trái để cất, bên phải để lấy.";
            Refresh();
        }

        public void Close()
        {
            panel?.SetActive(false);
            if (movement != null)
            {
                movement.enabled = true;
            }
        }

        private void Refresh()
        {
            ClearChildren(playerItemsRoot);
            ClearChildren(chestItemsRoot);
            GameProgressManager progress = GameProgressManager.Instance;
            if (progress == null || inventory == null)
            {
                return;
            }

            List<EquipmentData> owned = new List<EquipmentData>(inventory.OwnedItems());
            foreach (EquipmentData item in owned)
            {
                EquipmentData captured = item;
                CreateTransferCard(playerItemsRoot, item, "CẤT VÀO RƯƠNG", () =>
                {
                    inventory.UnequipIfMatching(captured.ItemId);
                    if (progress.MoveItemToChest(captured.ItemId))
                    {
                        progress.CaptureActivePlayer();
                        statusText.text = "Đã cất " + captured.DisplayName + ".";
                        Refresh();
                    }
                });
            }

            foreach (string itemId in progress.ChestItems)
            {
                EquipmentData item = inventory.FindById(itemId);
                if (item == null) continue;
                EquipmentData captured = item;
                CreateTransferCard(chestItemsRoot, item, "LẤY RA", () =>
                {
                    if (progress.MoveItemFromChest(captured.ItemId))
                    {
                        progress.CaptureActivePlayer();
                        statusText.text = "Đã lấy " + captured.DisplayName + ".";
                        Refresh();
                    }
                });
            }

            if (owned.Count == 0)
            {
                CreateEmptyMessage(playerItemsRoot, "Hành trang trống");
            }

            if (progress.ChestItems.Count == 0)
            {
                CreateEmptyMessage(chestItemsRoot, "Rương đang trống");
            }
        }

        private void BuildPanel()
        {
            if (panel != null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            panel = new GameObject("StorageChestPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.045f, 1f);

            TMP_Text title = CreateText(panel.transform, "Title", "RƯƠNG ĐỒ", 44f);
            Place(title.rectTransform, new Vector2(0f, -28f),
                new Vector2(900f, 60f), new Vector2(0.5f, 1f));
            title.color = new Color(1f, 0.78f, 0.25f);

            Button close = CreateButton(panel.transform, "Close", "ĐÓNG [ESC]",
                new Vector2(-35f, -30f), new Vector2(210f, 54f));
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            close.onClick.AddListener(Close);

            playerItemsRoot = CreateSide(panel.transform, "PlayerItems", "HÀNH TRANG",
                new Vector2(0.04f, 0.12f), new Vector2(0.49f, 0.88f));
            chestItemsRoot = CreateSide(panel.transform, "ChestItems", "TRONG RƯƠNG",
                new Vector2(0.51f, 0.12f), new Vector2(0.96f, 0.88f));

            statusText = CreateText(panel.transform, "Status", string.Empty, 21f);
            Place(statusText.rectTransform, new Vector2(0f, 28f),
                new Vector2(1600f, 45f), new Vector2(0.5f, 0f));
            statusText.color = new Color(0.65f, 0.9f, 1f);
        }

        private static Transform CreateSide(Transform parent, string name, string title,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject side = new GameObject(name, typeof(RectTransform), typeof(Image));
            side.transform.SetParent(parent, false);
            RectTransform sideRect = side.GetComponent<RectTransform>();
            sideRect.anchorMin = anchorMin;
            sideRect.anchorMax = anchorMax;
            sideRect.offsetMin = Vector2.zero;
            sideRect.offsetMax = Vector2.zero;
            side.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.085f, 1f);

            TMP_Text heading = CreateText(side.transform, "Heading", title, 28f);
            Place(heading.rectTransform, new Vector2(0f, -18f),
                new Vector2(700f, 46f), new Vector2(0.5f, 1f));

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform),
                typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            viewport.transform.SetParent(side.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0.03f, 0.04f);
            viewportRect.anchorMax = new Vector2(0.97f, 0.91f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 1f);

            GameObject content = new GameObject("Content", typeof(RectTransform),
                typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(330f, 280f);
            grid.spacing = new Vector2(20f, 20f);
            grid.padding = new RectOffset(22, 22, 22, 22);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            content.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            return content.transform;
        }

        private static void CreateTransferCard(Transform parent, EquipmentData item,
            string actionLabel, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(parent, item.ItemId, string.Empty,
                Vector2.zero, new Vector2(330f, 280f));
            button.GetComponent<Image>().color = Color.Lerp(
                new Color(0.045f, 0.065f, 0.1f, 1f), item.RarityColor, 0.16f);
            button.onClick.AddListener(action);

            TMP_Text name = CreateText(button.transform, "Name", item.DisplayName, 21f);
            Place(name.rectTransform, new Vector2(0f, -9f),
                new Vector2(300f, 34f), new Vector2(0.5f, 1f));
            name.color = item.RarityColor;

            TMP_Text rarity = CreateText(button.transform, "Rarity",
                item.RarityLabel.ToUpperInvariant() + "  •  SỨC MẠNH " +
                item.PowerScore, 13f);
            Place(rarity.rectTransform, new Vector2(0f, -40f),
                new Vector2(300f, 24f), new Vector2(0.5f, 1f));
            rarity.color = item.RarityColor;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(145f, 145f);
            iconRect.anchoredPosition = new Vector2(0f, 10f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = item.Icon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text label = CreateText(button.transform, "Action", actionLabel, 17f);
            Place(label.rectTransform, new Vector2(0f, 12f),
                new Vector2(300f, 34f), new Vector2(0.5f, 0f));
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.65f, 0.9f, 1f);

            TMP_Text stats = CreateText(button.transform, "Stats",
                item.BuildStatSummary(), 14f);
            Place(stats.rectTransform, new Vector2(0f, 45f),
                new Vector2(305f, 38f), new Vector2(0.5f, 0f));
            stats.textWrappingMode = TextWrappingModes.Normal;
            stats.color = new Color(0.82f, 0.88f, 0.96f);
        }

        private static void CreateEmptyMessage(Transform parent, string message)
        {
            TMP_Text empty = CreateText(parent, "Empty", message, 21f);
            empty.rectTransform.sizeDelta = new Vector2(330f, 100f);
            empty.color = new Color(0.5f, 0.58f, 0.68f);
        }

        private static TMP_Text CreateText(Transform parent, string name,
            string value, float size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            TMP_Text text = gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 position, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform),
                typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            gameObject.GetComponent<Image>().color = new Color(0.2f, 0.34f, 0.5f, 1f);
            Button button = gameObject.GetComponent<Button>();
            if (!string.IsNullOrEmpty(label))
            {
                TMP_Text text = CreateText(gameObject.transform, "Label", label, 18f);
                Stretch(text.rectTransform, 4f);
                text.fontStyle = FontStyles.Bold;
            }
            return button;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size,
            Vector2 anchor)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }
    }
}
