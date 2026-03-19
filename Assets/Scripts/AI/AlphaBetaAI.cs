using System.Collections.Generic;

// ================================================================
// AlphaBetaAI — Paranoid Alpha-Beta cho N người chơi
//
// Paranoid: Bot coi TẤT CẢ phe khác là đối thủ (minimizer).
//   - Lượt của mình (myIdx) → MAX node
//   - Lượt của bất kỳ phe nào khác → MIN node
//
// Mỗi bot được tạo với myPlayerIndex riêng → mỗi bot tự maximize
// điểm của chính mình, không phải của Trắng.
//
// Với 2 người: Paranoid = Minimax chuẩn.
// Với N > 2:   Paranoid là approximation hợp lý, dễ implement và
//               đã được chứng minh chơi tốt trong thực tế.
// ================================================================

public class AlphaBetaAI
{
    private readonly int maxDepth;
    private readonly int myIdx;   // index cua phe ma bot nay dai dien

    // myPlayerIndex: bot nay dang choi phe nao (0=Trang, 1=Den, ...)
    public AlphaBetaAI(int depth = 6, int myPlayerIndex = 0)
    {
        maxDepth = depth;
        myIdx    = myPlayerIndex;
    }

    // ── BestMove: entry point ─────────────────────────────────────
    public GameState BestMove(GameState state)
    {
        var children = DodgemRules.GetChildren(state);
        if (children.Count == 0) return null;
        if (children.Count == 1) return children[0];

        int alpha     = int.MinValue + 1;
        int beta      = int.MaxValue - 1;
        GameState best = null;

        // Move ordering: uu tien nuoc di co eval cao (cai thien pruning)
        children.Sort((a, b) =>
            EvalFunction.Eval(b, myIdx).CompareTo(EvalFunction.Eval(a, myIdx)));

        foreach (var child in children)
        {
            int val = Negamax(child, maxDepth - 1, alpha, beta);
            if (val > alpha)
            {
                alpha = val;
                best  = child;
            }
        }

        return best ?? children[0];
    }

    // ── Paranoid Negamax voi Alpha-Beta ───────────────────────────
    // Neu den luot cua myIdx → maximize
    // Neu den luot cua bat ky phe nao khac → minimize
    int Negamax(GameState state, int depth, int alpha, int beta)
    {
        if (depth == 0 || state.IsTerminal())
            return EvalFunction.Eval(state, myIdx);

        var children = DodgemRules.GetChildren(state);
        if (children.Count == 0)
            return EvalFunction.Eval(state, myIdx);

        bool isMyTurn = (state.currentPlayerIndex == myIdx);

        if (isMyTurn)
        {
            // MAX node
            children.Sort((a, b) =>
                EvalFunction.Eval(b, myIdx).CompareTo(EvalFunction.Eval(a, myIdx)));

            int best = int.MinValue + 1;
            foreach (var child in children)
            {
                int val = Negamax(child, depth - 1, alpha, beta);
                if (val > best) best = val;
                if (val > alpha) alpha = val;
                if (alpha >= beta) break; // beta cutoff
            }
            return best;
        }
        else
        {
            // MIN node (bat ky doi thu nao)
            children.Sort((a, b) =>
                EvalFunction.Eval(a, myIdx).CompareTo(EvalFunction.Eval(b, myIdx)));

            int best = int.MaxValue - 1;
            foreach (var child in children)
            {
                int val = Negamax(child, depth - 1, alpha, beta);
                if (val < best) best = val;
                if (val < beta) beta = val;
                if (alpha >= beta) break; // alpha cutoff
            }
            return best;
        }
    }
}