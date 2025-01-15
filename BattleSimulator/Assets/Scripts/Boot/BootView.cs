#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Presentation.ViewModels;
using GameLogic.ViewModels;
using Core.Services;
using UI.ViewModels;
using UnityEngine;
using Core.Enums;
using Core;
using System;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Core.Config;
#endif

namespace Boot
{
    /// <summary>
    /// Contains all the high-level logic that cannot be executed from within <see cref="GameLogic" /> namespace.
    /// </summary>
    [DisallowMultipleComponent]
    class BootView : MonoBehaviour
    {
        [SerializeField]
        EventSystem _eventSystem;

        static bool _isCoreSceneLoaded;
        static StateService<GameState> _stateMachine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // readonly fields are initialized only at the start and the null-forgiving operator is only a hint for the compiler.
        // Ultimately it will be null when readonly unless set differently.
        static readonly DebugConfig _config;
#endif

        void Awake()
        {
            // increase priority so that main menu can appear faster
            Application.backgroundLoadingPriority = ThreadPriority.High;

            ArchitectureService.InitializeA();

            // must be after the above as it is depended on SignalService
            GameLogicViewModel.BindControllers();

            // injection must be done in awake because fields cannot be injected into in the same method they are used in
            // start will be at least 1 frame later than Awake.
            ArchitectureService.InitializeB();
            ArchitectureService.ResolveBinding();
        }

        void Start()
        {
            SceneManager.sceneLoaded += (scene, _) =>
            {
                if (scene.buildIndex == Constants.CoreScene)
                {
                    SceneManager.UnloadSceneAsync(Constants.BootScene);
                    _isCoreSceneLoaded = true;
                }

                if (scene.buildIndex == Constants.UIScene)
                    UIViewModel.OnUISceneLoaded();
            };

            ArchitectureService.InvokeInitialization();
            _stateMachine = CreateStateMachine();

            GameStateService.OnChangeState += _stateMachine.ChangeState;
            GameStateService.OnGetCurrentGameState += _stateMachine.GetCurrentState;

            GameStateService.ChangeState(GameState.MainMenu);

            DontDestroyOnLoad(_eventSystem);
            DontDestroyOnLoad(this);

            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.ScriptOnly);
            Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
		}

        void Update()
        {
            if (GameStateService.CurrentState == GameState.Booting)
                return;

            if (_isCoreSceneLoaded)
            {
                GameLogicViewModel.CustomUpdate();
                PresentationViewModel.CustomUpdate();
                UIViewModel.CustomUpdate();

                ArchitectureService.ExecuteSentSignals();
            }
        }

        static StateService<GameState> CreateStateMachine() =>
            new(new List<(
                    GameState from,
                    GameState to,
                    Func<(int[]?, int[]?)>? scenesToLoadUnload)>
                {
                    (GameState.Booting,
                     GameState.MainMenu,
                     () => (new[] {Constants.MainMenuScene, Constants.CoreScene, Constants.UIScene}, null)),
                    (GameState.MainMenu,
                     GameState.Gameplay,
                     () => (null, new[] {Constants.MainMenuScene})),
                    (GameState.Gameplay,
                     GameState.MainMenu,
                     () => (new[] {Constants.MainMenuScene}, ScenesToUnloadFromGameplayToMainMenu()))
                },
                new (GameState, Action?, Action?)[]
                {
                    (GameState.Booting, null, BootingOnExit),
                    (GameState.MainMenu, MainMenuOnEntry, MainMenuOnExit),
                    (GameState.Gameplay, GameplayOnEntry, GameplayOnExit)
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                // ReSharper disable once MergeConditionalExpression
                // ReSharper disable once SimplifyConditionalTernaryExpression
                , _config is null ? false : _config.LogRequestedStateChange);
#else
            );
#endif

        static void BootingOnExit()
        {
            Application.backgroundLoadingPriority = ThreadPriority.Normal;

            GameLogicViewModel.BootingOnExit();
            PresentationViewModel.BootingOnExit();
            UIViewModel.BootingOnExit();
        }

        static void MainMenuOnEntry()
        {
            GameLogicViewModel.MainMenuOnEntry();
            PresentationViewModel.MainMenuOnEntry();
            UIViewModel.MainMenuOnEntry();
        }

        static void MainMenuOnExit()
        {
            GameLogicViewModel.MainMenuOnExit();
            PresentationViewModel.MainMenuOnExit();
            UIViewModel.MainMenuOnExit();
        }

        static void GameplayOnEntry()
        {
            GameLogicViewModel.GameplayOnEntry();
            PresentationViewModel.GameplayOnEntry();
            UIViewModel.GameplayOnEntry();
        }

        static void GameplayOnExit()
        {
            GameLogicViewModel.GameplayOnExit();
            PresentationViewModel.GameplayOnExit();
            UIViewModel.GameplayOnExit();
        }

        /// <summary>
        /// Returns ids of all currently open scenes except for <see cref="Constants.CoreScene" />, <see cref="Constants.MainMenuScene" />
        /// and <see cref="Constants.UIScene" />
        /// </summary>
        static int[] ScenesToUnloadFromGameplayToMainMenu()
        {
            int countLoaded = SceneManager.sceneCount;
            var scenesToUnload = new List<int>(countLoaded);

            for (int i = 0; i < countLoaded; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.buildIndex is Constants.CoreScene or Constants.MainMenuScene or Constants.UIScene)
                    continue;

                scenesToUnload.Add(scene.buildIndex);
            }

            return scenesToUnload.ToArray();
        }
    }
}