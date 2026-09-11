using System.Collections.Generic;

using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using TopDownGame.Save;
using TopDownGame.Skills;
using UnityEngine;
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

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            inventory = player != null ? player.GetComponent<SkillInventory>() : null;
            skillManager = player != null ? player.GetComponent<SkillManager>() : null;
            stats = player != null ? player.GetComponent<PlayerStats>() : null;
            EnsurePanel();
            if (panel != null) panel.SetActive(false);
            if (inventory != null) inventory.Changed += HandleInventoryChanged;
            if (stats != null) stats.GoldChanged += HandleGoldChanged;
            RefreshOwned();
            RefreshText();
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
            bool changed = false;
            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                selectedSkill--;
                changed = true;
            }
            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                selectedSkill++;
                changed = true;
            }
            if (keyboard.digit1Key.wasPressedThisFrame) { selectedSlot = 0; changed = true; }
            if (keyboard.digit2Key.wasPressedThisFrame) { selectedSlot = 1; changed = true; }
            if (keyboard.digit3Key.wasPressedThisFrame) { selectedSlot = 2; changed = true; }
            if (keyboard.digit4Key.wasPressedThisFrame) { selectedSlot = 3; changed = true; }

            NormalizeSelection();
            if (keyboard.enterKey.wasPressedThisFrame && owned.Count > 0)
            {
                EquipSelected();
            }
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                RemoveSelected();
            }
            if (keyboard.uKey.wasPressedThisFrame) UpgradeSelected();
            if (changed) RefreshText();
        }

        public void Open()
        {
            if (IsOpen || panel == null || inventory == null || Time.timeScale <= 0 ||
                GameplayState.IsBlockedByOther(this)) return;
            RefreshOwned();
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            if (launcherButton != null) launcherButton.gameObject.SetActive(false);
            GameplayState.EnterModal(this);
            RefreshText();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (launcherButton != null) launcherButton.gameObject.SetActive(true);
            GameplayState.ExitModal(this);
        }

        private void UpgradeSelected()
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || owned.Count == 0 || stats == null || skillManager == null) return;
            string id = owned[selectedSkill];
            SkillData data = skillManager.GetSkillData(id);
            int level = inventory.GetLevel(id);
            if (data == null || level >= data.MaxLevel) return;

            int cost = SkillInventory.UpgradeGoldCost(level);
            if (!stats.TrySpendGold(cost))
            {
                RefreshText();
                return;
            }

            if (!inventory.TryUpgrade(id, data.MaxLevel))
            {
                stats.AddGold(cost);
                return;
            }

            skillManager.RefreshSkills();
            Persist();
            RefreshText();
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
                  "\n\nMana cơ bản: "+data.ManaCost.ToString("0.#")+"\nHồi chiêu cơ bản: "+data.Cooldown.ToString("0.#")+" giây"+
                  "\n\n"+(atMaximum?"Đã đạt cấp tối đa.":"Nâng cấp tiếp: "+cost.ToString("N0")+" vàng")
                : "Nhận kỹ năng từ phần thưởng và rương kỹ năng.\n\nCác kỹ năng đã học sẽ xuất hiện ở danh sách bên trái.";
            bool duplicate=false;
            for(int i=0;i<slotButtons.Length;i++)
            {
                var slot=inventory.Slots[i];
                var slotData=skillManager!=null?skillManager.GetSkillData(slot.skillId):null;
                slotButtons[i].GetComponentInChildren<TMP_Text>().text=slot.key+"  ["+(i+1)+"]\n"+
                    (slot.IsEmpty?"Ô TRỐNG":(slotData!=null?slotData.DisplayName:slot.skillId)+"  Lv."+slot.level);
                slotIcons[i].sprite=slotData!=null?slotData.Icon:null;
                slotIcons[i].enabled=slotIcons[i].sprite!=null;
                slotButtons[i].transform.Find("Brass").GetComponent<Image>().color=i==selectedSlot?F.Gold:F.Edge;
                if(!slot.IsEmpty && slot.skillId==selectedId && i!=selectedSlot)duplicate=true;
            }
            equipButton.interactable=data!=null && !duplicate && inventory.Slots[selectedSlot].skillId!=selectedId;
            removeButton.interactable=!inventory.Slots[selectedSlot].IsEmpty;
            for(int i=0;i<skillButtons.Count;i++)
                skillButtons[i].transform.Find("Brass").GetComponent<Image>().color=i==selectedSkill?F.Gold:F.Edge;
            status.text=duplicate?"Kỹ năng này đã ở ô khác. Hãy tháo ô đó trước khi chuyển.":
                "Chọn kỹ năng → chọn ô Q / E / R / F → TRANG BỊ";
            if (upgradeLabel != null)
            {
                upgradeLabel.text = atMaximum
                    ? "ĐÃ TỐI ĐA"
                    : "NÂNG CẤP [U]  ·  " + cost.ToString("N0") + " VÀNG";
            }
            if (upgradeButton != null)
            {
                upgradeButton.interactable = data != null && !atMaximum &&
                    stats != null && stats.Gold >= cost;
            }
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
            if(inventory.TryEquip(owned[selectedSkill],selectedSlot))Persist();
            RefreshText();
        }
        private void RemoveSelected()
        {
            if(!IsOpen || GameplayState.IsBlockedByOther(this))return;
            inventory.RemoveFromSlot(selectedSlot);Persist();RefreshText();
        }
        private void BuildSkillCards()
        {
            if(skillList==null)return;
            F.Clear(skillList);skillButtons.Clear();
            for(int i=0;i<owned.Count;i++)
            {
                int index=i;var data=skillManager!=null?skillManager.GetSkillData(owned[i]):null;
                var button=F.Button(skillList,"Skill"+i,"",0,0,474,94,()=>{selectedSkill=index;RefreshText();});
                F.Icon(button.transform,"Icon",data!=null?data.Icon:null,14,13,66,66);
                F.Text(button.transform,"Name",data!=null?data.DisplayName:owned[i],96,16,358,32,24,F.TextColor);
                F.Text(button.transform,"Level","Cấp "+inventory.GetLevel(owned[i]),96,53,358,24,18,F.Muted);
                skillButtons.Add(button);
            }
        }
        private void EnsurePanel()
        {
            var canvas=GetComponentInParent<Canvas>();if(canvas==null||panel!=null)return;
            launcherButton=F.Button(canvas.transform,"SkillLauncher","KỸ NĂNG [K]",0,0,250,44,Open);
            var launch=launcherButton.GetComponent<RectTransform>();launch.anchorMin=launch.anchorMax=launch.pivot=Vector2.one;launch.anchoredPosition=new Vector2(-30,-154);
            launcherLabel=launcherButton.GetComponentInChildren<TMP_Text>();
            panel=new GameObject("SkillEquipmentPanel",typeof(RectTransform),typeof(Image));
            panel.transform.SetParent(canvas.transform,false);
            var overlay=panel.AddComponent<Canvas>();overlay.overrideSorting=true;overlay.sortingOrder=2000;
            panel.AddComponent<GraphicRaycaster>();
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var window=F.Window(EquipmentPanelLayout.CreateSurface(panel),"KỸ NĂNG","THƯ VIỆN  /  TRANG BỊ  /  NÂNG CẤP",Close);
            window.Find("KeyHints").GetComponent<TMP_Text>().text="K / ESC Đóng  •  ← → Chọn chiêu  •  1–4 Chọn ô";
            balance=F.Text(window,"Gold","",1190,39,392,42,24,F.Gold,TextAlignmentOptions.Right);
            var library=F.Frame(window,"Library",24,122,524,724,F.Panel);
            F.Text(library,"Heading","KỸ NĂNG ĐÃ HỌC",24,18,474,40,25,F.Gold);
            F.Grid(library,"Skills",20,78,484,620,new Vector2(474,94),1,out skillList);
            var detail=F.Frame(window,"Details",572,122,1116,460,F.Panel);
            F.Medallion(detail,24,28,172,F.Gold);
            selectedIcon=F.Icon(detail,"Icon",null,45,48,130,130);
            heading=F.Text(detail,"Name","",230,28,852,46,32,F.Gold);
            content=F.Text(detail,"Content","",230,92,852,336,23);
            content.alignment=TextAlignmentOptions.TopLeft;
            upgradeButton=F.Button(detail,"Upgrade","NÂNG CẤP [U]",24,278,186,116,UpgradeSelected,true);
            upgradeLabel=upgradeButton.GetComponentInChildren<TMP_Text>();
            var loadout=F.Frame(window,"Loadout",572,600,1116,246,F.Panel);
            F.Text(loadout,"Heading","BỐN Ô KỸ NĂNG",22,16,1068,28,20,F.Gold);
            for(int i=0;i<4;i++)
            {
                int slot=i;
                slotButtons[i]=F.Button(loadout,"Slot"+i,"",22+i*273,60,253,86,()=>{selectedSlot=slot;RefreshText();});
                slotIcons[i]=F.Icon(slotButtons[i].transform,"SkillIcon",null,12,15,56,56);
                var label=slotButtons[i].GetComponentInChildren<TMP_Text>();
                label.rectTransform.anchoredPosition=new Vector2(80,-8);
                label.rectTransform.sizeDelta=new Vector2(161,70);
                label.fontSizeMax=17;label.fontSizeMin=14;
            }
            equipButton=F.Button(loadout,"Equip","TRANG BỊ [ENTER]",22,170,522,54,EquipSelected,true);
            removeButton=F.Button(loadout,"Remove","THÁO [BACKSPACE]",566,170,526,54,RemoveSelected);
            status=F.Text(window,"Status","",740,874,940,28,16,F.Gold,TextAlignmentOptions.Right);
        }

        private void OnDisable() { Close(); }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= HandleInventoryChanged;
            if (stats != null) stats.GoldChanged -= HandleGoldChanged;
            GameplayState.ExitModal(this);
            if(panel!=null)Destroy(panel);
            if(launcherButton!=null)Destroy(launcherButton.gameObject);
        }
    }
}
