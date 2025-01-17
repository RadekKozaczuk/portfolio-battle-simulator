#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core;
using Core.Models;
using GameLogic.Config;
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

        static readonly UnitStatsConfig _config;

        [Preserve]
        internal InitializeBattleModelController() { }

        internal void InitializeModel(IBattleModel battleModel, Bounds[] spawnZones)
        {
            // todo: in the future add GetUnitCount
            Span<UnitModel> units = battleModel.GetUnits();

            CreateNativeArrays(units.Length);
            CreateUnitModels(2, battleModel, spawnZones);

            // apply health
            for (int armyId = 0; armyId < 2; armyId++)
                for (int unitType = 0; unitType < 2; unitType++)
                {
                    int health = _config.UnitData[unitType].Health;
                    Span<UnitModel> span = battleModel.GetUnits(armyId, unitType);

                    for (int i = 0; i < span.Length; i++)
                        span[i].Health = health;
                }
        }

        static void CreateNativeArrays(int totalUnitCount)
        {
            CoreData.UnitCurrPos = new NativeArray<float2>(totalUnitCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            CoreData.AttackingEnemyPos = new NativeArray<float2>(totalUnitCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < CoreData.AttackingEnemyPos.Length; i++)
                CoreData.AttackingEnemyPos[i] = new float2(float.MinValue, float.MinValue);
        }

        void CreateUnitModels(int armyCount, IBattleModel battleModel, Bounds[] spawnZones)
        {
            int index = 0;

            for (int armyId = 0; armyId < armyCount; armyId++)
            {
                Bounds bounds = spawnZones[armyId];
                Span<UnitModel> units = battleModel.GetUnits(armyId);

                for (int i = 0; i < units.Length; i++)
                {
                    CoreData.UnitCurrPos[index] = GetRandomPosInBounds(bounds);
                    index++;
                }
            }
        }

        float2 GetRandomPosInBounds(Bounds bounds) =>
            new(_random.NextFloat(bounds.min.x, bounds.max.x),
                _random.NextFloat(bounds.min.z, bounds.max.z));
    }
}