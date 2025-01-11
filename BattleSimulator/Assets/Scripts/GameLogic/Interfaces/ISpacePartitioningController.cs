using System.Collections.Generic;
using Unity.Mathematics;

namespace GameLogic.Interfaces
{
    interface ISpacePartitioningController
    {
        internal void AddUnit(int unitId, int armyId, float2 position);

        internal void KillUnit(int unitId);

        internal void UpdateUnits();

        internal int FindNearestEnemy(float2 position, int excludeArmyId);

        /// <summary>
        /// The returned list is pooled and should be returned in order to preserve memory.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="exceptUnitId"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        internal List<int> FindAllNearbyUnits(float2 position, int exceptUnitId, float maxDistance);

        internal void Release(List<int> list);
    }
}