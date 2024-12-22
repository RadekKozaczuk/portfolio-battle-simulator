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

        public void Execute(int index, TransformAccess transform)
        {
            // unit is dead todo: something is wrong with this value
            //if (Mathf.Approximately(AttackingEnemyPos[index].x, float.MinValue))
            //    return;

            float2 pos = Positions[index];
            transform.position = new Vector3(pos.x, 0, pos.y);
        }
    }
}