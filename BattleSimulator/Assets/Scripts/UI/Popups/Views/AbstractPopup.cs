#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core.Enums;
using UnityEngine;

namespace UI.Popups.Views
{
    [DisallowMultipleComponent]
    abstract class AbstractPopup : MonoBehaviour
    {
        static ScreenOrientation CurrentScreenOrientation => Screen.height < Screen.width
            ? ScreenOrientation.LandscapeLeft
            : ScreenOrientation.Portrait;

        internal readonly PopupType Type;

        protected AbstractPopup(PopupType type) => Type = type;

        internal virtual void Initialize()
        {
            var rect = GetComponent<RectTransform>();

            // if game is on portrait mode change popup anchors:
            // leave free 1% of screen on the right and left sides
            if (CurrentScreenOrientation == ScreenOrientation.Portrait)
            {
                rect.anchorMin = new Vector2(0.01f, rect.anchorMin.y);
                rect.anchorMax = new Vector2(0.99f, rect.anchorMax.y);
            }

            SetPopupHeightSize(rect);
        }

        internal virtual void Close() { }

        static void SetPopupHeightSize(RectTransform rect)
        {
            Rect r = rect.rect;
            Vector2 currentSize = r.size;
            float scaleMultiplierY = currentSize.y / currentSize.x;
            float newHeightSize = r.width * scaleMultiplierY;

            Vector2 deltaSize = new Vector2(currentSize.x, newHeightSize) - currentSize;

            Vector2 pivot = rect.pivot;
            rect.offsetMin -= new Vector2(deltaSize.x * pivot.x, deltaSize.y * pivot.y);
            pivot = rect.pivot;
            rect.offsetMax += new Vector2(deltaSize.x * (1f - pivot.x), deltaSize.y * (1f - pivot.y));
        }
    }
}