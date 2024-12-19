using UnityEngine;

namespace Core.Models
{
    public struct UnitModel
    {
        public float Health;

        /// <summary>
        /// Change to health in the given frame.
        /// </summary>
        public float HealthDelta;

        public float AttackCooldown;
        public int NearestEnemyId;

        /// <summary>
        /// True, if the unit attacked this turn.
        /// </summary>
        public bool Attacked;
        public Vector3 LastIncomingAttackDirection;

        public UnitModel(float health)
        {
            Health = health;
            AttackCooldown = 0f;
            NearestEnemyId = int.MinValue;
            Attacked = false;
            LastIncomingAttackDirection = Vector3.zero;
            HealthDelta = 0;
        }
    }
}