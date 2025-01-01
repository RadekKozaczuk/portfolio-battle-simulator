using Core;
using Unity.Mathematics;
using UnityEngine;

namespace GameLogic.Models
{
    internal struct ProjectileModel
    {
        internal float2 Position
        {
            get => _position;
            set
            {
                _position = value;
                Signals.ProjectilePositionChanged(Id, new Vector3(_position.x, 0f, _position.y));
            }
        }
        float2 _position;

        internal readonly int Id;
        internal int ArmyId;
        internal float2 Target;

        /// <summary>
        /// Normalized.
        /// </summary>
        internal float2 Direction;

        /// <summary>
        /// The projectile is long time dead and ready to be recycled.
        /// </summary>
        internal bool ReadyToRecycle;

        internal ProjectileModel(int id)
        {
            Id = id;
            ArmyId = int.MinValue;
            _position = float2.zero;
            Direction = float2.zero;
            Target = float2.zero;
            ReadyToRecycle = true;
        }

        internal void Recycle(int armyId, float2 position, float2 target)
        {
            ArmyId = armyId;
            _position = position;
            Direction = math.normalize(target - position);
            Target = target;
            ReadyToRecycle = false;

            Signals.ProjectileCreated(Id,
                                      armyId,
                                      new Vector3(_position.x, 0, _position.y),
                                      new Vector3(Direction.x, 0, Direction.y));
        }
    }
}
