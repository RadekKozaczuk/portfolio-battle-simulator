#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core.Enums;

// ReSharper disable InvalidXmlDocComment

namespace Core.Services
{
    public delegate void ChangeState(GameState requested, int[]? additionalScenesToLoad = null,
        int[]? additionalScenesToUnload = null, (StateTransitionParameter key, object value)[]? parameters = null,
        int[]? scenesToSynchronize = null);

    public delegate GameState GetCurrentGameState();
    public delegate void EndFrameSignal();
    public delegate object? GetTransitionParameter(StateTransitionParameter key);

    public static class GameStateService
    {
        public static event ChangeState OnChangeState = null!;
        public static event GetCurrentGameState OnGetCurrentGameState = null!;
        public static event EndFrameSignal OnEndFrameSignal = null!;
        public static event GetTransitionParameter OnGetTransitionParameter = null!;

        public static GameState CurrentState => OnGetCurrentGameState.Invoke();

        /// <summary>
        /// Scenes to load and unload are defined in <see cref="GameStateMachine{TState,TTransitionParameter}" />'s constructor.
        /// Additional scenes defined here are special cases that does not occur all the time and therefore could not be defined in the constructor.
        /// These scenes should not overlap with the ones defined in the GameStateMachine's constructor.
        /// Actual state change may be delayed in time. Consecutive calls are not allowed.
        /// </summary>
        public static void ChangeState(GameState state, int[]? additionalScenesToLoad = null,
            int[]? additionalScenesToUnload = null, (StateTransitionParameter key, object value)[]? parameters = null,
            int[]? scenesToSynchronize = null) =>
            OnChangeState.Invoke(state, additionalScenesToLoad, additionalScenesToUnload, parameters, scenesToSynchronize);

        /// <summary>
        /// Simplified version of <see cref="Systems.ChangeState"/>.
        /// </summary>
        public static void ChangeState(GameState state, (StateTransitionParameter key, bool value) parameter) =>
            OnChangeState.Invoke(state, null, null, new []{(parameter.key, (object)parameter.value)});

        public static void SendEndFrameSignal() => OnEndFrameSignal.Invoke();

        /// <summary>
        /// Returns the value of the given parameters if present, otherwise default.
        /// Meaning this method will return null for reference types, and default for value types.
        /// The parameter must be present otherwise method will throw an exception.
        /// </summary>
        public static object? GetTransitionParameter(StateTransitionParameter key) => OnGetTransitionParameter.Invoke(key);
    }
}