using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>Keeps keyboard-selected cards visible inside a clipped vertical catalog.</summary>
    public sealed class ScrollSelectionIntoView : MonoBehaviour, ISelectHandler
    {
        public void OnSelect(BaseEventData eventData)
        {
            Reveal();
        }

        public void Reveal()
        {
            ScrollRect scroll = GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null || scroll.viewport == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            Bounds item = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
            Rect viewport = scroll.viewport.rect;
            float shift = item.min.y < viewport.yMin ? viewport.yMin - item.min.y :
                item.max.y > viewport.yMax ? viewport.yMax - item.max.y : 0f;
            Vector2 position = scroll.content.anchoredPosition;
            position.y = Mathf.Clamp(position.y + shift, 0f,
                Mathf.Max(0f, scroll.content.rect.height - viewport.height));
            scroll.StopMovement();
            scroll.content.anchoredPosition = position;
        }
    }
}
