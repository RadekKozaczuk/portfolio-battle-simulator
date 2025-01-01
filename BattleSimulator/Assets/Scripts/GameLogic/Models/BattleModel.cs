#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using Core.Models;
using GameLogic.Interfaces;
using UnityEngine.Assertions;

namespace GameLogic.Models
{
    class BattleModel : IBattleModel
    {
        int IBattleModel.ArmyCount => _armyCount;

        readonly int[] _armyStarts;
        readonly int[] _armyLengths;
        readonly int[] _armyLengthSums; // running sums
        readonly int[] _unitTypeStarts;
        readonly int[] _unitTypeLengths;
        readonly int[] _unitTypeLengthsSums; // running sums

        /// <summary>
        /// Contains all the units from all armies.
        /// </summary>
        readonly UnitModel[] _units;

        readonly int _armyCount;
        readonly int _unitTypeCount;

        /// <summary>
        /// By army.
        /// </summary>
        readonly int[] _aliveUnitCount;

        internal BattleModel(List<ArmyModel> armies)
        {
            _armyCount = armies.Count;
            _aliveUnitCount = new int[_armyCount];

            int totalUnitCount = 0;
            for (int i = 0; i < armies.Count; i++)
            {
                ArmyModel army = armies[i];
                _aliveUnitCount[i] = army.UnitCount;
                totalUnitCount += army.UnitCount;
            }

            _units = new UnitModel[totalUnitCount];
            _armyStarts = new int[_armyCount];
            _armyLengths = new int[_armyCount];
            _armyLengthSums = new int[_armyCount];

            int sum;
            int totalSum = 0;
            for (int armyId = 0; armyId < _armyCount; armyId++)
            {
                _armyStarts[armyId] = totalSum;
                sum = armies[armyId].UnitCount;
                _armyLengths[armyId] = sum;
                totalSum += sum;
                _armyLengthSums[armyId] = totalSum;
            }

            _unitTypeCount = armies[0].UnitTypeCount; // this is equal for all armies
            _unitTypeStarts = new int[_armyCount * _unitTypeCount];
            _unitTypeLengths = new int[_armyCount * _unitTypeCount];
            _unitTypeLengthsSums = new int[_armyCount * _unitTypeCount];

            totalSum = 0;
            for (int armyId = 0; armyId < _armyCount; armyId++)
                for (int unitTypeId = 0; unitTypeId < _unitTypeCount; unitTypeId++)
                {
                    int index = armyId * _unitTypeCount + unitTypeId;
                    _unitTypeStarts[index] = totalSum;
                    sum = armies[armyId].GetUnitCount(unitTypeId);
                    _unitTypeLengths[index] = sum;
                    totalSum += sum;
                    _unitTypeLengthsSums[index] = totalSum;
                }

            int id = 0;
            for (int armyId = 0; armyId < _armyCount; armyId++)
                for (int unitTypeId = 0; unitTypeId < _unitTypeCount; unitTypeId++)
                    for (int unitId = 0; unitId < armies[armyId].GetUnitCount(unitTypeId); unitId++)
                    {
                        _units[id] = new UnitModel(id, unitTypeId, armyId);
                        id++;
                    }
        }

        bool IBattleModel.OneOrZeroArmiesLeft(out int numLeft)
        {
            numLeft = 0;
            for (int i = 0; i < _armyCount; i++)
                if (_aliveUnitCount[i] > 0)
                    numLeft++;

            return numLeft is 1 or 0;
        }

        void IBattleModel.UnitDied(int armyId) => _aliveUnitCount[armyId]--;

        ref UnitModel IBattleModel.GetUnit(int unitId) => ref _units[unitId];

        /// <summary>
        /// Returns all units.
        /// </summary>
        /// <returns>All units.</returns>
        Span<UnitModel> IBattleModel.GetUnits() => new(_units, 0, _units.Length);

        /// <summary>
        /// Returns all units from the given army.
        /// </summary>
        /// <param name="armyId">Only units from this army will be returned.</param>
        /// <returns>All units from the given army.</returns>
        Span<UnitModel> IBattleModel.GetUnits(int armyId)
        {
            Assert.IsTrue(armyId >=0 && armyId < _armyCount,
                          "ArmyId must be a valid number ranging from 0 to the total number of armies (exclusive).");

            return new Span<UnitModel>(_units, _armyStarts[armyId], _armyLengths[armyId]);
        }

        /// <summary>
        /// Returns all units of the given type from the given army.
        /// </summary>
        /// <param name="armyId">Only units from this army will be returned.</param>
        /// <param name="unitType">Only units of this type will be returned.</param>
        /// <returns>All units from the given army of the given type.</returns>
        Span<UnitModel> IBattleModel.GetUnits(int armyId, int unitType)
        {
            Assert.IsTrue(armyId >= 0 && armyId < _armyCount,
                          "ArmyId must be a valid number ranging from 0 to the total number of armies (exclusive).");

            int index = armyId * _unitTypeCount + unitType;
            return new Span<UnitModel>(_units, _unitTypeStarts[index], _unitTypeLengths[index]);
        }

