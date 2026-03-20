public interface IGameAI
{
    GameState BestMove(GameState state);
    string DisplayName { get; }
}