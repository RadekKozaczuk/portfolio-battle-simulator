using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace GameLogic.Interfaces
{
    interface ISpacePartitioningController
    {
        /// <summary>
        /// Returns all elements within the given distance.
        /// The returned list is pooled. To reduce the amount of allocation please return the element by calling <see cref="ReturnList"/>
        /// </summary>
        /*List<int> FindAllNearest(Vector3 position, float maxDistance);

        void AddElement(int id, Vector3 position);

        void RemoveElement(int id);

        void UpdateElement(int id, Vector3 position);

        void ReturnList(List<int> list);*/

        int FindNearestEnemy(float2 position, int excludeArmyId);

        /// <summary>
        /// The returned list is pooled and should be returned in order to preserve memory.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="armyId"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        List<int> FindAllAllies(float2 position, int armyId, float maxDistance);
    }
}