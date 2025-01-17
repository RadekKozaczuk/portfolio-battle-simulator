#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Core.Enums;
using Core.Models;
using Core.Services;
using GameLogic.ViewModels;
using Presentation.ViewModels;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Views
{
    [DisallowMultipleComponent]
    class RightPanelView : MonoBehaviour
    {
        [SerializeField]
        Button _start;

        [SerializeField]
        Button _quit;

        void Awake()
        {
            _start.onClick.AddListener(StartAction);
            _quit.onClick.AddListener(QuitAction);
        }

        static void StartAction()
        {
            LeftPanelView leftPanel = MainMenuSceneReferenceHolder.LeftPanel;
            List<ArmyModel> armies = leftPanel.Armies;
            Bounds[] spawnZones = PresentationViewModel.GetSpawnBounds();

            GameLogicViewModel.InitializeBattle(armies, spawnZones);
            PresentationViewModel.InstantiateUnits(armies);
            GameStateService.ChangeState(GameState.Gameplay);
        }

        static void QuitAction()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}