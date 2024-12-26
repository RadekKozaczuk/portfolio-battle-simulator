#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using GameLogic.Interfaces;
using GameLogic.Jobs;
using GameLogic.Models;
using JetBrains.Annotations;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

[assembly:InternalsVisibleTo("Tests")]
namespace GameLogic.Controllers
{
    /// <summary>
    /// Main controller serves 3 distinct roles:<br/>
    /// 1) It allows you to control signal execution order. For example, instead of reacting on many signals in many different controllers,
    /// you can have one signal, react on it here, and call necessary controllers/systems in the order of your liking.<br/>
    /// 2) Serves as a 'default' controller. When you don't know where to put some logic or the logic is too small for its own controller
    /// you can put it into the main controller.<br/>
    /// 3) Reduces the size of the viewmodel. We could move all (late/fixed)update calls to viewmodel but over time it would lead to viewmodel
    /// being too long to comprehend. We also do not want to react on signals in viewmodels for the exact same reason.<br/>
    /// For better code readability all controllers meant to interact with this controller should implement
    /// <see cref="ICustomLateUpdate" /> interface.<br/>
    /// </summary>
    [UsedImplicitly]
    class GameLogicMainController : IInitializable, ICustomUpdate
    {
        [Inject]
        static readonly WarriorController _warriorController;

        [Inject]
        static readonly ArcherController _archerController;

        [Inject]
        static readonly UpdateArmyCenterController _updateArmyCenterController;

        [Inject]
        static readonly InitializeBattleModelController _battleModelController;

        // todo: should be possible to inject directly
        readonly IUnitController[] _unitControllers = new IUnitController[2];

        IBattleModel _battleModel;
        Action<int>[] _behaviours;

        [Preserve]
        GameLogicMainController() { }

        public void Initialize()
        {
            _unitControllers[0] = _warriorController;
            _unitControllers[1] = _archerController;
        }

        public void InitializeModel(List<ArmyModel> armies, Bounds[] spawnZones)
        {
            IBattleModel model = new BattleModel(armies);
            _battleModel = model;
            _updateArmyCenterController.Initialize(_battleModel);
            _battleModelController.InitializeModel(model, spawnZones);
        }

        public void CustomUpdate()
        {
            if (GameStateService.CurrentState != GameState.Gameplay)
                return;

            GameLogicData.DeltaTime = Time.deltaTime;
            _updateArmyCenterController.CustomUpdate();

            var job = new MoveTowardCenterJob
            {
                Positions = CoreData.UnitCurrPos,
                CenterOfArmies = _updateArmyCenterController.CenterOfArmies,
                DeltaTime = GameLogicData.DeltaTime
            };

            JobHandle handle = job.Schedule(CoreData.UnitCurrPos.Length, 32); // todo: investigate innerloop batch count
            handle.Complete();

            // todo: Parallel For appears to be slower than single-threaded execution
            // todo: to check if splitting the execution into very small amount of tasks (f.e. 2 or 4) would be beneficial 

            // todo: in the future use parallel //Parallel.For(0, 2, armyId =>
            Span<UnitModel> units;

            // we go per army now (and then maybe per type)
            for (int armyId = 0; armyId < 2; armyId++)
            {
                units = _battleModel.GetUnits(armyId);

                for (int i = 0; i < units.Length; i++)
                {
                    int unitId = units[i].Id;
                    Memory<UnitModel>[] allies = _battleModel.GetUnitsExcept(armyId, unitId);

                    foreach (Memory<UnitModel> memory in allies)
                        PushAwayFromAllies(unitId, memory.Span);

                    Memory<UnitModel>[] enemies = _battleModel.GetEnemies(armyId);
                    foreach (Memory<UnitModel> memory in enemies)
                    {
                        // todo: additional check if not overwriting in case of more than 2 armies
                        int nearestEnemyId = PushAwayFromEnemiesAndFindNearest(unitId, memory.Span);
                        units[i].NearestEnemyId = nearestEnemyId;
                    }

                    units[i].AttackCooldown -= GameLogicData.DeltaTime;
                }

                if (armyId == 0)
                for (int unitType = 0; unitType < 2; unitType++)
                {
                    Action<int, int, IBattleModel> action = _unitControllers[unitType].GetBehavior(Strategy.Basic);
                    action(armyId, unitType, _battleModel);
                }
            }

            units = _battleModel.GetUnits();
            for (int i = 0; i < units.Length; i++)
            {
                units[i].Health -= units[i].HealthDelta;

                if (units[i].Health <= 0)
                    Signals.UnitDied(i);
            }
        }

