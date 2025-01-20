#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using Presentation.ViewModels;
using GameLogic.Controllers;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Core;
using Core.Enums;
using Core.Models;
using Core.Services;
using GameLogic.Interfaces;
using GameLogic.Models;
using UnityEngine;

namespace GameLogic.ViewModels
{
    [UsedImplicitly]
    public class GameLogicViewModel
    {
        [Inject]
        static readonly GameLogicMainController _mainController;

        [Preserve]
        GameLogicViewModel() { }

        public static void CustomUpdate()
        {
            if (GameStateService.CurrentState == GameState.Gameplay)
                _mainController.CustomUpdate();

            PresentationViewModel.CustomUpdate();
        }

        public static void BootingOnExit() { }

        public static void MainMenuOnEntry() { }

        public static void MainMenuOnExit() { }

        public static void GameplayOnEntry() { }

        public static void GameplayOnExit() { }

        public static void InitializeBattle(List<ArmyModel> armies, Bounds[] spawnZones)
        {
            IBattleModel model = new BattleModel(armies, spawnZones);
            DependencyInjectionService<ScriptableObject>.BindModel<IBattleModel>(model);
        }

        public static void BindControllers()
        {
            DependencyInjectionService<ScriptableObject>.BindInterface<IUnitController>(typeof(WarriorController));
            DependencyInjectionService<ScriptableObject>.BindInterface<IUnitController>(typeof(ArcherController));
        }
    }
}