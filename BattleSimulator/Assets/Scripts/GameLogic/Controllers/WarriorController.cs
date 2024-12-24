﻿#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
    class WarriorController : IUnitController
    {
        [Inject]
        static readonly UpdateArmyCenterController _updateArmyCenterController;

        //readonly Action<int>[] _strategies = {Basic, Defensive};
        readonly Action<int, int, IBattleModel>[] _strategies = {BasicV2, DefensiveV2};

        [Preserve]
        WarriorController() { }

        //Action<int> IUnitController.GetBehavior(Strategy strategy) => _strategies[(int)strategy];
        Action<int, int, IBattleModel> IUnitController.GetBehavior(Strategy strategy) => _strategies[(int)strategy];

        static void BasicV2(int armyId, int unitType, IBattleModel battleModel)
        {
            Span<UnitModel> units = battleModel.GetUnits(armyId, unitType);

            foreach (UnitModel model in units)
            {
                ref UnitModel unit = ref battleModel.GetUnit(model.Id);
                ref UnitModel enemy = ref battleModel.GetUnit(model.NearestEnemyId);
                Logic(ref unit, ref enemy);
            }
        }

        static void DefensiveV2(int armyId, int unitType, IBattleModel battleModel)
        {
            Span<UnitModel> units = battleModel.GetUnits(armyId, unitType);

            foreach (UnitModel model in units)
            {
                ref UnitModel unit = ref battleModel.GetUnit(model.Id);
                ref UnitModel enemy = ref battleModel.GetUnit(model.NearestEnemyId);

                ref UnitStatsModel sharedData = ref CoreData.UnitStats[model.UnitType];
                SharedUnitBehaviors.MoveTowardsCenter(ref unit, _updateArmyCenterController.GetArmyCenter(enemy.ArmyId), in sharedData);

                Logic(ref unit, ref enemy);
            }
        }

        /*static void Basic(int unitId)
        {
            ref UnitModel model = ref CoreData.Units[unitId];
            ref UnitModel enemyModel = ref CoreData.Units[model.NearestEnemyId];
            Logic(ref model, ref enemyModel);
        }

        static void Defensive(int unitId)
        {
            ref UnitModel model = ref CoreData.Units[unitId];
            ref UnitModel enemyModel = ref CoreData.Units[model.NearestEnemyId];

            ref UnitStatsModel sharedData = ref CoreData.UnitStats[model.UnitType];
            SharedUnitBehaviors.MoveTowardsCenter(ref model, CoreData.ArmyCenters[enemyModel.ArmyId], in sharedData);

            Logic(ref model, ref enemyModel);
        }*/

        static void Logic(ref UnitModel model, ref UnitModel enemyModel)
        {
            ref UnitStatsModel sharedData = ref CoreData.UnitStats[model.UnitType];

            // Movement Logic
            if (model.AttackCooldown > sharedData.CooldownDifference)
                return;

            float2 pos = CoreData.UnitCurrPos[model.Id];
            float2 enemyPos = CoreData.UnitCurrPos[enemyModel.Id];
            float2 dirToEnemy = math.normalize(enemyPos - pos);
            CoreData.UnitCurrPos[model.Id] += dirToEnemy * sharedData.Speed; // todo: should also be multiplied by time

            // Attack Logic
            if (model.AttackCooldown > 0)
                return;

            float distance = math.distance(pos, enemyPos);
            if (distance > sharedData.AttackRange)
                return;

            ref UnitStatsModel enemySharedData = ref CoreData.UnitStats[enemyModel.UnitType];

            model.Attacked = true;
            model.LastIncomingAttackDirection = dirToEnemy;
            model.AttackCooldown = sharedData.AttackCooldown;
            enemyModel.HealthDelta -= sharedData.Attack - enemySharedData.Defense;
        }
    }
}
