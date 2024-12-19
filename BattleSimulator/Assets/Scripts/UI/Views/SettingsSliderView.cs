#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace UI.Views
{
    class SettingsSliderView : MonoBehaviour
    {
        [SerializeField]
        RectTransform _sliderRect;

        [SerializeField]
        RectTransform _handleRect;

        // This setup have to be in Start (not Awake) method
        // because otherwise it is overwritten by the slider script
        void Start()
        {
            float sliderHeight = RectTransformUtility.PixelAdjustRect(_sliderRect, UISceneReferenceHolder.Canvas).height;
            _handleRect.sizeDelta = new Vector2(sliderHeight, 0);
        }
    }
}
