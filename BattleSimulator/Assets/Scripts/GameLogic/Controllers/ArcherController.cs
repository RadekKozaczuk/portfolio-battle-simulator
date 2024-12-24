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
        //readonly Action<int>[] _strategies = {Basic, Defensive};
        readonly Action<int, int, IBattleModel>[] _strategies = {};

        [Preserve]
        ArcherController() { }

        //Action<int> IUnitController.GetBehavior(Strategy strategy) => _strategies[(int)strategy];
        Action<int, int, IBattleModel> IUnitController.GetBehavior(Strategy strategy) => _strategies[(int)strategy];

        /*static void Basic(Span<UnitModel> units, int unitId)
        {
            for (int i = 0; i < units.Length; i++)
            {
                UnitModel model = units[i];

#if DEVELOPMENT_BUILD
                Assert.IsTrue(model.Health > 0, "Executing strategy for a unit that is dead is not allowed.");
#endif

                if (model.NearestEnemyId == int.MinValue)
                    return;

                ref UnitModel enemyModel = ref CoreData.Units[model.NearestEnemyId];
                ref UnitStatsModel sharedData = ref CoreData.UnitStats[model.UnitType];

                float2 pos = CoreData.UnitCurrPos[model.Id];
                float2 enemyPos = CoreData.UnitCurrPos[enemyModel.Id];

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

                model.Attacked = true;
                model.AttackCooldown = sharedData.AttackCooldown;

                // todo: create projectile
                //army.AddProjectileDto(model.Position, enemyModel.Position, sharedData.Attack);
            }
        }

        static void Defensive(int unitId) { }*/
    }
}
