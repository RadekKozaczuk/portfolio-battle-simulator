using Core;
using Unity.Mathematics;
using UnityEngine;

namespace GameLogic.Models
{
    internal struct ProjectileModel
    {
        static int _projectileIdCounter;

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

        internal int Id;
        internal int ArmyId;
        internal float2 Target;

        /// <summary>
        /// Normalized.
        /// </summary>
        internal float2 Direction;

        /// <summary>
        /// If false then the projectile is ready to be recycled.
        /// </summary>
        internal bool InUse;

        internal void Recycle(int armyId, float2 position, float2 target)
        {
            Id = _projectileIdCounter++;
            ArmyId = armyId;
            _position = position;
            Direction = math.normalize(target - position);
            Target = target;
            InUse = true;

            Signals.ProjectileCreated(Id,
                                      armyId,
                                      new Vector3(_position.x, 0, _position.y),
                                      new Vector3(Direction.x, 0, Direction.y));
        }
    }
}
