#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Core.Services
{
    public class StateService<TState> : AbstractStateService<TState>
        where TState : struct, Enum
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// If true then <see cref="ChangeState"/> was called, and it is still on going.
        /// Calling <see cref="ChangeState"/> at this point is not allowed.
        /// </summary>
        bool _transitioning;
        readonly bool _logRequestedStateChange;
#endif

        /// <summary>
        /// Can only be changed to true if machine is preloading.
        /// </summary>
        bool _finalizeTransitionRequest;

        // used to remember what state preload wanted to go
        // null otherwise
        TransitionDto? _transition;
        int[]? _additionalScenesToUnload;

		public StateService(
            IReadOnlyList<(TState from, TState to, Func<(int[]?, int[]?)>? scenesToLoadUnload)> transitions,
            IReadOnlyList<(TState state, Action? onEntry, Action? onExit)> states
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			, bool logRequestedStateChange = false
#endif
            )
			: base(transitions, states)
        {

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _logRequestedStateChange = logRequestedStateChange;
#endif
        }

        /// <summary>
        /// Actual state change may be delayed in time. Consecutive calls are not allowed.
        /// Additional scenes, whether to-load or to-unload, must not collide with the scenes defined in the constructor.
        /// </summary>
        /// <param name="state">State we transition to</param>
        /// <param name="additionalScenesToLoad">Additional scenes (not defined in the transition) to load during</param>
        /// <param name="additionalScenesToUnload"></param>
        /// <param name="scenesToSynchronize">Scenes listed here will have their root objects disabled immediately after scene load
        /// (so right after Awake call) and then enabled again on state OnEntry.</param>
        /// <exception cref="Exception"></exception>
        public async void ChangeState(TState state, int[]? additionalScenesToLoad = null,
            int[]? additionalScenesToUnload = null, int[]? scenesToSynchronize = null)
        {
            List<TransitionDto> transitions = _transitions.FindAll(t => Equal(t.From, _currentState) && Equal(t.To, state));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (transitions.Count > 1)
                throw new Exception($"Transition from {_currentState} to {state} is defined more than once.");
            if (transitions.Count == 0)
                throw new Exception($"Transition from {_currentState} to {state} is not defined.");
            if (_transitioning)
                throw new Exception("Game State machine is already transitioning to a different state. Consecutive calls are not allowed.");

            _transitioning = true;
#endif

            TransitionDto transition = transitions[0];
            (int[]? scenesToLoad, int[]? scenesToUnload)? scenesToLoadUnload = transition.ScenesToLoadUnload?.Invoke();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_logRequestedStateChange)
            {
                GetSceneLog(scenesToLoadUnload?.scenesToLoad, additionalScenesToLoad, out string toLoad);
                GetSceneLog(scenesToLoadUnload?.scenesToUnload, additionalScenesToUnload, out string toUnload);

                Debug.Log($"DEBUG LOG: Requested game state change from {_currentState} to {state}. "
                          + $"Scenes to load: {toLoad}. Scenes to unload: {toUnload}.");
            }

            _transitioning = true;

            if (scenesToLoadUnload != null)
            {
                Assert.IsFalse(Utils.HasDuplicates(CombineArrays(scenesToLoadUnload.Value.scenesToLoad, additionalScenesToLoad)),
                               "GameStateMachine was asked to load the same scene more than once.");
                Assert.IsFalse(Utils.HasDuplicates(CombineArrays(scenesToLoadUnload.Value.scenesToUnload, additionalScenesToUnload)),
                               "GameStateMachine was asked to unload the same scene more than once.");
            }
#endif 

            // execute state's on-exit code
            _states.TryGetValue(transition.From, out StateDto fromState);
            fromState.OnExit?.Invoke();

            if (scenesToLoadUnload != null)
                if (scenesToLoadUnload.Value.scenesToLoad is {Length: > 0} || additionalScenesToLoad is {Length: > 0})
                    await LoadScenes_NormalSimultaneous(CombineArrays(scenesToLoadUnload.Value.scenesToLoad, additionalScenesToLoad));

            // change state
            _currentState = state;

            // execute state's on-entry code
            _states.TryGetValue(transition.To, out StateDto toState);

            if (scenesToLoadUnload != null)
                if (scenesToLoadUnload.Value.scenesToUnload is { Length: > 0 } || additionalScenesToUnload is { Length: > 0})
                    UnloadScenes(CombineArrays(scenesToLoadUnload.Value.scenesToUnload, additionalScenesToUnload));

            // actual end of the transition
            toState.OnEntry?.Invoke();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_logRequestedStateChange)
                Debug.Log($"DEBUG LOG: GameStateSystem: State changed from {transition.From} to {transition.To}");

            _transitioning = false;
#endif
        }

        /// <summary>
        /// Scenes are loaded normally and all at once.
        /// </summary>
        static async Awaitable LoadScenes_NormalSimultaneous(params int[] scenes)
        {
            var asyncOperations = new AsyncOperation[scenes.Length];
            asyncOperations[0] = SceneManager.LoadSceneAsync(scenes[0], LoadSceneMode.Additive)!;

            for (int i = 1; i < scenes.Length; i++)
                asyncOperations[i] = SceneManager.LoadSceneAsync(scenes[i], LoadSceneMode.Additive)!;

            await AwaitAsyncOperations(asyncOperations);

            // wait a frame so every Awake and Start method is called
            await Awaitable.NextFrameAsync();
        }

        static void UnloadScenes(params int[] scenes)
        {
            // unload scenes shoot and forger
            foreach (int scene in scenes)
                SceneManager.UnloadSceneAsync(scene);
        }

        /// <summary>
        /// Waits until all operations are either done or have progress greater equal 0.9.
        /// </summary>
        static async Awaitable AwaitAsyncOperations(params AsyncOperation[] operations)
        {
            while (!operations.All(t => t.isDone))
                await Awaitable.NextFrameAsync();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static void GetSceneLog(int[]? scenes, IReadOnlyCollection<int>? additionalScenes, out string log)
        {
            if (scenes == null)
            {
                log = "None";
            }
            else
            {
                log = string.Join(", ", scenes);

                // ReSharper disable once MergeIntoPattern
                if (additionalScenes != null && additionalScenes.Count > 0)
                    log += ", " + string.Join(", ", additionalScenes);
            }
        }
#endif
    }
}