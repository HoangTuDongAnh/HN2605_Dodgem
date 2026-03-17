public class MinimaxAI
{
    private int maxDepth;

    public MinimaxAI(int depth = 4) { maxDepth = depth; }

    // MaxVal(u, h) - Hàm đệ quy cho đỉnh Trắng (MAX)
    public int MaxVal(GameState state, int h)
    {
        // Điều kiện dừng: đỉnh lá hoặc đỉnh kết thúc
        if (h == 0 || state.IsTerminal())
            return EvalFunction.Eval(state);

        int maxScore = int.MinValue;
        var children = DodgemRules.GetChildren(state);

        if (children.Count == 0)        // Không có nước đi
            return EvalFunction.Eval(state);

        foreach (var child in children)
            maxScore = System.Math.Max(maxScore, MinVal(child, h - 1));

        return maxScore;
    }

    // MinVal(u, h) - Hàm đệ quy cho đỉnh Đen (MIN)
    public int MinVal(GameState state, int h)
    {
        if (h == 0 || state.IsTerminal())
            return EvalFunction.Eval(state);

        int minScore = int.MaxValue;
        var children = DodgemRules.GetChildren(state);

        if (children.Count == 0)
            return EvalFunction.Eval(state);

        foreach (var child in children)
            minScore = System.Math.Min(minScore, MaxVal(child, h - 1));

        return minScore;
    }

    // Minimax(u, v) - Chọn nước đi tốt nhất cho Trắng
    public GameState BestMove(GameState state)
    {
        int bestVal = int.MinValue;
        GameState bestState = null;
        var children = DodgemRules.GetChildren(state);

        foreach (var child in children)
        {
            int val = MinVal(child, maxDepth - 1);
            if (val >= bestVal)
            {
                bestVal  = val;
                bestState = child;
            }
        }
        return bestState;
    }
}