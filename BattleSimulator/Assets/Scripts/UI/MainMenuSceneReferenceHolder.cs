#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UI.Views;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Keep in mind references stored here are only accessible AFTER UI scene is fully loaded up.
    /// Use, for example, <see cref="Controllers.UIMainController._uiSceneLoaded" /> to control the execution.
    /// </summary>
    class MainMenuSceneReferenceHolder : MonoBehaviour
    {
        internal static LeftPanelView LeftPanel;

        [SerializeField]
        LeftPanelView _leftPanel;

        void Awake()
        {
            LeftPanel = _leftPanel;
        }
    }
}