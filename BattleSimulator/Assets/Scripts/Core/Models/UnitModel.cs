using Unity.Mathematics;

namespace Core.Models
{
    public struct UnitModel
    {
        public int Id;
        public int ArmyId;
        public int Health;

        /// <summary>
        /// Change to health in the given frame.
        /// </summary>
        public int HealthDelta;

        public float AttackCooldown;
        public int NearestEnemyId;

        /// <summary>
        /// True, if the unit attacked this turn.
        /// </summary>
        public bool Attacked;
        public float2 LastIncomingAttackDirection;

        public UnitModel(int id, int armyId, int health)
        {
            Id = id;
            ArmyId = armyId;
            Health = health;
            AttackCooldown = 0f;
            NearestEnemyId = int.MinValue;
            Attacked = false;
            LastIncomingAttackDirection = float2.zero;
            HealthDelta = 0;
        }
    }
}