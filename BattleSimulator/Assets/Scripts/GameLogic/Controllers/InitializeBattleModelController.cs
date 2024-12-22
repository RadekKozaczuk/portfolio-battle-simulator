#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Models;
using GameLogic.Models;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
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

        internal void InitializeDataModel(List<ArmyModel> armies, Bounds leftSpawn, Bounds rightSpawn)
        {
            Assert.IsTrue(armies.Count >= 2, "There must be at least two armies for the simulation to happen.");

            // todo: for now here
            CoreData.UnitStats = new[]
            {
                new UnitStatsModel(50, 5, 20, 2.5f, 1f, 0f, 0.1f, 20f),
                new UnitStatsModel(5, 0, 10, 20f, 5f, 1f, 0.1f, 20f)
            };

            int totalUnitCount = armies.Sum(army => army.UnitCount);
            CoreData.ArmyCenters = new float2[armies.Count];
            CreateNativeArrays(totalUnitCount);
            CreateMemoryLayout(armies, totalUnitCount);
            CreateUnitModels(armies, leftSpawn, rightSpawn);

            Signals.BattleModelCreated();
        }

        internal MemoryLayoutModel[] CreateMemoryLayoutV2(List<ArmyModel> armies)
        {
            int totalUnitCount = armies.Sum(army => army.UnitCount);

            // first and last memory elements
            var memory = new MemoryLayoutModel[armies.Count];
            int firstArmyCount = armies[0].UnitCount;
            memory[0] = new MemoryLayoutModel(
                0,
                firstArmyCount,
                firstArmyCount,
                totalUnitCount - firstArmyCount);

            int lastArmyCount = armies[^1].UnitCount;
            memory[armies.Count - 1] = new MemoryLayoutModel(
                totalUnitCount - lastArmyCount,
                lastArmyCount,
                0,
                totalUnitCount - lastArmyCount);

            // middle memory elements
            int ongoingTotal = 0;
            for (int i = 1; i < armies.Count - 2; i++)
            {
                ArmyModel army = armies[i];
                ongoingTotal += armies[i - 1].UnitCount;
                memory[i] = new MemoryLayoutModel(
                    0,
                    ongoingTotal,
                    ongoingTotal + army.UnitCount,
                    totalUnitCount,
                    0,
                    0);
            }

            return memory;
        }

        static void CreateNativeArrays(int totalUnitCount)
        {
            CoreData.UnitCurrPos = new NativeArray<float2>(totalUnitCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            CoreData.AttackingEnemyPos = new NativeArray<float2>(totalUnitCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < CoreData.AttackingEnemyPos.Length; i++)
                CoreData.AttackingEnemyPos[i] = new float2(float.MinValue, float.MinValue);

            CoreData.Units = new UnitModel[totalUnitCount];
            CoreData.Projectiles = new ProjectileModel[10]; // initially 10, does upscale when needed
        }

        static void CreateMemoryLayout(List<ArmyModel> armies, int totalUnitCount)
        {
            // first and last memory elements
            GameLogicData.MemoryLayout = new MemoryLayoutModel[armies.Count];
            int firstArmyCount = armies[0].UnitCount;
            GameLogicData.MemoryLayout[0] = new MemoryLayoutModel(
                0,
                firstArmyCount,
                firstArmyCount,
                totalUnitCount - firstArmyCount);

            int lastArmyCount = armies[^1].UnitCount;
            GameLogicData.MemoryLayout[armies.Count - 1] = new MemoryLayoutModel(
                totalUnitCount - lastArmyCount,
                lastArmyCount,
                0,
                totalUnitCount - lastArmyCount);

            // middle memory elements
            int ongoingTotal = 0;
            for (int i = 1; i < armies.Count - 2; i++)
            {
                ArmyModel army = armies[i];
                ongoingTotal += armies[i - 1].UnitCount;
                GameLogicData.MemoryLayout[i] = new MemoryLayoutModel(
                    0,
                    ongoingTotal,
                    ongoingTotal + army.UnitCount,
                    totalUnitCount,
                    0,
                    0);
            }
        }

        void CreateUnitModels(List<ArmyModel> armies, Bounds leftSpawn, Bounds rightSpawn)
        {
            int index = 0;
            for (int i = 0; i < armies.Count; i++)
            {
                Bounds bounds = i == 0 ? leftSpawn : rightSpawn;

                // todo: merge these two
                for (int j = 0; j < armies[i].Warriors; j++)
                {
                    float2 pos = GetRandomPosInBounds(bounds);

                    CoreData.Units[index] = new UnitModel(index, 0, i, 50); // todo: retrieve health
                    CoreData.UnitCurrPos[index] = pos;
                    index++;
                }

                for (int j = 0; j < armies[i].Archers; j++)
                {
                    float2 pos = GetRandomPosInBounds(bounds);

                    CoreData.Units[index] = new UnitModel(index, 1, i, 50); // todo: retrieve health
                    CoreData.UnitCurrPos[index] = pos;
                    index++;
                }
            }
        }

        float2 GetRandomPosInBounds(Bounds bounds) =>
            new(_random.NextFloat(bounds.min.x, bounds.max.x),
                _random.NextFloat(bounds.min.z, bounds.max.z));
    }
}