using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Chua ham danh gia trang thai cho AI.
/// </summary>
public static class EvalFunction
{
    #region Constants

    private const int WIN_SCORE = 100000;

    private const int ESCAPED_PIECE_SCORE = 2200;
    private const int READY_TO_ESCAPE_SCORE = 380;
    private const int REACHABLE_EXIT_BONUS_BASE = 260;
    private const int UNREACHABLE_PENALTY = 180;
    private const int MOBILITY_WEIGHT_SELF = 14;
    private const int MOBILITY_WEIGHT_OPP = 9;
    private const int BLOCK_DIRECT_SCORE = 90;
    private const int BLOCK_NEAR_SCORE = 45;
    private const int TRAP_BONUS = 160;
    private const int ENDGAME_ESCAPED_BONUS = 300;
    private const int ENDGAME_READY_BONUS = 180;

    private const int INF = 999999;

    #endregion

    #region Public API

    /// <summary>
    /// Danh gia mac dinh theo player 0.
    /// </summary>
    public static int Eval(GameState state)
    {
        return Eval(state, 0);
    }

    /// <summary>
    /// Danh gia state theo goc nhin cua mot player.
    /// </summary>
    public static int Eval(GameState state, int perspectivePlayerIdx)
    {
        var winner = state.Winner();
        if (winner != null)
            return winner.playerIndex == perspectivePlayerIdx ? WIN_SCORE : -WIN_SCORE;

        // Tinh distance map 1 lan cho moi player
        var distanceMaps = new int[state.NumPlayers][,];
        for (int i = 0; i < state.NumPlayers; i++)
            distanceMaps[i] = BuildDistanceMap(state, state.players[i]);

        int score = 0;

        for (int i = 0; i < state.NumPlayers; i++)
        {
            int sign = (i == perspectivePlayerIdx) ? 1 : -1;
            score += sign * ScoreForPlayer(state, i, distanceMaps[i]);
        }

        score += BlockingScore(state, perspectivePlayerIdx);
        score += MobilityScore(state, perspectivePlayerIdx);
        score += TrapScore(state, perspectivePlayerIdx);
        score += TurnAdvantageScore(state, perspectivePlayerIdx);

        return score;
    }

    #endregion

    #region Player Scoring

    /// <summary>
    /// Tinh diem tong cho mot player cu the.
    /// </summary>
    static int ScoreForPlayer(GameState state, int playerIdx, int[,] distanceMap)
    {
        var player = state.players[playerIdx];
        int score = 0;

        score += player.escaped * ESCAPED_PIECE_SCORE;

        int activePieces = 0;
        int readyToEscape = 0;
        int reachablePieces = 0;
        int totalDistance = 0;

        foreach (var pos in player.pieces)
        {
            if (pos.x == -1) continue;
            if (!state.IsCellPlayable(pos)) continue;

            activePieces++;

            if (player.CanEscapeFrom(pos))
            {
                readyToEscape++;
                score += READY_TO_ESCAPE_SCORE;
            }

            int distance = distanceMap[pos.x, pos.y];
            if (distance >= INF)
            {
                score -= UNREACHABLE_PENALTY;
                continue;
            }

            reachablePieces++;
            totalDistance += distance;

            score += Mathf.Max(0, REACHABLE_EXIT_BONUS_BASE - distance * 42);

            if (distance == 0) score += 120;
            else if (distance == 1) score += 80;
            else if (distance == 2) score += 35;
        }

        score += reachablePieces * 55;

        if (activePieces > 0 && reachablePieces < activePieces)
            score -= (activePieces - reachablePieces) * 70;

        if (activePieces <= 2)
        {
            score += player.escaped * ENDGAME_ESCAPED_BONUS;
            score += readyToEscape * ENDGAME_READY_BONUS;
        }

        if (reachablePieces > 0)
            score += Mathf.Max(0, 140 - totalDistance * 10);

        return score;
    }

    #endregion

    #region Mobility / Blocking / Trap

    /// <summary>
    /// Tinh diem mobility cua minh va doi thu.
    /// </summary>
    static int MobilityScore(GameState state, int perspectiveIdx)
    {
        int myMoves = DodgemRules.CountLegalMoves(state, perspectiveIdx);

        if (myMoves == 0)
            return -2500;

        int opponentMoves = 0;
        int trappedOpponents = 0;

        for (int i = 0; i < state.NumPlayers; i++)
        {
            if (i == perspectiveIdx) continue;

            int moves = DodgemRules.CountLegalMoves(state, i);
            opponentMoves += moves;

            if (moves == 0)
                trappedOpponents++;
        }

        int score = 0;
        score += myMoves * MOBILITY_WEIGHT_SELF;
        score -= opponentMoves * MOBILITY_WEIGHT_OPP;
        score += trappedOpponents * 220;

        return score;
    }

