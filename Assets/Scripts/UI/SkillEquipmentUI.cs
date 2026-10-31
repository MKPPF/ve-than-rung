using System.Collections.Generic;

using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using F = TopDownGame.UI.FantasyEquipmentUI;

namespace TopDownGame.UI
{
    /// <summary>
    /// Interactive skill management panel supporting all playable heroes:
    /// - Forest Warrior (4 customizable elemental skill slots from owned spell library)
    /// - Thanh Giong (4 signature moves: Iron Sword + Horse, Ivory Bamboo Smash, 360 Whirl, Bamboo Charge)
    /// - Son Tinh (4 signature moves: Giant Boulder, Skyward Eruption, Ground-Slam Ridges, Surrounding Fortress)
    /// </summary>
    public sealed class SkillEquipmentUI : MonoBehaviour
    {
        private GameObject panel;
        private TMP_Text content;
        private TMP_Text launcherLabel;
        private TMP_Text upgradeLabel;
        private TMP_Text windowSubtitle;
        private TMP_Text libraryHeading;
        private Button launcherButton;
        private Button upgradeButton;
        private SkillInventory inventory;
        private SkillManager skillManager;
        private PlayerStats stats;
        private ThanhGiongHeroController heroController;
        private readonly List<string> owned = new List<string>();
        private int selectedSkill;
        private int selectedSlot;
        private Transform skillList;
        private TMP_Text heading, balance, status;
        private Image selectedIcon;
        private Button equipButton, removeButton;
        private readonly Button[] slotButtons = new Button[4];
        private readonly Image[] slotIcons = new Image[4];
        private readonly List<Button> skillButtons = new List<Button>();
        private readonly Dictionary<string, Button> cards = new Dictionary<string, Button>();
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private Button closeButton;
        private bool subscribed;
        private int lastUpgradeFrame = -1;

        public bool IsOpen => panel != null && panel.activeSelf;
        private HeroCharacterId ActiveHero => heroController != null ? heroController.CurrentHero : HeroCharacterId.ForestWarrior;
        private bool IsCustomHero => ActiveHero != HeroCharacterId.ForestWarrior;

        private void Start()
        {
            ResolvePlayerReferences();
            EnsurePanel();
            if (panel != null) panel.SetActive(false);
            Subscribe();
            RefreshOwned();
            RefreshText();
        }

        private void OnEnable()
        {
            ResolvePlayerReferences();
            EnsurePanel();
            if (launcherButton != null) launcherButton.gameObject.SetActive(true);
            Subscribe();
            RefreshOwned();
            RefreshText();
        }

        private void ResolvePlayerReferences()
        {
            if (inventory != null && skillManager != null && stats != null && heroController != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                inventory = player.GetComponent<SkillInventory>();
                skillManager = player.GetComponent<SkillManager>();
                stats = player.GetComponent<PlayerStats>();
                heroController = player.GetComponent<ThanhGiongHeroController>();
            }
        }

        private void Subscribe()
        {
            if (subscribed) return;
            ResolvePlayerReferences();
            if (inventory != null) inventory.Changed += HandleInventoryChanged;
            if (stats != null) stats.GoldChanged += HandleGoldChanged;
            if (heroController != null) heroController.HeroChanged += HandleHeroChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (inventory != null) inventory.Changed -= HandleInventoryChanged;
            if (stats != null) stats.GoldChanged -= HandleGoldChanged;
            if (heroController != null) heroController.HeroChanged -= HandleHeroChanged;
            subscribed = false;
        }

        private void HandleHeroChanged(HeroCharacterId newHero)
        {
            selectedSkill = 0;
            selectedSlot = 0;
            RefreshOwned();
            RefreshText();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || panel == null) return;

            if (keyboard.kKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
                return;
            }

            if (!IsOpen) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }

