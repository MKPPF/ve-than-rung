using System.Collections.Generic;
using TMPro;
using TopDownGame.Core;
using TopDownGame.Save;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Requires one meaningful chest reward and saves it before releasing modal ownership.</summary>
    public sealed class SkillSelectionUI : MonoBehaviour
    {
        [SerializeField] private SkillData[] skillDatabase;
        [SerializeField] private GameObject panel;
        private SkillInventory inventory;
        private readonly MenuModalFocus focus = new MenuModalFocus();
        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake() { if (panel != null) panel.SetActive(false); }

        public void Show(SkillInventory targetInventory)
        {
            if (IsOpen || GameplayState.IsBlockedByOther(this) || targetInventory == null || skillDatabase == null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            inventory = targetInventory;
            var choices = new List<SkillData>();
            var ids = new HashSet<SkillId>();
            foreach (SkillData data in skillDatabase)
                if (data != null && inventory.GetLevel(data.Id.ToString()) < data.MaxLevel && ids.Add(data.Id)) choices.Add(data);
            while (choices.Count > 3) choices.RemoveAt(Random.Range(0, choices.Count));
            if (panel != null) { panel.SetActive(false); Destroy(panel); }
            panel = new GameObject("SkillSelectionPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Transform surface = EquipmentPanelLayout.CreateSurface(panel, new Vector2(1390, 900));
            Transform frame = FantasyEquipmentUI.Frame(surface, "SkillRewardWindow", 345, 185, 1230, 710, FantasyEquipmentUI.Ink);
            FantasyEquipmentUI.Text(frame, "Title", "CHỌN MỘT KỸ NĂNG", 40, 30, 1150, 62, 36,
                FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
            FantasyEquipmentUI.Text(frame, "Hint", choices.Count > 0
                ? "Phần thưởng từ rương  •  Chọn một kỹ năng để tiếp tục."
                : "Bạn đã nâng tối đa toàn bộ kỹ năng trong rương này.",
                40, 102, 1150, 50, 22, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            Button first = null;
            for (int i = 0; i < choices.Count; i++)
            {
                SkillData choice = choices[i];
                float x = 615 - (choices.Count * 366 + (choices.Count - 1) * 26) / 2f + i * 392;
                Button button = FantasyEquipmentUI.Button(frame, "Reward_" + choice.Id, "", x, 174, 366, 420, () => Select(choice));
                int current = inventory.GetLevel(choice.Id.ToString());
                FantasyEquipmentUI.Icon(button.transform, "Icon", choice.Icon, 136, 20, 94, 94);
                FantasyEquipmentUI.Text(button.transform, "Name", choice.DisplayName, 20, 130, 326, 45, 25,
                    FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
                FantasyEquipmentUI.Text(button.transform, "Level", current == 0 ? "KỸ NĂNG MỚI" : "Cấp " + current + " → " + (current + 1),
                    20, 177, 326, 30, 20, FantasyEquipmentUI.Green, TextAlignmentOptions.Center);
                FantasyEquipmentUI.Text(button.transform, "Description", choice.Description, 24, 226, 318, 112, 22,
                    null, TextAlignmentOptions.TopLeft);
                FantasyEquipmentUI.Text(button.transform, "Action", current == 0 ? "HỌC KỸ NĂNG" : "NÂNG CẤP KỸ NĂNG",
                    20, 365, 326, 32, 22, FantasyEquipmentUI.Gold, TextAlignmentOptions.Center);
                if (first == null) first = button;
            }
            if (first == null)
                first = FantasyEquipmentUI.Button(frame, "CloseRewards", "TIẾP TỤC", 385, 390, 460, 64, Close, true);
            FantasyEquipmentUI.Text(frame, "Footer", "Kỹ năng mới tự vào ô trống. Nhấn K sau khi nhận để đổi kỹ năng đang dùng.",
                40, 631, 1150, 44, 21, FantasyEquipmentUI.Muted, TextAlignmentOptions.Center);
            GameplayState.EnterModal(this);
            focus.Capture(panel.transform);
            first.Select();
        }

        private void Select(SkillData data)
        {
            if (!IsOpen || GameplayState.IsBlockedByOther(this) || inventory == null || data == null) return;
            string id = data.Id.ToString();
            if (inventory.GetLevel(id) >= data.MaxLevel) return;
            bool alreadyOwned = inventory.Owns(id);
            inventory.AddOrUpgrade(id, data.MaxLevel);
            if (!alreadyOwned)
                for (int i = 0; i < inventory.Slots.Count; i++)
                    if (inventory.Slots[i].IsEmpty) { inventory.TryEquip(id, i); break; }
            inventory.GetComponent<SkillManager>()?.RefreshSkills();
            GameProgressManager.Instance?.CaptureActivePlayer();
            Close();
        }

        private void Close()
        {
            if (panel != null) panel.SetActive(false);
            GameplayState.ExitModal(this);
            focus.Release();
        }
        private void OnDisable() => Close();
        private void OnDestroy()
        {
            Close();
            if (panel != null) Destroy(panel);
        }
    }
}
