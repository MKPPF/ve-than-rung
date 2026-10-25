using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private readonly MenuModalFocus focus = new MenuModalFocus();
        private Button closeButton;
        private TMP_Text status;
        private bool subscribed;
        private int lastSpendFrame = -1;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            stats = player != null ? player.GetComponent<PlayerStats>() : null;
            BuildPanel();
            if (panel != null) panel.SetActive(false);
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
            if (subscribed || stats == null) return;
            stats.PotentialChanged += Refresh;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (stats != null) stats.PotentialChanged -= Refresh;
            subscribed = false;
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
            if (!isActiveAndEnabled || IsOpen || panel == null || stats == null || Time.timeScale <= 0 ||
                GameplayState.IsBlockedByOther(this)) return;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            focus.Capture(panel.transform);
            launcherButton.gameObject.SetActive(false);
            GameplayState.EnterModal(this);
            status.text = "Mỗi lần cộng được lưu ngay. Điểm đã cộng không thể hoàn lại trong màn hình này.";
            Refresh();
            DefaultButton().Select();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (launcherButton != null) launcherButton.gameObject.SetActive(isActiveAndEnabled);
            GameplayState.ExitModal(this);
            focus.Release();
        }

        private void Spend(PotentialAttribute attribute)
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || stats == null ||
                stats.AvailablePotentialPoints <= 0 || lastSpendFrame == Time.frameCount) return;
            lastSpendFrame = Time.frameCount;
            if (!stats.TrySpendPotential(attribute)) return;
            string[] names = { "Sinh lực", "Sức mạnh", "Phòng thủ", "Tinh thông" };
            status.text = "Đã cộng 1 điểm " + names[(int)attribute] + ". Còn " + stats.AvailablePotentialPoints + " điểm. Đã lưu.";
            Refresh();
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
            if (IsOpen && EventSystem.current != null)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected == null || selected.GetComponent<Selectable>() is Selectable control && !control.IsInteractable())
                    DefaultButton().Select();
            }
        }

        private Button DefaultButton() => stats != null && stats.AvailablePotentialPoints > 0 ? upgradeButtons[0] : closeButton;

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
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var window=F.Window(EquipmentPanelLayout.CreateSurface(panel),"TIỀM NĂNG","NHÂN VẬT  /  PHÁT TRIỂN",Close);
            window.Find("KeyHints").GetComponent<TMP_Text>().text="P / ESC  Đóng    •    1–4  Cộng điểm    •    Mỗi lần cộng được lưu ngay";
            closeButton = window.Find("Close").GetComponent<Button>();
            var balance = F.Text(window,"Points","ĐIỂM TIỀM NĂNG",814,39,392,42,24,F.Gold,TextAlignmentOptions.Right);
            status = F.Text(window,"Status","",32,910,1272,26,17,F.Gold);
            panel.GetComponent<EquipmentPanelLayout>().ConfigureWindow(window, true, balance, status);
            var profile=F.Frame(window,"Profile",24,122,1288,130,F.Panel);
            summaryText=F.Text(profile,"Summary","",24,16,410,100,24);
            statText=F.Text(profile,"Stats","",470,12,790,110,23,F.Muted);
            string[] names={"SINH LỰC","SỨC MẠNH","PHÒNG THỦ","TINH THÔNG"};
            string[] descriptions={"+ "+PlayerStats.VitalityHealthPerPoint+" máu tối đa / điểm",
                "+ "+PlayerStats.PowerDamagePerPoint+" sát thương / điểm","+ "+PlayerStats.GuardArmorPerPoint+" giáp / điểm",
                "+ "+(PlayerStats.HasteCooldownPerPoint*100)+"% tốc độ hồi kỹ năng / điểm"};
            for(int i=0;i<4;i++)
            {
                int index=i;
                var card=F.Frame(window,"Attribute"+i,24+(i%2)*656,270+(i/2)*292,632,278,F.Panel);
                F.Text(card,"Title",names[i],26,16,580,38,28,F.Gold);
                values[i]=F.Text(card,"Value","",26,64,580,68,25);
                F.Text(card,"Effect",descriptions[i],26,145,580,48,21,F.Green);
                upgradeButtons[i]=F.Button(card,"Spend"+i,"+1 ĐIỂM  ["+(i+1)+"]",26,210,580,48,()=>Spend((PotentialAttribute)index),true);
            }
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
