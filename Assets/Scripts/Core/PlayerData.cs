using UnityEngine;

public enum PlayerType
{
    Human,
    Bot
}

public enum EscapeDirection
{
    Right,
    Top,
    Left,
    Bottom
}

[System.Serializable]
public class PlayerData
{
    public int             playerIndex;
    public string          playerName;
    public Color           pieceColor;
    public EscapeDirection escapeDir;

    public PlayerType type;
    public int        botDepth;

    public Vector2Int[] pieces;
    public int          escaped;

    [Header("Danh sach o ma neu quan dang dung o do thi co the thoat")]
    public Vector2Int[] exitCells;

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
            escaped     = escaped,
            exitCells   = exitCells != null ? (Vector2Int[])exitCells.Clone() : new Vector2Int[0]
        };
    }

    public bool HasWon()
    {
        int totalPieces = pieces != null ? pieces.Length : 0;
        return escaped >= totalPieces;
    }

    public bool HasPieceAt(Vector2Int pos)
    {
        foreach (var p in pieces)
            if (p == pos) return true;
        return false;
    }

    public bool CanEscapeFrom(Vector2Int pos)
    {
        if (exitCells == null) return false;
        foreach (var c in exitCells)
            if (c == pos) return true;
        return false;
    }

    public Vector2Int EscapeMarker(Vector2Int piecePos, int boardWidth, int boardHeight)
    {
        switch (escapeDir)
        {
            case EscapeDirection.Right:  return new Vector2Int(boardWidth, piecePos.y);
            case EscapeDirection.Top:    return new Vector2Int(piecePos.x, boardHeight);
            case EscapeDirection.Left:   return new Vector2Int(-1, piecePos.y);
            case EscapeDirection.Bottom: return new Vector2Int(piecePos.x, -1);
            default:                     return new Vector2Int(boardWidth, piecePos.y);
        }
    }

    public Vector2Int ForwardDir()
    {
        switch (escapeDir)
        {
            case EscapeDirection.Right:  return new Vector2Int(1, 0);
            case EscapeDirection.Top:    return new Vector2Int(0, 1);
            case EscapeDirection.Left:   return new Vector2Int(-1, 0);
            case EscapeDirection.Bottom: return new Vector2Int(0, -1);
            default:                     return new Vector2Int(1, 0);
        }
    }

    public Vector2Int[] ValidDirs()
    {
        var fwd = ForwardDir();
        var perp1 = new Vector2Int(fwd.y, fwd.x);
        var perp2 = new Vector2Int(-fwd.y, -fwd.x);
        return new Vector2Int[] { fwd, perp1, perp2 };
    }
}