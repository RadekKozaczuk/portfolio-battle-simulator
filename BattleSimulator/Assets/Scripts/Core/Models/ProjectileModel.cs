using Unity.Mathematics;

namespace Core.Models
{
    public struct ProjectileModel
    {
        public int ArmyId;
        public float2 Target;

        /// <summary>
        /// Normalized.
        /// </summary>
        public float2 Direction;
        public float2 Position;

        public const float Speed = 2.5f; // todo: move to config
        public int Attack;             // todo: temporarily present - in the future will be in dedicated shared data

        /// <summary>
        /// Indicates that the arrow has died due to reaching its maximum range this frame.
        /// Goes back to false after that frame.
        /// </summary>
        public bool OutOfRange;

        /// <summary>
        /// Does not have machine View object yet.
        /// Set to false after instantiation.
        /// </summary>
        public bool NewlySpawned;

        /// <summary>
        /// Means that the dto is long time dead and ready to be recycled.
        /// </summary>
        public bool ReadyToBeRecycled;

        public void Recycle(int armyId, float2 position, float2 target, int attack)
        {
            ArmyId = armyId;
            Position = position;
            Direction = math.normalize(target - position);
            Target = target;
            Attack = attack;
            OutOfRange = false;
            NewlySpawned = true;
            ReadyToBeRecycled = false;
        }
    }
}
