namespace Core.Models
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct UnitDataModel
    {
        public readonly int Health;
        public readonly int Defense;
        public readonly int Attack;
        public readonly float AttackRange;
        public readonly float AttackCooldown;
        public readonly int Speed;
        public readonly float DistanceToEnemyCenterThreshold;

        public UnitDataModel(int health, int defense, int attack, float attackRange,
            float attackCooldown, int speed, float distanceToEnemyCenterThreshold)
        {
            Health = health;
            Defense = defense;
            Attack = attack;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            Speed = speed;
            DistanceToEnemyCenterThreshold = distanceToEnemyCenterThreshold;
        }
    }
}