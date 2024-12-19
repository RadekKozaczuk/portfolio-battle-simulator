#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Presentation.ViewModels;
using GameLogic.Controllers;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Core;

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
    }
}