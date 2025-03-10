#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using GameLogic.Config;
using GameLogic.Interfaces;
using GameLogic.Jobs;
using GameLogic.Models;
using JetBrains.Annotations;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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
    /// </summary>
    [UsedImplicitly]
    class GameLogicMainController : ICustomUpdate
    {
        [Inject]
        static readonly IUnitController[] _unitControllers;

        [Inject]
        static readonly UpdateArmyCenterController _updateArmyCenterController;

        [Inject]
        static readonly InitializeBattleModelController _battleModelController;

        readonly ISpacePartitioningController _spacePartitioningController;
        readonly IBattleModel _battleModel;
        ProjectileModel[] _projectiles = new ProjectileModel[50];
        bool _finished;

        static readonly SpacePartitioningConfig _config;

        [Inject]
        GameLogicMainController(IBattleModel model)
        {
            _battleModel = model;

            _updateArmyCenterController.Initialize(_battleModel);
            _battleModelController.InitializeModel(_battleModel);

            Span<UnitModel> units = _battleModel.GetUnits();

            _spacePartitioningController = new SpacePartitioningController(_config.Bounds, _config.QuadrantCount, units.Length);

            for (int i = 0; i < units.Length; i++)
                _spacePartitioningController.AddUnit(i, units[i].ArmyId, CoreData.UnitCurrPos[i]);

            _spacePartitioningController.UpdateUnits();
        }

        public void CustomUpdate()
        {
            if (_finished)
                return;

            if (_battleModel.OneOrZeroArmiesLeft(out int numLeft))
            {
                _finished = true;
                Signals.Victory(numLeft);
                return;
            }

            GameLogicData.DeltaTime = Time.deltaTime;
            _updateArmyCenterController.CustomUpdate();

            var job = new MoveTowardCenterJob
            {
                Positions = CoreData.UnitCurrPos,
                CenterOfArmies = _updateArmyCenterController.CenterOfArmies,
                DeltaTime = GameLogicData.DeltaTime
            };

            JobHandle handle = job.Schedule(CoreData.UnitCurrPos.Length, 32);
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
                    // skip dead ones
                    if (units[i].Health <= 0)
                        continue;

                    int unitId = units[i].Id;
                    float2 pos = CoreData.UnitCurrPos[unitId];
                    List<int> nearbyUnits = _spacePartitioningController.FindAllNearbyUnits(pos, unitId, 2f);

                    PushAway(unitId, nearbyUnits);
                    _spacePartitioningController.Release(nearbyUnits);

                    units[i].NearestEnemyId = _spacePartitioningController.FindNearestEnemy(pos, armyId);
                    units[i].AttackCooldown -= GameLogicData.DeltaTime;
                }

                for (int unitType = 0; unitType < 2; unitType++)
                {
                    Strategy strategy = _battleModel.GetStrategy(armyId, unitType);
                    Action<int, UnitType, IBattleModel> action = _unitControllers[unitType].GetBehavior(strategy);
                    action(armyId, (UnitType)unitType, _battleModel);
                }
            }

            UpdateProjectiles();

            // Apply damage
            units = _battleModel.GetUnits();
            for (int i = 0; i < units.Length; i++)
            {
                // check if unit is alive at the moment
                if (units[i].Health <= 0)
                    continue;

                units[i].Health += units[i].HealthDelta;
                units[i].HealthDelta = 0;

                // check if unit died after getting hit
                if (units[i].Health > 0)
                    continue;

                _battleModel.UnitDied(units[i].ArmyId);
                _spacePartitioningController.KillUnit(units[i].Id);
                Signals.UnitDied(units[i].Id);
            }

            _spacePartitioningController.UpdateUnits();
        }

        internal void AddProjectile(int armyId, float2 pos, float2 targetPos)
        {
            ref ProjectileModel projectile = ref _projectiles[0];

            // try to find a dto to recycle
            int i = 0;
            do
            {
                projectile = ref _projectiles[i++];

                if (projectile.InUse)
                    continue;

                projectile.Recycle(armyId, pos, targetPos);
                return;
            }
            while (i < _projectiles.Length);

            // reallocate
            var tempDtos = new ProjectileModel[_projectiles.Length * 2];

            // copy over
            i = 0;
            for (; i < _projectiles.Length; i++)
                tempDtos[i] = _projectiles[i];

            _projectiles = tempDtos;

            projectile = ref _projectiles[++i]; // first empty slot
            projectile.Recycle(armyId, pos, targetPos);
        }

        /// <summary>
        /// Pushes all units (regardless of their army membership) away from each other.
        /// </summary>
        static void PushAway(int unitId, List<int> nearbyUnits)
        {
            float2 currPos = CoreData.UnitCurrPos[unitId];
            float2 posDelta = float2.zero;

            // on allies calculate only pushback
            foreach (int otherUnit in nearbyUnits)
            {
                float2 otherUnitPos = CoreData.UnitCurrPos[otherUnit];
                float distance = math.distance(currPos, otherUnitPos);

                // calculating normal from 0,0 results in a NaN
                float2 difference = currPos - otherUnitPos;
                float2 normal = math.normalizesafe(difference);
                posDelta -= normal * (2.0f - distance);
            }

            CoreData.UnitCurrPos[unitId] -= posDelta;
        }

        /// <summary>
        /// Goes through all projectiles and calculate update their state.
        /// On top of it also update the damage to the hit enemy.
        /// </summary>
        void UpdateProjectiles()
        {
            for (int i = 0; i < _projectiles.Length; i++)
            {
                ref ProjectileModel projectile = ref _projectiles[i];

                if (!projectile.InUse)
                    continue;

                const float Speed = 50f; // todo: should be taken from shared data
                float2 vectorTraveled = projectile.Direction * Speed * GameLogicData.DeltaTime;
                float distanceTraveled = math.distance(float2.zero, vectorTraveled);

                projectile.Position += vectorTraveled;
                float2 pos = projectile.Position;

                float distance = math.distance(pos, projectile.Target);
                if (distance <= distanceTraveled) // projectile reached the maximum range
                {
                    projectile.InUse = false;
                    Signals.ProjectileDestroyed(projectile.Id);
                    continue;
                }

                Memory<UnitModel>[] memories = _battleModel.GetEnemies(projectile.ArmyId);

                foreach (Memory<UnitModel> m in memories)
                {
                    Span<UnitModel> enemies = m.Span;
                    for (int j = 0; j < enemies.Length; j++)
                    {
                        ref UnitModel enemyModel = ref enemies[j];

                        if (enemyModel.Health <= 0)
                            continue;

                        float2 enemyPos = CoreData.UnitCurrPos[enemyModel.Id];
                        distance = math.distance(pos, enemyPos);

                        // arrow cannot reach the target this frame
                        if (distance >= 2.5f)
                            continue;

                        float2 vec = pos - enemyPos;
                        enemyModel.HealthDelta -= 12 - 6; // todo: projectile attack and enemy defense should be taken from the shared data
                        projectile.InUse = false;

                        Signals.UnitHit(enemyModel.Id, new Vector3(vec.x, 0, vec.y));
                        Signals.ProjectileDestroyed(projectile.Id);

                        goto Found;
                    }
                }

            Found: ;
            }
        }
    }
}