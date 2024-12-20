#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    class UnitPanelView : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI _title;

        [SerializeField]
        Slider _slider;

        [SerializeField]
        TextMeshProUGUI _amount;

        void Awake()
        {
            _slider.onValueChanged.AddListener(value => _amount.text = ((int)value).ToString());
        }

        internal void Initialize(string title)
        {
            _title.text = title;
        }
    }
}
