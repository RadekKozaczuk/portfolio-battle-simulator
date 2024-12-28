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
using UnitModel = UI.Model.UnitModel;

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

                foreach (UnitModel model in _units.Select(unit => unit.Model))
                {
                    unitAmounts.Add(model.Amount);
                    strategies.Add(model.Strategy);
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
            foreach (UnitModel unit in army.Units)
            {
                UnitPanelView panel = Instantiate(_config.UnitPanelPrefab, _container.transform);
                panel.Initialize(unit);
                _units.Add(panel);
            }
        }
    }
}