            // Number keys 1-4 and Q/E/R/F to select slot
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame) SelectSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame) SelectSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame) SelectSlot(2);
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame) SelectSlot(3);

            NormalizeSelection();
            if (keyboard.backspaceKey.wasPressedThisFrame && !IsCustomHero)
            {
                RemoveSelected();
            }
            if (keyboard.uKey.wasPressedThisFrame) UpgradeSelected();
        }

        public void Open()
        {
            ResolvePlayerReferences();
            EnsurePanel();
            if (IsOpen || panel == null) return;
            if (Application.isPlaying && (!isActiveAndEnabled || Time.timeScale <= 0 || GameplayState.IsBlockedByOther(this))) return;

            if (ActiveHero == HeroCharacterId.ForestWarrior && inventory != null)
            {
                inventory.EnsureDefaultSlots();
            }

            RefreshOwned();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            focus.Capture(panel.transform);
            if (launcherButton != null) launcherButton.gameObject.SetActive(false);
            if (Application.isPlaying) GameplayState.EnterModal(this);
            RefreshText();

            SetStatus(IsCustomHero
                ? "Bộ 4 kỹ năng anh hùng được trang bị sẵn vào các ô Q / E / R / F. Nhấn U để nâng cấp."
                : "Chọn kỹ năng → chọn ô Q / E / R / F → TRANG BỊ. Nhấn U để nâng cấp.");
            DefaultButton()?.Select();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (launcherButton != null) launcherButton.gameObject.SetActive(isActiveAndEnabled);
            if (Application.isPlaying) GameplayState.ExitModal(this);
            focus.Release();
        }

        private SkillData ResolveSkillData(string id, int index)
        {
            HeroCharacterId hero = ActiveHero;
            if (hero == HeroCharacterId.ThanhGiong)
            {
                heroController?.EnsureSkillDefinitions();
                int idx = (index >= 0 && index < 4) ? index : ParseIndexFromId(id);
                return heroController?.GetThanhGiongSkillData(idx);
            }

            if (hero == HeroCharacterId.SonTinh)
            {
                heroController?.EnsureSkillDefinitions();
                int idx = (index >= 0 && index < 4) ? index : ParseIndexFromId(id);
                return heroController?.GetSonTinhSkillData(idx);
            }

            return skillManager != null ? skillManager.GetSkillData(id) : null;
        }

        private int ResolveSkillLevel(string id, int index)
        {
            HeroCharacterId hero = ActiveHero;
            if (hero == HeroCharacterId.ThanhGiong || hero == HeroCharacterId.SonTinh)
            {
                int idx = (index >= 0 && index < 4) ? index : ParseIndexFromId(id);
                return heroController != null ? heroController.GetEffectiveSkillLevel(idx) : 1;
            }

            return inventory != null ? inventory.GetLevel(id) : 1;
        }

        private static int ParseIndexFromId(string id)
        {
            if (!string.IsNullOrEmpty(id) && id.Length > 2 && char.IsDigit(id[id.Length - 1]))
            {
                return id[id.Length - 1] - '0';
            }
            return 0;
        }

        private void UpgradeSelected()
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || owned.Count == 0 || stats == null || lastUpgradeFrame == Time.frameCount) return;

            HeroCharacterId hero = ActiveHero;
            string id = owned[selectedSkill];
            SkillData data = ResolveSkillData(id, selectedSkill);
            int level = ResolveSkillLevel(id, selectedSkill);
            int maxLevel = data != null ? data.MaxLevel : 5;
            if (data == null || level >= maxLevel) return;

            int cost = SkillInventory.UpgradeGoldCost(level);
            if (!stats.TrySpendGold(cost))
            {
                RefreshText();
                SetStatus("Thiếu " + (cost - stats.Gold).ToString("N0") + " vàng để nâng cấp.");
                return;
            }

            lastUpgradeFrame = Time.frameCount;

            if (hero == HeroCharacterId.ThanhGiong || hero == HeroCharacterId.SonTinh)
            {
                heroController?.UpgradeHeroSkill(hero, selectedSkill);
            }
            else
            {
                if (inventory == null || !inventory.TryUpgrade(id, maxLevel))
                {
                    stats.AddGold(cost);
                    return;
                }
            }

            Persist();
            RefreshOwned();
            RefreshText();
            SetStatus("Đã nâng " + data.DisplayName + " lên cấp " + (level + 1) + " (-" + cost.ToString("N0") + " vàng).");
        }

        private void Persist()
        {
            skillManager?.RefreshSkills();
            GameProgressManager.Instance?.CaptureActivePlayer();
        }

        private void HandleInventoryChanged()
        {
            RefreshOwned();
            RefreshText();
        }

        private void HandleGoldChanged(int amount)
        {
            RefreshText();
        }

        private void RefreshOwned()
        {
            ResolvePlayerReferences();
            string previousId = owned.Count > 0 && selectedSkill >= 0 && selectedSkill < owned.Count ? owned[selectedSkill] : null;
            owned.Clear();

            HeroCharacterId hero = ActiveHero;
            if (hero == HeroCharacterId.ThanhGiong)
            {
                heroController?.EnsureSkillDefinitions();
                for (int i = 0; i < 4; i++)
                {
                    owned.Add("ThanhGiong_" + i);
                }
            }
            else if (hero == HeroCharacterId.SonTinh)
            {
                heroController?.EnsureSkillDefinitions();
                for (int i = 0; i < 4; i++)
                {
                    owned.Add("SonTinh_" + i);
                }
            }
            else
            {
                if (inventory != null)
                {
                    inventory.EnsureDefaultSlots();
                    foreach (string id in inventory.OwnedSkills.Keys)
                    {
                        owned.Add(id);
                    }
                }
                owned.Sort(System.StringComparer.OrdinalIgnoreCase);
            }

            if (previousId != null && owned.Contains(previousId))
            {
                selectedSkill = owned.IndexOf(previousId);
            }
            NormalizeSelection();
            BuildSkillCards();
            if (launcherLabel != null)
            {
                launcherLabel.text = "K  KỸ NĂNG  [" + owned.Count + "]";
            }
        }

        private void NormalizeSelection()
        {
            selectedSkill = owned.Count > 0
                ? (selectedSkill % owned.Count + owned.Count) % owned.Count
                : 0;
            selectedSlot = Mathf.Clamp(selectedSlot, 0, 3);
        }

        private void RefreshText()
        {
            EnsurePanel();
            if (content == null || panel == null || slotButtons == null || slotButtons.Length == 0 || slotButtons[0] == null) return;
            NormalizeSelection();

            HeroCharacterId hero = ActiveHero;
            string selectedId = owned.Count > 0 ? owned[selectedSkill] : string.Empty;
            SkillData data = ResolveSkillData(selectedId, selectedSkill);
            int level = ResolveSkillLevel(selectedId, selectedSkill);
            int maxLevel = data != null ? data.MaxLevel : 5;
            bool atMaximum = data != null && level >= maxLevel;
            int cost = level > 0 ? SkillInventory.UpgradeGoldCost(level) : 0;

            if (windowSubtitle != null)
            {
                windowSubtitle.text = hero switch
                {
                    HeroCharacterId.ThanhGiong => "THÁNH GIÓNG  •  BỘ 4 TUYỆT KỸ THIÊN VƯƠNG",
                    HeroCharacterId.SonTinh => "SƠN TINH  •  BỘ 4 THẦN LỰC DỜI NON DỰNG NÚI",
                    _ => "CHIẾN BINH RỪNG  •  THƯ VIỆN & TRANG BỊ"
                };
            }

            if (libraryHeading != null)
            {
                libraryHeading.text = hero switch
                {
                    HeroCharacterId.ThanhGiong => "KỸ NĂNG THÁNH GIÓNG",
                    HeroCharacterId.SonTinh => "KỸ NĂNG SƠN TINH",
                    _ => "KỸ NĂNG ĐÃ HỌC"
                };
            }

            if (heading != null) heading.text = data != null ? data.DisplayName : "CHƯA CÓ KỸ NĂNG";
            if (balance != null) balance.text = (stats != null ? stats.Gold.ToString("N0") : "0") + " VÀNG";
            if (selectedIcon != null)
            {
                selectedIcon.sprite = data != null ? data.Icon : null;
                selectedIcon.enabled = selectedIcon.sprite != null;
            }
            if (content != null)
            {
                content.text = data != null
                    ? "CẤP " + level + " / " + maxLevel + "\n\n" + data.Description + "\n\n" + UpgradeEffectLabel(data.Id) +
                      "\n\nMana cơ bản: " + data.ManaCost.ToString("0.#") + "\nHồi chiêu cơ bản: " + data.Cooldown.ToString("0.#") + " giây"
                    : "Nhận kỹ năng từ phần thưởng và rương kỹ năng.\n\nCác kỹ năng đã học sẽ xuất hiện ở danh sách bên trái.";
            }

            bool duplicate = false;
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null) continue;

                SkillData slotData;
                int slotLevel;
                string keyLabel = i switch { 0 => "Q", 1 => "E", 2 => "R", _ => "F" };

                if (hero == HeroCharacterId.ThanhGiong)
                {
                    slotData = heroController != null ? heroController.GetThanhGiongSkillData(i) : null;
                    slotLevel = heroController != null ? heroController.GetEffectiveSkillLevel(i) : 1;
                }
                else if (hero == HeroCharacterId.SonTinh)
                {
                    slotData = heroController != null ? heroController.GetSonTinhSkillData(i) : null;
                    slotLevel = heroController != null ? heroController.GetEffectiveSkillLevel(i) : 1;
                }
                else
                {
                    var slot = inventory != null && i < inventory.Slots.Count ? inventory.Slots[i] : null;
                    slotData = slot != null && !slot.IsEmpty && skillManager != null ? skillManager.GetSkillData(slot.skillId) : null;
                    slotLevel = slot != null ? slot.level : 1;
                    if (slot != null && !slot.IsEmpty && slot.skillId == selectedId && i != selectedSlot)
                    {
                        duplicate = true;
                    }
                }

                TMP_Text btnText = slotButtons[i].GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    string slotName = slotData != null ? slotData.DisplayName : "Ô TRỐNG";
                    btnText.text = keyLabel + "  [" + (i + 1) + "]\n" + slotName + "  •  Cấp " + slotLevel;
                }

                if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
                {
                    slotIcons[i].sprite = slotData != null ? slotData.Icon : null;
                    slotIcons[i].enabled = slotIcons[i].sprite != null;
                }

                Transform brass = slotButtons[i].transform.Find("Brass");
                if (brass != null)
                {
                    brass.GetComponent<Image>().color = i == selectedSlot ? F.Gold : F.Edge;
                }
            }

            if (hero == HeroCharacterId.ThanhGiong || hero == HeroCharacterId.SonTinh)
            {
                if (equipButton != null)
                {
                    equipButton.interactable = false;
                    TMP_Text txt = equipButton.GetComponentInChildren<TMP_Text>();
                    if (txt != null) txt.text = "KỸ NĂNG CỐ ĐỊNH";
                }
                if (removeButton != null)
                {
                    removeButton.interactable = false;
                    TMP_Text txt = removeButton.GetComponentInChildren<TMP_Text>();
                    if (txt != null) txt.text = "BỘ CHIÊU THIÊN BẨM";
                }
            }
            else
            {
                if (equipButton != null)
                {
                    TMP_Text txt = equipButton.GetComponentInChildren<TMP_Text>();
                    if (txt != null) txt.text = "TRANG BỊ";
                    string curId = (inventory != null && selectedSlot < inventory.Slots.Count) ? inventory.Slots[selectedSlot].skillId : string.Empty;
                    equipButton.interactable = data != null && !duplicate && curId != selectedId;
                }
                if (removeButton != null)
                {
                    TMP_Text txt = removeButton.GetComponentInChildren<TMP_Text>();
                    if (txt != null) txt.text = "THÁO [BACKSPACE]";
                    removeButton.interactable = inventory != null && selectedSlot < inventory.Slots.Count && !inventory.Slots[selectedSlot].IsEmpty;
                }
            }

            for (int i = 0; i < skillButtons.Count; i++)
            {
                if (skillButtons[i] != null)
                {
                    Transform b = skillButtons[i].transform.Find("Brass");
                    if (b != null) b.GetComponent<Image>().color = i == selectedSkill ? F.Gold : F.Edge;
                }
            }

            if (upgradeLabel != null)
            {
                upgradeLabel.text = data == null ? "CHƯA CÓ KỸ NĂNG" : atMaximum
                    ? "ĐÃ TỐI ĐA (CẤP 5)"
                    : "NÂNG CẤP [U]  ·  " + cost.ToString("N0") + " VÀNG";
            }
            if (upgradeButton != null)
            {
                upgradeButton.interactable = data != null && !atMaximum &&
                    stats != null && stats.Gold >= cost;
            }

            if (IsOpen && EventSystem.current != null)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected == null || (selected.GetComponent<Selectable>() is Selectable control && !control.IsInteractable()))
                {
                    DefaultButton()?.Select();
                }
            }
        }

        private Button DefaultButton() => skillButtons.Count > 0 && selectedSkill < skillButtons.Count ? skillButtons[selectedSkill] : closeButton;

        private void SetStatus(string message) { if (status != null) status.text = message; }

        private void SelectSkill(string id)
        {
            if (!IsOpen) return;
            selectedSkill = owned.IndexOf(id);
            if (IsCustomHero && selectedSkill >= 0 && selectedSkill < 4)
            {
                selectedSlot = selectedSkill;
            }
            RefreshText();
            SetStatus(IsCustomHero
                ? "Đang xem chiêu " + ResolveSkillData(id, selectedSkill)?.DisplayName + ". Nhấn NÂNG CẤP [U] để gia tăng uy lực."
                : "Chọn ô Q / E / R / F rồi TRANG BỊ. Nếu kỹ năng ở ô khác, tháo ô đó trước khi chuyển.");
        }

        private void SelectSlot(int index)
        {
            if (!IsOpen) return;
            selectedSlot = Mathf.Clamp(index, 0, 3);
            if (IsCustomHero && selectedSlot < owned.Count)
            {
                selectedSkill = selectedSlot;
            }
            else if (!IsCustomHero && inventory != null && selectedSlot < inventory.Slots.Count)
            {
                string slotSkillId = inventory.Slots[selectedSlot].skillId;
                if (!string.IsNullOrEmpty(slotSkillId) && owned.Contains(slotSkillId))
                {
                    selectedSkill = owned.IndexOf(slotSkillId);
                }
            }
            RefreshText();
            if (slotButtons[selectedSlot] != null)
            {
                slotButtons[selectedSlot].Select();
            }
            string keyLabel = selectedSlot switch { 0 => "Q", 1 => "E", 2 => "R", _ => "F" };
            SetStatus(IsCustomHero
                ? "Đã chọn ô " + keyLabel + " (" + ResolveSkillData(owned[selectedSkill], selectedSkill)?.DisplayName + "). Nhấn NÂNG CẤP [U] để tăng cấp."
                : "Đã chọn ô " + keyLabel + ". TRANG BỊ để thay kỹ năng; THÁO để để trống ô.");
        }

        private static string UpgradeEffectLabel(SkillId id)
        {
            return id switch
            {
                SkillId.Healing => "Mỗi cấp: +20% hồi máu, hồi chiêu nhanh hơn.",
                SkillId.Shield => "Mỗi cấp: +0.5 giây khiên, hồi chiêu nhanh hơn.",
                _ => "Mỗi cấp: +20% sát thương, +10% tầm/diện rộng, hồi chiêu nhanh hơn."
            };
        }

        private void EquipSelected()
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || owned.Count == 0 || IsCustomHero || inventory == null) return;
            NormalizeSelection();
            string id = owned[selectedSkill];
            if (inventory.Slots[selectedSlot].skillId == id) return;
            bool equipped = inventory.TryEquip(id, selectedSlot);
            if (equipped) Persist();
            RefreshText();
            SetStatus(equipped ? "Đã trang bị " + skillManager?.GetSkillData(id)?.DisplayName + " vào ô " + inventory.Slots[selectedSlot].key + "." :
                "Kỹ năng này đã ở ô khác. Hãy tháo ô đó trước khi chuyển.");
        }

        private void RemoveSelected()
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || IsCustomHero || inventory == null) return;
            if (inventory.Slots[selectedSlot].IsEmpty) return;
            inventory.RemoveFromSlot(selectedSlot);
            Persist();
            RefreshText();
            SetStatus("Đã tháo kỹ năng khỏi ô " + inventory.Slots[selectedSlot].key + ". Kỹ năng vẫn ở thư viện.");
        }

        private void BuildSkillCards()
        {
            if (skillList == null) return;
            skillButtons.Clear();
            foreach (var pair in cards)
            {
                pair.Value.gameObject.SetActive(owned.Contains(pair.Key));
            }

            for (int i = 0; i < owned.Count; i++)
            {
                string id = owned[i];
                int cardIndex = i;
                SkillData data = ResolveSkillData(id, cardIndex);
                int level = ResolveSkillLevel(id, cardIndex);

                if (!cards.TryGetValue(id, out Button button))
                {
                    button = F.Button(skillList, "Skill" + id, "", 0, 0, 326, 100, () => SelectSkill(id));
                    button.gameObject.AddComponent<ScrollSelectionIntoView>();
                    F.Icon(button.transform, "Icon", data != null ? data.Icon : null, 12, 18, 60, 60);
                    F.Text(button.transform, "Name", data != null ? data.DisplayName : id, 84, 8, 230, 50, 22, F.TextColor);
                    F.Text(button.transform, "Level", "", 84, 68, 230, 24, 18, F.Muted);
                    cards.Add(id, button);
                }

                button.transform.SetAsLastSibling();
                Image iconImg = button.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.sprite = data != null ? data.Icon : null;
                    iconImg.enabled = iconImg.sprite != null;
                }
                TMP_Text nameText = button.transform.Find("Name")?.GetComponent<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = data != null ? data.DisplayName : id;
                }
                TMP_Text levelText = button.transform.Find("Level")?.GetComponent<TMP_Text>();
                if (levelText != null)
                {
                    levelText.text = "Cấp " + level;
                }

                skillButtons.Add(button);
            }

            if (IsOpen && EventSystem.current != null)
            {
                EventSystem.current.currentSelectedGameObject?.GetComponent<ScrollSelectionIntoView>()?.Reveal();
            }
        }

        private void EnsurePanel()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || panel != null) return;

            launcherButton = F.Button(canvas.transform, "SkillLauncher", "KỸ NĂNG [K]", 0, 0, 250, 44, Open);
            var launch = launcherButton.GetComponent<RectTransform>();
            launch.anchorMin = launch.anchorMax = launch.pivot = Vector2.one;
            launch.anchoredPosition = new Vector2(-30, -154);
            launcherLabel = launcherButton.GetComponentInChildren<TMP_Text>();

            panel = new GameObject("SkillEquipmentPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var window = F.Window(EquipmentPanelLayout.CreateSurface(panel), "KỸ NĂNG", "THƯ VIỆN  /  TRANG BỊ  /  NÂNG CẤP", Close);
            windowSubtitle = window.Find("Sub")?.GetComponent<TMP_Text>();

            var hints = window.Find("KeyHints").GetComponent<TMP_Text>();
            hints.text = "MŨI TÊN / ENTER  Chọn   •   1–4 / Q–F  Chọn ô   •   U  Nâng cấp   •   BACKSPACE  Tháo   •   K / ESC  Đóng";
            hints.rectTransform.sizeDelta = new Vector2(1272, 26);

            closeButton = window.Find("Close").GetComponent<Button>();
            balance = F.Text(window, "Gold", "", 1190, 39, 392, 42, 24, F.Gold, TextAlignmentOptions.Right);

            var library = F.Frame(window, "Library", 24, 122, 374, 724, F.Panel);
            libraryHeading = F.Text(library, "Heading", "KỸ NĂNG ĐÃ HỌC", 24, 18, 326, 40, 24, F.Gold);
            F.Grid(library, "Skills", 20, 78, 350, 620, new Vector2(326, 100), 1, out skillList);

            var detail = F.Frame(window, "Details", 422, 122, 890, 460, F.Panel);
            F.Medallion(detail, 22, 28, 138, F.Gold);
            selectedIcon = F.Icon(detail, "Icon", null, 35, 42, 112, 112);
            heading = F.Text(detail, "Name", "", 182, 28, 680, 46, 30, F.Gold);
            content = F.Text(detail, "Content", "", 182, 92, 680, 272, 23);
            content.alignment = TextAlignmentOptions.TopLeft;
            upgradeButton = F.Button(detail, "Upgrade", "NÂNG CẤP [U]", 22, 386, 846, 52, UpgradeSelected, true);
            upgradeLabel = upgradeButton.GetComponentInChildren<TMP_Text>();

            var loadout = F.Frame(window, "Loadout", 422, 600, 890, 246, F.Panel);
            F.Text(loadout, "Heading", "BỐN Ô KỸ NĂNG [Q / E / R / F]", 22, 12, 846, 28, 20, F.Gold);
            for (int i = 0; i < 4; i++)
            {
                int slot = i;
                slotButtons[i] = F.Button(loadout, "Slot" + i, "", 22 + (i % 2) * 431, 46 + (i / 2) * 66, 415, 60, () => SelectSlot(slot));
                slotIcons[i] = F.Icon(slotButtons[i].transform, "SkillIcon", null, 10, 8, 44, 44);
                var label = slotButtons[i].GetComponentInChildren<TMP_Text>();
                label.rectTransform.anchoredPosition = new Vector2(66, -4);
                label.rectTransform.sizeDelta = new Vector2(337, 52);
                label.fontSizeMax = 19;
                label.fontSizeMin = 16;
            }
            equipButton = F.Button(loadout, "Equip", "TRANG BỊ", 22, 188, 415, 42, EquipSelected, true);
            removeButton = F.Button(loadout, "Remove", "THÁO [BACKSPACE]", 453, 188, 415, 42, RemoveSelected);
            status = F.Text(window, "Status", "", 32, 910, 1272, 26, 17, F.Gold);
            panel.GetComponent<EquipmentPanelLayout>().ConfigureWindow(window, true, balance, status);
        }

        private void OnDisable() { Close(); Unsubscribe(); }

        private void OnDestroy()
        {
            Unsubscribe();
            focus.Release();
            GameplayState.ExitModal(this);
            if (panel != null) Destroy(panel);
            if (launcherButton != null) Destroy(launcherButton.gameObject);
        }
    }
}
