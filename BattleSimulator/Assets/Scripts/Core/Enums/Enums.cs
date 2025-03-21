#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace Core.Enums
{
    public enum GameState
    {
        Booting,
        MainMenu,
        Gameplay
    }

    public enum PopupType
    {
        /// <summary>
        /// Settings accessible from the main menu.
        /// </summary>
        Settings,
        Victory
    }

    public enum UnitType
    {
        Archer = 0,
        Warrior = 1
    }

    /// <summary>
    /// Strategy can be applied on a per-unit basis. Must be an enum as adding new without recompiling the code would not be easily achievable.
    /// </summary>
    public enum Strategy
    {
        Basic = 0,
        Defensive = 1
    }
}