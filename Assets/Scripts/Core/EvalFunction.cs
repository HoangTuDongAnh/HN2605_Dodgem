using UnityEngine;
using System.Collections.Generic;

public static class EvalFunction
{
    const int WIN_SCORE = 100000;

    // Trong so tong quat
    const int ESCAPED_PIECE_SCORE       = 2200;
    const int READY_TO_ESCAPE_SCORE     = 380;
    const int REACHABLE_EXIT_BONUS_BASE = 260;
    const int UNREACHABLE_PENALTY       = 180;
    const int MOBILITY_WEIGHT_SELF      = 14;
    const int MOBILITY_WEIGHT_OPP       = 9;
    const int BLOCK_DIRECT_SCORE        = 90;
    const int BLOCK_NEAR_SCORE          = 45;
    const int TRAP_BONUS                = 160;
    const int ENDGAME_ESCAPED_BONUS     = 300;
    const int ENDGAME_READY_BONUS       = 180;

    public static int Eval(GameState state)
    {
        return Eval(state, 0);
    }

    public static int Eval(GameState state, int perspectivePlayerIdx)
    {
        var winner = state.Winner();
        if (winner != null)
            return winner.playerIndex == perspectivePlayerIdx ? WIN_SCORE : -WIN_SCORE;

        int score = 0;

        for (int i = 0; i < state.NumPlayers; i++)
        {
            int sign = (i == perspectivePlayerIdx) ? 1 : -1;
            score += sign * ScoreForPlayer(state, i);
        }

        score += BlockingScore(state, perspectivePlayerIdx);
        score += MobilityScore(state, perspectivePlayerIdx);
        score += TrapScore(state, perspectivePlayerIdx);
        score += TurnAdvantageScore(state, perspectivePlayerIdx);

        return score;
    }

    static int ScoreForPlayer(GameState state, int playerIdx)
    {
        var player = state.players[playerIdx];
        int score = 0;

        // 1) Thuong so quan da thoat
        score += player.escaped * ESCAPED_PIECE_SCORE;

        int activePieces = 0;
        int readyToEscape = 0;
        int reachablePieces = 0;
        int totalDist = 0;

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

            int dist = DistanceToNearestExit(state, player, pos);
            if (dist >= 9999)
            {
                score -= UNREACHABLE_PENALTY;
                continue;
            }

            reachablePieces++;
            totalDist += dist;

            // Cang gan exit cang duoc thuong
            // dist=0 => diem cao nhat
            score += Mathf.Max(0, REACHABLE_EXIT_BONUS_BASE - dist * 42);

            // Thuong them neu gan thoat that su
            if (dist == 0) score += 120;
            else if (dist == 1) score += 80;
            else if (dist == 2) score += 35;
        }

        // 2) Thuong neu nhieu quan con co duong den exit
        score += reachablePieces * 55;

        // 3) Phat neu quan con lai nhieu ma chua tiep can duoc lo thoat
        if (activePieces > 0 && reachablePieces < activePieces)
            score -= (activePieces - reachablePieces) * 70;

        // 4) Endgame: khi it quan, uu tien thoat nhanh hon
        if (activePieces <= 2)
        {
            score += player.escaped * ENDGAME_ESCAPED_BONUS;
            score += readyToEscape * ENDGAME_READY_BONUS;
        }

        // 5) Thuong nhe neu tong khoang cach nho
        if (reachablePieces > 0)
            score += Mathf.Max(0, 140 - totalDist * 10);

