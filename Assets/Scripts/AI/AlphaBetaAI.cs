using System.Collections.Generic;
using UnityEngine;

// ================================================================
// AlphaBetaAI - Giai doan 5
// - Trien khai IGameAI
// - Paranoid alpha-beta cho nhieu nguoi choi
// - Move ordering uu tien:
//   1) thang ngay
//   2) thoat ngay
//   3) lam doi thu rat it / het nuoc
//   4) block tot
//   5) toi EvalFunction
// ================================================================
public class AlphaBetaAI : IGameAI
{
    private readonly int maxDepth;
    private readonly int myPlayerIndex;

    const int WIN_SCORE = 1000000;

    public string DisplayName => "Alpha-Beta";

    public AlphaBetaAI(int depth, int myPlayerIndex)
    {
        this.maxDepth = Mathf.Max(1, depth);
        this.myPlayerIndex = myPlayerIndex;
    }

    public GameState BestMove(GameState state)
    {
        if (state == null) return null;

        var children = DodgemRules.GetChildren(state);
        if (children == null || children.Count == 0) return null;

        OrderMoves(children);

        GameState bestChild = null;
        int bestScore = int.MinValue;

        int alpha = int.MinValue + 1;
        int beta  = int.MaxValue - 1;

        foreach (var child in children)
        {
            int score = Search(child, maxDepth - 1, alpha, beta);

            if (score > bestScore || bestChild == null)
            {
                bestScore = score;
                bestChild = child;
            }

            if (score > alpha)
                alpha = score;
        }

        return bestChild;
    }

    int Search(GameState state, int depth, int alpha, int beta)
    {
        if (depth <= 0 || state.IsTerminal())
            return EvalFunction.Eval(state, myPlayerIndex);

        var children = DodgemRules.GetChildren(state);
        if (children == null || children.Count == 0)
            return EvalFunction.Eval(state, myPlayerIndex);

        OrderMoves(children);

        bool isMyTurn = state.currentPlayerIndex == myPlayerIndex;

        if (isMyTurn)
        {
            int best = int.MinValue;

            foreach (var child in children)
            {
                int score = Search(child, depth - 1, alpha, beta);
                if (score > best) best = score;
                if (score > alpha) alpha = score;
                if (beta <= alpha) break;
            }

            return best;
        }
        else
        {
            int best = int.MaxValue;

            foreach (var child in children)
            {
                int score = Search(child, depth - 1, alpha, beta);
                if (score < best) best = score;
                if (score < beta) beta = score;
                if (beta <= alpha) break;
            }

            return best;
        }
    }

    void OrderMoves(List<GameState> children)
    {
        children.Sort((a, b) =>
        {
            int sa = QuickMoveScore(a);
            int sb = QuickMoveScore(b);
            return sb.CompareTo(sa);
        });
    }

    int QuickMoveScore(GameState state)
    {
        var winner = state.Winner();
        if (winner != null)
        {
            if (winner.playerIndex == myPlayerIndex) return WIN_SCORE;
            return -WIN_SCORE;
        }

        int score = 0;

        // 1) Uu tien nuoc lam minh thoat them quan
        score += state.players[myPlayerIndex].escaped * 20000;

        // 2) Danh gia mobility doi thu
        int oppMobility = 0;
        int trappedOpps = 0;

        var tmp = state.Clone();
        for (int i = 0; i < state.NumPlayers; i++)
        {
            if (i == myPlayerIndex) continue;

            tmp.currentPlayerIndex = i;
            int moves = DodgemRules.GetChildren(tmp).Count;

            oppMobility += moves;
            if (moves == 0) trappedOpps++;
            else if (moves == 1) score += 500;
        }

        score += trappedOpps * 4000;
        score -= oppMobility * 10;

        // 3) Uu tien neu minh con nhieu lua chon
        tmp.currentPlayerIndex = myPlayerIndex;
        int myMobility = DodgemRules.GetChildren(tmp).Count;
        score += myMobility * 12;

        // 4) Block pressure
        score += BlockPressureScore(state, myPlayerIndex);

        // 5) Eval tong quan
        score += EvalFunction.Eval(state, myPlayerIndex);

        return score;
    }

    int BlockPressureScore(GameState state, int perspectiveIdx)
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

                Vector2Int fwd = opp.ForwardDir();
                Vector2Int front1 = oppPos + fwd;
                Vector2Int front2 = oppPos + fwd + fwd;

                foreach (var myPos in me.pieces)
                {
                    if (myPos.x == -1) continue;

                    if (myPos == front1) score += 220;
                    else if (myPos == front2) score += 90;
                }
            }
        }

        return score;
    }
}