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
    /// <summary>Mouse-friendly skill cards reuse existing ownership, upgrade costs and save services.</summary>
    public sealed class SkillEquipmentUI : MonoBehaviour
    {
        private GameObject panel;
        private TMP_Text content;
        private TMP_Text launcherLabel;
        private TMP_Text upgradeLabel;
        private Button launcherButton;
        private Button upgradeButton;
        private SkillInventory inventory;
        private SkillManager skillManager;
        private PlayerStats stats;
        private readonly List<string> owned = new List<string>();
        private int selectedSkill;
        private int selectedSlot;
        private Transform skillList;
        private TMP_Text heading, balance, status;
        private Image selectedIcon;
        private Button equipButton, removeButton;
        private readonly Button[] slotButtons=new Button[4];
        private readonly Image[] slotIcons=new Image[4];
        private readonly List<Button> skillButtons=new List<Button>();
        private readonly Dictionary<string, Button> cards = new Dictionary<string, Button>();
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private Button closeButton;
        private bool subscribed;
        private int lastUpgradeFrame = -1;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            inventory = player != null ? player.GetComponent<SkillInventory>() : null;
            skillManager = player != null ? player.GetComponent<SkillManager>() : null;
            stats = player != null ? player.GetComponent<PlayerStats>() : null;
            EnsurePanel();
            if (panel != null) panel.SetActive(false);
            Subscribe();
            RefreshOwned();
            RefreshText();
        }

        private void OnEnable()
        {
            if (launcherButton != null) launcherButton.gameObject.SetActive(true);
            Subscribe();
            RefreshOwned();
            RefreshText();
        }

        private void Subscribe()
        {
            if (subscribed || inventory == null) return;
            inventory.Changed += HandleInventoryChanged;
            if (stats != null) stats.GoldChanged += HandleGoldChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (inventory != null) inventory.Changed -= HandleInventoryChanged;
            if (stats != null) stats.GoldChanged -= HandleGoldChanged;
            subscribed = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || inventory == null || panel == null) return;

            if (keyboard.kKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
                return;
            }

            if (!IsOpen) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            // Arrows and Enter belong to EventSystem navigation and Submit.
            if (keyboard.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (keyboard.digit4Key.wasPressedThisFrame) SelectSlot(3);

            NormalizeSelection();
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                RemoveSelected();
            }
            if (keyboard.uKey.wasPressedThisFrame) UpgradeSelected();
        }

        public void Open()
        {
            if (!isActiveAndEnabled || IsOpen || panel == null || inventory == null || Time.timeScale <= 0 ||
                GameplayState.IsBlockedByOther(this)) return;
            RefreshOwned();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            focus.Capture(panel.transform);
            if (launcherButton != null) launcherButton.gameObject.SetActive(false);
            GameplayState.EnterModal(this);
            RefreshText();
            SetStatus("Chọn kỹ năng → chọn ô Q / E / R / F → TRANG BỊ.");
            DefaultButton().Select();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (launcherButton != null) launcherButton.gameObject.SetActive(isActiveAndEnabled);
            GameplayState.ExitModal(this);
            focus.Release();
        }

        private void UpgradeSelected()
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || owned.Count == 0 || stats == null || skillManager == null || lastUpgradeFrame == Time.frameCount) return;
            string id = owned[selectedSkill];
            SkillData data = skillManager.GetSkillData(id);
            int level = inventory.GetLevel(id);
            if (data == null || level >= data.MaxLevel) return;

            int cost = SkillInventory.UpgradeGoldCost(level);
            if (!stats.TrySpendGold(cost))
            {
                RefreshText();
                SetStatus("Thiếu " + (cost - stats.Gold).ToString("N0") + " vàng để nâng cấp.");
                return;
            }

            lastUpgradeFrame = Time.frameCount;

            if (!inventory.TryUpgrade(id, data.MaxLevel))
            {
                stats.AddGold(cost);
                return;
            }

            skillManager.RefreshSkills();
            Persist();
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
            string previousId=owned.Count>0 && selectedSkill>=0 && selectedSkill<owned.Count ? owned[selectedSkill] : null;
            owned.Clear();
            if (inventory != null)
            {
                foreach (string id in inventory.OwnedSkills.Keys) owned.Add(id);
            }
            owned.Sort(System.StringComparer.OrdinalIgnoreCase);
            if(previousId!=null && owned.Contains(previousId))selectedSkill=owned.IndexOf(previousId);
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
            if (content == null || inventory == null) return;
            NormalizeSelection();
            string selectedId = owned.Count > 0 ? owned[selectedSkill] : string.Empty;
            SkillData data = skillManager != null
                ? skillManager.GetSkillData(selectedId)
                : null;
            int level = inventory.GetLevel(selectedId);
            bool atMaximum = data != null && level >= data.MaxLevel;
            int cost = level > 0 ? SkillInventory.UpgradeGoldCost(level) : 0;

            heading.text=data!=null?data.DisplayName:"CHƯA CÓ KỸ NĂNG";
            balance.text=(stats!=null?stats.Gold.ToString("N0"):"0")+" VÀNG";
            selectedIcon.sprite=data!=null?data.Icon:null;selectedIcon.enabled=selectedIcon.sprite!=null;
            content.text=data!=null
                ? "CẤP "+level+" / "+data.MaxLevel+"\n\n"+data.Description+"\n\n"+UpgradeEffectLabel(data.Id)+
                  "\n\nMana cơ bản: "+data.ManaCost.ToString("0.#")+"\nHồi chiêu cơ bản: "+data.Cooldown.ToString("0.#")+" giây"
                : "Nhận kỹ năng từ phần thưởng và rương kỹ năng.\n\nCác kỹ năng đã học sẽ xuất hiện ở danh sách bên trái.";
            bool duplicate=false;
            for(int i=0;i<slotButtons.Length;i++)
            {
                var slot=inventory.Slots[i];
                var slotData=skillManager!=null?skillManager.GetSkillData(slot.skillId):null;
                slotButtons[i].GetComponentInChildren<TMP_Text>().text=slot.key+"  ["+(i+1)+"]\n"+
                    (slot.IsEmpty?"Ô TRỐNG":(slotData!=null?slotData.DisplayName:slot.skillId)+"  •  Cấp "+slot.level);
                slotIcons[i].sprite=slotData!=null?slotData.Icon:null;
                slotIcons[i].enabled=slotIcons[i].sprite!=null;
                slotButtons[i].transform.Find("Brass").GetComponent<Image>().color=i==selectedSlot?F.Gold:F.Edge;
                if(!slot.IsEmpty && slot.skillId==selectedId && i!=selectedSlot)duplicate=true;
            }
            equipButton.interactable=data!=null && !duplicate && inventory.Slots[selectedSlot].skillId!=selectedId;
            removeButton.interactable=!inventory.Slots[selectedSlot].IsEmpty;
            for(int i=0;i<skillButtons.Count;i++)
                skillButtons[i].transform.Find("Brass").GetComponent<Image>().color=i==selectedSkill?F.Gold:F.Edge;
            if (upgradeLabel != null)
            {
                upgradeLabel.text = data == null ? "CHƯA CÓ KỸ NĂNG" : atMaximum
                    ? "ĐÃ TỐI ĐA"
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
                if (selected == null || selected.GetComponent<Selectable>() is Selectable control && !control.IsInteractable())
                    DefaultButton().Select();
            }
        }

        private Button DefaultButton() => skillButtons.Count > 0 ? skillButtons[selectedSkill] : closeButton;

        private void SetStatus(string message) { if (status != null) status.text = message; }

        private void SelectSkill(string id)
        {
            if (!IsOpen) return;
            selectedSkill = owned.IndexOf(id);
            RefreshText();
            SetStatus("Chọn ô Q / E / R / F rồi TRANG BỊ. Nếu kỹ năng ở ô khác, tháo ô đó trước khi chuyển.");
        }

        private void SelectSlot(int index)
        {
            if (!IsOpen) return;
            selectedSlot = index;
            RefreshText();
            slotButtons[index].Select();
            SetStatus("Đã chọn ô " + inventory.Slots[index].key + ". TRANG BỊ để thay kỹ năng; THÁO để để trống ô.");
        }

        private static string UpgradeEffectLabel(SkillId id)
        {
            return id switch
            {
                SkillId.Healing => "Mỗi cấp: +20% hồi máu, hồi chiêu nhanh hơn.",
                SkillId.Shield => "Mỗi cấp: +0.5 giây khiên, hồi chiêu nhanh hơn.",
                _ => "Mỗi cấp: +20% sát thương, +10% tầm, hồi chiêu nhanh hơn."
            };
        }

        private void EquipSelected()
        {
            if(!IsOpen || GameplayState.IsBlockedByOther(this) || owned.Count==0)return;
            NormalizeSelection();
            string id = owned[selectedSkill];
            if (inventory.Slots[selectedSlot].skillId == id) return;
            bool equipped = inventory.TryEquip(id, selectedSlot);
            if (equipped) Persist();
            RefreshText();
            SetStatus(equipped ? "Đã trang bị " + skillManager.GetSkillData(id)?.DisplayName + " vào ô " + inventory.Slots[selectedSlot].key + "." :
                "Kỹ năng này đã ở ô khác. Hãy tháo ô đó trước khi chuyển.");
        }
        private void RemoveSelected()
        {
            if(!IsOpen || GameplayState.IsBlockedByOther(this))return;
            if (inventory.Slots[selectedSlot].IsEmpty) return;
            inventory.RemoveFromSlot(selectedSlot);Persist();RefreshText();
            SetStatus("Đã tháo kỹ năng khỏi ô " + inventory.Slots[selectedSlot].key + ". Kỹ năng vẫn ở thư viện.");
        }
        private void BuildSkillCards()
        {
            if(skillList==null)return;
            skillButtons.Clear();
            foreach (var pair in cards) pair.Value.gameObject.SetActive(owned.Contains(pair.Key));
            for(int i=0;i<owned.Count;i++)
            {
                string id = owned[i];
                var data=skillManager!=null?skillManager.GetSkillData(id):null;
                if (!cards.TryGetValue(id, out Button button))
                {
                    button=F.Button(skillList,"Skill"+id,"",0,0,326,100,()=>SelectSkill(id));
                    button.gameObject.AddComponent<ScrollSelectionIntoView>();
                    F.Icon(button.transform,"Icon",data!=null?data.Icon:null,12,18,60,60);
                    F.Text(button.transform,"Name",data!=null?data.DisplayName:id,84,8,230,50,22,F.TextColor);
                    F.Text(button.transform,"Level","",84,68,230,24,18,F.Muted);
                    cards.Add(id, button);
                }
                button.transform.SetAsLastSibling();
                button.transform.Find("Level").GetComponent<TMP_Text>().text="Cấp "+inventory.GetLevel(id);
                skillButtons.Add(button);
            }
            if (IsOpen && EventSystem.current != null)
                EventSystem.current.currentSelectedGameObject?.GetComponent<ScrollSelectionIntoView>()?.Reveal();
        }
        private void EnsurePanel()
        {
            var canvas=GetComponentInParent<Canvas>();if(canvas==null||panel!=null)return;
            launcherButton=F.Button(canvas.transform,"SkillLauncher","KỸ NĂNG [K]",0,0,250,44,Open);
            var launch=launcherButton.GetComponent<RectTransform>();launch.anchorMin=launch.anchorMax=launch.pivot=Vector2.one;launch.anchoredPosition=new Vector2(-30,-154);
            launcherLabel=launcherButton.GetComponentInChildren<TMP_Text>();
            panel=new GameObject("SkillEquipmentPanel",typeof(RectTransform),typeof(Image));
            panel.transform.SetParent(canvas.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var window=F.Window(EquipmentPanelLayout.CreateSurface(panel),"KỸ NĂNG","THƯ VIỆN  /  TRANG BỊ  /  NÂNG CẤP",Close);
            var hints = window.Find("KeyHints").GetComponent<TMP_Text>();
            hints.text="MŨI TÊN / ENTER  Chọn   •   1–4  Chọn ô   •   U  Nâng cấp   •   BACKSPACE  Tháo   •   K / ESC  Đóng";
            hints.rectTransform.sizeDelta = new Vector2(1272, 26);
            closeButton = window.Find("Close").GetComponent<Button>();
            balance=F.Text(window,"Gold","",1190,39,392,42,24,F.Gold,TextAlignmentOptions.Right);
            var library=F.Frame(window,"Library",24,122,374,724,F.Panel);
            F.Text(library,"Heading","KỸ NĂNG ĐÃ HỌC",24,18,326,40,24,F.Gold);
            F.Grid(library,"Skills",20,78,350,620,new Vector2(326,100),1,out skillList);
            var detail=F.Frame(window,"Details",422,122,890,460,F.Panel);
            F.Medallion(detail,22,28,138,F.Gold);
            selectedIcon=F.Icon(detail,"Icon",null,35,42,112,112);
            heading=F.Text(detail,"Name","",182,28,680,46,30,F.Gold);
            content=F.Text(detail,"Content","",182,92,680,272,23);
            content.alignment=TextAlignmentOptions.TopLeft;
            upgradeButton=F.Button(detail,"Upgrade","NÂNG CẤP [U]",22,386,846,52,UpgradeSelected,true);
            upgradeLabel=upgradeButton.GetComponentInChildren<TMP_Text>();
            var loadout=F.Frame(window,"Loadout",422,600,890,246,F.Panel);
            F.Text(loadout,"Heading","BỐN Ô KỸ NĂNG",22,12,846,28,20,F.Gold);
            for(int i=0;i<4;i++)
            {
                int slot=i;
                slotButtons[i]=F.Button(loadout,"Slot"+i,"",22+(i%2)*431,46+(i/2)*66,415,60,()=>SelectSlot(slot));
                slotIcons[i]=F.Icon(slotButtons[i].transform,"SkillIcon",null,10,8,44,44);
                var label=slotButtons[i].GetComponentInChildren<TMP_Text>();
                label.rectTransform.anchoredPosition=new Vector2(66,-4);
                label.rectTransform.sizeDelta=new Vector2(337,52);
                label.fontSizeMax=19;label.fontSizeMin=16;
            }
            equipButton=F.Button(loadout,"Equip","TRANG BỊ",22,188,415,42,EquipSelected,true);
            removeButton=F.Button(loadout,"Remove","THÁO [BACKSPACE]",453,188,415,42,RemoveSelected);
            status=F.Text(window,"Status","",32,910,1272,26,17,F.Gold);
            panel.GetComponent<EquipmentPanelLayout>().ConfigureWindow(window, true, balance, status);
        }

        private void OnDisable() { Close(); Unsubscribe(); }

        private void OnDestroy()
        {
            Unsubscribe();
            focus.Release();
            GameplayState.ExitModal(this);
            if(panel!=null)Destroy(panel);
            if(launcherButton!=null)Destroy(launcherButton.gameObject);
        }
    }
}
