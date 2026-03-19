using System.Collections.Generic;
using UnityEngine;

// ================================================================
// DodgemRules — luật chơi và sinh nước đi
//
// THAY ĐỔI SO VỚI BẢN CŨ:
//   - Bỏ hardcode "3" → dùng state.boardSize
//   - Bỏ whiteDirs/blackDirs cố định → dùng PlayerData.ValidDirs()
//   - GetChildren() xét phe state.currentPlayerIndex (thay vì isWhiteTurn)
//   - GetValidMovesForPiece() nhận playerIdx thay vì bool isWhite
//   - IsOccupied() delegate về state.IsOccupied()
//
// Backward-compat: IsOccupied(state, pos) static vẫn còn để không
// lỗi các chỗ gọi cũ trong EvalFunction.
// ================================================================

public static class DodgemRules
{
    // ── Sinh tất cả GameState con hợp lệ ─────────────────────────
    public static List<GameState> GetChildren(GameState state)
    {
        var children = new List<GameState>();
        var player   = state.CurrentPlayer;
        GenerateMoves(state, player, children);
        return children;
    }

    // ── Sinh ô đích hợp lệ cho 1 quân cụ thể (dùng để highlight UI) ──
    // Ô thoát trả về EscapeMarker: x=boardSize (Right), y=boardSize (Top),
    // x=-1 (Left), y=-1 (Bottom)
    public static List<Vector2Int> GetValidMovesForPiece(
        GameState state, Vector2Int piecePos, int playerIdx)
    {
        var result = new List<Vector2Int>();
        var player = state.players[playerIdx];
        var dirs   = player.ValidDirs();
        int N      = state.boardSize;

        foreach (var dir in dirs)
        {
            Vector2Int next = piecePos + dir;

            // Kiểm tra thoát bàn theo hướng của phe này
            if (IsEscapeMove(next, player.escapeDir, N))
            {
                result.Add(player.EscapeMarker(piecePos, N));
                continue;
            }

            // Ngoài bàn nhưng không phải thoát → bỏ qua
            if (!InBounds(next, N)) continue;

            // Ô bị chiếm → bỏ qua
            if (state.IsOccupied(next)) continue;

            result.Add(next);
        }
        return result;
    }

    // ── Backward-compat: vẫn nhận bool isWhite ───────────────────
    public static List<Vector2Int> GetValidMovesForPiece(
        GameState state, Vector2Int piecePos, bool isWhite)
        => GetValidMovesForPiece(state, piecePos, isWhite ? 0 : 1);

    // ── Generate moves cho một phe ────────────────────────────────
    static void GenerateMoves(GameState state, PlayerData player, List<GameState> children)
    {
        var dirs = player.ValidDirs();
        int N    = state.boardSize;
        int idx  = player.playerIndex;

        for (int i = 0; i < player.pieces.Length; i++)
        {
            if (player.pieces[i].x == -1) continue; // quân đã thoát

            foreach (var dir in dirs)
            {
                Vector2Int next = player.pieces[i] + dir;

                // Thoát bàn
                if (IsEscapeMove(next, player.escapeDir, N))
                {
                    var ns = state.Clone();
                    ns.players[idx].pieces[i] = new Vector2Int(-1, -1);
                    ns.players[idx].escaped++;
                    ns.NextTurn();
                    children.Add(ns);
                    continue;
                }

                // Ngoài bàn không phải thoát → bỏ qua
                if (!InBounds(next, N)) continue;

                // Ô bị chiếm
                if (state.IsOccupied(next)) continue;

                var newState = state.Clone();
                newState.players[idx].pieces[i] = next;
                newState.NextTurn();
                children.Add(newState);
            }
        }
    }

    // ── Kiểm tra nước đi có phải là thoát bàn ────────────────────
    static bool IsEscapeMove(Vector2Int next, EscapeDirection dir, int boardSize)
    {
        switch (dir)
        {
            case EscapeDirection.Right:  return next.x >= boardSize;
            case EscapeDirection.Top:    return next.y >= boardSize;
            case EscapeDirection.Left:   return next.x < 0;
            case EscapeDirection.Bottom: return next.y < 0;
            default: return false;
        }
    }

    // ── Kiểm tra trong bàn ────────────────────────────────────────
    public static bool InBounds(Vector2Int pos, int boardSize)
    {
        return pos.x >= 0 && pos.x < boardSize &&
               pos.y >= 0 && pos.y < boardSize;
    }

    // ── Backward-compat static IsOccupied ────────────────────────
    public static bool IsOccupied(GameState state, Vector2Int pos)
        => state.IsOccupied(pos);
}