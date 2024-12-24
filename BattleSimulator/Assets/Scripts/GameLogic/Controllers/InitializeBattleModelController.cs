#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core;
using Core.Models;
using GameLogic.Interfaces;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using Random = Unity.Mathematics.Random;

namespace GameLogic.Controllers
{
    [UsedImplicitly]
    class InitializeBattleModelController
    {
        Random _random = new(123);

        [Preserve]
        internal InitializeBattleModelController() { }

        internal void InitializeModel(IBattleModel battleModel, Bounds[] spawnZones)
        {
            // todo: for now here - should be in config
            CoreData.UnitStats = new[]
            {
                new UnitStatsModel(50, 5, 20, 2.5f, 1f, 0f, 0.1f, 20f),
                new UnitStatsModel(5, 0, 10, 20f, 5f, 1f, 0.1f, 20f)
            };

            // todo: in the future add GetUnitCount
            Span<UnitModel> units = battleModel.GetUnits();

            CreateNativeArrays(units.Length);
            CreateUnitModels(2, 2, battleModel, spawnZones);

            // apply health
            for (int armyId = 0; armyId < 2; armyId++)
                for (int unitType = 0; unitType < 2; unitType++)
                {
                    int health = CoreData.UnitStats[unitType].Health;
                    Span<UnitModel> qq = battleModel.GetUnits(armyId, unitType);

                    foreach (UnitModel model in qq)
                    {
                        ref UnitModel w = ref battleModel.GetUnit(model.Id);
                        w.Health = health;
                    }
                }
        }

        static void CreateNativeArrays(int totalUnitCount)
        {
            CoreData.UnitCurrPos = new NativeArray<float2>(totalUnitCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            CoreData.AttackingEnemyPos = new NativeArray<float2>(totalUnitCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < CoreData.AttackingEnemyPos.Length; i++)
                CoreData.AttackingEnemyPos[i] = new float2(float.MinValue, float.MinValue);

            CoreData.Projectiles = new ProjectileModel[10]; // initially 10, does upscale when needed
        }

        void CreateUnitModels(int armyCount, int unitTypeCount, IBattleModel battleModel, Bounds[] spawnZones)
        {
            int index = 0;

            for (int armyId = 0; armyId < armyCount; armyId++)
            {
                Bounds bounds = spawnZones[armyId];

                for (int unitType = 0; unitType < unitTypeCount; unitType++)
                {
                    Span<UnitModel> units = battleModel.GetUnits(armyId, unitType);

                    for (int i = 0; i < units.Length; i++)
                    {
                        CoreData.UnitCurrPos[index] = GetRandomPosInBounds(bounds);
                        index++;
                    }
                }
            }
        }

        float2 GetRandomPosInBounds(Bounds bounds) =>
            new(_random.NextFloat(bounds.min.x, bounds.max.x),
                _random.NextFloat(bounds.min.z, bounds.max.z));
    }
}