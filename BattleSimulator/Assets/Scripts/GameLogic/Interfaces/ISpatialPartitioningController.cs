using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Interfaces
{
    interface ISpatialPartitioningController
    {
        /// <summary>
        /// Returns all elements within the given distance.
        /// The returned list is pooled. To reduce the amount of allocation please return the element by calling <see cref="ReturnList"/>
        /// </summary>
        List<int> FindAllNearest(Vector3 position, float maxDistance);

        void AddElement(int id, Vector3 position);

        void RemoveElement(int id);

        void UpdateElement(int id, Vector3 position);

        void ReturnList(List<int> list);
    }
}