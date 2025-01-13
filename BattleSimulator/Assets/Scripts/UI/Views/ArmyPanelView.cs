#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using System.Linq;
using Core.Enums;
using Core.Models;
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
        internal ArmyModel Model
        {
            get
            {
                var unitAmounts = new List<int>();
                var strategies = new List<Strategy>();

                foreach (UnitData data in _units.Select(unit => unit.Data))
                {
                    unitAmounts.Add(data.Amount);
                    strategies.Add(data.Strategy);
                }

                return new ArmyModel(unitAmounts, strategies, Color.green);
            }
        }

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