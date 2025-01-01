#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Unity.Collections;
using UnityEngine.Jobs;

namespace Presentation
{
    /// <summary>
    /// Assembly-level data.
    /// </summary>
    static class PresentationData
    {
        internal static TransformAccessArray UnitTransformAccess;

        /// <summary>
        /// Stores values from 0 to 1 (both inclusive) indicating how fast unit walks.
        /// </summary>
        internal static NativeArray<float> MovementSpeedArray;
    }
}