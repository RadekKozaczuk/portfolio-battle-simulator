#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Sirenix.OdinInspector;
using UI.Popups.Views;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Config
{
    [CreateAssetMenu(fileName = "PopupConfig", menuName = "Config/UI/PopupConfig")]
    class PopupConfig : ScriptableObject
    {
        [InfoBox("Order should match Core.Enums.PopupType enum.", InfoMessageType.None)]
        [SerializeField]
        internal AbstractPopup[] PopupPrefabs;

        [SerializeField]
        internal Image BlockingPanelPrefab;
    }
}