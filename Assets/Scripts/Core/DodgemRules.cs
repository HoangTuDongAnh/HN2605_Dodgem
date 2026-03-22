using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chua cac luat sinh nuoc di va kiem tra nuoc di hop le.
/// </summary>
public static class DodgemRules
{
    #region Public API

    /// <summary>
    /// Sinh danh sach state con hop le tu state hien tai.
    /// </summary>
    public static List<GameState> GetChildren(GameState state)
    {
        var children = new List<GameState>();
        var player = state.CurrentPlayer;
        GenerateMoves(state, player, children);
        return children;
    }

    /// <summary>
    /// Kiem tra nguoi choi co con nuoc di hop le hay khong.
    /// </summary>
    public static bool HasAnyLegalMove(GameState state, int playerIdx)
    {
        return CountLegalMoves(state, playerIdx) > 0;
    }

    /// <summary>
    /// Dem tong so nuoc di hop le cua mot nguoi choi ma khong clone state.
    /// </summary>
    public static int CountLegalMoves(GameState state, int playerIdx)
    {
        if (state == null || playerIdx < 0 || playerIdx >= state.NumPlayers)
            return 0;

        int count = 0;
        var player = state.players[playerIdx];

        foreach (var piece in player.pieces)
        {
            if (piece.x == -1) continue;
            if (!state.IsCellPlayable(piece)) continue;

            if (player.CanEscapeFrom(piece))
                count++;

            foreach (var dir in player.ValidDirs())
            {
                Vector2Int next = piece + dir;
                if (!InBounds(next, state)) continue;
                if (!state.IsCellPlayable(next)) continue;
                if (state.IsOccupied(next)) continue;
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Dem so quan co the thoat ngay o luot hien tai.
    /// </summary>
    public static int CountImmediateEscapes(GameState state, int playerIdx)
    {
        if (state == null || playerIdx < 0 || playerIdx >= state.NumPlayers)
            return 0;

        int count = 0;
        var player = state.players[playerIdx];

        foreach (var piece in player.pieces)
        {
            if (piece.x == -1) continue;
            if (!state.IsCellPlayable(piece)) continue;
            if (player.CanEscapeFrom(piece))
                count++;
        }

        return count;
    }

    /// <summary>
    /// Lay cac nuoc di hop le cua mot quan.
    /// </summary>
    public static List<Vector2Int> GetValidMovesForPiece(GameState state, Vector2Int piecePos, int playerIdx)
    {
        var result = new List<Vector2Int>();
        var player = state.players[playerIdx];

        if (!state.IsCellPlayable(piecePos))
            return result;

        if (player.CanEscapeFrom(piecePos))
            result.Add(player.EscapeMarker(piecePos, state.boardWidth, state.boardHeight));

        foreach (var dir in player.ValidDirs())
        {
            Vector2Int next = piecePos + dir;

            if (!InBounds(next, state)) continue;
            if (!state.IsCellPlayable(next)) continue;
            if (state.IsOccupied(next)) continue;

            result.Add(next);
        }

        return result;
    }

    /// <summary>
    /// Kiem tra toa do co nam trong kich thuoc board hay khong.
    /// </summary>
    public static bool InBounds(Vector2Int pos, GameState state)
    {
        return pos.x >= 0 && pos.x < state.boardWidth &&
               pos.y >= 0 && pos.y < state.boardHeight;
    }

    /// <summary>
    /// Kiem tra o co dang bi chiem hay khong.
    /// </summary>
    public static bool IsOccupied(GameState state, Vector2Int pos)
    {
        return state.IsOccupied(pos);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Sinh cac state con tu tat ca nuoc di hop le cua player hien tai.
    /// </summary>
    static void GenerateMoves(GameState state, PlayerData player, List<GameState> children)
    {
        int playerIndex = player.playerIndex;

        for (int i = 0; i < player.pieces.Length; i++)
        {
            Vector2Int piecePos = player.pieces[i];
            if (piecePos.x == -1) continue;
            if (!state.IsCellPlayable(piecePos)) continue;

            if (player.CanEscapeFrom(piecePos))
            {
                var escapedState = state.Clone();
                escapedState.players[playerIndex].pieces[i] = new Vector2Int(-1, -1);
                escapedState.players[playerIndex].escaped++;
                escapedState.lastMoverIndex = playerIndex;
                escapedState.NextTurn();
                children.Add(escapedState);
            }

            foreach (var dir in player.ValidDirs())
            {
                Vector2Int next = piecePos + dir;

                if (!InBounds(next, state)) continue;
                if (!state.IsCellPlayable(next)) continue;
                if (state.IsOccupied(next)) continue;

                var movedState = state.Clone();
                movedState.players[playerIndex].pieces[i] = next;
                movedState.lastMoverIndex = playerIndex;
                movedState.NextTurn();
                children.Add(movedState);
            }
        }

        // Neu khong sinh duoc nuoc nao: player bi block, phai bo luot (pass).
        // Tao pass state: giu nguyen board, chuyen sang luot ke tiep.
        // Day la luat Dodgem chinh xac - bi block khong phai thua ngay.
        if (children.Count == 0)
        {
            var passState = state.Clone();
            passState.lastMoverIndex = playerIndex;
            passState.NextTurn();
            children.Add(passState);
        }
    }

    #endregion
}