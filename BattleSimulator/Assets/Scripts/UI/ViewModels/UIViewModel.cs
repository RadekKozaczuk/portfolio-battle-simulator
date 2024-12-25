#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core;
using Core.Interfaces;
using JetBrains.Annotations;
using UI.Config;
using UI.Controllers;
using UI.Popups;
using UI.Services;
using UnityEngine.Scripting;

namespace UI.ViewModels
{
    [UsedImplicitly]
    public class UIViewModel : IInitializable
    {
        static readonly UIConfig _uiConfig;

        [Inject]
        static readonly UIMainController _uiMainController;

        [Preserve]
        UIViewModel() { }

        public void Initialize() => InputService.Initialize();

        public static void CustomUpdate() => _uiMainController.CustomUpdate();

        public static void OnUISceneLoaded() => UIMainController.OnUISceneLoaded();

        public static void BootingOnExit() { }

        public static void MainMenuOnEntry() { }

        public static void MainMenuOnExit() { }

        public static void GameplayOnEntry()
        {
            _uiConfig.InputActionAsset.FindActionMap(UIConstants.PlayerActionMap).Enable();

            // this happens only when we start a client game starts
            if (PopupSystem.CurrentPopup != null)
                PopupSystem.CloseCurrentPopup();
        }

        public static void GameplayOnExit() =>
            _uiConfig.InputActionAsset.FindActionMap(UIConstants.PlayerActionMap).Disable();
    }
}