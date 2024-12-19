#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Core.Services
{
    public class StateService<TState, TTransitionParameter> : AbstractStateService<TState, TTransitionParameter>
        where TState : struct, Enum
        where TTransitionParameter : struct, Enum
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// If true then <see cref="ChangeState"/> was called, and it is still on going.
        /// Calling <see cref="ChangeState"/> at this point is not allowed.
        /// </summary>
        bool _transitioning;
        readonly bool _logRequestedStateChange;
        int _lastEndFrameSignal = -1;
        readonly Dictionary<TTransitionParameter, (Type type, bool isDirty)> _parameterSafetyChecks = new();
#endif

        /// <summary>
        /// Null - no preloading requested.
        /// False - is preloading but not yet finished.
        /// True - finished preloading.
        /// </summary>
        bool? _finishedPreLoading;

        /// <summary>
        /// Can only be changed to true if machine is preloading.
        /// </summary>
        bool _finalizeTransitionRequest;

        // used to remember what state preload wanted to go
        // null otherwise
        TransitionDto? _transition;
        int[]? _additionalScenesToUnload;

        /// <summary>
        /// This list must be empty between request calls
        /// </summary>
        readonly List<int> _scenesToBeDisabled = new();

        /// <summary>
        /// Must be empty between request calls.
        /// </summary>
        readonly Dictionary<int, List<int>> _disabledRootsPerScene = new();

        /// <summary>
        /// 'betweenLoadAndUnload' action is the best suitable for scenarios when we need to just when scenes stopped loading but right before they start to unload.
        /// Great example would be when we go from a level to a level and the level we are leaving is going to disappear.
        /// </summary>
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

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Add transition parameter with a default value (null for reference types, default for value types).
        /// </summary>
        public void AddTransitionParameter(TTransitionParameter key, Type type)
        {
            _parameters.Add(key, type.IsValueType ? Activator.CreateInstance(type) : null);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _parameterSafetyChecks.Add(key, (type, false));
#endif
        }

        public object? GetTransitionParameter(TTransitionParameter key)
        {
            if (_parameters.TryGetValue(key, out object? value))
                return value;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            throw new Exception("No transition parameter for the given key. Please use AddTransitionParameter to add parameters.");
#endif

#pragma warning disable CS0162 // Unreachable code detected
            // ReSharper disable once HeuristicUnreachableCode
            return null;
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Actual state change may be delayed in time. Consecutive calls are not allowed.
        /// Additional scenes, whether to-load or to-unload, must not collide with the scenes defined in the constructor.
        /// </summary>
        /// <param name="state">State we transition to</param>
        /// <param name="additionalScenesToLoad">Additional scenes (not defined in the transition) to load during</param>
        /// <param name="additionalScenesToUnload"></param>
        /// <param name="parameters"></param>
        /// <param name="scenesToSynchronize">Scenes listed here will have their root objects disabled immediately after scene load
        /// (so right after Awake call) and then enabled again on state OnEntry.</param>
        /// <exception cref="Exception"></exception>
        public async void ChangeState(TState state, int[]? additionalScenesToLoad = null,
            int[]? additionalScenesToUnload = null, (TTransitionParameter key, object value)[]? parameters = null,
            int[]? scenesToSynchronize = null)
        {
            if (scenesToSynchronize != null)
                foreach (int id in scenesToSynchronize)
                    _scenesToBeDisabled.Add(id);

            UpdateParameters(parameters);
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

            EnableRootsIfAny();

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

            _shouldDefaultParameters = true;
        }

        /// <summary>
        /// Should be called at the end of the frame.
        /// Should not be called more than once per frame.
        /// </summary>
        public new void EndFrameSignal()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert.IsFalse(_lastEndFrameSignal == Time.frameCount, "EndFrameSignal should not be called more than once per frame.");
            _lastEndFrameSignal = Time.frameCount;

            // reset flags
            if (_shouldDefaultParameters)
                for (int i = 0; i < _parameterSafetyChecks.Count; i++)
                {
                    KeyValuePair<TTransitionParameter, (Type type, bool isDirty)> element = _parameterSafetyChecks.ElementAt(i);
                    _parameterSafetyChecks[element.Key] = (element.Value.type, false);
                }
#endif

            base.EndFrameSignal();
        }

        void UpdateParameters(IReadOnlyList<(TTransitionParameter key, object value)>? parameters)
        {
            if (parameters == null)
                return;

            // mark parameters as dirty && check type
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            foreach ((TTransitionParameter key, object) _ in parameters)
            {
                (TTransitionParameter key, object value) = _;
                if (_parameterSafetyChecks.TryGetValue(key, out (Type type, bool isDirty) check))
                {
                    Assert.IsFalse(check.isDirty,
                                   "Transition parameters cannot be changed more than once in a single transition. "
                                   + "Transition may last more than one frame.");
                    Assert.IsTrue(check.type == value.GetType(),
                                  "Transition parameter type must much the type provided to the machine when AddTransitionParameter was called.");
                    _parameterSafetyChecks[key] = (check.type, true);
                    continue;
                }

                Assert.IsTrue(_parameterSafetyChecks.TryGetValue(key, out (Type type, bool isDirty) _),
                              "No transition parameter for the given key. Please use AddTransitionParameter to add parameters.");
            }
#endif

            foreach ((TTransitionParameter key, object value) in parameters)
                _parameters[key] = value;
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

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_scenesToBeDisabled.Count <= 0)
                return;

            if (!_scenesToBeDisabled.Contains(scene.buildIndex))
                return;

            List<int> ids = new ();
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
                // add only if originally enabled
                if (root.activeSelf)
                {
                    root.SetActive(false);
                    ids.Add(root.GetInstanceID());
                }

            _disabledRootsPerScene.Add(scene.buildIndex, ids);
        }

        /// <summary>
        /// Enables roots previously disabled
        /// </summary>
        void EnableRootsIfAny()
        {
            // how many scenes are loaded
            int sceneCount = SceneManager.sceneCount;

            // iterate over all loaded scenes
            for (int i = 0; i < sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                // if scene is in to_be_synced list
                if (!_scenesToBeDisabled.Contains(scene.buildIndex))
                    continue;

                Debug.Log($"Enabled Roots for scene {scene.buildIndex}, frame: {Time.frameCount}");

                // get roots to be activated
                _ = _disabledRootsPerScene.TryGetValue(scene.buildIndex, out List<int> rootIds);

                // iterate over all roots
                GameObject[] roots = scene.GetRootGameObjects();
                foreach (GameObject root in roots)
                    // if previously deactivated activate it
                    if (rootIds!.Contains(root.GetInstanceID()))
                        root.SetActive(true);
            }

            _scenesToBeDisabled.Clear();
            _disabledRootsPerScene.Clear();
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