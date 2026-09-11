using TopDownGame.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TopDownGame.Player
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.25f)] private float interactionRadius = 1.5f;
        [SerializeField] private LayerMask interactionLayers = ~0;
        [SerializeField, Range(0.15f, 0.75f)] private float doubleClickWindow = 0.45f;
        [SerializeField, Min(0f)] private float clickHitRadius = 0.25f;
        [SerializeField] private bool worldDoubleClickEnabled = true;

        private IInteractable currentTarget;
        private IInteractable lastClickedTarget;
        private IInteractable pendingDoubleClickTarget;
        private float lastClickTime = -10f;
        private Camera worldCamera;

        public bool HasTarget => currentTarget != null && currentTarget.CanInteract;
        public string CurrentPrompt => HasTarget ? currentTarget.InteractionPrompt : string.Empty;

        private void Update()
        {
            if (GameplayState.IsInputBlocked || Time.timeScale <= 0f) return;
            RefreshTarget();
            if (worldDoubleClickEnabled) HandleWorldDoubleClick();
            TryCompletePendingInteraction();

            if (HasTarget && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                currentTarget.Interact(this);
            }
        }

        private void HandleWorldDoubleClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            IInteractable clicked = FindClickedInteractable();
            if (clicked == null)
            {
                if (EventSystem.current == null ||
                    !EventSystem.current.IsPointerOverGameObject())
                {
                    lastClickedTarget = null;
                }
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            bool isDoubleClick = ReferenceEquals(clicked, lastClickedTarget) &&
                Time.unscaledTime - lastClickTime <= doubleClickWindow;
            lastClickedTarget = clicked;
            lastClickTime = Time.unscaledTime;

            if (!isDoubleClick)
            {
                return;
            }

            pendingDoubleClickTarget = clicked;
            lastClickedTarget = null;
            TryCompletePendingInteraction();
        }

        private IInteractable FindClickedInteractable()
        {
            // Use Unity's overloaded null check so a camera destroyed during a
            // scene change is replaced instead of leaving a stale reference.
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return null;
            }

            Vector2 point = worldCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D[] hits = clickHitRadius > 0f
                ? Physics2D.OverlapCircleAll(point, clickHitRadius, interactionLayers)
                : Physics2D.OverlapPointAll(point, interactionLayers);
            float nearestDistance = float.MaxValue;
            IInteractable nearest = null;
            foreach (Collider2D hit in hits)
            {
                IInteractable interactable = hit.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract)
                {
                    continue;
                }

                float distance = ((Vector2)hit.ClosestPoint(point) - point).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }

            return nearest;
        }

        private void TryCompletePendingInteraction()
        {
            if (pendingDoubleClickTarget == null)
            {
                return;
            }

            Component targetComponent = pendingDoubleClickTarget as Component;
            if (targetComponent == null || !pendingDoubleClickTarget.CanInteract)
            {
                pendingDoubleClickTarget = null;
                return;
            }

            float distance = Vector2.Distance(transform.position,
                targetComponent.transform.position);
            if (distance > interactionRadius)
            {
                return;
            }

            IInteractable target = pendingDoubleClickTarget;
            pendingDoubleClickTarget = null;
            target.Interact(this);
        }

        private void RefreshTarget()
        {
            Collider2D[] candidates = Physics2D.OverlapCircleAll(
                transform.position, interactionRadius, interactionLayers);
            float nearestDistance = float.MaxValue;
            IInteractable nearest = null;

            foreach (Collider2D candidate in candidates)
            {
                IInteractable interactable = candidate.GetComponentInParent<IInteractable>();
                if (interactable == null || !interactable.CanInteract)
                {
                    continue;
                }

                float distance = ((Vector2)candidate.transform.position -
                    (Vector2)transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = interactable;
                }
            }

            currentTarget = nearest;
        }
    }
}
