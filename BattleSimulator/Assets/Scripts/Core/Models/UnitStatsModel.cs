namespace Core.Models
{
    public readonly struct UnitStatsModel
    {
        public readonly int Health;
        public readonly int Defense;
        public readonly int Attack;
        public readonly float AttackRange;
        public readonly float AttackCooldown;
        public readonly float Speed;
        public readonly float DistanceToEnemyCenterThreshold;

        /// <summary>
        /// Equals <see cref="AttackCooldown"/> - postAttackDelay.
        /// </summary>
        public readonly float CooldownDifference;

        public UnitStatsModel(int health, int defense, int attack, float attackRange,
            float attackCooldown, float postAttackDelay, float speed, float distanceToEnemyCenterThreshold)
        {
            Health = health;
            Defense = defense;
            Attack = attack;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
            Speed = speed;
            DistanceToEnemyCenterThreshold = distanceToEnemyCenterThreshold;

            CooldownDifference = attackCooldown - postAttackDelay;
        }
    }
}