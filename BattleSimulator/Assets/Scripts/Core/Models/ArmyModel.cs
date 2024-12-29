#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using System.Linq;
using Core.Enums;
using Core.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;

namespace Core.Models
{
    /// <summary>
    /// Representation of the army. The amount and the strategy of every unit type.
    /// </summary>
    public class ArmyModel : IArmyModel
    {
        public int UnitCount => _amounts.Sum();
        public int UnitTypeCount => _amounts.Length;

        public Color Color;

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
            Assert.IsTrue(unitAmounts.Length == strategies.Length, "Number of unit types must be equal to the number of strategies.");

            _amounts = unitAmounts;
            _strategies = strategies;
            Color = color;
        }

        public ArmyModel(List<int> unitAmounts, List<Strategy> strategies, Color color)
        {
            Assert.IsTrue(unitAmounts.Count == strategies.Count, "Number of unit types must be equal to the number of strategies.");

            _amounts = unitAmounts.ToArray();
            _strategies = strategies.ToArray();
            Color = color;
        }

        public int GetUnitCount(int unitType) => _amounts[unitType];

        public Strategy GetStrategy(int unitType) => _strategies[unitType];
    }
}