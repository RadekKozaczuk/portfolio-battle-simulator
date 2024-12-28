#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core.Enums;
using TMPro;
using UI.Model;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    class UnitPanelView : MonoBehaviour
    {
        internal UnitModel Model => new()
        {
            Amount = int.Parse(_amount.text),
            Strategy = Strategy.Basic,
            Name = _title.text.ToLower()
        };

        [SerializeField]
        TextMeshProUGUI _title;

        [SerializeField]
        Slider _slider;

        [SerializeField]
        TextMeshProUGUI _amount;

        void Awake()
        {
            _slider.onValueChanged.AddListener(SliderAction);
        }

        internal void Initialize(UnitModel unit)
        {
            _title.text = unit.Name;
            _amount.text = unit.Amount.ToString();
        }

        void SliderAction(float value)
        {
            _amount.text = ((int)value).ToString();
        }
    }
}
