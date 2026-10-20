using TMPro;
using TopDownGame.Core;
using TopDownGame.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>
    /// Adds explicit automation controls to both HUDs using the existing runtime
    /// UI style, so existing scenes and future builder output share the same controls.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAutomationUI : MonoBehaviour
    {
        private GameObject root;
        private PlayerCombat combat;
        private PlayerHealth health;
        private Button attackButton, pickupButton, gestureButton;
        private TMP_Text attackLabel, pickupLabel, gestureStatus;
        private bool? displayedAttack, displayedPickup, displayedGesture;
        private GestureInputReceiver gestureReceiver;

        public static void Ensure(GameObject owner)
        {
            if (owner.GetComponent<PlayerAutomationUI>() == null)
                owner.AddComponent<PlayerAutomationUI>();
        }

        private void Start()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            GameObject player = GameObject.FindWithTag("Player");
            if (canvas == null || player == null) return;
            combat = player.GetComponent<PlayerCombat>();
            health = player.GetComponent<PlayerHealth>();

            RectTransform rect = FantasyEquipmentUI.Rect(canvas.transform,
                "AutomationControls", 0, 0, 250, 218);
            root = rect.gameObject;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            bool lobby = GetComponent<LobbyHUD>() != null;
            rect.anchoredPosition = lobby ? new Vector2(-22, -214) : new Vector2(-30, -266);
            if (lobby) rect.localScale = Vector3.one * 0.76f;
            attackButton = FantasyEquipmentUI.Button(rect, "AutoAttackToggle", "",
                0, 0, 250, 44, ToggleAttack);
            pickupButton = FantasyEquipmentUI.Button(rect, "AutoPickupToggle", "",
                0, 54, 250, 44, TogglePickup);
            gestureButton = FantasyEquipmentUI.Button(rect, "GestureToggle", "",
                0, 108, 250, 44, ToggleGestures);
            gestureStatus = FantasyEquipmentUI.Text(rect, "GestureStatus", "",
                0, 158, 250, 54, 14, FantasyEquipmentUI.Gold);
            gestureStatus.textWrappingMode = TextWrappingModes.Normal;
            gestureReceiver = FindAnyObjectByType<GestureInputReceiver>();
            attackButton.navigation = new Navigation { mode = Navigation.Mode.None };
            pickupButton.navigation = new Navigation { mode = Navigation.Mode.None };
            gestureButton.navigation = new Navigation { mode = Navigation.Mode.None };
            attackLabel = attackButton.GetComponentInChildren<TMP_Text>();
            pickupLabel = pickupButton.GetComponentInChildren<TMP_Text>();
            Refresh();
        }

        private bool CanToggle => !GameplayState.IsInputBlocked && Time.timeScale > 0f &&
            (health == null || health.IsAlive);

        private void ToggleAttack()
        {
            if (!CanToggle || combat == null) return;
            combat.SetAutoAttackEnabled(!combat.AutoAttackEnabled, true);
            Refresh();
        }

        private void TogglePickup()
        {
            if (!CanToggle) return;
            PlayerAutomationState.SetAutoPickupEnabled(!PlayerAutomationState.AutoPickupEnabled);
            Refresh();
        }

        private void ToggleGestures()
        {
            if (!CanToggle || gestureReceiver == null) return;
            gestureReceiver.SetControlEnabled(!gestureReceiver.ControlEnabled);
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (root == null) return;
            attackButton.interactable = CanToggle && combat != null;
            pickupButton.interactable = CanToggle;
            gestureButton.interactable = CanToggle && gestureReceiver != null;
            bool attack = combat != null && combat.AutoAttackEnabled;
            bool pickup = PlayerAutomationState.AutoPickupEnabled;
            if (displayedAttack != attack)
            {
                displayedAttack = attack;
                SetState(attackButton, attackLabel, "TỰ ĐÁNH [T]", attack);
            }
            if (displayedPickup != pickup)
            {
                displayedPickup = pickup;
                SetState(pickupButton, pickupLabel, "TỰ NHẶT", pickup);
            }
            bool gestures = gestureReceiver != null && gestureReceiver.ControlEnabled;
            if (displayedGesture != gestures)
            {
                displayedGesture = gestures;
                SetState(gestureButton, gestureButton.GetComponentInChildren<TMP_Text>(), "TAY [F8]", gestures);
            }
            string connection = gestureReceiver != null ? gestureReceiver.StatusText : "Chưa có bộ nhận điều khiển tay.";
            if (gestureStatus.text != connection) gestureStatus.text = connection;
        }

        private static void SetState(Button button, TMP_Text label, string title, bool enabled)
        {
            label.text = title + (enabled ? ": BẬT" : ": TẮT");
            label.color = enabled ? FantasyEquipmentUI.Green : FantasyEquipmentUI.Muted;
            button.transform.Find("Brass").GetComponent<Image>().color = enabled
                ? FantasyEquipmentUI.Green : FantasyEquipmentUI.Edge;
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root);
        }
    }
}
