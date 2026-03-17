using System.Collections.Generic;
using UnityEngine;

public static class DodgemRules
{
    // ================================================================
    // LUẬT CHƠI:
    //   Quân TRẮNG (AI): thoát qua cột PHẢI (x >= 3)
    //                    Đi được: phải(+x), lên(+y), xuống(-y)
    //                    KHÔNG đi trái
    //
    //   Quân ĐEN (người): thoát qua hàng TRÊN (y >= 3)
    //                     Đi được: lên(+y), trái(-x), phải(+x)
    //                     KHÔNG đi xuống
    //
    //   Không đi chéo. Mỗi lượt đi đúng 1 ô.
    // ================================================================

    // Trắng: phải, lên, xuống  (KHÔNG trái)
    static readonly Vector2Int[] whiteDirs = {
        new Vector2Int( 1,  0),  // phải
        new Vector2Int( 0,  1),  // lên
        new Vector2Int( 0, -1),  // xuống
    };

    // Đen: lên, trái, phải  (KHÔNG xuống)
    static readonly Vector2Int[] blackDirs = {
        new Vector2Int( 0,  1),  // lên
        new Vector2Int(-1,  0),  // trái
        new Vector2Int( 1,  0),  // phải
    };

    // Trả về tất cả GameState con hợp lệ
    public static List<GameState> GetChildren(GameState state)
    {
        var children = new List<GameState>();
        GenerateMoves(state, state.isWhiteTurn, children);
        return children;
    }

    // Trả về danh sách ô đích hợp lệ cho một quân cụ thể (để highlight UI)
    // Ô thoát dùng ký hiệu: Trắng thoát → (3, y), Đen thoát → (x, 3)
    public static List<Vector2Int> GetValidMovesForPiece(GameState state, Vector2Int piecePos, bool isWhite)
    {
        var result = new List<Vector2Int>();
        var dirs   = isWhite ? whiteDirs : blackDirs;

        foreach (var dir in dirs)
        {
            Vector2Int next = piecePos + dir;

            if (isWhite && next.x >= 3)       // Trắng thoát phải
            { result.Add(new Vector2Int(3, piecePos.y)); continue; }

            if (!isWhite && next.y >= 3)      // Đen thoát trên
            { result.Add(new Vector2Int(piecePos.x, 3)); continue; }

            if (next.x < 0 || next.x > 2 || next.y < 0 || next.y > 2) continue;
            if (IsOccupied(state, next)) continue;

            result.Add(next);
        }
        return result;
    }

    static void GenerateMoves(GameState state, bool isWhite, List<GameState> children)
    {
        var pieces = isWhite ? state.whitePieces : state.blackPieces;
        var dirs   = isWhite ? whiteDirs : blackDirs;

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].x == -1) continue; // quân đã thoát

            foreach (var dir in dirs)
            {
                Vector2Int next = pieces[i] + dir;

                // Trắng thoát cột phải
                if (isWhite && next.x >= 3)
                {
                    var ns = state.Clone();
                    ns.whitePieces[i] = new Vector2Int(-1, -1);
                    ns.whiteEscaped++;
                    ns.isWhiteTurn = false;
                    children.Add(ns);
                    continue;
                }

                // Đen thoát hàng trên
                if (!isWhite && next.y >= 3)
                {
                    var ns = state.Clone();
                    ns.blackPieces[i] = new Vector2Int(-1, -1);
                    ns.blackEscaped++;
                    ns.isWhiteTurn = true;
                    children.Add(ns);
                    continue;
                }

                if (next.x < 0 || next.x > 2 || next.y < 0 || next.y > 2) continue;
                if (IsOccupied(state, next)) continue;

                var newState = state.Clone();
                if (isWhite) newState.whitePieces[i] = next;
                else         newState.blackPieces[i]  = next;
                newState.isWhiteTurn = !isWhite;
                children.Add(newState);
            }
        }
    }

    public static bool IsOccupied(GameState state, Vector2Int pos)
    {
        foreach (var wp in state.whitePieces)
            if (wp == pos) return true;
        foreach (var bp in state.blackPieces)
            if (bp == pos) return true;
        return false;
    }
}