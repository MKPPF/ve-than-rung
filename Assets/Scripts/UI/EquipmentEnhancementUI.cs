using System.Text;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    [DisallowMultipleComponent]
    public sealed class EquipmentEnhancementUI : MonoBehaviour
    {
        private static readonly EquipmentSlot[] Slots =
        {
            EquipmentSlot.Weapon, EquipmentSlot.Helmet, EquipmentSlot.Armor,
            EquipmentSlot.Pants, EquipmentSlot.Necklace
        };

        private GameObject panel;
        private Button launcherButton;
        private Button enhanceButton;
        private TMP_Text launcherLabel;
        private TMP_Text content;
        private TMP_Text enhanceLabel;
        private EquipmentInventory inventory;
        private int selectedSlot;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            inventory = player != null ? player.GetComponent<EquipmentInventory>() : null;
            BuildPanel();
            if (panel != null) panel.SetActive(false);
            if (inventory != null) inventory.EquipmentChanged += HandleEquipmentChanged;
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.HunterEssenceChanged += HandleEssenceChanged;
            }
            Refresh();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.nKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
                return;
            }
            if (!IsOpen) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                selectedSlot = (selectedSlot + Slots.Length - 1) % Slots.Length;
                Refresh();
            }
            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                selectedSlot = (selectedSlot + 1) % Slots.Length;
                Refresh();
            }
            if (keyboard.digit1Key.wasPressedThisFrame) Select(0);
            if (keyboard.digit2Key.wasPressedThisFrame) Select(1);
            if (keyboard.digit3Key.wasPressedThisFrame) Select(2);
            if (keyboard.digit4Key.wasPressedThisFrame) Select(3);
            if (keyboard.digit5Key.wasPressedThisFrame) Select(4);
            if (keyboard.uKey.wasPressedThisFrame) Enhance();
        }

        public void Open()
        {
            if (panel == null || inventory == null ||
                GameplayState.IsBlockedByOther(this)) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            if (launcherButton != null) launcherButton.gameObject.SetActive(false);
            GameplayState.EnterModal(this);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (launcherButton != null) launcherButton.gameObject.SetActive(true);
            GameplayState.ExitModal(this);
        }

        private void Select(int index)
        {
            selectedSlot = Mathf.Clamp(index, 0, Slots.Length - 1);
            Refresh();
        }

        private void Enhance()
        {
            if (inventory == null) return;
            inventory.TryEnhanceEquipped(Slots[selectedSlot], out _);
            Refresh();
        }

        private void Refresh()
        {
            if (content == null || inventory == null) return;
            GameProgressManager progress = GameProgressManager.Instance;
            EquipmentData selected = inventory.GetEquipped(Slots[selectedSlot]);
            int level = inventory.GetEnhancementLevel(selected);
            int cost = GameProgressManager.EnhancementEssenceCost(level);
            int essence = progress != null ? progress.HunterEssence : 0;

            StringBuilder builder = new StringBuilder(480);
            builder.AppendLine("LÒ RÈN THỢ SĂN  [N]");
            builder.AppendLine("Trang bị trùng tự phân rã thành Tinh hoa săn.");
            builder.AppendLine("Mỗi cấp cường hóa tăng 8% toàn bộ chỉ số của món đồ.");
            builder.AppendLine();
            builder.AppendLine("Tinh hoa hiện có: " + essence);
            builder.AppendLine("←/→ hoặc 1-5: chọn ô   |   U: cường hóa");
            builder.AppendLine();
            for (int i = 0; i < Slots.Length; i++)
            {
                EquipmentData item = inventory.GetEquipped(Slots[i]);
                int itemLevel = inventory.GetEnhancementLevel(item);
                builder.AppendLine((i == selectedSlot ? "> " : "  ") + (i + 1) +
                    "  " + SlotLabel(Slots[i]) + ": " +
                    (item != null ? item.DisplayName + "  +" + itemLevel +
                    "   [SM " + inventory.EffectivePowerScore(item) + "]" : "Trống"));
            }
            builder.AppendLine();
            if (selected != null)
            {
                builder.AppendLine("Đang chọn: " + selected.DisplayName + "  +" + level +
                    "/" + EquipmentInventory.MaximumEnhancementLevel);
                builder.AppendLine(level >= EquipmentInventory.MaximumEnhancementLevel
                    ? "Đã đạt cường hóa tối đa."
                    : "Cấp tiếp theo cần " + cost + " Tinh hoa.");
            }
            else builder.AppendLine("Hãy trang bị một món đồ vào ô đang chọn.");
            content.text = builder.ToString();

            bool canEnhance = selected != null &&
                level < EquipmentInventory.MaximumEnhancementLevel && essence >= cost;
            if (enhanceButton != null) enhanceButton.interactable = canEnhance;
            if (enhanceLabel != null)
            {
                enhanceLabel.text = selected == null ? "Ô TRANG BỊ ĐANG TRỐNG"
                    : level >= EquipmentInventory.MaximumEnhancementLevel ? "ĐÃ TỐI ĐA"
                    : "CƯỜNG HÓA [U]  -  " + cost + " TINH HOA";
            }
            if (launcherLabel != null)
            {
                launcherLabel.text = "N  LÒ RÈN  [" + essence + "]";
            }
        }

        private static string SlotLabel(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Helmet => "Mũ",
            EquipmentSlot.Armor => "Giáp",
            EquipmentSlot.Pants => "Quần",
            EquipmentSlot.Necklace => "Dây chuyền",
            _ => "Vũ khí"
        };

        private void BuildPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || panel != null) return;
            launcherButton = CreateButton(canvas.transform, "ForgeLauncher",
                new Vector2(-30f, -208f), new Vector2(205f, 44f),
                new Color(0.48f, 0.24f, 0.08f, 0.97f), true);
            launcherLabel = launcherButton.GetComponentInChildren<TMP_Text>();
            launcherButton.onClick.AddListener(Open);

            panel = new GameObject("EquipmentEnhancementPanel", typeof(RectTransform),
                typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 50f);
            rect.sizeDelta = new Vector2(700f, 500f);
            panel.GetComponent<Image>().color = new Color(0.06f, 0.035f, 0.025f, 0.98f);

            GameObject textObject = new GameObject("Content", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(38f, 86f);
            textRect.offsetMax = new Vector2(-38f, -28f);
            content = textObject.GetComponent<TMP_Text>();
            content.fontSize = 19f;
            content.color = Color.white;
            content.textWrappingMode = TextWrappingModes.Normal;

            enhanceButton = CreateButton(panel.transform, "Enhance",
                new Vector2(0f, 26f), new Vector2(470f, 48f),
                new Color(0.74f, 0.4f, 0.08f, 1f), false);
            enhanceLabel = enhanceButton.GetComponentInChildren<TMP_Text>();
            enhanceButton.onClick.AddListener(Enhance);

            Button close = CreateButton(panel.transform, "ĐÓNG [N]",
                new Vector2(-20f, -18f), new Vector2(110f, 38f),
                new Color(0.45f, 0.16f, 0.18f, 1f), true);
            close.onClick.AddListener(Close);
        }

        private static Button CreateButton(Transform parent, string label,
            Vector2 position, Vector2 size, Color color, bool topRight)
        {
            GameObject root = new GameObject(label, typeof(RectTransform),
                typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = topRight ? Vector2.one : new Vector2(0.5f, 0f);
            rect.pivot = topRight ? Vector2.one : new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            root.GetComponent<Image>().color = color;
            GameObject labelObject = new GameObject("Label", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TMP_Text text = labelObject.GetComponent<TMP_Text>();
            text.text = label;
            text.fontSize = 16f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            return root.GetComponent<Button>();
        }

        private void HandleEquipmentChanged(EquipmentData item) => Refresh();
        private void HandleEssenceChanged(int amount) => Refresh();

        private void OnDestroy()
        {
            if (inventory != null) inventory.EquipmentChanged -= HandleEquipmentChanged;
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.HunterEssenceChanged -= HandleEssenceChanged;
            }
            GameplayState.ExitModal(this);
        }
    }
}
