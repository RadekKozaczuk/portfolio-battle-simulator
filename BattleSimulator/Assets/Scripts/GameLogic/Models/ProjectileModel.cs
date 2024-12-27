using Core;
using Unity.Mathematics;
using UnityEngine;

namespace GameLogic.Models
{
    internal struct ProjectileModel
    {
        internal float2 Target;

        /// <summary>
        /// Normalized.
        /// </summary>
        internal float2 Direction;
        internal float2 Position;

        internal const float Speed = 2.5f; // todo: move to config
        internal int Attack;             // todo: temporarily present - in the future will be in dedicated shared data

        /// <summary>
        /// Indicates that the arrow has died due to reaching its maximum range this frame.
        /// Goes back to false after that frame.
        /// </summary>
        internal bool OutOfRange;

        /// <summary>
        /// Does not have machine View object yet.
        /// Set to false after instantiation.
        /// </summary>
        internal bool NewlySpawned;

        /// <summary>
        /// Means that the dto is long time dead and ready to be recycled.
        /// </summary>
        internal bool ReadyToBeRecycled;

        internal void Recycle(int armyId, float2 position, float2 target, int attack)
        {
            Position = position;
            Direction = math.normalize(target - position);
            Target = target;
            Attack = attack;
            OutOfRange = false;
            NewlySpawned = true;
            ReadyToBeRecycled = false;

            Signals.ProjectileCreated(armyId,
                                      new Vector3(position.x, 0, position.y),
                                      new Vector3(target.x, 0, target.y));
        }
    }
}
