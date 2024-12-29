#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using Presentation.Interfaces;
using Unity.Collections;
using UnityEngine.Jobs;

namespace Presentation
{
    /// <summary>
    /// Assembly-level data.
    /// </summary>
    static class PresentationData
    {
        /// <summary>
        /// Null when dead.
        /// </summary>
        internal static IUnit?[] Units;
        internal static List<IProjectile> Projectiles;

        internal static TransformAccessArray UnitTransformAccess;
        internal static TransformAccessArray ProjectileTransformAccess;

        /// <summary>
        /// Stores values from 0 to 1 (both inclusive) indicating how fast unit walks.
        /// </summary>
        internal static NativeArray<float> MovementSpeedArray;
    }
}