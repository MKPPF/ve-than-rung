using System.Collections.Generic;
using TMPro;
using TopDownGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Offers a compact character drawer while preserving controller-owned buttons, shortcuts and modal focus.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-700)]
    public sealed class HudUtilityLayout : MonoBehaviour
    {
        private static readonly string[] LauncherNames =
            { "InventoryLauncher", "PotentialLauncher", "SkillLauncher", "ForgeLauncher" };
        private static readonly string[] AutomationNames =
            { "AutoAttackToggle", "AutoPickupToggle", "GestureToggle" };
        private static readonly string[] DodgeTextNames = { "DodgeKey", "DodgeStatus" };
        private static readonly string[] DodgeBarNames = { "Track", "Cooldown" };
        private readonly List<Button> controls = new List<Button>(8);
        private Canvas canvas;
        private Button drawerButton;
        private TMP_Text drawerLabel, drawerHint, gestureSummary;
        private RectTransform drawerRoot, summaryBacking;
        private GestureInputReceiver gestureReceiver;
        private bool expanded;
        public bool IsExpanded => expanded;

        private void Awake() => canvas = GetComponentInParent<Canvas>();

        private void Update()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            bool ownsSelection = false;
            foreach (Button button in controls)
                if (button != null && button.gameObject == selected) { ownsSelection = true; break; }
            if (GameplayState.IsBlockedByOther(this)) return;
            if (expanded && (keyboard != null && keyboard.escapeKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame))
            {
                SetExpanded(false);
                EventSystem.current?.SetSelectedGameObject(null);
                GameplayState.ConsumeNavigationInput();
                return;
            }
            if ((keyboard != null && keyboard.tabKey.wasPressedThisFrame || pad != null && pad.selectButton.wasPressedThisFrame) &&
                !GameplayState.IsBlockedByOther(this) && controls.Count > 0 && EventSystem.current != null)
            {
                SetExpanded(!ownsSelection);
                EventSystem.current.SetSelectedGameObject(ownsSelection ? null : drawerButton.gameObject);
                GameplayState.ConsumeNavigationInput();
                return;
            }
            if (!ownsSelection) return;
            var module = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            if (module == null) return;
            // Read the actual UI actions so WASD, alternate sticks and Submit
            // bindings stay in sync with the generated Input System asset.
            bool navigation = module.move != null && module.move.action != null && module.move.action.ReadValue<Vector2>().sqrMagnitude > 0.01f;
            navigation |= module.submit != null && module.submit.action != null && module.submit.action.IsPressed();
            if (navigation) GameplayState.ConsumeNavigationInput();
        }

        private void LateUpdate()
        {
            if (canvas == null) return;
            EnsureDrawer();
            // CanvasScaler's reference resolution alone makes small-screen text
            // too small. Keep 24-point labels at least 16.8 physical pixels.
            float scale = Mathf.Max(1f, 0.7f / Mathf.Max(0.01f, canvas.scaleFactor));
            LayoutStatusCards(scale);
            controls.Clear();
            drawerRoot.localScale = Vector3.one * scale;
            drawerRoot.anchoredPosition = new Vector2(-22, -22);
            drawerButton.interactable = !GameplayState.IsBlockedByOther(this);
            StyleButton((RectTransform)drawerButton.transform);
            TMP_Text potential = canvas.transform.Find("PotentialLauncher/Label")?.GetComponent<TMP_Text>();
            drawerLabel.text = "NHÂN VẬT" + (potential != null && potential.text.Contains("+") ? " (+)" : "") + (expanded ? "  [-]" : "  [+]");
            drawerHint.text = expanded ? "ESC / B: thu gọn · TAB: rời HUD" : "TAB / VIEW: mở · I K P N";
            drawerHint.gameObject.SetActive(!expanded);
            LayoutSummary();
            for (int i = 0; i < LauncherNames.Length; i++)
            {
                var rect = canvas.transform.Find(LauncherNames[i]) as RectTransform;
                if (rect == null) continue;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                rect.localScale = Vector3.one * scale;
                rect.anchoredPosition = new Vector2(-22, -22 - (64 + i * 64) * scale);
                ApplyVisibility(rect, expanded);
                StyleButton(rect);
            }
            var automation = canvas.transform.Find("AutomationControls") as RectTransform;
            if (automation != null)
            {
                automation.anchorMin = automation.anchorMax = automation.pivot = Vector2.one;
                automation.localScale = Vector3.one * scale;
                automation.anchoredPosition = new Vector2(-22, -22 - 320 * scale);
                ApplyVisibility(automation, expanded);
                for (int i = 0; i < AutomationNames.Length; i++)
                {
                    var rect = automation.Find(AutomationNames[i]) as RectTransform;
                    if (rect == null) continue;
                    rect.anchoredPosition = new Vector2(0, -i * 64);
                    StyleButton(rect);
                }
                TMP_Text status = automation.Find("GestureStatus")?.GetComponent<TMP_Text>();
                float hintHeight = 44;
                if (status != null)
                {
                    status.rectTransform.anchoredPosition = new Vector2(10, -198);
                    status.enableAutoSizing = false;
                    status.fontSize = 22;
                    status.alignment = TextAlignmentOptions.TopLeft;
                    hintHeight = Mathf.Clamp(status.GetPreferredValues(status.text, 280, 0).y + 12, 44, 108);
                    status.rectTransform.sizeDelta = new Vector2(280, hintHeight - 12);
                }
                var hint = automation.Find("GestureHintBacking") as RectTransform;
                if (hint != null)
                {
                    hint.anchoredPosition = new Vector2(0, -192);
                    hint.sizeDelta = new Vector2(300, hintHeight);
                    Resize(hint, "Brass", 296, hintHeight - 4);
                    Resize(hint, "Inset", 292, hintHeight - 8);
                    Resize(hint, "Highlight", 290, 1);
                }
                TMP_Text navigationHint = automation.Find("NavigationHint")?.GetComponent<TMP_Text>();
                if (navigationHint != null)
                {
                    navigationHint.rectTransform.anchoredPosition = new Vector2(10, -204 - hintHeight);
                    navigationHint.rectTransform.sizeDelta = new Vector2(280, 84);
                    navigationHint.enableAutoSizing = false;
                    navigationHint.fontSize = 21;
                }
                var navigationBacking = automation.Find("NavigationHintBacking") as RectTransform;
                if (navigationBacking != null)
                {
                    navigationBacking.anchoredPosition = new Vector2(0, -200 - hintHeight);
                    navigationBacking.sizeDelta = new Vector2(300, 92);
                }
                automation.sizeDelta = new Vector2(300, 292 + hintHeight);
            }
            for (int i = 0; i < controls.Count; i++)
                controls[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? controls[i - 1] : null,
                    selectOnDown = i + 1 < controls.Count ? controls[i + 1] : null
                };
        }

        private void EnsureDrawer()
        {
            if (drawerButton != null) return;
            drawerRoot = FantasyEquipmentUI.Rect(canvas.transform, "CharacterDrawer", 0, 0, 300, 88);
            drawerRoot.anchorMin = drawerRoot.anchorMax = drawerRoot.pivot = Vector2.one;
            drawerButton = FantasyEquipmentUI.Button(drawerRoot, "Toggle", "", 0, 0, 300, 54, () =>
            {
                if (GameplayState.IsBlockedByOther(this)) return;
                SetExpanded(!expanded);
                GameplayState.ConsumeNavigationInput();
            });
            drawerLabel = drawerButton.GetComponentInChildren<TMP_Text>();
            drawerHint = FantasyEquipmentUI.Text(drawerRoot, "Hint", "", 0, 58, 300, 28, 21, FantasyEquipmentUI.TextColor);
            drawerHint.enableAutoSizing = false;
            summaryBacking = (RectTransform)FantasyEquipmentUI.Frame(drawerRoot, "GestureSummaryBacking", 0, 92, 300, 96, FantasyEquipmentUI.Ink);
            gestureSummary = FantasyEquipmentUI.Text(drawerRoot, "GestureSummary", "", 10, 98, 280, 84, 22, FantasyEquipmentUI.Gold, TextAlignmentOptions.TopLeft);
            gestureSummary.enableAutoSizing = false;
            gestureReceiver = FindAnyObjectByType<GestureInputReceiver>();
        }

        private void SetExpanded(bool value)
        {
            expanded = value;
            if (!value && EventSystem.current != null)
                foreach (Button control in controls)
                    if (control != null && EventSystem.current.currentSelectedGameObject == control.gameObject)
                    { EventSystem.current.SetSelectedGameObject(null); break; }
            // Apply before EventSystem processes Submit/navigation this frame.
            foreach (string name in LauncherNames)
                if (canvas.transform.Find(name) is RectTransform rect) ApplyVisibility(rect, value);
            if (canvas.transform.Find("AutomationControls") is RectTransform automation) ApplyVisibility(automation, value);
        }

        private static void ApplyVisibility(RectTransform rect, bool visible)
        {
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1 : 0;
            // Modal focus temporarily owns these flags while a panel is open.
            if (GameplayState.IsInputBlocked && GameplayState.IsPaused) return;
            group.interactable = group.blocksRaycasts = visible;
        }

        private void LayoutSummary()
        {
            bool show = !expanded && gestureReceiver != null && gestureReceiver.ControlEnabled;
            summaryBacking.gameObject.SetActive(show);
            gestureSummary.gameObject.SetActive(show);
            drawerRoot.sizeDelta = new Vector2(300, expanded ? 54 : 88);
            if (!show) return;
            gestureSummary.text = gestureReceiver.StatusText;
            float height = Mathf.Clamp(gestureSummary.GetPreferredValues(gestureSummary.text, 280, 0).y + 12, 44, 108);
            gestureSummary.rectTransform.sizeDelta = new Vector2(280, height - 12);
            summaryBacking.sizeDelta = new Vector2(300, height);
            drawerRoot.sizeDelta = new Vector2(300, 92 + height);
            Resize(summaryBacking, "Brass", 296, height - 4);
            Resize(summaryBacking, "Inset", 292, height - 8);
        }

        private void OnDestroy()
        {
            if (drawerRoot != null) Destroy(drawerRoot.gameObject);
        }

        private void LayoutStatusCards(float scale)
        {
            Transform lobby = canvas.transform.Find("LobbyHUDRoot");
            if (lobby != null)
            {
                if (lobby.Find("LocationCard") is RectTransform location) location.localScale = Vector3.one * scale;
                if (lobby.Find("Prompt") is RectTransform prompt) prompt.localScale = Vector3.one * scale;
            }
            var dodge = canvas.transform.Find("DodgeCard") as RectTransform;
            if (dodge == null) return;
            dodge.localScale = Vector3.one * scale;
            dodge.sizeDelta = new Vector2(300, 78);
            dodge.anchoredPosition = new Vector2(22, lobby != null ? -22 - 134 * scale : -310);
            Resize(dodge, "Brass", 296, 74);
            Resize(dodge, "Inset", 292, 70);
            Resize(dodge, "Highlight", 290, 1);
            foreach (string name in DodgeTextNames)
            {
                TMP_Text text = dodge.Find(name)?.GetComponent<TMP_Text>();
                if (text == null) continue;
                text.rectTransform.anchoredPosition = new Vector2(12, name == "DodgeKey" ? -4 : -32);
                text.rectTransform.sizeDelta = new Vector2(276, 28);
                text.enableAutoSizing = false;
                text.fontSize = 22;
            }
            foreach (string name in DodgeBarNames)
                if (dodge.Find(name) is RectTransform bar)
                {
                    bar.anchoredPosition = new Vector2(12, -66);
                    bar.sizeDelta = new Vector2(276, 4);
                }
            if (canvas.transform.Find("CombatHUDRoot/ControlHints") is RectTransform controlsRect)
                controlsRect.anchoredPosition = new Vector2(22, -310 - 78 * scale - 16);
        }

        private void StyleButton(RectTransform rect)
        {
            rect.sizeDelta = new Vector2(300, 54);
            Resize(rect, "Brass", 296, 50);
            Resize(rect, "Inset", 292, 46);
            Resize(rect, "Highlight", 290, 1);
            TMP_Text label = rect.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.rectTransform.sizeDelta = new Vector2(284, 46);
                label.enableAutoSizing = false;
                label.fontSize = 24;
            }
            Button button = rect.GetComponent<Button>();
            if (button != null && button.IsActive() && button.IsInteractable()) controls.Add(button);
        }

        private static void Resize(Transform parent, string name, float width, float height)
        {
            if (parent.Find(name) is RectTransform layer) layer.sizeDelta = new Vector2(width, height);
        }
    }
}
