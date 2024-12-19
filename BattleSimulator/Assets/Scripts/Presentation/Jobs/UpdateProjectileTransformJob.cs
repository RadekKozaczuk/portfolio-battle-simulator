using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace Presentation.Jobs
{
    /// <summary>
    /// Projectile direction does not change mid-flight.
    /// </summary>
    public struct UpdateProjectileTransformJob : IJobParallelForTransform
    {
        // Jobs declare all data that will be accessed in the job
        // By declaring it as read only, multiple jobs are allowed to access the data in parallel
        [ReadOnly]
        public NativeArray<Vector3> Positions;

        // The code actually running on the job
        public void Execute(int index, TransformAccess transform) => transform.position = Positions[index];
    }
}