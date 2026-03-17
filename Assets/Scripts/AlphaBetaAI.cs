using System.Collections.Generic;

public class AlphaBetaAI
{
    private int maxDepth;

    public AlphaBetaAI(int depth = 6) { maxDepth = depth; }

    // ── MaxVal: đỉnh Trắng (MAX player) ──────────────────────────────
    int MaxVal(GameState state, int h, int alpha, int beta)
    {
        if (h == 0 || state.IsTerminal())
            return EvalFunction.Eval(state);

        var children = DodgemRules.GetChildren(state);
        if (children.Count == 0) return EvalFunction.Eval(state);

        // Sắp xếp: ưu tiên state có eval cao hơn trước (cải thiện pruning)
        children.Sort((a, b) => EvalFunction.Eval(b).CompareTo(EvalFunction.Eval(a)));

        foreach (var child in children)
        {
            int val = MinVal(child, h - 1, alpha, beta);
            if (val > alpha) alpha = val;
            if (alpha >= beta) break; // beta cutoff
        }
        return alpha;
    }

    // ── MinVal: đỉnh Đen (MIN player) ────────────────────────────────
    int MinVal(GameState state, int h, int alpha, int beta)
    {
        if (h == 0 || state.IsTerminal())
            return EvalFunction.Eval(state);

        var children = DodgemRules.GetChildren(state);
        if (children.Count == 0) return EvalFunction.Eval(state);

        // Sắp xếp: ưu tiên state có eval thấp hơn trước
        children.Sort((a, b) => EvalFunction.Eval(a).CompareTo(EvalFunction.Eval(b)));

        foreach (var child in children)
        {
            int val = MaxVal(child, h - 1, alpha, beta);
            if (val < beta) beta = val;
            if (alpha >= beta) break; // alpha cutoff
        }
        return beta;
    }

    // ── BestMove: chọn nước đi tốt nhất cho Trắng ────────────────────
    public GameState BestMove(GameState state)
    {
        int alpha = int.MinValue + 1; // tránh overflow khi negation
        int beta  = int.MaxValue - 1;
        GameState bestState = null;

        var children = DodgemRules.GetChildren(state);
        if (children.Count == 0) return null;

        // Sắp xếp lần đầu để xét nước đi hứa hẹn trước
        children.Sort((a, b) => EvalFunction.Eval(b).CompareTo(EvalFunction.Eval(a)));

        foreach (var child in children)
        {
            // FIX: truyền alpha/beta đúng thay vì dùng int.MinValue/MaxValue mỗi lần
            int val = MinVal(child, maxDepth - 1, alpha, beta);
            if (val > alpha)
            {
                alpha     = val;
                bestState = child;
            }
        }

        // Nếu vẫn null (tất cả val == int.MinValue+1), chọn nước đầu tiên
        return bestState ?? children[0];
    }
}