namespace VoxelRPG.Core
{
    /// <summary>
    /// Represents the current state of the game.
    /// </summary>
    public enum GameState
    {
        /// <summary>Game is initializing.</summary>
        Initializing,

        /// <summary>Main menu is displayed.</summary>
        MainMenu,

        /// <summary>Game is actively being played.</summary>
        Playing,

        /// <summary>Game is paused.</summary>
        Paused,

        /// <summary>Game is loading.</summary>
        Loading,

        /// <summary>Game is saving.</summary>
        Saving
    }
}
