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

                // todo: create projectile
                //army.AddProjectileDto(model.Position, enemyModel.Position, sharedData.Attack);
            }
        }

        static void Defensive(int armyId, int unitType, IBattleModel battleModel)
        {

        }
    }
}
