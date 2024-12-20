#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Presentation.Interfaces;
using UnityEngine.Jobs;

namespace Presentation
{
    /// <summary>
    /// Assembly-level data.
    /// </summary>
    static class PresentationData
    {
        internal static IUnit[] Units;
        internal static IProjectile[] Projectiles;

        internal static TransformAccessArray UnitTransformAccess;
        internal static TransformAccessArray ProjectileTransformAccess;
    }
}