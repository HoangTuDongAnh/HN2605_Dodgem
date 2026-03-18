using UnityEngine;

// ================================================================
// GameState — trạng thái toàn bộ ván cờ
//
// THAY ĐỔI SO VỚI BẢN CŨ:
//   - Bỏ: whitePieces, blackPieces, isWhiteTurn, whiteEscaped, blackEscaped
//   - Thêm: players[] (mảng PlayerData), boardSize, currentPlayerIndex
//
// Backward-compat helpers (IsWhiteTurn, WhitePieces, BlackPieces...)
// vẫn còn để AlphaBetaAI và EvalFunction cũ không cần sửa ngay.
// ================================================================

[System.Serializable]
public class GameState
{
    // ── Cấu hình bàn cờ ──────────────────────────────────────────
    public int boardSize;            // 3, 4, 5 …  (số ô mỗi chiều)

    // ── Dữ liệu người chơi ───────────────────────────────────────
    public PlayerData[] players;     // players[0]=Trắng, [1]=Đen, [2]=Đỏ, [3]=Xanh

    // Chỉ số người chơi hiện tại (quay vòng 0 → N-1 → 0)
    public int currentPlayerIndex;

    // ── Computed helpers ──────────────────────────────────────────
    public int  NumPlayers   => players.Length;
    public int  PiecesPerSide => boardSize - 1;   // 3x3→2 quân, 4x4→3 quân, 5x5→4 quân
    public PlayerData CurrentPlayer => players[currentPlayerIndex];

    // ── Backward-compat (để AlphaBetaAI/EvalFunction cũ không lỗi) ──
    // Trắng luôn là players[0], Đen là players[1]
    public Vector2Int[] whitePieces
    {
        get => players[0].pieces;
        set => players[0].pieces = value;
    }
    public Vector2Int[] blackPieces
    {
        get => players[1].pieces;
        set => players[1].pieces = value;
    }
    public bool isWhiteTurn
    {
        get => currentPlayerIndex == 0;
        set => currentPlayerIndex = value ? 0 : 1;
    }
    public int whiteEscaped
    {
        get => players[0].escaped;
        set => players[0].escaped = value;
    }
    public int blackEscaped
    {
        get => players[1].escaped;
        set => players[1].escaped = value;
    }

    // ── Clone ─────────────────────────────────────────────────────
    public GameState Clone()
    {
        var clonedPlayers = new PlayerData[players.Length];
        for (int i = 0; i < players.Length; i++)
            clonedPlayers[i] = players[i].Clone();

        return new GameState
        {
            boardSize            = boardSize,
            players              = clonedPlayers,
            currentPlayerIndex   = currentPlayerIndex
        };
    }

    // ── IsTerminal ────────────────────────────────────────────────
    // Trả về true nếu có phe nào đã thắng, hoặc phe hiện tại hết nước đi
    public bool IsTerminal()
    {
        foreach (var p in players)
            if (p.HasWon(boardSize)) return true;
        return false;
    }

    // Phe nào đã thắng? null nếu chưa ai thắng
    public PlayerData Winner()
    {
        foreach (var p in players)
            if (p.HasWon(boardSize)) return p;
        return null;
    }

    // ── IsOccupied ────────────────────────────────────────────────
    // Kiểm tra ô pos có bị chiếm bởi bất kỳ quân nào không
    public bool IsOccupied(Vector2Int pos)
    {
        foreach (var pl in players)
            if (pl.HasPieceAt(pos)) return true;
        return false;
    }

    // ── Advance turn ──────────────────────────────────────────────
    // Chuyển lượt sang người tiếp theo (quay vòng)
    public void NextTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % NumPlayers;
    }

    // ── Factory: tạo GameState mặc định ──────────────────────────
    // Dùng trong GameManager.CreateInitialState()
    // config = null → dùng mặc định 3x3, 2 người (1 Human + 1 Bot)
    public static GameState CreateDefault(GameConfig config = null)
    {
        int size       = config != null ? config.boardSize       : 3;
        int numPlayers = config != null ? config.numPlayers      : 2;
        int pieces     = size - 1;

        // Tạo PlayerData cho từng phe
        var players = new PlayerData[numPlayers];

        // Cấu hình mặc định cho từng phe theo vị trí (tối đa 4 phe)
        var defaults = DefaultPlayerSetups(size);

        for (int i = 0; i < numPlayers; i++)
        {
            var setup = defaults[i];
            players[i] = new PlayerData
            {
                playerIndex = i,
                playerName  = setup.name,
                pieceColor  = setup.color,
                escapeDir   = setup.dir,
                type        = config != null ? config.playerTypes[i] : (i == 0 ? PlayerType.Bot : PlayerType.Human),
                botDepth    = config != null ? config.botDepths[i]   : (i == 0 ? 6 : 0),
                pieces      = GenerateStartPieces(i, numPlayers, size),
                escaped     = 0
            };
        }

        return new GameState
        {
            boardSize          = size,
            players            = players,
            currentPlayerIndex = 0   // Trắng đi trước
        };
    }

    // ── Vị trí xuất phát theo phe ────────────────────────────────
    static Vector2Int[] GenerateStartPieces(int playerIdx, int numPlayers, int boardSize)
    {
        int n = boardSize - 1; // số quân mỗi phe
        var pieces = new Vector2Int[n];

        // 2 người: Trắng cột trái, Đen hàng dưới
        // 3 người: thêm Đỏ cột phải
        // 4 người: thêm Xanh hàng trên

        switch (playerIdx)
        {
            case 0: // Trắng — cột trái x=0, hàng 1 đến N-1
                for (int i = 0; i < n; i++)
                    pieces[i] = new Vector2Int(0, i + 1);
                break;
            case 1: // Đen — hàng dưới y=0, cột 1 đến N-1
                for (int i = 0; i < n; i++)
                    pieces[i] = new Vector2Int(i + 1, 0);
                break;
            case 2: // Đỏ — cột phải x=N-1, hàng 0 đến N-2
                for (int i = 0; i < n; i++)
                    pieces[i] = new Vector2Int(boardSize - 1, i);
                break;
            case 3: // Xanh — hàng trên y=N-1, cột 0 đến N-2
                for (int i = 0; i < n; i++)
                    pieces[i] = new Vector2Int(i, boardSize - 1);
                break;
        }
        return pieces;
    }

    // ── Cấu hình mặc định mỗi phe ────────────────────────────────
    static (string name, Color color, EscapeDirection dir)[] DefaultPlayerSetups(int boardSize)
    {
        return new[]
        {
            ("Trang", Color.white,                        EscapeDirection.Right),
            ("Den",   new Color(0.15f, 0.15f, 0.15f),    EscapeDirection.Top),
            ("Do",    new Color(0.85f, 0.2f,  0.2f),     EscapeDirection.Left),
            ("Xanh",  new Color(0.2f,  0.6f,  0.9f),     EscapeDirection.Bottom),
        };
    }
}