#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using GameLogic.Interfaces;
using GameLogic.Models;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

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
    class GameLogicMainController : ICustomFixedUpdate, ICustomUpdate, ICustomLateUpdate, IInitializable
    {
        [Inject]
        static readonly WarriorController _warriorController;

        [Inject]
        static readonly ArcherController _archerController;

        // todo: should be possible to inject directly
        readonly IUnitController[] _unitControllers = new IUnitController[2];

        [Preserve]
        GameLogicMainController() { }

        public void Initialize()
        {
            _unitControllers[0] = _warriorController;
            _unitControllers[1] = _archerController;
        }

        public void CustomFixedUpdate() { }

        public void CustomUpdate()
        {
            if (GameStateService.CurrentState != GameState.Gameplay)
                return;

            GameLogicData.DeltaTime = Time.deltaTime;

            CalculateArmyCenter(0);
            CalculateArmyCenter(1);
            CalculateCenterOfArmies();

            // todo: Parallel For appears to be slower than single-threaded execution
            // todo: to check if splitting the execution into very small amount of tasks (f.e. 2 or 4) would be beneficial 

            // todo: in the future use parallel //Parallel.For(0, 2, armyId =>

            // we go per army now (and then maybe per type)
            for (int armyId = 0; armyId < 2; armyId++)
            {
                ref MemoryLayoutModel layout = ref GameLogicData.MemoryLayout[armyId];
                Span<UnitModel> allies = CoreData.Units.AsSpan(layout.AllyIndex, layout.AllyLength);
                Span<UnitModel> enemies1 = CoreData.Units.AsSpan(layout.EnemyIndex1, layout.EnemyLength1);

                Span<UnitModel> enemies2 = default;
                // true if enemy are stored in a continues block of memory
                if (layout.EnemyIndex2 != int.MinValue)
                    enemies2 = CoreData.Units.AsSpan(layout.EnemyIndex2, layout.EnemyLength2);

                //Parallel.For(0, CoreData.Units.Length, unitId =>
                for (int unitId = layout.AllyIndex; unitId < layout.AllyIndex + layout.AllyLength; unitId++)
                {
                    ref UnitModel unit = ref CoreData.Units[unitId];

                    if (unit.Health <= 0) // todo: we could technically sort the array so that health units are always next to each other
                        return;

                    MoveTowardCenter(unitId);
                    PushAwayFromAllies(allies, unitId);
                    PushAwayFromEnemiesAndFindNearest(enemies1, unitId);

                    // true if enemy are stored in a continues block of memory
                    if (layout.EnemyIndex2 != int.MinValue)
                        PushAwayFromEnemiesAndFindNearest(enemies2, unitId);

                    unit.AttackCooldown -= GameLogicData.DeltaTime;

                    // === works up to this point ===

                    // update behaviours
                    // todo: should be cached
                    _unitControllers[unit.UnitType].GetBehavior(Strategy.Basic)(unitId); // todo: this breaks
                }
            }
        }

        public void CustomLateUpdate() { }

        static void CalculateArmyCenter(int armyId)
        {
            int unitCount = 0;
            MemoryLayoutModel memory = GameLogicData.MemoryLayout[armyId];
            float2 sum = float2.zero;
            for (int i = memory.AllyIndex; i < memory.AllyIndex + memory.AllyLength; i++)
            {
                ref UnitModel unit = ref CoreData.Units[i];

                // skip dead units
                if (unit.Health <= 0)
                    continue;

                unitCount++;
                sum += CoreData.UnitCurrPos[i];
            }

            CoreData.ArmyCenters[armyId] = sum / unitCount;
        }

        static void CalculateCenterOfArmies()
        {
            float2 sum = float2.zero;

            for (int i = 0; i < 2; i++)
                sum += CoreData.ArmyCenters[i];

            CoreData.CenterOfArmies = sum / 2;
        }

        static void MoveTowardCenter(int unitId)
        {
            float2 currPos = CoreData.UnitCurrPos[unitId];
            float distanceToCenter = math.distance(currPos, CoreData.CenterOfArmies);

            if (distanceToCenter <= 80.0f)
                return;

            float2 normal = math.normalize(CoreData.CenterOfArmies - currPos);
            CoreData.UnitCurrPos[unitId] -= normal * (80.0f - distanceToCenter);
        }

        /// <summary>
        /// Pushes all units (regardless of their army membership) away from each other.
        /// </summary>
        static void PushAwayFromAllies(Span<UnitModel> allies, int unitId)
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

                // comparing with yourself would result in NaN
                float2 difference = otherUnitPos - currPos;
                // ReSharper disable once InvertIf
                if (math.any(difference))
                {
                    float2 normal = math.normalize(difference);
                    posDelta -= normal * (2.0f - distance); // todo: maybe we can move the normal calculation to later
                }
            }

            CoreData.UnitCurrPos[unitId] -= posDelta;
        }

        static void PushAwayFromEnemiesAndFindNearest(Span<UnitModel> enemies, int unitId)
        {
            float2 currPos = CoreData.UnitCurrPos[unitId];

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

                float2 difference = enemyCurrPos - currPos;
                if (math.any(difference))
                {
                    float2 normal = math.normalize(difference);
                    CoreData.UnitCurrPos[unitId] -= normal * (2.0f - distance);
                }
            }

            // todo: additional check if not overwriting in case of more than 2 armies
            CoreData.Units[unitId].NearestEnemyId = nearestEnemyId;
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

                enemyModel.LastIncomingAttackDirection = proPos - enemyPos;
                enemyModel.HealthDelta -= model.Attack - 6; // todo: attack should also be taken from the shared data
                //enemyModel.HealthDelta -= model.Attack - enemyArmy.SharedUnitData[enemyModel.UnitType].Defense; // todo: take from shared data
            }
        }
    }
}