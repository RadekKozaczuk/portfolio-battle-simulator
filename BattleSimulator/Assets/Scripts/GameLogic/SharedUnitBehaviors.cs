#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core;
using Core.Models;
using Unity.Mathematics;

namespace GameLogic
{
    static class SharedUnitBehaviors
    {
        /// <summary>
        /// Throws exception if the unit passed is not alive (health is lower equal zero).
        /// </summary>
        internal static void MoveTowardsCenter(ref UnitModel unit, in float2 enemyArmyCenter, in UnitStatsModel sharedData)
        {
#if DEVELOPMENT_BUILD
            Assert.IsTrue(unit.Health > 0, "Unit must be alive in order to be processed.");
#endif

            float2 currPos = CoreData.UnitCurrPos[unit.Id];
            float distance = math.distance(currPos, enemyArmyCenter);

            if (distance <= sharedData.DistanceToEnemyCenterThreshold)
                return;

            if (enemyArmyCenter.x < currPos.x)
            {
                if (unit.AttackCooldown <= sharedData.CooldownDifference)
                    CoreData.UnitCurrPos[unit.Id] += new float2(-1, 0) * sharedData.Speed;
            }
            else if (enemyArmyCenter.x > currPos.x)
            {
                if (unit.AttackCooldown <= sharedData.CooldownDifference)
                    CoreData.UnitCurrPos[unit.Id] += new float2(1, 0) * sharedData.Speed;
            }
        }
    }
}