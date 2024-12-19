using System;
using UnityEngine;

namespace Core.Models
{
    /// <summary>
    /// Contains all data necessary to perform a simulation.
    /// </summary>
    // todo: consider struct
    public ref struct BattleModel
    {
        public Vector3 CenterOfArmies;

        readonly unsafe UnitModel*[][] _memorySpans;
        readonly UnitModel[] _models;

        public BattleModel(int armyCount)
        {
            CenterOfArmies = default;

            // for now assume 3 armies, 2 unit types, 50 units each type
            const int ArmyCountTemp = 3;
            const int UnitTypeCountTemp = 2;
            const int UnitCountTemp = 50; // todo: for now constant - may be dynamic in the final version

            // todo: for now constant may be dynamic in the final version
            const int TotalNumberOfUnitsPerArmy = UnitTypeCountTemp * UnitCountTemp;

            _models = new UnitModel[ArmyCountTemp * UnitTypeCountTemp * UnitCountTemp];

            unsafe
            {
                _memorySpans = new UnitModel*[ArmyCountTemp][];

                for (int armyId = 0; armyId < ArmyCountTemp; armyId++)
                    for (int unitType = 0; unitType < UnitTypeCountTemp; unitType++)
                    {

                        Span<UnitModel> span = _models.AsSpan(armyId * TotalNumberOfUnitsPerArmy + unitType * UnitCountTemp, UnitCountTemp);
                        fixed (UnitModel* ptr = span)
                        {
                            _memorySpans[armyId][unitType] = ptr;
                        }
                    }
            }
        }

        public void CalculateCenterOfArmies()
        {
            CenterOfArmies = Vector3.zero;
            int armyCount = 0;//ArmyModels.Length;

            /*for (int i = 0; i < armyCount; i++)
            {
                ref ArmyModel army = ref ArmyModels[i];
                CenterOfArmies += army.Center;
            }*/

            CenterOfArmies /= armyCount;
        }
    }
}