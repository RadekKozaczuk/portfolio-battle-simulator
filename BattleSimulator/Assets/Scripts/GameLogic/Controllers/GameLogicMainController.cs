#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Threading.Tasks;
using Core;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
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
    class GameLogicMainController : ICustomFixedUpdate, ICustomUpdate, ICustomLateUpdate
    {
        [Preserve]
        GameLogicMainController() { }

        public void CustomFixedUpdate() { }

        public void CustomUpdate()
        {
            if (GameStateService.CurrentState != GameState.Gameplay)
                return;

            float deltaTime = Time.deltaTime;

            CalculateCenterOfArmies();

            // todo: Parallel For appears to be slower than single-threaded execution
            // todo: to check if splitting the execution into very small amount of tasks (f.e. 2 or 4) would be beneficial 
            //Parallel.For(0, CoreData.Units.Length, unitId =>
            for (int unitId = 0; unitId < CoreData.Units.Length; unitId++)
            {
                ref UnitModel unit = ref CoreData.Units[unitId];

                if (unit.Health <= 0)
                    return;

                MoveTowardCenter(unit.Id);

                ref MemoryLayoutModel layout = ref GameLogicData.MemoryLayout[unit.ArmyId];
                Span<UnitModel> allies = CoreData.Units.AsSpan(layout.AllyIndex, layout.AllyLength);
                PushAwayFromAllies(ref allies, unit.Id);

                Span<UnitModel> enemies1 = CoreData.Units.AsSpan(layout.EnemyIndex1, layout.EnemyLength1);
                PushAwayFromEnemiesAndFindNearest(enemies1, unit.Id);

                // true if enemy are stored in a continues block of memory
                if (layout.EnemyIndex2 == int.MinValue)
                    return;

                Span<UnitModel> enemies2 = CoreData.Units.AsSpan(layout.EnemyIndex2, layout.EnemyLength2);
                PushAwayFromEnemiesAndFindNearest(enemies2, unit.Id);

                unit.AttackCooldown -= deltaTime;
            }//);

            /*Parallel.For(0, CoreData.Projectiles.Length, projectileId =>
            {
                ref ProjectileModel projectile = ref CoreData.Projectiles[projectileId];
                ref MemoryLayoutModel layout = ref GameLogicData.MemoryLayout[projectile.ArmyId];

                Span<UnitModel> enemies1 = CoreData.Units.AsSpan(layout.EnemyIndex1, layout.EnemyLength1);
                UpdateProjectile(projectileId, enemies1);

                // true if enemy are stored in a continues block of memory
                if (layout.EnemyIndex2 == int.MinValue)
                    return;

                Span<UnitModel> enemies2 = CoreData.Units.AsSpan(layout.EnemyIndex2, layout.EnemyLength2);
                UpdateProjectile(projectileId, enemies2);
            });*/
        }

        public void CustomLateUpdate() { }

        static void CalculateCenterOfArmies()
        {
            int unitCount = 0;
            CoreData.CenterOfArmies = float2.zero;

            for (int i = 0; i < CoreData.UnitCurrPos.Length; i++)
            {
                // skip dead units
                if (CoreData.Units[i].Health <= 0)
                    continue;

                unitCount++;
                CoreData.CenterOfArmies += CoreData.UnitCurrPos[i];
            }

            CoreData.CenterOfArmies /= unitCount;
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
        static void PushAwayFromAllies(ref Span<UnitModel> allies, int unitId)
        {
            float2 currPos = CoreData.UnitCurrPos[unitId];

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
                if (math.any(otherUnitPos - currPos))
                {
                    float2 normal = math.normalize(difference);
                    CoreData.UnitCurrPos[unitId] -= normal * (2.0f - distance);
                }
            }
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

                float2 normal = math.normalize(enemyCurrPos - currPos);
                CoreData.UnitCurrPos[unitId] -= normal * (2.0f - distance);
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