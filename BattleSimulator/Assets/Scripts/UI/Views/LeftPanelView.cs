#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using Core.Models;
using UI.Config;
using UI.Data;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    [DisallowMultipleComponent]
    class LeftPanelView : MonoBehaviour
    {
        internal List<ArmyModel> Armies
        {
            get
            {
                var retVal = new List<ArmyModel>();

                // ReSharper disable once LoopCanBeConvertedToQuery
                for (int i = 0; i < _armyViews.Count; i++)
                {
                    ArmyPanelView view = _armyViews[i];
                    retVal.Add(new ArmyModel(view.UnitAmounts, view.Strategies, _config.Armies[i].Color));
                }

                return retVal;
            }
        }

        static readonly UIConfig _config;

        [SerializeField]
        VerticalLayoutGroup _container;

        readonly List<ArmyPanelView> _armyViews = new();

        void Awake()
        {
            foreach (ArmyData army in _config.Armies)
            {
                ArmyPanelView view = Instantiate(_config.ArmyPanelPrefab, _container.transform);
                view.Initialize(army);
                _armyViews.Add(view);
            }
        }
    }
}