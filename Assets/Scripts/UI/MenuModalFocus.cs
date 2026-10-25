using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TopDownGame.UI
{
    /// <summary>Suspends underlying canvas controls while a menu owns keyboard focus.</summary>
    public sealed class MenuModalFocus
    {
        private readonly List<(CanvasGroup group, bool interactable, bool raycasts)> suspended =
            new List<(CanvasGroup, bool, bool)>();
        private GameObject previousSelection;
        private bool captured;

        public void Capture(Transform overlay)
        {
            Release();
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            captured = true;
            // Travel uses its own root canvas; automatic navigation can still
            // reach another canvas unless its controls are suspended as well.
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>())
            {
                if (!canvas.isRootCanvas || canvas.transform == overlay || canvas.transform.IsChildOf(overlay)) continue;
                if (overlay.IsChildOf(canvas.transform))
                {
                    foreach (Transform child in canvas.transform)
                        if (child != overlay && !overlay.IsChildOf(child)) Suspend(child);
                }
                else Suspend(canvas.transform);
            }
        }

        private void Suspend(Transform target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
            suspended.Add((group, group.interactable, group.blocksRaycasts));
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        public void Release()
        {
            if (!captured) return;
            captured = false;
            foreach (var state in suspended)
            {
                if (state.group == null) continue;
                state.group.interactable = state.interactable;
                state.group.blocksRaycasts = state.raycasts;
            }
            suspended.Clear();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
        }
    }
}
