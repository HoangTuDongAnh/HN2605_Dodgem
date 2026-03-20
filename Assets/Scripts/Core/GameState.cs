using UnityEngine;

[System.Serializable]
public class GameState
{
    public int          boardWidth;
    public int          boardHeight;
    public PlayerData[] players;
    public int          currentPlayerIndex;

    [Header("Mask o hop le tren ban")]
    public bool[,]      validCells;

    public int        boardSize     => Mathf.Max(boardWidth, boardHeight); // backward-compat
    public int        NumPlayers    => players.Length;
    public PlayerData CurrentPlayer => players[currentPlayerIndex];

    public Vector2Int[] whitePieces { get => players[0].pieces; set => players[0].pieces = value; }
    public Vector2Int[] blackPieces { get => players[1].pieces; set => players[1].pieces = value; }
    public bool isWhiteTurn         { get => currentPlayerIndex == 0; set => currentPlayerIndex = value ? 0 : 1; }
    public int  whiteEscaped        { get => players[0].escaped; set => players[0].escaped = value; }
    public int  blackEscaped        { get => players[1].escaped; set => players[1].escaped = value; }

    public GameState Clone()
    {
        var p = new PlayerData[players.Length];
        for (int i = 0; i < players.Length; i++) p[i] = players[i].Clone();

        bool[,] validClone = null;
        if (validCells != null)
        {
            validClone = new bool[boardWidth, boardHeight];
            for (int x = 0; x < boardWidth; x++)
                for (int y = 0; y < boardHeight; y++)
                    validClone[x, y] = validCells[x, y];
        }

        return new GameState
        {
            boardWidth = boardWidth,
            boardHeight = boardHeight,
            players = p,
            currentPlayerIndex = currentPlayerIndex,
            validCells = validClone
        };
    }

    public bool IsTerminal()
    {
        foreach (var p in players)
            if (p.HasWon())
                return true;

        return !DodgemRules.HasAnyLegalMove(this, currentPlayerIndex);
    }

    public PlayerData Winner()
    {
        foreach (var p in players)
            if (p.HasWon())
                return p;

        if (!DodgemRules.HasAnyLegalMove(this, currentPlayerIndex))
        {
            int prev = (currentPlayerIndex - 1 + NumPlayers) % NumPlayers;
            return players[prev];
        }

        return null;
    }

    public bool IsOccupied(Vector2Int pos)
    {
        foreach (var pl in players)
            if (pl.HasPieceAt(pos))
                return true;
        return false;
    }

    public bool IsCellPlayable(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= boardWidth || pos.y < 0 || pos.y >= boardHeight)
            return false;

        if (validCells == null) return true;
        return validCells[pos.x, pos.y];
    }

    public void NextTurn() => currentPlayerIndex = (currentPlayerIndex + 1) % NumPlayers;

    public static GameState CreateFromPreset(GamePreset preset,
                                             PlayerType[] overrideTypes = null,
                                             int[] overrideDepths = null)
    {
        string err;
        if (!preset.IsValid(out err))
        {
            Debug.LogError("[GameState] Preset khong hop le: " + err);
            return null;
        }

        int W = preset.boardWidth;
        int H = preset.boardHeight;
        int numP = preset.NumPlayers;
        var players = new PlayerData[numP];

        for (int i = 0; i < numP; i++)
        {
            var cfg      = preset.GetConfigSafe(i);
            var startPos = preset.GetStartPositions(i);

            PlayerType pType  = (overrideTypes  != null && i < overrideTypes.Length) ? overrideTypes[i] : cfg.type;
            int        pDepth = (overrideDepths != null && i < overrideDepths.Length) ? overrideDepths[i] : cfg.botDepth;

            players[i] = new PlayerData
            {
                playerIndex = i,
                playerName  = cfg.playerName,
                pieceColor  = cfg.pieceColor,
                escapeDir   = cfg.escapeDir,
                type        = pType,
                botDepth    = pDepth,
                pieces      = startPos,
                escaped     = 0,
                exitCells   = preset.GetExitCellsSafe(i)
            };
        }

        return new GameState
        {
            boardWidth = W,
            boardHeight = H,
            players = players,
            currentPlayerIndex = 0,
            validCells = preset.BuildValidCellMap()
        };
    }
}