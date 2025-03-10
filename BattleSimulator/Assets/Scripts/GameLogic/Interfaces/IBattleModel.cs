#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core.Enums;
using Core.Models;
using UnityEngine;

namespace GameLogic.Interfaces
{
    internal interface IBattleModel
    {
        internal int ArmyCount { get; }
        internal Bounds[] SpawnZones { get; }

        internal bool OneOrZeroArmiesLeft(out int numLeft);

        /// <summary>
        /// Keeps track of the remaining unit count.
        /// </summary>
        internal void UnitDied(int armyId);

        internal Strategy GetStrategy(int armyId, int unitType);

        internal ref UnitModel GetUnit(int unitId);

        internal Span<UnitModel> GetUnits();

        internal Span<UnitModel> GetUnits(int armyId);

        internal Span<UnitModel> GetUnits(int armyId, UnitType unitType);

        internal Memory<UnitModel>[] GetUnitsExcept(int armyId, int exceptUnitId);

        internal Memory<UnitModel>[] GetUnitsExcept(int armyId, int unitType, int exceptUnitId);

        internal Memory<UnitModel>[] GetEnemies(int armyId);
    }
}