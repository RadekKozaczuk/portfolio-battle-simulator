#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Presentation.Jobs
{
    /// <summary>
    /// Updates position and forward vector of every unit.
    /// </summary>
    public struct UpdateUnitTransformJob : IJobParallelForTransform
    {
        // Jobs declare all data that will be accessed in the job
        // By declaring it as read only, multiple jobs are allowed to access the data in parallel
        /// <summary>
        /// Contains -infinity if the unit is dead (in such case the presentational model will be destroyed and no update possible.
        /// </summary>
        [ReadOnly]
        public NativeArray<float2> Positions;

        public NativeArray<float> DifferenceArray;

        [ReadOnly]
        public float Speed;

        public void Execute(int index, TransformAccess transform)
        {
            Vector3 lastPos = transform.position;
            float2 pos = Positions[index];
            var currentPos = new Vector3(pos.x, 0, pos.y);
            DifferenceArray[index] = 1;
            //DifferenceArray[index] = (currentPos - lastPos).magnitude;// / Speed;

            //if (index == 0)
            //    Debug.Log($"index: {index} DifferenceArray[index]: {DifferenceArray[index]}");

            transform.position = new Vector3(pos.x, 0, pos.y);
        }
    }
}