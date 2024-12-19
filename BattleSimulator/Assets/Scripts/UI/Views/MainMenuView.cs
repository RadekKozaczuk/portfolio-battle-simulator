#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine.UI;
using UnityEngine;
using Core.Enums;
using UI.Popups;
using Core;
using Core.Services;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Views
{
    [DisallowMultipleComponent]
    class MainMenuView : MonoBehaviour
    {
        [SerializeField]
        Button _newGame;

        [SerializeField]
        Button _settings;

        [SerializeField]
        Button _quit;

        void Awake()
        {
            _newGame.onClick.AddListener(NewGame);
            _settings.onClick.AddListener(Settings);
            _quit.onClick.AddListener(Quit);
        }

        static void NewGame()
        {
            CoreData.CurrentLevel = Level.HubLocation;
            GameStateService.ChangeState(GameState.Gameplay, new[] {(int)CoreData.CurrentLevel});
        }

        static void Settings()
        {
            PopupSystem.ShowPopup(PopupType.Settings);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}