using System.Collections.Generic;
using Unity.Mathematics;

namespace GameLogic.Interfaces
{
    interface ISpacePartitioningController
    {
        internal void AddUnit(int unitId, int armyId, float2 position);

        internal void UpdateUnit(int unitId, float2 position);

        internal void KillUnit(int unitId);

        internal int FindNearestEnemy(float2 position, int excludeArmyId);

        /// <summary>
        /// The returned list is pooled and should be returned in order to preserve memory.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="armyId"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        internal List<int> FindAllAllies(float2 position, int armyId, float maxDistance);
    }
}