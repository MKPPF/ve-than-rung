using TMPro;
using TopDownGame.Core;
using TopDownGame.Skills;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TopDownGame.Player
{
    /// <summary>Accepts one dodge intent before combat input; locomotion retains ownership of collision-resolved physics.</summary>
    [DefaultExecutionOrder(-60)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerHealth), typeof(PlayerInput))]
    public sealed class PlayerDodge : MonoBehaviour
    {
        public const float DefaultDuration = 0.28f;
        public const float DefaultInvulnerability = 0.18f;
        public const float DefaultCooldown = 1f;
        public const float DefaultSpeed = 7.5f;

        [Header("References")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerCharacterAnimator characterAnimator;
        [SerializeField] private PlayerInput playerInput;

        [Header("Dodge")]
        [SerializeField] private string actionName = "Dodge";
        [SerializeField, Min(0.05f)] private float duration = DefaultDuration;
        [SerializeField, Min(0f)] private float invulnerability = DefaultInvulnerability;
        [SerializeField, Min(0.05f)] private float cooldown = DefaultCooldown;
        [SerializeField, Min(0.1f)] private float speed = DefaultSpeed;

        private InputAction dodgeAction;
        private float endsAt, readyAt;
        private bool active;
        public bool IsDodging => active && Time.time < endsAt;
        public Vector2 Direction { get; private set; }
        public float Speed => speed;
        public float Duration => duration;
        public float InvulnerabilityDuration => Mathf.Min(invulnerability, duration);
        public float CooldownDuration => Mathf.Max(cooldown, duration);
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
        public bool HasRequiredReferences => movement != null && health != null && characterAnimator != null && playerInput != null;

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (characterAnimator == null) characterAnimator = GetComponent<PlayerCharacterAnimator>();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            CacheAction();
        }

        private void OnEnable() => CacheAction();

        private void CacheAction()
        {
            if (playerInput != null && playerInput.actions != null)
                dodgeAction = playerInput.actions.FindAction(actionName, false);
            dodgeAction?.Enable();
        }

        private void Update()
        {
            // Inventory suspends its movement component while the shared pause clock is frozen.
            if (active && (!health.IsAlive || !movement.isActiveAndEnabled && !GameplayState.IsPaused || Time.time >= endsAt)) Cancel();
            if (dodgeAction == null || dodgeAction.actionMap.asset != playerInput.actions) CacheAction();
            bool pressed = dodgeAction != null && dodgeAction.WasPressedThisFrame();
            pressed |= Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (!pressed) return;
            // The same gamepad button submits UI. Let a focused HUD control own that press.
            GameObject selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame &&
                selection != null && selection.GetComponent<Selectable>() is Selectable control && control.IsInteractable()) return;
            TryDodge(movement.ReadInputDirection());
        }

        public bool TryDodge(Vector2 intent)
        {
            if (!isActiveAndEnabled || movement == null || !movement.isActiveAndEnabled || health == null || !health.IsAlive ||
                active || CooldownRemaining > 0f || movement.IsMovementLocked || Time.timeScale <= 0f ||
                GameplayState.IsInputBlocked || GameplayState.ModalClosedThisFrame ||
                GetComponent<PlayerCombat>()?.IsAttacking == true || GetComponent<SkillCastController>()?.IsCasting == true) return false;
            GameObject selection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selection != null && (selection.GetComponent<TMP_InputField>() != null || selection.GetComponent<InputField>() != null)) return false;
            if (intent.sqrMagnitude <= 0.001f) intent = movement.MoveDirection;
            if (intent.sqrMagnitude <= 0.001f) intent = characterAnimator != null ? characterAnimator.FacingDirection : Vector2.down;
            Direction = intent.normalized;
            active = true;
            endsAt = Time.time + duration;
            readyAt = Time.time + CooldownDuration;
            movement.LockMovement(duration);
            health.BeginDodgeInvulnerability(InvulnerabilityDuration);
            characterAnimator?.PlayDodge(Direction, duration);
            return true;
        }

        public void Cancel()
        {
            if (!active) return;
            active = false;
            endsAt = Time.time;
            health?.EndDodgeInvulnerability();
            movement?.ReleaseMovementLock();
            characterAnimator?.CancelDodgePose();
        }

        private void OnDisable() => Cancel();
    }
}
