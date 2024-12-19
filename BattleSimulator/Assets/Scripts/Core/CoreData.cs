#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core.Enums;
using Core.Models;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Application-level (global) data.
    /// All static data objects used across the project are stored here.
    /// </summary>
    public static class CoreData
    {
        public static Vector3 CenterOfArmies;
        public static Level CurrentLevel;

        public static NativeArray<float2> UnitCurrPos;

        /// <summary>
        /// Contains two <see cref="float.MinValue"/> values if the corresponding unit was not attacked.
        /// </summary>
        public static NativeArray<float2> AttackingEnemyPos;

        // initially size = 10, does upscale when needed
        public static NativeArray<float2> ProjectileCurrPos = new(10, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        public static unsafe UnitModel*[] UnitSpans;

        /// <summary>
        /// Contains all the units from all armies.
        /// </summary>
        public static UnitModel[] Models;
    }
}