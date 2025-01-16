#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using Presentation.ViewModels;
using GameLogic.Controllers;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Core;
using Core.Models;
using Core.Services;
using GameLogic.Interfaces;
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
            _mainController.CustomUpdate();
            PresentationViewModel.CustomUpdate();
        }

        public static void BootingOnExit() { }

        public static void MainMenuOnEntry() { }

        public static void MainMenuOnExit() { }

        public static void GameplayOnEntry() { }

        public static void GameplayOnExit() { }

        public static void InitializeBattle(List<ArmyModel> armies, Bounds[] spawnZones) =>
            _mainController.InitializeModel(armies, spawnZones);

        public static void BindControllers()
        {
            DependencyInjectionService<ScriptableObject>.BindToInterface<IUnitController>(typeof(WarriorController));
            DependencyInjectionService<ScriptableObject>.BindToInterface<IUnitController>(typeof(ArcherController));
        }
    }
}