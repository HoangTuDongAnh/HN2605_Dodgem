public static class EvalFunction
{
    // ================================================================
    // Trắng (AI) thoát sang PHẢI (x tăng)
    // Đen (người) thoát lên TRÊN (y tăng)
    //
    // Nguyên tắc điểm: dương = tốt cho Trắng, âm = tốt cho Đen
    // ================================================================

    public static int Eval(GameState state)
    {
        // Kết thúc tuyệt đối
        if (state.whiteEscaped == 2) return  20000;
        if (state.blackEscaped == 2) return -20000;

        int score = 0;

        // ── 1. Thưởng lớn cho quân đã thoát ─────────────────────────
        score += state.whiteEscaped * 500;
        score -= state.blackEscaped * 500;

        int whitePiecesOnBoard = 0;
        int blackPiecesOnBoard = 0;

        // ── 2. Điểm tiến về đích ─────────────────────────────────────
        foreach (var wp in state.whitePieces)
        {
            if (wp.x == -1) continue;
            whitePiecesOnBoard++;
            // Trắng tiến phải: x=0→0, x=1→20, x=2→50 (phi tuyến — gần đích thưởng nhiều hơn)
            score += wp.x == 0 ? 0 : wp.x == 1 ? 20 : 50;
        }

        foreach (var bp in state.blackPieces)
        {
            if (bp.x == -1) continue;
            blackPiecesOnBoard++;
            // Đen tiến lên: y=0→0, y=1→20, y=2→50
            score -= bp.y == 0 ? 0 : bp.y == 1 ? 20 : 50;
        }

        // ── 3. Thưởng AI còn quân — tránh để quân bị kẹt ────────────
        // Nếu Trắng không còn quân nào trên bàn mà chưa thắng → rất tệ
        if (whitePiecesOnBoard == 0 && state.whiteEscaped < 2) score -= 1000;
        if (blackPiecesOnBoard == 0 && state.blackEscaped < 2) score += 1000;

        // ── 4. Điểm cản (quan trọng nhất trong Dodgem) ───────────────
        score += BlockingScore(state);

        // ── 5. Phạt nếu AI bị chặn hoàn toàn ────────────────────────
        // Đếm nước đi có thể của Trắng — càng ít càng xấu cho AI
        int whiteMoves = CountMoves(state, isWhite: true);
        int blackMoves = CountMoves(state, isWhite: false);
        if (whiteMoves == 0) score -= 800;  // Trắng hết nước đi rất xấu
        if (blackMoves == 0) score += 400;  // Đen hết nước đi tốt cho Trắng

        return score;
    }

    static int BlockingScore(GameState state)
    {
        int score = 0;
        foreach (var bp in state.blackPieces)
        {
            if (bp.x == -1) continue;
            foreach (var wp in state.whitePieces)
            {
                if (wp.x == -1) continue;

                // ── Trắng cản Đen đi lên ──────────────────────────────
                // Trắng đứng ngay trên Đen (cùng cột, wp.y = bp.y+1)
                if (wp.x == bp.x && wp.y == bp.y + 1)
                    score += 60;  // cản trực tiếp
                // Trắng cách 2 ô nhưng cùng cột
                else if (wp.x == bp.x && wp.y == bp.y + 2)
                    score += 30;  // cản gián tiếp

                // ── Đen cản Trắng đi phải ─────────────────────────────
                // Đen đứng ngay bên phải Trắng (cùng hàng, bp.x = wp.x+1)
                if (bp.y == wp.y && bp.x == wp.x + 1)
                    score -= 60;
                else if (bp.y == wp.y && bp.x == wp.x + 2)
                    score -= 30;

                // ── Thưởng thêm: Trắng cản toàn bộ đường thoát Đen ───
                // Nếu Trắng đứng trên hàng y=2 cùng cột với Đen đang ở y=2
                // → Đen không thể thoát lên
                if (wp.y == 2 && bp.y == 2 && wp.x == bp.x)
                    score += 80;
            }
        }
        return score;
    }

    // Đếm số nước đi hợp lệ của một bên (dùng DodgemRules)
    static int CountMoves(GameState state, bool isWhite)
    {
        // Tạo state giả với lượt của bên cần đếm
        var tmp = state.Clone();
        tmp.isWhiteTurn = isWhite;
        return DodgemRules.GetChildren(tmp).Count;
    }
}