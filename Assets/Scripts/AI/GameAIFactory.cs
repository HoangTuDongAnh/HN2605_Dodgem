using UnityEngine;

/// <summary>
/// Tao AI phu hop theo loai bot va depth hien tai.
/// </summary>
public static class GameAIFactory
{
    #region Public API

    /// <summary>
    /// Tao instance AI tu du lieu player.
    /// </summary>
    public static IGameAI Create(PlayerData player)
    {
        if (player == null || player.type != PlayerType.Bot)
            return null;

        int depth = Mathf.Max(1, player.botDepth);

        if (depth <= 2)
            return new MinimaxAI(depth, player.playerIndex);

        return new AlphaBetaAI(depth, player.playerIndex);
    }

    /// <summary>
    /// Chuyen bot depth sang nhan difficulty hien thi.
    /// </summary>
    public static string DifficultyLabel(int depth)
    {
        if (depth <= 2) return "Easy";
        if (depth <= 4) return "Medium";
        return "Hard";
    }

    #endregion
}