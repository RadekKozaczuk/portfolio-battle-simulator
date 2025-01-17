#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace GameLogic.Data
{
    [CreateAssetMenu(fileName = "UnitData", menuName = "Data/GameLogic/UnitData")]
    class UnitData : ScriptableObject
    {
        internal float CooldownDifference => AttackCooldown - PostAttackDelay;

        [SerializeField]
        internal string Name;

        [SerializeField]
        internal int Health;

        [SerializeField]
        internal int Defense;

        [SerializeField]
        internal int Attack;

        [SerializeField]
        internal float AttackRange;

        [SerializeField]
        internal float AttackCooldown;

        [SerializeField]
        internal float PostAttackDelay;

        [SerializeField]
        internal float Speed;

        [SerializeField]
        internal float DistanceToEnemyCenterThreshold;
    }
}