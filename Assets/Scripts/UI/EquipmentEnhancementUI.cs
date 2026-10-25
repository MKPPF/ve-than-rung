using System.Text;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Equipment;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using F = TopDownGame.UI.FantasyEquipmentUI;

namespace TopDownGame.UI
{
    /// <summary>Previews the actual next enhancement before spending essence, with one transaction per input frame.</summary>
    [DisallowMultipleComponent]
    public sealed class EquipmentEnhancementUI : MonoBehaviour
    {
        private static readonly EquipmentSlot[] Slots =
        {
            EquipmentSlot.Weapon, EquipmentSlot.Helmet, EquipmentSlot.Armor,
            EquipmentSlot.Pants, EquipmentSlot.Necklace
        };

        private GameObject panel;
        private Button launcherButton, enhanceButton, inventoryButton;
        private TMP_Text launcherLabel, enhanceLabel, balance, nameText, levelText, preview, columns, costText, status;
        private Image selectedIcon;
        private EquipmentInventory inventory;
        private InventoryUI inventoryUI;
        private GameProgressManager progress;
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private readonly Button[] slotButtons = new Button[5];
        private readonly Image[] slotIcons = new Image[5];
        private readonly TMP_Text[] slotNames = new TMP_Text[5];
        private readonly TMP_Text[] slotLevels = new TMP_Text[5];
        private int selectedSlot;
        private int lastEnhanceFrame = -1;
        private bool subscribed;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            inventory = player != null ? player.GetComponent<EquipmentInventory>() : null;
            inventoryUI = GetComponentInParent<Canvas>()?.GetComponentInChildren<InventoryUI>();
            progress = GameProgressManager.Instance;
            BuildPanel();
            Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            if (launcherButton != null) launcherButton.gameObject.SetActive(true);
            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            if (subscribed || inventory == null) return;
            inventory.EquipmentChanged += HandleEquipmentChanged;
            if (progress != null) progress.HunterEssenceChanged += HandleEssenceChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (inventory != null) inventory.EquipmentChanged -= HandleEquipmentChanged;
            if (progress != null) progress.HunterEssenceChanged -= HandleEssenceChanged;
            subscribed = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.nKey.wasPressedThisFrame)
            {
                if (IsOpen) Close(); else Open();
                return;
            }
            if (!IsOpen) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (keyboard.iKey.wasPressedThisFrame) { OpenInventory(); return; }
            // Arrows belong to EventSystem navigation. Number keys select equipment directly.
            if (keyboard.digit1Key.wasPressedThisFrame) Select(0);
            if (keyboard.digit2Key.wasPressedThisFrame) Select(1);
            if (keyboard.digit3Key.wasPressedThisFrame) Select(2);
            if (keyboard.digit4Key.wasPressedThisFrame) Select(3);
            if (keyboard.digit5Key.wasPressedThisFrame) Select(4);
            if (keyboard.uKey.wasPressedThisFrame) Enhance();
        }

        public void Open()
        {
            if (!isActiveAndEnabled || IsOpen || panel == null || inventory == null || Time.timeScale <= 0 ||
                GameplayState.IsBlockedByOther(this)) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            focus.Capture(panel.transform);
            launcherButton.gameObject.SetActive(false);
            GameplayState.EnterModal(this);
            status.text = "Chọn ô đang mặc để xem chỉ số và chi phí trước khi cường hóa.";
            Refresh();
            slotButtons[selectedSlot].Select();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (launcherButton != null) launcherButton.gameObject.SetActive(isActiveAndEnabled);
            GameplayState.ExitModal(this);
            focus.Release();
        }

        private void Select(int index)
        {
            if (!IsOpen) return;
            selectedSlot = Mathf.Clamp(index, 0, Slots.Length - 1);
            status.text = "Trang bị trùng tự phân rã thành Tinh hoa săn. Cường hóa được lưu ngay.";
            Refresh();
            slotButtons[selectedSlot].Select();
        }

        private void Enhance()
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || inventory == null ||
                !enhanceButton.interactable || lastEnhanceFrame == Time.frameCount) return;
            lastEnhanceFrame = Time.frameCount;
            inventory.TryEnhanceEquipped(Slots[selectedSlot], out string message);
            status.text = message;
            Refresh();
        }

        private void OpenInventory()
        {
            if (!IsOpen || inventoryUI == null || !inventoryUI.isActiveAndEnabled) return;
            Close();
            inventoryUI.Open();
        }

        private void Refresh()
        {
            int essence = progress != null ? progress.HunterEssence : 0;
            if (launcherLabel != null) launcherLabel.text = "N  CƯỜNG HÓA  [" + essence + "]";
            if (!IsOpen || inventory == null) return;
            balance.text = essence.ToString("N0") + "  TINH HOA";
            for (int i = 0; i < Slots.Length; i++)
            {
                EquipmentData item = inventory.GetEquipped(Slots[i]);
                int itemLevel = inventory.GetEnhancementLevel(item);
                slotIcons[i].sprite = item != null ? item.Icon : null;
                slotIcons[i].enabled = slotIcons[i].sprite != null;
                slotNames[i].text = item != null ? item.DisplayName : "Chưa trang bị";
                slotNames[i].color = item != null ? F.TextColor : F.Muted;
                slotLevels[i].text = item != null ? "+" + itemLevel + "  •  Sức mạnh " + inventory.EffectivePowerScore(item) :
                    "Trang bị món đồ từ hành trang [I]";
                slotButtons[i].transform.Find("Brass").GetComponent<Image>().color = i == selectedSlot ? F.Gold : F.Edge;
                slotButtons[i].transform.Find("Inset").GetComponent<Image>().color = i == selectedSlot ? F.Hex("433B31") : F.Hex("303443");
            }
            EquipmentData selected = inventory.GetEquipped(Slots[selectedSlot]);
            int level = inventory.GetEnhancementLevel(selected);
            bool capped = level >= EquipmentInventory.MaximumEnhancementLevel;
            int cost = GameProgressManager.EnhancementEssenceCost(level);
            nameText.text = selected != null ? selected.DisplayName : "Ô " + F.SlotLabel(Slots[selectedSlot]).ToLowerInvariant() + " đang trống";
            levelText.text = selected != null ? "+" + level + "  →  " + (capped ? "ĐÃ TỐI ĐA" : "+" + (level + 1)) :
                "Hãy trang bị một món đồ trước khi cường hóa.";
            selectedIcon.sprite = selected != null ? selected.Icon : null;
            selectedIcon.enabled = selectedIcon.sprite != null;
            inventoryButton.gameObject.SetActive(selected == null && inventoryUI != null && inventoryUI.isActiveAndEnabled);
            preview.text = selected != null ? BuildPreview(selected, level) : "Chọn ô có trang bị để xem chỉ số hiện tại và cấp tiếp theo.";
            columns.gameObject.SetActive(selected != null);
            columns.text = "CHỈ SỐ<pos=48%>HIỆN TẠI<pos=75%>" + (capped ? "TỐI ĐA" : "CẤP TIẾP");
            costText.text = selected == null ? "Không tiêu tốn Tinh hoa khi ô trống." : capped ? "ĐÃ ĐẠT CẤP +" + EquipmentInventory.MaximumEnhancementLevel :
                "CHI PHÍ: " + cost.ToString("N0") + " TINH HOA   •   " + (essence >= cost ? "CÒN LẠI: " + (essence - cost).ToString("N0") : "THIẾU: " + (cost - essence).ToString("N0"));
            enhanceButton.interactable = selected != null && !capped && essence >= cost;
            enhanceLabel.text = selected == null ? "CHƯA CÓ TRANG BỊ" : capped ? "ĐÃ CƯỜNG HÓA TỐI ĐA" :
                essence < cost ? "THIẾU " + (cost - essence) + " TINH HOA" : "CƯỜNG HÓA +" + (level + 1) + "  [U]";
            if (EventSystem.current != null && (EventSystem.current.currentSelectedGameObject == null ||
                !enhanceButton.interactable && EventSystem.current.currentSelectedGameObject == enhanceButton.gameObject))
                slotButtons[selectedSlot].Select();
        }

        private static string BuildPreview(EquipmentData item, int level)
        {
            float current = EquipmentInventory.EnhancementMultiplier(level);
            float next = EquipmentInventory.EnhancementMultiplier(Mathf.Min(level + 1, EquipmentInventory.MaximumEnhancementLevel));
            var text = new StringBuilder(420);
            AppendRow(text, "Sát thương", item.BonusDamage * current, item.BonusDamage * next);
            AppendRow(text, "Giáp", item.BonusArmor * current, item.BonusArmor * next);
            AppendRow(text, "Máu tối đa", item.BonusMaxHealth * current, item.BonusMaxHealth * next);
            AppendRow(text, "Chí mạng", item.BonusCriticalChance * current * 100, item.BonusCriticalChance * next * 100, "%");
            AppendRow(text, "Tốc hồi chiêu", item.BonusCooldownRate * current * 100, item.BonusCooldownRate * next * 100, "%");
            AppendRow(text, "Sức mạnh", Mathf.Max(1, Mathf.RoundToInt(item.PowerScore * current)), Mathf.Max(1, Mathf.RoundToInt(item.PowerScore * next)));
            return text.ToString();
        }

        private static void AppendRow(StringBuilder text, string label, float current, float next, string unit = "")
        {
            if (text.Length > 0) text.AppendLine();
            text.Append(label).Append("<pos=48%>").Append(current.ToString("0.#")).Append(unit)
                .Append("<pos=75%><color=#82C5A1>").Append(next.ToString("0.#")).Append(unit).Append("</color>");
        }

        private void BuildPanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || panel != null) return;
            launcherButton = F.Button(canvas.transform, "ForgeLauncher", "", 0, 0, 205, 44, Open);
            var launchRect = (RectTransform)launcherButton.transform;
            launchRect.anchorMin = launchRect.anchorMax = launchRect.pivot = Vector2.one;
            launchRect.anchoredPosition = new Vector2(-30, -208);
            launcherLabel = launcherButton.GetComponentInChildren<TMP_Text>();
            panel = new GameObject("EquipmentEnhancementPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel);
            Transform window = F.Window(surface, "CƯỜNG HÓA", "LÒ RÈN THỢ SĂN  /  TRANG BỊ ĐANG MẶC", Close);
            TMP_Text hints = window.Find("KeyHints").GetComponent<TMP_Text>();
            hints.text = "MŨI TÊN / ENTER  Chọn   •   1–5  Chọn ô   •   U  Cường hóa   •   I  Túi đồ   •   N / ESC  Đóng";
            ((RectTransform)hints.transform).sizeDelta = new Vector2(1272, 26);
            balance = F.Text(window, "Essence", "", 814, 39, 392, 42, 24, F.Gold, TextAlignmentOptions.Right);
            status = F.Text(window, "Status", "", 32, 910, 1272, 26, 17, F.Gold);
            panel.GetComponent<EquipmentPanelLayout>().ConfigureWindow(window, true, balance, status);
            Transform list = F.Frame(window, "EquippedList", 24, 122, 540, 724, F.Panel);
            F.Text(list, "Heading", "CHỌN Ô TRANG BỊ", 24, 20, 492, 34, 23, F.Gold);
            for (int i = 0; i < Slots.Length; i++)
            {
                int index = i;
                Button button = F.Button(list, "EnhancementSlot" + Slots[i], "", 24, 80 + i * 122, 492, 110, () => Select(index));
                slotButtons[i] = button;
                slotIcons[i] = F.Icon(button.transform, "ItemIcon", null, 14, 16, 78, 78);
                F.Text(button.transform, "SlotName", (i + 1) + "  " + F.SlotLabel(Slots[i]).ToUpperInvariant(), 108, 10, 366, 24, 16, F.Muted);
                slotNames[i] = F.Text(button.transform, "ItemName", "", 108, 38, 366, 34, 22);
                slotLevels[i] = F.Text(button.transform, "Level", "", 108, 78, 366, 24, 17, F.Green);
            }
            Transform detail = F.Frame(window, "EnhancementPreview", 588, 122, 724, 724, F.Panel);
            F.Text(detail, "Heading", "XEM TRƯỚC CƯỜNG HÓA", 28, 20, 668, 30, 20, F.Muted);
            selectedIcon = F.Icon(detail, "SelectedIcon", null, 282, 56, 160, 160);
            inventoryButton = F.Button(detail, "OpenInventory", "MỞ HÀNH TRANG  [I]", 184, 116, 356, 48, OpenInventory);
            nameText = F.Text(detail, "SelectedName", "", 28, 230, 668, 40, 28, F.Gold, TextAlignmentOptions.Center);
            levelText = F.Text(detail, "SelectedLevel", "", 28, 278, 668, 28, 20, F.Green, TextAlignmentOptions.Center);
            F.Text(detail, "Rule", "Mỗi cấp tăng thêm " + (EquipmentInventory.BonusPerEnhancementLevel * 100).ToString("0.#") +
                "% chỉ số gốc. Tối đa +" + EquipmentInventory.MaximumEnhancementLevel + ".", 28, 326, 668, 32, 19, F.Muted, TextAlignmentOptions.Center);
            F.Line(detail, 28, 370, 668);
            columns = F.Text(detail, "Columns", "", 28, 384, 668, 26, 16, F.Muted);
            preview = F.Text(detail, "StatPreview", "", 28, 418, 668, 168, 21);
            preview.alignment = TextAlignmentOptions.TopLeft;
            preview.lineSpacing = 3;
            costText = F.Text(detail, "Cost", "", 28, 600, 668, 34, 23, F.Gold, TextAlignmentOptions.Center);
            enhanceButton = F.Button(detail, "Enhance", "", 28, 654, 668, 48, Enhance, true);
            enhanceLabel = enhanceButton.GetComponentInChildren<TMP_Text>();
            panel.SetActive(false);
        }

        private void HandleEquipmentChanged(EquipmentData item) => Refresh();
        private void HandleEssenceChanged(int amount) => Refresh();
        private void OnDisable()
        {
            Close();
            Unsubscribe();
        }
        private void OnDestroy()
        {
            Close();
            Unsubscribe();
            if (panel != null) Destroy(panel);
            if (launcherButton != null) Destroy(launcherButton.gameObject);
        }
    }
}