    /// <summary>
    /// Tinh diem chan duong doi thu.
    /// </summary>
    static int BlockingScore(GameState state, int perspectiveIdx)
    {
        int score = 0;
        var me = state.players[perspectiveIdx];

        for (int oppIdx = 0; oppIdx < state.NumPlayers; oppIdx++)
        {
            if (oppIdx == perspectiveIdx) continue;
            var opponent = state.players[oppIdx];

            foreach (var opponentPos in opponent.pieces)
            {
                if (opponentPos.x == -1) continue;
                if (!state.IsCellPlayable(opponentPos)) continue;

                Vector2Int opponentForward = opponent.ForwardDir();
                Vector2Int front1 = opponentPos + opponentForward;
                Vector2Int front2 = opponentPos + opponentForward + opponentForward;

                foreach (var myPos in me.pieces)
                {
                    if (myPos.x == -1) continue;

                    if (myPos == front1)
                        score += BLOCK_DIRECT_SCORE;
                    else if (myPos == front2)
                        score += BLOCK_NEAR_SCORE;
                }

                if (DodgemRules.InBounds(front1, state) &&
                    state.IsCellPlayable(front1) &&
                    state.IsOccupied(front1))
                {
                    score += 24;
                }
            }
        }

        return score;
    }

    /// <summary>
    /// Tinh diem trap dua tren so nuoc di con lai.
    /// </summary>
    static int TrapScore(GameState state, int perspectiveIdx)
    {
        int score = 0;

        for (int i = 0; i < state.NumPlayers; i++)
        {
            int moves = DodgemRules.CountLegalMoves(state, i);

            if (i == perspectiveIdx)
            {
                if (moves <= 1)
                    score -= TRAP_BONUS;
            }
            else
            {
                if (moves <= 1)
                    score += TRAP_BONUS;
            }
        }

        return score;
    }

    /// <summary>
    /// Thuong nhe neu dang la luot cua minh va co nuoc thoat ngay.
    /// </summary>
    static int TurnAdvantageScore(GameState state, int perspectiveIdx)
    {
        if (state.currentPlayerIndex != perspectiveIdx)
            return 0;

        int immediateEscapes = DodgemRules.CountImmediateEscapes(state, perspectiveIdx);
        if (immediateEscapes > 0)
            return 90 + immediateEscapes * 20;

        return 0;
    }

    #endregion

    #region Distance Map

    /// <summary>
    /// Tao distance map toi exit cells bang reverse BFS.
    /// </summary>
    static int[,] BuildDistanceMap(GameState state, PlayerData player)
    {
        int[,] dist = new int[state.boardWidth, state.boardHeight];

        for (int x = 0; x < state.boardWidth; x++)
            for (int y = 0; y < state.boardHeight; y++)
                dist[x, y] = INF;

        if (player == null || player.exitCells == null || player.exitCells.Length == 0)
            return dist;

        var queue = new Queue<Vector2Int>();

        foreach (var exit in player.exitCells)
        {
            if (!DodgemRules.InBounds(exit, state)) continue;
            if (!state.IsCellPlayable(exit)) continue;

            if (dist[exit.x, exit.y] > 0)
            {
                dist[exit.x, exit.y] = 0;
                queue.Enqueue(exit);
            }
        }

        // Reverse dirs: neu tu A co the di den B theo dir,
        // thi khi BFS nguoc tu B, ta quay lai A bang -dir.
        var forwardDirs = player.ValidDirs();

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int baseDist = dist[current.x, current.y];

            foreach (var dir in forwardDirs)
            {
                Vector2Int prev = current - dir;

                if (!DodgemRules.InBounds(prev, state)) continue;
                if (!state.IsCellPlayable(prev)) continue;

                int newDist = baseDist + 1;
                if (newDist < dist[prev.x, prev.y])
                {
                    dist[prev.x, prev.y] = newDist;
                    queue.Enqueue(prev);
                }
            }
        }

        return dist;
    }

    #endregion
}