public interface IGameAI
{
    /// <summary>
    /// Tim nuoc di tot nhat, co tinh den lich su state de tranh lap.
    /// </summary>
    GameState BestMove(GameState state, System.Collections.Generic.Dictionary<string, int> stateHistory);

    /// <summary>
    /// Fallback khong co history (cho AI noi bo / snapshot).
    /// </summary>
    GameState BestMove(GameState state);

    string DisplayName { get; }
}