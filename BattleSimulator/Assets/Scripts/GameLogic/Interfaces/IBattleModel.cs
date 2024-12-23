using System;
using Core.Models;

namespace GameLogic.Interfaces
{
    public interface IBattleModel
    {
        internal Span<UnitModel> GetUnits();

        internal Span<UnitModel> GetUnits(int armyId);

        internal Span<UnitModel> GetUnits(int armyId, int unitType);

        internal Memory<UnitModel>[] GetUnitsExcept(int armyId, int exceptUnitId);

        internal Memory<UnitModel>[] GetUnitsExcept(int armyId, int unitType, int exceptUnitId);

        internal Memory<UnitModel>[] GetEnemies(int armyId);
    }
}