#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using Presentation.ViewModels;
using GameLogic.Controllers;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Core;
using Core.Models;
using Unity.Collections;
using Unity.Mathematics;

namespace GameLogic.ViewModels
{
    [UsedImplicitly]
    public class GameLogicViewModel
    {
        [Inject]
        static readonly GameLogicMainController _gameLogicMainController;

        [Preserve]
        GameLogicViewModel() { }

        public static void CustomUpdate()
        {
            _gameLogicMainController.CustomUpdate();
            PresentationViewModel.CustomUpdate();
        }

        public static void CustomFixedUpdate() => _gameLogicMainController.CustomFixedUpdate();

        public static void CustomLateUpdate() => _gameLogicMainController.CustomLateUpdate();

        public static void BootingOnExit() { }

        public static void MainMenuOnEntry() { }

        public static void MainMenuOnExit() { }

        public static void GameplayOnEntry() { }

        public static void GameplayOnExit() { }

        /// <summary>
        /// If the instance hosted a lobby, the lobby will be deleted.
        /// </summary>
        public static void QuitGame() { }

        public static void WinMission() => Signals.MissionComplete();

        public static void FailMission() => Signals.MissionFailed();

        public static void InitializeBattleModel(List<ArmyData> armies)
        {
            int unitCount = 0;

            foreach (ArmyData army in armies)
            {
                unitCount += army.Warriors;
                unitCount += army.Archers;
            }

            CoreData.UnitCurrPos = new NativeArray<float2>(unitCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            CoreData.AttackingEnemyPos = new NativeArray<float2>(unitCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // initially size = 10, upscales when needed
            CoreData.ProjectileCurrPos = new NativeArray<float2>(10, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
    }
}