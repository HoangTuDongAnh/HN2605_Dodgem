using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI alpha-beta theo huong paranoid cho nhieu nguoi choi.
/// </summary>
public class AlphaBetaAI : IGameAI
{
    #region Fields

    private readonly int maxDepth;
    private readonly int myPlayerIndex;

    private const int WIN_SCORE = 1000000;
    private const int REPEAT_PENALTY_SOFT  =   500;  // count=1
    private const int REPEAT_PENALTY_HARD  =  2000;  // count=2
    private const int REPEAT_PENALTY_FATAL = WIN_SCORE; // count>=maxRepeat
    private const int MAX_REPEAT_COUNT = 3;

    private Dictionary<string, int> repetitionHistory;

    #endregion

    #region Properties

    public string DisplayName => "Alpha-Beta";

    #endregion

    #region Constructor

    /// <summary>
    /// Khoi tao AI voi depth va player index tuong ung.
    /// </summary>
    public AlphaBetaAI(int depth, int myPlayerIndex)
    {
        this.maxDepth = Mathf.Max(1, depth);
        this.myPlayerIndex = myPlayerIndex;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Tim nuoc di tot nhat cho state hien tai.
    /// </summary>
    public GameState BestMove(GameState state)
    {
        return BestMove(state, null);
    }

    public GameState BestMove(GameState state, Dictionary<string, int> stateHistory)
    {
        if (state == null) return null;

        repetitionHistory = stateHistory;

        var children = DodgemRules.GetChildren(state);
        if (children == null || children.Count == 0) return null;

        int effectiveDepth = GetEffectiveDepth(state, children.Count);

        OrderMoves(children);

        GameState bestChild = null;
        int bestScore = int.MinValue;

        int alpha = int.MinValue + 1;
        int beta = int.MaxValue - 1;

        foreach (var child in children)
        {
            int score = Search(child, effectiveDepth - 1, alpha, beta);

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

    #endregion

    #region Search

    /// <summary>
    /// Duyet cay alpha-beta theo huong paranoid minimax.
    /// </summary>
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

    #endregion

    #region Repetition Penalty

    /// <summary>
    /// Tinh muc phat cho state da xuat hien truoc do.
    /// Cang lap nhieu lan thi phat cang nang, tranh phat fatal o muc draw threshold.
    /// </summary>
    int RepetitionPenalty(GameState state)
    {
        if (repetitionHistory == null) return 0;

        string key = state.StateKey();
        if (!repetitionHistory.TryGetValue(key, out int count))
            return 0;

        if (count >= MAX_REPEAT_COUNT - 1)
            return -REPEAT_PENALTY_FATAL; // gan muc hoa, tranh tuyet doi

        if (count == 2)
            return -REPEAT_PENALTY_HARD;

        if (count == 1)
            return -REPEAT_PENALTY_SOFT;

        return 0;
    }

    #endregion

    #region Move Ordering

    /// <summary>
    /// Sap xep nuoc di de cat tia tot hon.
    /// </summary>
    void OrderMoves(List<GameState> children)
    {
        children.Sort((a, b) =>
        {
            int scoreA = QuickMoveScore(a);
            int scoreB = QuickMoveScore(b);
            return scoreB.CompareTo(scoreA);
        });
    }

    /// <summary>
    /// Tinh diem nhanh de uu tien thu tu nuoc di.
    /// </summary>
    int QuickMoveScore(GameState state)
    {
        var winner = state.Winner();
        if (winner != null)
        {
            if (winner.playerIndex == myPlayerIndex) return WIN_SCORE;
            return -WIN_SCORE;
        }

        int score = RepetitionPenalty(state);
        if (score <= -REPEAT_PENALTY_FATAL) return score; // cat tia ngay

        // Uu tien thoat ngay
        score += state.players[myPlayerIndex].escaped * 20000;
        score += DodgemRules.CountImmediateEscapes(state, myPlayerIndex) * 2000;

        // Uu tien lam doi thu it nuoc
        int opponentMobility = 0;
        int trappedOpponents = 0;

        for (int i = 0; i < state.NumPlayers; i++)
        {
            if (i == myPlayerIndex) continue;

            int moves = DodgemRules.CountLegalMoves(state, i);

            opponentMobility += moves;
            if (moves == 0) trappedOpponents++;
            else if (moves == 1) score += 500;
        }

        score += trappedOpponents * 4000;
        score -= opponentMobility * 10;

        // Uu tien minh con nhieu lua chon
        int myMobility = DodgemRules.CountLegalMoves(state, myPlayerIndex);
        score += myMobility * 12;

        // Uu tien block
        score += BlockPressureScore(state, myPlayerIndex);

        return score;
    }

    /// <summary>
    /// Uoc luong muc do chan duong doi thu.
    /// </summary>
    int BlockPressureScore(GameState state, int perspectiveIdx)
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

                Vector2Int forward = opponent.ForwardDir();
                Vector2Int front1 = opponentPos + forward;
                Vector2Int front2 = opponentPos + forward + forward;

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

    #endregion

    #region Depth Control

    /// <summary>
    /// Dieu chinh depth thuc te de giu AI muot hon khi nhieu bot.
    /// </summary>
    int GetEffectiveDepth(GameState state, int rootMoveCount)
    {
        int depth = maxDepth;

        // Nhiều người chơi => giảm nhẹ depth
        if (state.NumPlayers >= 3)
            depth -= 1;

        if (state.NumPlayers >= 4)
            depth -= 1;

        // Branching lớn => giảm thêm
        if (rootMoveCount >= 18)
            depth -= 1;

        if (rootMoveCount >= 28)
            depth -= 1;

        // Giữ chênh lệch độ khó nhưng không để tụt quá sâu
        return Mathf.Clamp(depth, 1, maxDepth);
    }

    #endregion
}