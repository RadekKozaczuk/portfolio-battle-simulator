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
        /// Contains -infinity if the unit is dead (in such case the presentetianal model will be destroyed and no update possible.
        /// </summary>
        [ReadOnly]
        public NativeArray<float2> Positions;

        /// <summary>
        /// If the corresponding unit was attacked by one or more enemies this frame it holds the position of the last one,
        /// <see cref="float2.zero"/> otherwise.
        /// </summary>
        [ReadOnly]
        public NativeArray<float2> AttackingEnemyPosition;

        // The code actually running on the job
        public void Execute(int index, TransformAccess transform)
        {
            // unit dead
            if (Mathf.Approximately(AttackingEnemyPosition[index].x, float.MinValue))
                return;

            if (math.any(AttackingEnemyPosition[index] == float2.zero))
            {
                float2 dir = Positions[index] - AttackingEnemyPosition[index];
                transform.rotation = Quaternion.Euler(dir.x, 0, dir.y);
            }

            transform.position = new Vector3(Positions[index].x, 0, Positions[index].y);
        }
    }
}