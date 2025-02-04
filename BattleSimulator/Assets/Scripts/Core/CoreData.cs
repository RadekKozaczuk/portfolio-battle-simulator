#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Unity.Collections;
using Unity.Mathematics;

namespace Core
{
    /// <summary>
    /// Application-level (global) data.
    /// All static data objects used across the project are stored here.
    /// </summary>
    public static class CoreData
    {
        public static NativeArray<float2> UnitCurrPos;

        /// <summary>
        /// Contains two <see cref="float.MinValue"/> values if the corresponding unit was not attacked.
        /// </summary>
        public static NativeArray<float2> AttackingEnemyPos;
    }
}