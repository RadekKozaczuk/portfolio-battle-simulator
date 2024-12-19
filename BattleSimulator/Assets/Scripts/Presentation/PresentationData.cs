using Presentation.Interfaces;
using UnityEngine.Jobs;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Presentation
{
    /// <summary>
    /// Assembly-level data.
    /// </summary>
    static class PresentationData
    {
        internal static IProjectile[] Projectiles;

        internal static TransformAccessArray UnitTransformAccess;
        internal static TransformAccessArray ProjectileTransformAccess;
    }
}