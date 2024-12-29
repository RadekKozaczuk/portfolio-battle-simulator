using Core;
using Unity.Mathematics;
using UnityEngine;

namespace GameLogic.Models
{
    internal struct ProjectileModel
    {
        internal readonly int Id;
        internal float2 Target;

        /// <summary>
        /// Normalized.
        /// </summary>
        internal float2 Direction;
        internal float2 Position;

        /// <summary>
        /// Indicates that the arrow has died due to reaching its maximum range this frame.
        /// Goes back to false after that frame.
        /// </summary>
        internal bool OutOfRange;

        /// <summary>
        /// The projectile is long time dead and ready to be recycled.
        /// </summary>
        internal bool ReadyToRecycle;

        internal ProjectileModel(int id)
        {
            Id = id;
            Position = float2.zero;
            Direction = float2.zero;
            Target = float2.zero;
            OutOfRange = false;
            ReadyToRecycle = true;
        }

        internal void Recycle(int armyId, float2 position, float2 target)
        {
            Position = position;
            Direction = math.normalize(target - position);
            Target = target;
            OutOfRange = false;
            ReadyToRecycle = false;

            Signals.ProjectileCreated(Id,
                                      armyId,
                                      new Vector3(position.x, 0, position.y),
                                      new Vector3(target.x, 0, target.y));
        }
    }
}
