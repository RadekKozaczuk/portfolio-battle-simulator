namespace Core.Models
{
    public struct UnitModel
    {
        // todo: Health and HealthDelta -> one int
        // todo: Id, UnitType, ArmyId and NearestEnemyId -> one int
        public readonly int Id;
        public readonly int UnitType;
        public readonly int ArmyId;
        public int NearestEnemyId;

        public int Health;

        /// <summary>
        /// Change to health in the given frame.
        /// </summary>
        public int HealthDelta;

        public float AttackCooldown;

        public UnitModel(int id, int unitType, int armyId)
        {
            Id = id;
            UnitType = unitType;
            ArmyId = armyId;
            Health = 0;
            AttackCooldown = 0f;
            NearestEnemyId = int.MinValue;
            HealthDelta = 0;
        }
    }
}