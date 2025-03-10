#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using System.Linq;
using Core.Enums;
using TMPro;
using UI.Config;
using UI.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    [DisallowMultipleComponent]
    class ArmyPanelView : MonoBehaviour
    {
        internal List<int> UnitAmounts => _units.Select(unit => unit.Data).Select(data => data.Amount).ToList();

        internal List<Strategy> Strategies => _units.Select(unit => unit.Data).Select(data => data.Strategy).ToList();

        static readonly UIConfig _config;

        [SerializeField]
        TextMeshProUGUI _title;

        [SerializeField]
        VerticalLayoutGroup _container;

        readonly List<UnitPanelView> _units = new();

        internal void Initialize(ArmyData army)
        {
            _title.text = army.Name;

            foreach (UnitData data in army.Units)
            {
                UnitPanelView panel = Instantiate(_config.UnitPanelPrefab, _container.transform);
                panel.Initialize(data);
                _units.Add(panel);
            }
        }
    }
}