        return score;
    }

    static int MobilityScore(GameState state, int perspectiveIdx)
    {
        var tmp = state.Clone();

        tmp.currentPlayerIndex = perspectiveIdx;
        int myMoves = DodgemRules.GetChildren(tmp).Count;

        if (myMoves == 0) return -2500;

        int oppMoves = 0;
        int trappedOpponents = 0;

        for (int i = 0; i < state.NumPlayers; i++)
        {
            if (i == perspectiveIdx) continue;

            tmp.currentPlayerIndex = i;
            int c = DodgemRules.GetChildren(tmp).Count;
            oppMoves += c;
            if (c == 0) trappedOpponents++;
        }

        int score = 0;
        score += myMoves * MOBILITY_WEIGHT_SELF;
        score -= oppMoves * MOBILITY_WEIGHT_OPP;
        score += trappedOpponents * 220;

        return score;
    }

    static int BlockingScore(GameState state, int perspectiveIdx)
    {
        int score = 0;
        var me = state.players[perspectiveIdx];

        for (int oppIdx = 0; oppIdx < state.NumPlayers; oppIdx++)
        {
            if (oppIdx == perspectiveIdx) continue;
            var opp = state.players[oppIdx];

            foreach (var oppPos in opp.pieces)
            {
                if (oppPos.x == -1) continue;
                if (!state.IsCellPlayable(oppPos)) continue;

                Vector2Int oppFwd = opp.ForwardDir();
                Vector2Int front1 = oppPos + oppFwd;
                Vector2Int front2 = oppPos + oppFwd + oppFwd;

                foreach (var myPos in me.pieces)
                {
                    if (myPos.x == -1) continue;

                    if (myPos == front1)
                        score += BLOCK_DIRECT_SCORE;
                    else if (myPos == front2)
                        score += BLOCK_NEAR_SCORE;
                }

                // Thuong them neu o truoc mat doi thu la o hop le nhung dang bi chan
                if (DodgemRules.InBounds(front1, state) && state.IsCellPlayable(front1) && state.IsOccupied(front1))
                    score += 24;
            }
        }

        return score;
    }

    static int TrapScore(GameState state, int perspectiveIdx)
    {
        int score = 0;
        var tmp = state.Clone();

        for (int i = 0; i < state.NumPlayers; i++)
        {
            tmp.currentPlayerIndex = i;
            int moves = DodgemRules.GetChildren(tmp).Count;

            if (i == perspectiveIdx)
            {
                if (moves <= 1) score -= TRAP_BONUS;
            }
            else
            {
                if (moves <= 1) score += TRAP_BONUS;
            }
        }

        return score;
    }

    static int TurnAdvantageScore(GameState state, int perspectiveIdx)
    {
        // Thuong nhe neu dang la luot cua minh va minh co nuoc thoat ngay
        if (state.currentPlayerIndex != perspectiveIdx)
            return 0;

        var me = state.players[perspectiveIdx];
        foreach (var pos in me.pieces)
        {
            if (pos.x == -1) continue;
            if (me.CanEscapeFrom(pos))
                return 90;
        }

        return 0;
    }

    static int DistanceToNearestExit(GameState state, PlayerData player, Vector2Int start)
    {
        if (player.exitCells == null || player.exitCells.Length == 0) return 9999;
        if (!state.IsCellPlayable(start)) return 9999;

        var exitSet = new HashSet<Vector2Int>();
        foreach (var e in player.exitCells)
        {
            if (state.IsCellPlayable(e))
                exitSet.Add(e);
        }

        if (exitSet.Count == 0) return 9999;
        if (exitSet.Contains(start)) return 0;

        var visited = new bool[state.boardWidth, state.boardHeight];
        var q = new Queue<Node>();

        visited[start.x, start.y] = true;
        q.Enqueue(new Node(start, 0));

        Vector2Int[] dirs = player.ValidDirs();

        while (q.Count > 0)
        {
            var cur = q.Dequeue();

            foreach (var dir in dirs)
            {
                Vector2Int next = cur.pos + dir;

                if (!DodgemRules.InBounds(next, state)) continue;
                if (!state.IsCellPlayable(next)) continue;
                if (visited[next.x, next.y]) continue;

                if (exitSet.Contains(next))
                    return cur.dist + 1;

                visited[next.x, next.y] = true;
                q.Enqueue(new Node(next, cur.dist + 1));
            }
        }

        return 9999;
    }

    struct Node
    {
        public Vector2Int pos;
        public int dist;

        public Node(Vector2Int pos, int dist)
        {
            this.pos = pos;
            this.dist = dist;
        }
    }
}