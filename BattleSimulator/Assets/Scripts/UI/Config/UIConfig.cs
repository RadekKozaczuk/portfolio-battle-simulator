#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UI.Views;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.Config
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "Config/UI/UIConfig")]
    class UIConfig : ScriptableObject
    {
        [SerializeField]
        internal InputActionAsset InputActionAsset;

        [SerializeField]
        internal UnitPanelView UnitPanelPrefab;
    }
}