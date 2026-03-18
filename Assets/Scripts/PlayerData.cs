using UnityEngine;

// ================================================================
// PlayerData — dữ liệu của một phe chơi
// Thay thế whitePieces/blackPieces hardcode trong GameState cũ.
// Mỗi player có:
//   - Vị trí xuất phát  (cột hoặc hàng biên)
//   - Hướng thoát       (sang phải, lên trên, sang trái, xuống dưới)
//   - Loại              (Human hoặc Bot với độ khó)
// ================================================================

public enum PlayerType
{
    Human,
    Bot
}

// Hướng thoát của mỗi phe — cũng xác định hướng đi chính của phe đó
public enum EscapeDirection
{
    Right,   // Thoát sang phải (x tăng) — Trắng
    Top,     // Thoát lên trên  (y tăng) — Đen
    Left,    // Thoát sang trái (x giảm) — Đỏ (3-4 người)
    Bottom   // Thoát xuống dưới(y giảm) — Xanh (4 người)
}

[System.Serializable]
public class PlayerData
{
    // ── Định danh ─────────────────────────────────────────────────
    public int             playerIndex;   // 0, 1, 2, 3
    public string          playerName;    // "Trắng", "Đen", "Đỏ", "Xanh"
    public Color           pieceColor;    // màu quân cờ
    public EscapeDirection escapeDir;     // hướng thoát của phe này

    // ── Loại người chơi ───────────────────────────────────────────
    public PlayerType type;       // Human hoặc Bot
    public int        botDepth;   // độ sâu AI: 2=Easy, 4=Medium, 6=Hard

    // ── Trạng thái trong game ─────────────────────────────────────
    public Vector2Int[] pieces;   // vị trí các quân trên bàn; (-1,-1) = đã thoát
    public int          escaped;  // số quân đã thoát thành công

    // ── Clone (dùng trong GameState.Clone()) ─────────────────────
    public PlayerData Clone()
    {
        return new PlayerData
        {
            playerIndex = playerIndex,
            playerName  = playerName,
            pieceColor  = pieceColor,
            escapeDir   = escapeDir,
            type        = type,
            botDepth    = botDepth,
            pieces      = (Vector2Int[])pieces.Clone(),
            escaped     = escaped
        };
    }

    // ── Helpers ───────────────────────────────────────────────────

    // Số quân cần thoát để thắng = tổng số quân (boardSize - 1)
    public bool HasWon(int boardSize) => escaped >= boardSize - 1;

    // Quân tại vị trí pos không?
    public bool HasPieceAt(Vector2Int pos)
    {
        foreach (var p in pieces)
            if (p == pos) return true;
        return false;
    }

    // Ký hiệu ô thoát (dùng để highlight UI):
    // Right  → x = boardSize (ngoài cột phải)
    // Top    → y = boardSize (ngoài hàng trên)
    // Left   → x = -1       (ngoài cột trái)
    // Bottom → y = -1       (ngoài hàng dưới)
    public Vector2Int EscapeMarker(Vector2Int piecePos, int boardSize)
    {
        switch (escapeDir)
        {
            case EscapeDirection.Right:  return new Vector2Int(boardSize,  piecePos.y);
            case EscapeDirection.Top:    return new Vector2Int(piecePos.x,  boardSize);
            case EscapeDirection.Left:   return new Vector2Int(-1,          piecePos.y);
            case EscapeDirection.Bottom: return new Vector2Int(piecePos.x,  -1);
            default:                     return new Vector2Int(boardSize,  piecePos.y);
        }
    }

    // Hướng đi chính (forward) của phe này
    public Vector2Int ForwardDir()
    {
        switch (escapeDir)
        {
            case EscapeDirection.Right:  return new Vector2Int( 1,  0);
            case EscapeDirection.Top:    return new Vector2Int( 0,  1);
            case EscapeDirection.Left:   return new Vector2Int(-1,  0);
            case EscapeDirection.Bottom: return new Vector2Int( 0, -1);
            default:                     return new Vector2Int( 1,  0);
        }
    }

    // Các hướng đi hợp lệ: forward + 2 bên vuông góc (KHÔNG đi lùi)
    public Vector2Int[] ValidDirs()
    {
        var fwd = ForwardDir();
        // Hai hướng vuông góc
        var perp1 = new Vector2Int( fwd.y,  fwd.x);
        var perp2 = new Vector2Int(-fwd.y, -fwd.x);
        return new Vector2Int[] { fwd, perp1, perp2 };
    }
}