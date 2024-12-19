#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Keep in mind references stored here are only accessible AFTER UI scene is fully loaded up.
    /// Use, for example, <see cref="Controllers.UIMainController._uiSceneLoaded" /> to control the execution.
    /// </summary>
    class UISceneReferenceHolder : MonoBehaviour
    {
        internal static Transform PopupContainer;
        internal static Canvas Canvas;

        [SerializeField]
        Transform _popupContainer;

        [SerializeField]
        Canvas _canvas;

        void Awake()
        {
            PopupContainer = _popupContainer;
            Canvas = _canvas;
        }
    }
}