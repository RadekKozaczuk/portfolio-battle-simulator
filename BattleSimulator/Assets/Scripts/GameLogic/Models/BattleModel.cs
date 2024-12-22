using System;
using System.Collections.Generic;
using System.Linq;
using Core.Models;

namespace GameLogic.Models
{
    class BattleModel
    {
        /// <summary>
        /// 
        /// </summary>
        readonly int[] _armyStarts;
        readonly int[] _armyLengths;

        // length = army count * unit type
        readonly int[] _unityTypeStarts;
        readonly int[] _unityTypeLengths;

        /// <summary>
        /// Contains all the units from all armies.
        /// </summary>
        readonly UnitModel[] _units;

        readonly int _armyCount;

        /// <summary>
        /// Amount of unit types.
        /// </summary>
        /// <returns></returns>
        readonly int _unitTypeCount;

        internal BattleModel(List<ArmyModel> armies)
        {
            int totalUnitCount = armies.Sum(army => army.UnitCount);
            _armyCount = armies.Count;

            _units = new UnitModel[totalUnitCount];

            _armyStarts = new int[_armyCount];
            _armyLengths = new int[_armyCount];

            int sum = 0;
            for (int armyId = 0; armyId < _armyCount; armyId++)
            {
                _armyStarts[armyId] = sum;
                sum = armies[armyId].UnitCount;
                _armyLengths[armyId] = sum;
            }

            _unitTypeCount = armies[0].UnitTypeCount; // this is equal for all armies
            _unityTypeStarts = new int[_armyCount * _unitTypeCount];
            _unityTypeLengths = new int[_armyCount * _unitTypeCount];

            sum = 0;

            for (int armyId = 0; armyId < _armyCount; armyId++)
                for (int unitTypeId = 0; unitTypeId < _unitTypeCount; unitTypeId++)
                {
                    int index = armyId * _unitTypeCount + unitTypeId;
                    _unityTypeStarts[index] = sum;
                    sum = armies[armyId].GetUnitCount(unitTypeId);
                    _unityTypeLengths[index] = sum;
                }
        }

        /// <summary>
        /// Returns all units from the given army.
        /// </summary>
        /// <param name="armyId">Only units from this army will be returned.</param>
        /// <returns>All units from the given army.</returns>
        internal Span<UnitModel> GetUnits(int armyId) =>
            new(_units, _armyStarts[armyId], _armyLengths[armyId]);

        /// <summary>
        /// Returns all units of the given type from the given army.
        /// </summary>
        /// <param name="armyId">Only units from this army will be returned.</param>
        /// <param name="unitType">Only units of this type will be returned.</param>
        /// <returns>All units from the given army of the given type.</returns>
        internal Span<UnitModel> GetUnits(int armyId, int unitType)
        {
            int index = armyId * _unitTypeCount + unitType;
            return new Span<UnitModel>(_units, _unityTypeStarts[index], _unityTypeLengths[index]);
        }

        /// <summary>
        /// Returns all units from the given army except for the unit with the given id.
        /// </summary>
        /// <param name="armyId">Only units from this army will be returned.</param>
        /// <param name="exceptUnitId">Unit will this ID will be excluded.</param>
        /// <returns>All units from the given army except for the unit with the given ID.</returns>
        internal Memory<UnitModel>[] GetUnitsExcept(int armyId, int exceptUnitId)
        {
            if (_armyStarts[armyId] == exceptUnitId) // the except one is on the first spot
                return new Memory<UnitModel>[]
                {
                    new(_units, _armyStarts[armyId] + 1, _armyLengths[armyId] - 1)
                };

            if (_armyStarts[armyId] + _armyLengths[armyId] == exceptUnitId) // the except one is on the first spot
                return new Memory<UnitModel>[]
                {
                    new(_units, _armyStarts[armyId], _armyLengths[armyId] - 1)
                };

            // is somewhere in the middle therefore there will be two memories
            return new Memory<UnitModel>[]
            {
                new(_units, _armyStarts[armyId], _armyStarts[armyId] - exceptUnitId),
                new(_units, exceptUnitId + 1, _armyLengths[armyId] - exceptUnitId + 1)
            };
        }

        /// <summary>
        /// Returns all units of the given type from the given army except for the unit with the given id.
        /// </summary>
        /// <param name="armyId">Only units from this army will be returned.</param>
        /// <param name="unitType">Only units of this type will be returned.</param>
        /// <param name="exceptUnitId">Unit will this ID will be excluded.</param>
        /// <returns>All units from the given army of the given type except for the unit with the given ID.</returns>
        internal Memory<UnitModel>[] GetUnitsExcept(int armyId, int unitType, int exceptUnitId) // todo: fix
        {
            if (_armyStarts[armyId] == exceptUnitId) // the except one is on the first spot
                return new Memory<UnitModel>[]
                {
                    new(_units, _armyStarts[armyId] + 1, _armyLengths[armyId] - 1)
                };

            if (_armyStarts[armyId] + _armyLengths[armyId] == exceptUnitId) // the except one is on the first spot
                return new Memory<UnitModel>[]
                {
                    new(_units, _armyStarts[armyId], _armyLengths[armyId] - 1)
                };

            // is somewhere in the middle therefore there will be two memories
            return new Memory<UnitModel>[]
            {
                new(_units, _armyStarts[armyId], _armyStarts[armyId] - exceptUnitId),
                new(_units, exceptUnitId + 1, _armyLengths[armyId] - exceptUnitId + 1)
            };
        }

        internal Memory<UnitModel>[] GetEnemies(int armyId)
        {
            if (armyId == 0) // first one
                return new Memory<UnitModel>[]
                {
                    new(_units, _armyStarts[1], _units.Length - _armyLengths[0])
                };

            if (armyId == _armyCount - 1) // last one
                return new Memory<UnitModel>[]
                {
                    new(_units, _armyStarts[0], _units.Length - _armyLengths[_armyCount - 1])
                };

            // in the middle
            return new Memory<UnitModel>[]
            {
                new(_units, 0, _armyStarts[armyId]),
                new(_units, _armyStarts[armyId] + _armyLengths[armyId], _units.Length - _armyLengths[armyId])
            };
        }
    }
}