#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core;
using Core.Enums;
using Core.Models;
using GameLogic.Interfaces;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace GameLogic.Controllers
{
    class WarriorController : IUnitController
    {
        [Inject]
        static readonly UpdateArmyCenterController _updateArmyCenterController;

        readonly Action<int, int, IBattleModel>[] _strategies = {Basic, Defensive};

        [Preserve]
        WarriorController() { }

        Action<int, int, IBattleModel> IUnitController.GetBehavior(Strategy strategy) => _strategies[(int)strategy];

        static void Basic(int armyId, int unitType, IBattleModel battleModel)
        {
            Span<UnitModel> units = battleModel.GetUnits(armyId, unitType);

            foreach (UnitModel model in units)
            {
                // skip dead ones
                if (model.Health <= 0)
                    continue;

                ref UnitModel unit = ref battleModel.GetUnit(model.Id);
                ref UnitModel enemy = ref battleModel.GetUnit(model.NearestEnemyId);
                Logic(ref unit, ref enemy);
            }
        }

        static void Defensive(int armyId, int unitType, IBattleModel battleModel)
        {
            Span<UnitModel> units = battleModel.GetUnits(armyId, unitType);

            foreach (UnitModel model in units)
            {
                // skip dead ones
                if (model.Health <= 0)
                    continue;

                ref UnitModel unit = ref battleModel.GetUnit(model.Id);
                ref UnitModel enemy = ref battleModel.GetUnit(model.NearestEnemyId);

                ref UnitStatsModel sharedData = ref CoreData.UnitStats[model.UnitType];
                SharedUnitBehaviors.MoveTowardsCenter(ref unit, _updateArmyCenterController.GetArmyCenter(enemy.ArmyId), in sharedData);

                Logic(ref unit, ref enemy);
            }
        }

        static void Logic(ref UnitModel model, ref UnitModel enemyModel)
        {
            ref UnitStatsModel sharedData = ref CoreData.UnitStats[model.UnitType];

            // Movement Logic
            if (model.AttackCooldown > sharedData.CooldownDifference)
                return;

            float2 pos = CoreData.UnitCurrPos[model.Id];
            float2 enemyPos = CoreData.UnitCurrPos[enemyModel.Id];
            float2 difference = enemyPos - pos;
            float2 dirToEnemy = math.normalize(enemyPos - pos);

            // enemy and pos maybe the same and in such case normal would have been NaN so this check is needed
            if (math.any(difference))
                CoreData.UnitCurrPos[model.Id] += dirToEnemy * sharedData.Speed * GameLogicData.DeltaTime;

            // Attack Logic
            if (model.AttackCooldown > 0)
                return;

            float distance = math.distance(pos, enemyPos);
            if (distance > sharedData.AttackRange)
                return;

            ref UnitStatsModel enemySharedData = ref CoreData.UnitStats[enemyModel.UnitType];

            Signals.UnitAttacked(model.Id);
            model.AttackCooldown = sharedData.AttackCooldown;
            enemyModel.HealthDelta -= sharedData.Attack - enemySharedData.Defense;
            Signals.UnitHit(enemyModel.Id, new Vector3(dirToEnemy.x, 0, dirToEnemy.y));
        }
    }
}
