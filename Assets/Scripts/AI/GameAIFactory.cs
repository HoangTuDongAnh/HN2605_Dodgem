using UnityEngine;

// ================================================================
// GameAIFactory
// Quy uoc do kho:
// - depth <= 2  => MinimaxAI (Easy)
// - depth <= 4  => AlphaBetaAI (Medium)
// - depth >= 6  => AlphaBetaAI (Hard)
// ================================================================
public static class GameAIFactory
{
    public static IGameAI Create(PlayerData player)
    {
        if (player == null || player.type != PlayerType.Bot)
            return null;

        int depth = Mathf.Max(1, player.botDepth);

        if (depth <= 2)
            return new MinimaxAI(depth, player.playerIndex);

        return new AlphaBetaAI(depth, player.playerIndex);
    }

    public static string DifficultyLabel(int depth)
    {
        if (depth <= 2) return "Easy";
        if (depth <= 4) return "Medium";
        return "Hard";
    }
}