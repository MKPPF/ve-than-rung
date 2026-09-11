using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using F = TopDownGame.UI.FantasyEquipmentUI;

namespace TopDownGame.UI
{
    /// <summary>Attribute cards share the equipment UI language and delegate spending/persistence to PlayerStats.</summary>
    [DisallowMultipleComponent]
    public sealed class PotentialUpgradeUI : MonoBehaviour
    {
        private GameObject panel;
        private Button launcherButton;
        private TMP_Text launcherLabel;
        private TMP_Text summaryText;
        private TMP_Text statText;
        private readonly TMP_Text[] values = new TMP_Text[4];
        private readonly Button[] upgradeButtons = new Button[4];
        private PlayerStats stats;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            stats = player != null ? player.GetComponent<PlayerStats>() : null;
            BuildPanel();
            if (panel != null) panel.SetActive(false);
            if (stats != null) stats.PotentialChanged += Refresh;
            Refresh();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.pKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
                return;
            }

            if (!IsOpen) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (keyboard.digit1Key.wasPressedThisFrame) Spend(PotentialAttribute.Vitality);
            if (keyboard.digit2Key.wasPressedThisFrame) Spend(PotentialAttribute.Power);
            if (keyboard.digit3Key.wasPressedThisFrame) Spend(PotentialAttribute.Guard);
            if (keyboard.digit4Key.wasPressedThisFrame) Spend(PotentialAttribute.Haste);
        }

        public void Open()
        {
            if (IsOpen || panel == null || stats == null || Time.timeScale <= 0 ||
                GameplayState.IsBlockedByOther(this)) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            GameplayState.EnterModal(this);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            GameplayState.ExitModal(this);
        }

        private void Spend(PotentialAttribute attribute)
        {
            if (IsOpen && !GameplayState.IsBlockedByOther(this) && stats != null && stats.TrySpendPotential(attribute)) Refresh();
        }

        private void Refresh()
        {
            if (stats == null) return;
            if (launcherLabel != null)
            {
                launcherLabel.text = stats.AvailablePotentialPoints > 0
                    ? "P  TIỀM NĂNG  +" + stats.AvailablePotentialPoints
                    : "P  TIỀM NĂNG";
            }

            if (summaryText != null)
            {
                summaryText.text = "ĐIỂM CÒN LẠI: " + stats.AvailablePotentialPoints +
                    "\nMỗi lần lên cấp nhận 1 điểm. Chọn hướng phát triển lâu dài.";
            }

            if (statText != null)
            {
                statText.text =
                    "Sinh lực " + stats.VitalityPoints + "   |   Máu " +
                    Mathf.RoundToInt(stats.MaxHealth) +
                    "\nSức mạnh " + stats.PowerPoints + "   |   Sát thương " +
                    stats.BaseDamage.ToString("0.0") +
                    "\nPhòng thủ " + stats.GuardPoints + "   |   Giáp " +
                    stats.Armor.ToString("0.0") +
                    "\nTinh thông " + stats.HastePoints + "   |   Hồi kỹ năng x" +
                    stats.CooldownRate.ToString("0.00");
            }

            int[] invested = { stats.VitalityPoints, stats.PowerPoints, stats.GuardPoints, stats.HastePoints };
            string[] current = { stats.MaxHealth.ToString("N0")+" máu", stats.BaseDamage.ToString("0.0")+" sát thương",
                stats.Armor.ToString("0.00")+" giáp", (stats.CooldownRate*100).ToString("0.#")+"% tốc hồi" };
            for(int i=0;i<4;i++) if(values[i]!=null) values[i].text="ĐÃ CỘNG  "+invested[i]+" ĐIỂM\n"+current[i];

            foreach (Button button in upgradeButtons)
            {
                if (button != null) button.interactable = stats.AvailablePotentialPoints > 0;
            }
        }

        private void BuildPanel()
        {
            Canvas canvas=GetComponentInParent<Canvas>();
            if(canvas==null || panel!=null)return;
            launcherButton=F.Button(canvas.transform,"PotentialLauncher","TIỀM NĂNG [P]",0,0,250,44,Open);
            var launch=launcherButton.GetComponent<RectTransform>();
            launch.anchorMin=launch.anchorMax=launch.pivot=Vector2.one;launch.anchoredPosition=new Vector2(-30,-100);
            launcherLabel=launcherButton.GetComponentInChildren<TMP_Text>();
            panel=new GameObject("PotentialPanel",typeof(RectTransform),typeof(Image));
            panel.transform.SetParent(canvas.transform,false);
            var overlay=panel.AddComponent<Canvas>();overlay.overrideSorting=true;overlay.sortingOrder=2000;
            panel.AddComponent<GraphicRaycaster>();
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var window=F.Window(EquipmentPanelLayout.CreateSurface(panel),"TIỀM NĂNG","NHÂN VẬT  /  PHÁT TRIỂN",Close);
            window.Find("KeyHints").GetComponent<TMP_Text>().text="P / ESC  Đóng    •    1–4  Cộng điểm    •    Mỗi lần cộng được lưu ngay";
            var profile=F.Frame(window,"Profile",24,122,450,724,F.Panel);
            F.Text(profile,"Heading","TIỀM NĂNG THỢ SĂN",26,22,394,42,27,F.Gold);
            summaryText=F.Text(profile,"Summary","",26,84,394,112,24);
            statText=F.Text(profile,"Stats","",26,222,394,238,23,F.Muted);
            F.Text(profile,"Note","Mỗi cấp nhận 1 điểm tiềm năng.\n\nĐiểm được cộng trực tiếp, không thể hoàn lại trong màn hình này.",26,510,394,160,21,F.Muted);
            string[] names={"SINH LỰC","SỨC MẠNH","PHÒNG THỦ","TINH THÔNG"};
            string[] descriptions={"+ "+PlayerStats.VitalityHealthPerPoint+" máu tối đa / điểm",
                "+ "+PlayerStats.PowerDamagePerPoint+" sát thương / điểm","+ "+PlayerStats.GuardArmorPerPoint+" giáp / điểm",
                "+ "+(PlayerStats.HasteCooldownPerPoint*100)+"% tốc độ hồi kỹ năng / điểm"};
            for(int i=0;i<4;i++)
            {
                int index=i;
                var card=F.Frame(window,"Attribute"+i,498+(i%2)*598,122+(i/2)*372,574,352,F.Panel);
                F.Text(card,"Title",names[i],26,22,522,38,28,F.Gold);
                values[i]=F.Text(card,"Value","",26,80,522,100,25);
                F.Text(card,"Effect",descriptions[i],26,188,522,52,21,F.Green);
                upgradeButtons[i]=F.Button(card,"Spend"+i,"+1 ĐIỂM  ["+(i+1)+"]",26,272,522,54,()=>Spend((PotentialAttribute)index),true);
            }
        }

        private void OnDisable() { Close(); }

        private void OnDestroy()
        {
            if (stats != null) stats.PotentialChanged -= Refresh;
            GameplayState.ExitModal(this);
            if(panel!=null)Destroy(panel);
            if(launcherButton!=null)Destroy(launcherButton.gameObject);
        }
    }
}
