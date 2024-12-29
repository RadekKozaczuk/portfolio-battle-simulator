#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core.Enums;

// ReSharper disable InvalidXmlDocComment

namespace Core.Services
{
    public delegate void ChangeState(GameState requested, int[]? additionalScenesToLoad = null,
        int[]? additionalScenesToUnload = null, int[]? scenesToSynchronize = null);

    public delegate GameState GetCurrentGameState();

    public static class GameStateService
    {
        public static event ChangeState OnChangeState = null!;
        public static event GetCurrentGameState OnGetCurrentGameState = null!;

        public static GameState CurrentState => OnGetCurrentGameState.Invoke();

        /// <summary>
        /// Scenes to load and unload are defined in <see cref="GameStateMachine{TState}" />'s constructor.
        /// Additional scenes defined here are special cases that does not occur all the time and therefore could not be defined in the constructor.
        /// These scenes should not overlap with the ones defined in the GameStateMachine's constructor.
        /// </summary>
        public static void ChangeState(GameState state, int[]? additionalScenesToLoad = null,
            int[]? additionalScenesToUnload = null, int[]? scenesToSynchronize = null) =>
            OnChangeState.Invoke(state, additionalScenesToLoad, additionalScenesToUnload, scenesToSynchronize);
    }
}