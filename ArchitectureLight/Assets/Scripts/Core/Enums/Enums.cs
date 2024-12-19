#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Core.Enums
{
    public enum GameState
    {
        Booting,
        MainMenu,
        Gameplay
    }

    public enum StateTransitionParameter { }

    public enum Level
    {
        HubLocation = 4,
        Level0 = 5
    }

    public enum PopupType
    {
        /// <summary>
        /// Settings accessible from the main menu.
        /// </summary>
        Settings
    }
}