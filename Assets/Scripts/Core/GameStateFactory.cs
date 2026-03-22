using UnityEngine;

/// <summary>
/// Tao runtime game state tu preset va player setup hien tai.
/// </summary>
public static class GameStateFactory
{
    #region Public API

    /// <summary>
    /// Tao game state tu preset va cac gia tri override.
    /// </summary>
    public static GameState CreateFromPreset(
        GamePreset preset,
        PlayerType[] overrideTypes = null,
        int[] overrideDepths = null)
    {
        if (preset == null)
        {
            Debug.LogError("[GameStateFactory] Preset is null.");
            return null;
        }

        string error;
        if (!preset.IsValid(out error))
        {
            Debug.LogError("[GameStateFactory] Invalid preset: " + error);
            return null;
        }

        int width = preset.boardWidth;
        int height = preset.boardHeight;
        int numPlayers = preset.NumPlayers;

        var players = new PlayerData[numPlayers];

        for (int i = 0; i < numPlayers; i++)
        {
            var cfg = preset.GetConfigSafe(i);
            var startPositions = preset.GetStartPositions(i);

            PlayerType playerType =
                (overrideTypes != null && i < overrideTypes.Length)
                    ? overrideTypes[i]
                    : cfg.type;

            int botDepth =
                (overrideDepths != null && i < overrideDepths.Length)
                    ? overrideDepths[i]
                    : cfg.botDepth;

            players[i] = new PlayerData
            {
                playerIndex = i,
                playerName = cfg.playerName,
                pieceColor = cfg.pieceColor,
                escapeDir = cfg.escapeDir,
                type = playerType,
                botDepth = botDepth,
                pieces = startPositions,
                escaped = 0,
                exitCells = preset.GetExitCellsSafe(i)
            };
        }

        return new GameState
        {
            boardWidth = width,
            boardHeight = height,
            players = players,
            currentPlayerIndex = 0,
            validCells = preset.BuildValidCellMap()
        };
    }

    #endregion
}