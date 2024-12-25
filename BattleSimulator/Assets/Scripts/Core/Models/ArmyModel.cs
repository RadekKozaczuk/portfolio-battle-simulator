using System.Linq;
using Core.Enums;
using Core.Interfaces;
using UnityEngine;

namespace Core.Models
{
    /// <summary>
    /// Representation of the army. The amount and the strategy of every unit type.
    /// </summary>
    public class ArmyModel : IArmyModel
    {
        public int UnitCount => _amounts.Sum();
        public int UnitTypeCount => _amounts.Length;
        public readonly Color Color;

        readonly int[] _amounts;
        readonly Strategy[] _strategies;

        public ArmyModel(int unitType1Count, int unitType2Count)
        {
            _amounts = new[] {unitType1Count, unitType2Count};
            _strategies = new[] {Strategy.Basic, Strategy.Basic};
            Color = Color.red;
        }

        public ArmyModel(int[] unitAmounts, Strategy[] strategies, Color color)
        {
            _amounts = unitAmounts;
            _strategies = strategies;
            Color = color;
        }

        public int GetUnitCount(int unitType) => _amounts[unitType];

        public Strategy GetStrategy(int unitType) => _strategies[unitType];
    }
}