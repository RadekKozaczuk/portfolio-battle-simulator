#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core;
using Core.Enums;
using Core.Models;
using GameLogic.Interfaces;
using Unity.Mathematics;
using UnityEngine.Scripting;
#if DEVELOPMENT_BUILD
using UnityEngine.Assertions;
#endif

namespace GameLogic.Controllers
{
    class ArcherController : IUnitController
    {
        [Inject]
        static readonly GameLogicMainController _gameLogicMainController;

        readonly Action<int, int, IBattleModel>[] _strategies = {Basic, Defensive};

        [Preserve]
        ArcherController() { }

        Action<int, int, IBattleModel> IUnitController.GetBehavior(Strategy strategy) => _strategies[(int)strategy];

        static void Basic(int armyId, int unitType, IBattleModel battleModel)
        {
            Span<UnitModel> units = battleModel.GetUnits(armyId, unitType);

            foreach (UnitModel model in units)
            {
                int unitId = model.Id;

                ref UnitModel unit = ref battleModel.GetUnit(unitId);
                ref UnitModel enemy = ref battleModel.GetUnit(model.NearestEnemyId);

#if DEVELOPMENT_BUILD
                Assert.IsTrue(model.Health > 0, "Executing strategy for a unit that is dead is not allowed.");
#endif

                if (unit.NearestEnemyId == int.MinValue)
                    return;

                ref UnitStatsModel sharedData = ref CoreData.UnitStats[model.UnitType];

                float2 pos = CoreData.UnitCurrPos[unitId];
                float2 enemyPos = CoreData.UnitCurrPos[enemy.Id];

                // Movement Logic
                if (model.AttackCooldown <= sharedData.CooldownDifference)
                {
                    float2 normal = math.normalize(enemyPos - pos);
                    CoreData.UnitCurrPos[unitId] += normal * sharedData.Speed;
                }

                // Attack Logic
                if (model.AttackCooldown > 0)
                    return;

                float distance = math.distance(pos, enemyPos);
                if (distance > sharedData.AttackRange)
                    return;

                Signals.UnitAttacked(model.Id);
                unit.AttackCooldown = sharedData.AttackCooldown;
                _gameLogicMainController.AddProjectile(armyId, pos, enemyPos, 5);
            }
        }

        static void Defensive(int armyId, int unitType, IBattleModel battleModel)
        {
            /*ref UnitModel model = ref army.Models[unitId];

#if DEVELOPMENT_BUILD
            Assert.IsTrue(model.Health > 0, "Executing strategy for a unit that is dead is not allowed.");
#endif

            ref UnitSharedDataModel sharedData = ref army.SharedUnitData[model.UnitType];
            SharedUnitBehaviors.MoveTowardsCenter(ref model, in enemyArmy.Center, in sharedData);

            ref UnitModel enemyModel = ref enemyArmy.Models[model.NearestEnemyId];
            Vector3 vector = enemyModel.Position - model.Position;
            float distance = Mathf.Sqrt(vector.x * vector.x + vector.z * vector.z);

            Vector3 toNearest = vector.normalized;
            if (distance < sharedData.AttackRange)
            {
                if (model.AttackCooldown <= sharedData.CooldownDifference)
                {
                    Vector3 flank = Quaternion.Euler(0, 90, 0) * toNearest;
                    model.Position += -(toNearest + flank).normalized * sharedData.Speed;
                }
            }
            else
            {
                // if I can shoot but don't have anybody in range I go towards the nearest enemy
                if (model.AttackCooldown <= sharedData.CooldownDifference)
                    model.Position += toNearest * sharedData.Speed;
            }

            // Attack Logic
            if (model.AttackCooldown > 0)
                return;

            if (distance > sharedData.AttackRange)
                return;

            model.Attacked = true;
            model.AttackCooldown = sharedData.MaxAttackCooldown;
            army.AddProjectileDto(model.Position, enemyModel.Position, sharedData.Attack);*/
        }
    }
}
