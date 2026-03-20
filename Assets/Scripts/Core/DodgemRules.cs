using System.Collections.Generic;
using UnityEngine;

public static class DodgemRules
{
    public static List<GameState> GetChildren(GameState state)
    {
        var children = new List<GameState>();
        var player   = state.CurrentPlayer;
        GenerateMoves(state, player, children);
        return children;
    }

    public static bool HasAnyLegalMove(GameState state, int playerIdx)
    {
        if (state == null || playerIdx < 0 || playerIdx >= state.NumPlayers)
            return false;

        var player = state.players[playerIdx];

        foreach (var piece in player.pieces)
        {
            if (piece.x == -1) continue;
            if (!state.IsCellPlayable(piece)) continue;

            if (player.CanEscapeFrom(piece))
                return true;

            var dirs = player.ValidDirs();
            foreach (var dir in dirs)
            {
                Vector2Int next = piece + dir;
                if (!InBounds(next, state)) continue;
                if (!state.IsCellPlayable(next)) continue;
                if (state.IsOccupied(next)) continue;
                return true;
            }
        }

        return false;
    }

    public static List<Vector2Int> GetValidMovesForPiece(GameState state, Vector2Int piecePos, int playerIdx)
    {
        var result = new List<Vector2Int>();
        var player = state.players[playerIdx];
        var dirs   = player.ValidDirs();

        if (!state.IsCellPlayable(piecePos))
            return result;

        if (player.CanEscapeFrom(piecePos))
            result.Add(player.EscapeMarker(piecePos, state.boardWidth, state.boardHeight));

        foreach (var dir in dirs)
        {
            Vector2Int next = piecePos + dir;

            if (!InBounds(next, state)) continue;
            if (!state.IsCellPlayable(next)) continue;
            if (state.IsOccupied(next)) continue;

            result.Add(next);
        }

        return result;
    }

    public static List<Vector2Int> GetValidMovesForPiece(GameState state, Vector2Int piecePos, bool isWhite)
        => GetValidMovesForPiece(state, piecePos, isWhite ? 0 : 1);

    static void GenerateMoves(GameState state, PlayerData player, List<GameState> children)
    {
        var dirs = player.ValidDirs();
        int idx  = player.playerIndex;

        for (int i = 0; i < player.pieces.Length; i++)
        {
            Vector2Int piecePos = player.pieces[i];
            if (piecePos.x == -1) continue;
            if (!state.IsCellPlayable(piecePos)) continue;

            if (player.CanEscapeFrom(piecePos))
            {
                var ns = state.Clone();
                ns.players[idx].pieces[i] = new Vector2Int(-1, -1);
                ns.players[idx].escaped++;
                ns.NextTurn();
                children.Add(ns);
            }

            foreach (var dir in dirs)
            {
                Vector2Int next = piecePos + dir;

                if (!InBounds(next, state)) continue;
                if (!state.IsCellPlayable(next)) continue;
                if (state.IsOccupied(next)) continue;

                var ns = state.Clone();
                ns.players[idx].pieces[i] = next;
                ns.NextTurn();
                children.Add(ns);
            }
        }
    }

    public static bool InBounds(Vector2Int pos, GameState state)
    {
        return pos.x >= 0 && pos.x < state.boardWidth &&
               pos.y >= 0 && pos.y < state.boardHeight;
    }

    // backward-compat
    public static bool InBounds(Vector2Int pos, int boardSize)
    {
        return pos.x >= 0 && pos.x < boardSize &&
               pos.y >= 0 && pos.y < boardSize;
    }

    public static bool IsOccupied(GameState state, Vector2Int pos)
        => state.IsOccupied(pos);
}