using UnityEngine;

[System.Serializable]
public class GameState
{
    public int          boardSize;
    public PlayerData[] players;
    public int          currentPlayerIndex;

    public int        NumPlayers    => players.Length;
    public int        PiecesPerSide => boardSize - 1;
    public PlayerData CurrentPlayer => players[currentPlayerIndex];

    // Backward-compat
    public Vector2Int[] whitePieces { get => players[0].pieces; set => players[0].pieces = value; }
    public Vector2Int[] blackPieces { get => players[1].pieces; set => players[1].pieces = value; }
    public bool isWhiteTurn         { get => currentPlayerIndex == 0; set => currentPlayerIndex = value ? 0 : 1; }
    public int  whiteEscaped        { get => players[0].escaped; set => players[0].escaped = value; }
    public int  blackEscaped        { get => players[1].escaped; set => players[1].escaped = value; }

    public GameState Clone()
    {
        var p = new PlayerData[players.Length];
        for (int i = 0; i < players.Length; i++) p[i] = players[i].Clone();
        return new GameState { boardSize = boardSize, players = p, currentPlayerIndex = currentPlayerIndex };
    }

    public bool IsTerminal()
    {
        foreach (var p in players) if (p.HasWon(boardSize)) return true;
        return false;
    }

    public PlayerData Winner()
    {
        foreach (var p in players) if (p.HasWon(boardSize)) return p;
        return null;
    }

    public bool IsOccupied(Vector2Int pos)
    {
        foreach (var pl in players) if (pl.HasPieceAt(pos)) return true;
        return false;
    }

    public void NextTurn() => currentPlayerIndex = (currentPlayerIndex + 1) % NumPlayers;

    // ── Factory: tu GamePreset ────────────────────────────────────
    // FIX: dung GetConfigSafe() de xu ly preset assets thieu data
    public static GameState CreateFromPreset(GamePreset preset,
                                             PlayerType[] overrideTypes  = null,
                                             int[]        overrideDepths = null)
    {
        string err;
        if (!preset.IsValid(out err))
        {
            Debug.LogError("[GameState] Preset khong hop le: " + err);
            return null;
        }

        int N    = preset.boardSize;
        int numP = preset.NumPlayers;
        var players = new PlayerData[numP];

        for (int i = 0; i < numP; i++)
        {
            // GetConfigSafe tra ve data da fill default neu asset thieu data
            var cfg      = preset.GetConfigSafe(i);
            var startPos = preset.GetStartPositions(i);

            PlayerType pType  = (overrideTypes  != null && i < overrideTypes.Length)  ? overrideTypes[i]  : cfg.type;
            int        pDepth = (overrideDepths != null && i < overrideDepths.Length)  ? overrideDepths[i] : cfg.botDepth;

            players[i] = new PlayerData
            {
                playerIndex = i,
                playerName  = cfg.playerName,
                pieceColor  = cfg.pieceColor,
                escapeDir   = cfg.escapeDir,
                type        = pType,
                botDepth    = pDepth,
                pieces      = startPos,
                escaped     = 0
            };
        }

        return new GameState { boardSize = N, players = players, currentPlayerIndex = 0 };
    }

    // Backward-compat factory
    public static GameState CreateDefault(GameConfig config = null)
    {
        int size       = config?.boardSize  ?? 3;
        int numPlayers = config?.numPlayers ?? 2;
        var players    = new PlayerData[numPlayers];
        var defaults   = DefaultPlayerSetups(size);

        for (int i = 0; i < numPlayers; i++)
        {
            var d = defaults[i];
            players[i] = new PlayerData
            {
                playerIndex = i,
                playerName  = d.name,
                pieceColor  = d.color,
                escapeDir   = d.dir,
                type        = config != null ? config.playerTypes[i] : (i == 0 ? PlayerType.Bot : PlayerType.Human),
                botDepth    = config != null ? config.botDepths[i]   : (i == 0 ? 6 : 0),
                pieces      = GenerateStartPieces(i, size),
                escaped     = 0
            };
        }
        return new GameState { boardSize = size, players = players, currentPlayerIndex = 0 };
    }

    static Vector2Int[] GenerateStartPieces(int idx, int N)
    {
        int n = N - 1;
        var pieces = new Vector2Int[n];
        switch (idx)
        {
            case 0: for (int i = 0; i < n; i++) pieces[i] = new Vector2Int(0,   i+1); break;
            case 1: for (int i = 0; i < n; i++) pieces[i] = new Vector2Int(i+1, 0);   break;
            case 2: for (int i = 0; i < n; i++) pieces[i] = new Vector2Int(N-1, i);   break;
            case 3: for (int i = 0; i < n; i++) pieces[i] = new Vector2Int(i,   N-1); break;
        }
        return pieces;
    }

    static (string name, Color color, EscapeDirection dir)[] DefaultPlayerSetups(int s)
    {
        return new[]
        {
            ("Trang", Color.white,                    EscapeDirection.Right),
            ("Den",   new Color(0.15f,0.15f,0.15f),  EscapeDirection.Top),
            ("Do",    new Color(0.85f,0.2f, 0.2f),   EscapeDirection.Left),
            ("Xanh",  new Color(0.2f, 0.6f, 0.9f),   EscapeDirection.Bottom),
        };
    }
}