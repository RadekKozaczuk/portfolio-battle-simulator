#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using System.Linq;
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
        internal List<ArmyModel> Armies => _armies.Select(army => army.Model).ToList();

        static readonly UIConfig _config;

        [SerializeField]
        VerticalLayoutGroup _container;

        readonly List<ArmyPanelView> _armies = new();

        void Awake()
        {
            foreach (ArmyData army in _config.Armies)
            {
                ArmyPanelView panel = Instantiate(_config.ArmyPanelPrefab, _container.transform);
                panel.Initialize(army);
                _armies.Add(panel);
            }
        }
    }
}