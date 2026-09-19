namespace FarmFuryStampede.Data
{
    /// <summary>Top-level application/game flow states tracked by GameManager.</summary>
    public enum GameState
    {
        MainMenu,
        WorldSelect,
        LevelSelect,
        CharacterSelect,
        Playing,
        LevelComplete,
        LevelFailed,
        Paused
    }
}
