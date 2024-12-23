#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace GameLogic.Jobs
{
    /// <summary>
    /// Projectile direction does not change mid-flight.
    /// </summary>
    public struct MoveTowardCenterJob : IJobParallelFor
    {
        // Jobs declare all data that will be accessed in the job
        // By declaring it as read only, multiple jobs are allowed to access the data in parallel
        public NativeArray<float2> Positions;

        [ReadOnly]
        public float2 CenterOfArmies;

        // The code actually running on the job
        public void Execute(int index)
        {
            float2 currPos = Positions[index];
            float distanceToCenter = math.distance(currPos, CenterOfArmies);

            if (distanceToCenter <= 80.0f)
                return;

            float2 normal = math.normalize(CenterOfArmies - currPos);
            Positions[index] -= normal * (80.0f - distanceToCenter);
        }
    }
}