using Unity.Mathematics;

namespace Core.Models
{
    public struct UnitModel
    {
        // todo: convert all these variables into one int
        // todo: id would take 10 bits max = 1024
        // todo: unitType - 2 bits = 4 combanations
        // todo: armyId - 2 bits = 4 comb
        // todo: health - 10 bits = 1024
        // todo: attacked - 1 bit

        // todo: so health and healthDelta -> one int
        // todo: id, unit Type, armyId and attacked -> one int
        public int Id;
        public int UnitType;
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

        public UnitModel(int id, int unitType, int armyId)
        {
            Id = id;
            UnitType = unitType;
            ArmyId = armyId;
            Health = 0;
            AttackCooldown = 0f;
            NearestEnemyId = int.MinValue;
            Attacked = false;
            LastIncomingAttackDirection = float2.zero;
            HealthDelta = 0;
        }

        public UnitModel(int id, int unitType, int armyId, int health)
        {
            Id = id;
            UnitType = unitType;
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