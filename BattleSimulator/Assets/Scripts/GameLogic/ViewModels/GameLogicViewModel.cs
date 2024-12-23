#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using Presentation.ViewModels;
using GameLogic.Controllers;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Core;
using Core.Models;
using UnityEngine;

namespace GameLogic.ViewModels
{
    [UsedImplicitly]
    public class GameLogicViewModel
    {
        [Inject]
        static readonly GameLogicMainController _mainController;

        [Inject]
        static readonly InitializeBattleModelController _battleModelController;

        [Preserve]
        GameLogicViewModel() { }

        public static void CustomUpdate()
        {
            _mainController.CustomUpdate();
            PresentationViewModel.CustomUpdate();
        }

        public static void BootingOnExit() { }

        public static void MainMenuOnEntry() { }

        public static void MainMenuOnExit() { }

        public static void GameplayOnEntry() { }

        public static void GameplayOnExit() { }

        /// <summary>
        /// If the instance hosted a lobby, the lobby will be deleted.
        /// </summary>
        public static void QuitGame() { }

        public static void InitializeBattleModel(List<ArmyModel> armies, Bounds leftSpawn, Bounds rightSpawn) =>
            _battleModelController.InitializeDataModel(armies, leftSpawn, rightSpawn);
    }
}