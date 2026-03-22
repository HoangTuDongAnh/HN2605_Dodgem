using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI minimax co ban, dung cho muc do de.
/// </summary>
public class MinimaxAI : IGameAI
{
    #region Fields

    private readonly int maxDepth;
    private readonly int myPlayerIndex;

    private const int WIN_SCORE = 1000000;
    private const int REPEAT_PENALTY_SOFT  =   500;
    private const int REPEAT_PENALTY_HARD  =  2000;
    private const int REPEAT_PENALTY_FATAL = WIN_SCORE;
    private const int MAX_REPEAT_COUNT = 3;

    private Dictionary<string, int> repetitionHistory;

    #endregion

    #region Properties

    public string DisplayName => "Minimax";

    #endregion

    #region Constructor

    /// <summary>
    /// Khoi tao AI minimax voi depth va player index.
    /// </summary>
    public MinimaxAI(int depth, int myPlayerIndex)
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

        GameState bestState = null;
        int bestValue = int.MinValue;

        foreach (var child in children)
        {
            int penalty = RepetitionPenalty(child);
            int value = Search(child, effectiveDepth - 1) + penalty;
            if (bestState == null || value > bestValue)
            {
                bestValue = value;
                bestState = child;
            }
        }

        return bestState;
    }

    #endregion

    #region Search

    /// <summary>
    /// Duyet cay minimax theo huong paranoid.
    /// </summary>
    int Search(GameState state, int depth)
    {
        if (depth <= 0 || state.IsTerminal())
            return EvalFunction.Eval(state, myPlayerIndex);

        var children = DodgemRules.GetChildren(state);
        if (children == null || children.Count == 0)
            return EvalFunction.Eval(state, myPlayerIndex);

        bool isMyTurn = state.currentPlayerIndex == myPlayerIndex;

        if (isMyTurn)
        {
            int best = int.MinValue;
            foreach (var child in children)
            {
                int score = Search(child, depth - 1);
                if (score > best) best = score;
            }
            return best;
        }
        else
        {
            int best = int.MaxValue;
            foreach (var child in children)
            {
                int score = Search(child, depth - 1);
                if (score < best) best = score;
            }
            return best;
        }
    }

    #endregion

    #region Repetition Penalty

    int RepetitionPenalty(GameState state)
    {
        if (repetitionHistory == null) return 0;

        string key = state.StateKey();
        if (!repetitionHistory.TryGetValue(key, out int count))
            return 0;

        if (count >= MAX_REPEAT_COUNT - 1)
            return -REPEAT_PENALTY_FATAL;

        if (count == 2)
            return -REPEAT_PENALTY_HARD;

        if (count == 1)
            return -REPEAT_PENALTY_SOFT;

        return 0;
    }

    #endregion

    #region Depth Control

    /// <summary>
    /// Dieu chinh depth thuc te de giu AI muot hon khi nhieu bot.
    /// </summary>
    int GetEffectiveDepth(GameState state, int rootMoveCount)
    {
        int depth = maxDepth;

        if (state.NumPlayers >= 3)
            depth -= 1;

        if (rootMoveCount >= 20)
            depth -= 1;

        return Mathf.Clamp(depth, 1, maxDepth);
    }

    #endregion
}