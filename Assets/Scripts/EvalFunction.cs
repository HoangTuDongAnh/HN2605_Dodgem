using UnityEngine;

// ================================================================
// EvalFunction — hàm đánh giá trạng thái cho AI
//
// THAY ĐỔI SO VỚI BẢN CŨ:
//   - Bỏ bảng điểm cố định {0,5,10} → tính theo tỉ lệ boardSize
//   - Bỏ hardcode boardSize=3 trong BlockingScore
//   - Thêm Eval(state, playerIdx) cho Paranoid AI sau này
//   - Eval(state) không tham số vẫn hoạt động (backward-compat)
//
// Quy ước điểm: dương = tốt cho players[0] (Trắng/AI)
// ================================================================

public static class EvalFunction
{
    // ── Eval không tham số — backward-compat cho AlphaBetaAI ─────
    // Luôn đánh giá từ góc nhìn của players[0] (Trắng)
    public static int Eval(GameState state)
    {
        return Eval(state, perspectivePlayerIdx: 0);
    }

    // ── Eval có tham số — dùng cho Paranoid AI (Giai đoạn 3) ─────
    // perspectivePlayerIdx: phe nào muốn maximize điểm
    public static int Eval(GameState state, int perspectivePlayerIdx)
    {
        int N = state.boardSize;

        // Kiểm tra kết thúc
        var winner = state.Winner();
        if (winner != null)
            return winner.playerIndex == perspectivePlayerIdx ? 20000 : -20000;

        int score = 0;

        for (int i = 0; i < state.NumPlayers; i++)
        {
            var player  = state.players[i];
            int sign    = (i == perspectivePlayerIdx) ? 1 : -1;
            int pScore  = ScoreForPlayer(player, N);
            score      += sign * pScore;
        }

        // Thưởng thêm: phe hiện tại có nhiều nước đi hơn thì tốt hơn
        score += MobilityBonus(state, perspectivePlayerIdx);

        return score;
    }

    // ── Điểm của một phe ─────────────────────────────────────────
    static int ScoreForPlayer(PlayerData player, int boardSize)
    {
        int score = 0;
        int N     = boardSize;

        // Thưởng lớn mỗi quân đã thoát
        score += player.escaped * 500;

        // Điểm tiến gần đích — phi tuyến, gần cuối thưởng nhiều hơn
        foreach (var pos in player.pieces)
        {
            if (pos.x == -1) continue;

            // Khoảng cách đến đích (0 = ngay trước cửa thoát)
            int distToEscape = DistanceToEscape(pos, player.escapeDir, N);
            int maxDist      = N - 1;

            // Điểm phi tuyến: càng gần đích điểm càng cao
            // dist=N-1 → 0, dist=1 → 50, dist=0 → 100
            int posScore = (int)(100f * (1f - (float)distToEscape / maxDist));
            score += posScore;
        }

        return score;
    }

    // ── Khoảng cách đến ô thoát ───────────────────────────────────
    static int DistanceToEscape(Vector2Int pos, EscapeDirection dir, int boardSize)
    {
        int N = boardSize;
        switch (dir)
        {
            case EscapeDirection.Right:  return (N - 1) - pos.x;  // cần đi thêm bao nhiêu ô sang phải
            case EscapeDirection.Top:    return (N - 1) - pos.y;
            case EscapeDirection.Left:   return pos.x;             // cần đi thêm bao nhiêu ô sang trái
            case EscapeDirection.Bottom: return pos.y;
            default: return N;
        }
    }

    // ── Điểm cản ─────────────────────────────────────────────────
    // Kiểm tra quân của phe perspective có đang cản quân đối thủ không
    static int BlockingScore(GameState state, int perspectiveIdx)
    {
        int score = 0;
        int N     = state.boardSize;
        var me    = state.players[perspectiveIdx];

        for (int oppIdx = 0; oppIdx < state.NumPlayers; oppIdx++)
        {
            if (oppIdx == perspectiveIdx) continue;
            var opp = state.players[oppIdx];

            foreach (var myPos in me.pieces)
            {
                if (myPos.x == -1) continue;
                foreach (var oppPos in opp.pieces)
                {
                    if (oppPos.x == -1) continue;

                    // Quân của tôi đứng ngay trên đường tiến của đối thủ
                    // → kiểm tra myPos có nằm phía trước oppPos theo hướng đi của đối thủ
                    var oppFwd = opp.ForwardDir();

                    // Cản trực tiếp: myPos = oppPos + oppFwd
                    if (myPos == oppPos + oppFwd)
                        score += 60;
                    // Cản gián tiếp: myPos = oppPos + 2*oppFwd
                    else if (myPos == oppPos + oppFwd + oppFwd)
                        score += 30;
                }
            }
        }
        return score;
    }

    // ── Mobility bonus ────────────────────────────────────────────
    // Thưởng nếu phe perspective có nhiều nước đi hơn đối thủ
    static int MobilityBonus(GameState state, int perspectiveIdx)
    {
        var tmp = state.Clone();
        tmp.currentPlayerIndex = perspectiveIdx;
        int myMoves = DodgemRules.GetChildren(tmp).Count;

        if (myMoves == 0) return -800;   // hết nước đi rất xấu

        int oppMoves = 0;
        for (int i = 0; i < state.NumPlayers; i++)
        {
            if (i == perspectiveIdx) continue;
            tmp.currentPlayerIndex = i;
            oppMoves += DodgemRules.GetChildren(tmp).Count;
        }
        if (oppMoves == 0) return 400;   // đối thủ hết nước đi tốt

        return 0;
    }
}