        /// <summary>
        /// Returns all units from the given army except for the unit with the given id.
        /// </summary>
        /// <param name="armyId">Only units from this army will be returned.</param>
        /// <param name="exceptUnitId">Unit will this ID will be excluded.</param>
        /// <returns>All units from the given army except for the unit with the given ID.</returns>
        Memory<UnitModel>[] IBattleModel.GetUnitsExcept(int armyId, int exceptUnitId)
        {
            Assert.IsTrue(armyId >= 0 && armyId < _armyCount,
                          "ArmyId must be a valid number ranging from 0 to the total number of armies (exclusive).");

            Assert.IsTrue(exceptUnitId >= 0 && exceptUnitId < _units.Length,
                          "UnitId must be a valid number ranging from 0 to the total number of units (exclusive).");

            int start1 = _armyStarts[armyId];

            if (start1 == exceptUnitId) // the excluded unit is on the first spot
                return new Memory<UnitModel>[] {new(_units, _armyStarts[armyId] + 1, _armyLengths[armyId] - 1)};

            if (start1 + _armyLengths[armyId] - 1 == exceptUnitId) // the excluded unit is on the last spot
                return new Memory<UnitModel>[] {new(_units, _armyStarts[armyId], _armyLengths[armyId] - 1)};

            // the excluded unit is outside the requested block
            if (exceptUnitId < start1 || exceptUnitId > start1 + _armyLengths[armyId] - 1)
                return new Memory<UnitModel>[] {new(_units, start1, _armyLengths[armyId])};

            // is somewhere in the middle therefore there will be two memories
            int length1 = exceptUnitId - _armyStarts[armyId];
            int start2 = exceptUnitId + 1;
            int length2 = _armyLengthSums[armyId] - start2;

            return new Memory<UnitModel>[] {new(_units, start1, length1), new(_units, start2, length2)};
        }

        /// <summary>
        /// Returns all units of the given type from the given army except for the unit with the given id.
        /// </summary>
        /// <param name="armyId">Only units from this army will be returned.</param>
        /// <param name="unitType">Only units of this type will be returned.</param>
        /// <param name="exceptUnitId">Unit will this ID will be excluded.</param>
        /// <returns>All units from the given army of the given type except for the unit with the given ID.</returns>
        Memory<UnitModel>[] IBattleModel.GetUnitsExcept(int armyId, int unitType, int exceptUnitId)
        {
            Assert.IsTrue(armyId >= 0 && armyId < _armyCount,
                          "ArmyId must be a number ranging from 0 to the total number of armies (exclusive).");

            Assert.IsTrue(unitType >= 0 && unitType < _unitTypeCount,
                          "UnitType must be a number ranging from 0 to the total number of unit types (exclusive).");

            Assert.IsTrue(exceptUnitId >= 0 && exceptUnitId < _units.Length,
                          "ExceptionUnitId must be a number ranging from 0 to the total number of units (exclusive).");

            int index = armyId * _unitTypeCount + unitType;

            // there is zero units of the requested type
            if (_unitTypeLengths[index] == 0)
                return new Memory<UnitModel>[]
                {
                    new(_units, _unitTypeStarts[index], 0)
                };

            // the excluded unit is at the beginning of the memory block
            int start1 = _unitTypeStarts[index];
            if (start1 == exceptUnitId)
                return new Memory<UnitModel>[]
                {
                    new(_units, _unitTypeStarts[index] + 1, _unitTypeLengths[index] - 1)
                };

            // the excluded unit is on the last spot
            int end = _unitTypeStarts[index] + _unitTypeLengths[index] - 1;
            if (end == exceptUnitId)
            {
                int arrIndex = _unitTypeCount - 1;
                int start = _unitTypeStarts[arrIndex];
                int length = _unitTypeLengths[arrIndex];

                if (_unitTypeStarts[arrIndex] + _unitTypeLengths[arrIndex] == exceptUnitId)
                    length--;

                return new Memory<UnitModel>[] {new(_units, start, length)};
            }

            // the excluded unit is outside the requested block
            if (exceptUnitId < start1 || exceptUnitId > end)
                return new Memory<UnitModel>[] {new(_units, start1, _unitTypeLengths[index])};

            int length1 = exceptUnitId - _unitTypeStarts[index];
            int start2 = exceptUnitId + 1;
            int length2 = _unitTypeLengthsSums[index] - start2;

            return new Memory<UnitModel>[]
            {
                new(_units, start1, length1),
                new(_units, start2, length2)
            };
        }

        /// <summary>
        /// Returns all units that are NOT from the given army (enemies).
        /// </summary>
        /// <param name="armyId">Units from army with the given id will be excluded.</param>
        /// <returns>All units from all other armies except the given one.</returns>
        Memory<UnitModel>[] IBattleModel.GetEnemies(int armyId)
        {
            Assert.IsTrue(armyId >= 0 && armyId < _armyCount,
                          "ArmyId must be a valid number ranging from 0 to the total number of armies (exclusive).");

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