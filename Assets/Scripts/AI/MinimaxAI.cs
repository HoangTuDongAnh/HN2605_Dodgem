using System.Collections.Generic;
using UnityEngine;

// ================================================================
// MinimaxAI - dung cho muc Easy
// - Ho tro nhieu nguoi choi theo huong paranoid minimax
// - Dung EvalFunction.Eval(..., myPlayerIndex)
// - Chu y: khong cat tia alpha-beta, co y de giu muc de
// ================================================================
public class MinimaxAI : IGameAI
{
    private readonly int maxDepth;
    private readonly int myPlayerIndex;

    public string DisplayName => "Minimax";

    public MinimaxAI(int depth, int myPlayerIndex)
    {
        this.maxDepth = Mathf.Max(1, depth);
        this.myPlayerIndex = myPlayerIndex;
    }

    public GameState BestMove(GameState state)
    {
        if (state == null) return null;

        var children = DodgemRules.GetChildren(state);
        if (children == null || children.Count == 0) return null;

        GameState bestState = null;
        int bestVal = int.MinValue;

        foreach (var child in children)
        {
            int val = Search(child, maxDepth - 1);
            if (bestState == null || val > bestVal)
            {
                bestVal = val;
                bestState = child;
            }
        }

        return bestState;
    }

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
}