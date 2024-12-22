#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using GameLogic.ViewModels;
using UI.Config;
using UI.Popups;
using UnityEngine.InputSystem;

namespace UI.Services
{
    static class InputService
    {
        const string Cancel = "Cancel";
        const string Submit = "Submit";

        static readonly UIConfig _uiConfig;

        static InputAction _movementAction;

        internal static void Initialize()
        {
            // MainMenu bindings
            InputActionMap ui = _uiConfig.InputActionAsset.FindActionMap(UIConstants.UIActionMap);
            ui.FindAction(Cancel).performed += _ =>
            {
                // if there is a popup - close it
                // otherwise quit the game
                if (PopupSystem.CurrentPopup == null)
                {
                    GameLogicViewModel.QuitGame();

#if UNITY_EDITOR
                    UnityEditor.EditorApplication.ExitPlaymode();
#else
                    UnityEngine.Application.Quit();
#endif
                }
                else
                    PopupSystem.CloseCurrentPopup();
            };
        }

        internal static void CustomUpdate() { }
    }
}