        /// <summary>
        /// Pushes all units (regardless of their army membership) away from each other.
        /// </summary>
        static void PushAwayFromAllies(int unitId, Span<UnitModel> allies)
        {
            float2 currPos = CoreData.UnitCurrPos[unitId];
            float2 posDelta = float2.zero;

            // on allies calculate only pushback
            for (int i = 0; i < allies.Length; i++)
            {
                ref UnitModel otherUnit = ref allies[i];

                if (otherUnit.Health <= 0)
                    continue;

                float2 otherUnitPos = CoreData.UnitCurrPos[otherUnit.Id];
                float distance = math.distance(currPos, otherUnitPos);

                if (distance >= 2f)
                    continue;

                // calculating normal from 0,0 results in a NaN
                float2 difference = currPos - otherUnitPos;

                // in case units are on the same spot we ignore the calculation to prevent error from happening
                if (math.any(difference))
                {
                    float2 normal = math.normalize(difference);
                    posDelta -= normal * (2.0f - distance);
                }
            }

            CoreData.UnitCurrPos[unitId] -= posDelta;
        }

        static int PushAwayFromEnemiesAndFindNearest(int unitId, Span<UnitModel> enemies)
        {
            float2 currPos = CoreData.UnitCurrPos[unitId];
            float2 posDelta = float2.zero;

            // on enemies calculate evasion as well as the nearest unit id
            float distanceToNearest = float.MaxValue;
            int nearestEnemyId = int.MinValue;
            for (int i = 0; i < enemies.Length; i++)
            {
                ref UnitModel enemy = ref enemies[i];

                if (enemy.Health <= 0)
                    continue;

                float2 enemyCurrPos = CoreData.UnitCurrPos[enemy.Id];
                float distance = math.distance(currPos, enemyCurrPos);

                // find nearest
                if (distance < distanceToNearest)
                {
                    distanceToNearest = distance;
                    nearestEnemyId = enemy.Id;
                }

                if (distance >= 2f)
                    continue;

                // calculating normal from 0,0 results in a NaN
                float2 difference = currPos - enemyCurrPos;

                // in case units are on the same spot we ignore the calculation to prevent error from happening
                if (math.any(difference))
                {
                    float2 normal = math.normalize(difference);
                    posDelta -= normal * (2.0f - distance);
                }
            }

            CoreData.UnitCurrPos[unitId] -= posDelta;
            return nearestEnemyId;
        }

        /// <summary>
        /// Goes through all projectiles and calculate update their state.
        /// On top of it also update the damage to the hit enemy.
        /// </summary>
        static void UpdateProjectile(int projectileId, Span<UnitModel> enemies)
        {
            ref ProjectileModel model = ref CoreData.Projectiles[projectileId];

            if (model.ReadyToBeRecycled)
                return;

            model.Position += model.Direction * ProjectileModel.Speed;

            float2 proPos = CoreData.ProjectileCurrPos[projectileId];

            float distance = math.distance(proPos, model.Target);
            if (distance < ProjectileModel.Speed)
                model.OutOfRange = true; // will be destroyed by the end of this frame

            for (int i = 0; i < enemies.Length; i++)
            {
                ref UnitModel enemyModel = ref enemies[i];

                if (enemyModel.Health <= 0)
                    continue;

                float2 enemyPos = CoreData.UnitCurrPos[enemyModel.Id];
                distance = math.distance(proPos, enemyPos);

                // arrow cannot reach the target this frame
                if (distance >= ProjectileModel.Speed)
                    continue;

                float2 vec = proPos - enemyPos;
                enemyModel.HealthDelta -= model.Attack - 6; // todo: attack should also be taken from the shared data
                Signals.UnitHit(enemyModel.Id, new Vector3(vec.x, 0, vec.y));
                //enemyModel.HealthDelta -= model.Attack - enemyArmy.SharedUnitData[enemyModel.UnitType].Defense; // todo: take from shared data
            }
        }
    }
}