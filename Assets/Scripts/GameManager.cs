using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// ================================================================
// GameManager — điều phối toàn bộ luồng game
//
// THAY ĐỔI SO VỚI BẢN CŨ:
//   - Đọc cấu hình từ GameConfig (boardSize, numPlayers, types)
//   - Lượt chơi dùng currentPlayerIndex thay vì isWhiteTurn
//   - Hỗ trợ nhiều Bot chạy tuần tự
//   - Escape logic dùng boardSize thay vì hardcode y==2, y==3
//   - Backward-compat: nếu không có GameConfig → dùng mặc định 3x3
// ================================================================

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public BoardRenderer   boardRenderer;
    public TextMeshProUGUI statusText;
    public Button          restartButton;

    [Header("Config (tùy chọn — để trống = dùng mặc định 3x3)")]
    public GameConfig gameConfig;

    // ── Private state ─────────────────────────────────────────────
    private GameState        currentState;
    private AlphaBetaAI[]    bots;          // 1 bot instance cho mỗi phe
    private bool             isAnimating = false;

    // Chọn quân (chỉ dùng khi lượt Human)
    private Vector2Int       selectedPiece = new Vector2Int(-1, -1);
    private List<Vector2Int> validMoves    = new List<Vector2Int>();

    // ── Init ──────────────────────────────────────────────────────
    void Start()
    {
        if (boardRenderer == null)
        { Debug.LogError("[GameManager] boardRenderer chưa gán!"); return; }

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        boardRenderer.Init();
        StartGame();
    }

    void StartGame()
    {
        // Tạo state từ config (hoặc mặc định)
        currentState = GameState.CreateDefault(gameConfig);

        // Tạo bot cho mỗi phe Bot
        bots = new AlphaBetaAI[currentState.NumPlayers];
        for (int i = 0; i < currentState.NumPlayers; i++)
        {
            var p = currentState.players[i];
            if (p.type == PlayerType.Bot)
                bots[i] = new AlphaBetaAI(depth: p.botDepth);
        }

        Deselect();
        isAnimating = false;
        boardRenderer.Render(currentState);

        // Bắt đầu lượt của người đầu tiên
        StartCoroutine(HandleTurn());
    }

    // ── Vòng lặp lượt chơi ────────────────────────────────────────
    IEnumerator HandleTurn()
    {
        while (true)
        {
            var player = currentState.CurrentPlayer;
            SetStatus(TurnStatus(player));

            if (player.type == PlayerType.Bot)
            {
                yield return StartCoroutine(BotTurn(player.playerIndex));
            }
            else
            {
                // Chờ input từ người chơi — HandleTurn dừng ở đây
                // OnCellClicked() sẽ gọi ContinueTurn() khi xong
                yield break;
            }

            if (CheckWinCondition()) yield break;
        }
    }

    // ── Bot Turn ──────────────────────────────────────────────────
    IEnumerator BotTurn(int botIdx)
    {
        isAnimating = true;
        yield return new WaitForSeconds(0.4f);

        GameState old  = currentState;
        GameState next = bots[botIdx]?.BestMove(currentState);

        if (next != null)
        {
            currentState = next;
            yield return StartCoroutine(boardRenderer.RenderAnimated(old, currentState));
        }

        isAnimating = false;
    }

    // Được gọi sau khi người chơi hoặc bot xong lượt
    void ContinueTurn()
    {
        if (CheckWinCondition()) return;
        StartCoroutine(HandleTurn());
    }

    // ── Player Input ──────────────────────────────────────────────
    public void OnCellClicked(Vector2Int pos)
    {
        if (isAnimating) return;

        var player = currentState.CurrentPlayer;
        if (player.type != PlayerType.Human) return;  // không phải lượt người chơi

        int N = currentState.boardSize;

        // Click ô exit: EscapeMarker trả về ngoài biên
        if (IsEscapeClick(pos, player, N))
        {
            if (selectedPiece.x == -1)
            { SetStatus("Chọn quân trước!"); return; }

            // Kiểm tra quân chọn có ở sát biên thoát không
            if (!CanEscape(selectedPiece, player, N))
            { SetStatus("Quân phải ở sát biên để thoát!"); return; }

            GameState escapeState = TryFindEscapeMove(selectedPiece, player.playerIndex);
            if (escapeState != null) { ExecutePlayerMove(escapeState); return; }

            SetStatus("Không thể thoát!");
            return;
        }

        // Chỉ xử lý ô trong bàn
        if (!DodgemRules.InBounds(pos, N)) return;

        // ── Chưa chọn quân ──
        if (selectedPiece.x == -1)
        {
            if (!player.HasPieceAt(pos)) return;

            var moves = DodgemRules.GetValidMovesForPiece(
                currentState, pos, player.playerIndex);
            if (moves.Count == 0)
            { SetStatus("Quân này không đi được, chọn quân khác!"); return; }

            selectedPiece = pos;
            validMoves    = moves;
            boardRenderer.HighlightSelected(selectedPiece, validMoves,
                player.playerIndex, currentState);
            SetStatus($"Đã chọn ({pos.x},{pos.y}) — Click ô xanh để đi");
            return;
        }

        // ── Click lại chính quân → bỏ chọn ──
        if (pos == selectedPiece)
        {
            Deselect();
            SetStatus(TurnStatus(player));
            return;
        }

        // ── Click quân cùng phe khác → chuyển chọn ──
        if (player.HasPieceAt(pos))
        {
            var moves = DodgemRules.GetValidMovesForPiece(
                currentState, pos, player.playerIndex);
            if (moves.Count > 0)
            {
                selectedPiece = pos;
                validMoves    = moves;
                boardRenderer.HighlightSelected(selectedPiece, validMoves,
                    player.playerIndex, currentState);
                SetStatus($"Đã chọn ({pos.x},{pos.y}) — Click ô xanh để đi");
            }
            else SetStatus("Quân đó không đi được!");
            return;
        }

        // ── Click ô đích ──
        GameState chosen = TryFindMove(selectedPiece, pos, player.playerIndex);
        if (chosen == null)
        { SetStatus("Không thể đi tới đó! Chọn ô xanh."); return; }

        ExecutePlayerMove(chosen);
    }

    // ── Tìm nước đi ───────────────────────────────────────────────
    GameState TryFindMove(Vector2Int from, Vector2Int to, int playerIdx)
    {
        foreach (var child in DodgemRules.GetChildren(currentState))
            for (int i = 0; i < currentState.players[playerIdx].pieces.Length; i++)
                if (currentState.players[playerIdx].pieces[i] == from &&
                    child.players[playerIdx].pieces[i] == to)
                    return child;
        return null;
    }

    GameState TryFindEscapeMove(Vector2Int from, int playerIdx)
    {
        foreach (var child in DodgemRules.GetChildren(currentState))
            for (int i = 0; i < currentState.players[playerIdx].pieces.Length; i++)
                if (currentState.players[playerIdx].pieces[i] == from &&
                    child.players[playerIdx].pieces[i].x == -1 &&
                    child.players[playerIdx].escaped > currentState.players[playerIdx].escaped)
                    return child;
        return null;
    }

    // ── Kiểm tra click thoát ──────────────────────────────────────
    // Ô exit nằm ngoài bàn theo đúng hướng của phe
    bool IsEscapeClick(Vector2Int pos, PlayerData player, int N)
    {
        switch (player.escapeDir)
        {
            case EscapeDirection.Right:  return pos.x >= N;
            case EscapeDirection.Top:    return pos.y >= N;
            case EscapeDirection.Left:   return pos.x < 0;
            case EscapeDirection.Bottom: return pos.y < 0;
            default: return false;
        }
    }

    // Quân tại 'pos' có thể thoát không (đang ở sát biên thoát)?
    bool CanEscape(Vector2Int pos, PlayerData player, int N)
    {
        switch (player.escapeDir)
        {
            case EscapeDirection.Right:  return pos.x == N - 1;
            case EscapeDirection.Top:    return pos.y == N - 1;
            case EscapeDirection.Left:   return pos.x == 0;
            case EscapeDirection.Bottom: return pos.y == 0;
            default: return false;
        }
    }

    // ── Execute ───────────────────────────────────────────────────
    void ExecutePlayerMove(GameState next)
    {
        isAnimating       = true;
        GameState old     = currentState;
        currentState      = next;
        Deselect();
        StartCoroutine(PlayerMoveCoroutine(old, next));
    }

    IEnumerator PlayerMoveCoroutine(GameState old, GameState next)
    {
        yield return StartCoroutine(boardRenderer.RenderAnimated(old, next));
        isAnimating = false;
        ContinueTurn();
    }

    // ── Helpers ───────────────────────────────────────────────────
    void Deselect()
    {
        selectedPiece = new Vector2Int(-1, -1);
        validMoves.Clear();
        if (boardRenderer != null) boardRenderer.ResetAllColors();
    }

    bool CheckWinCondition()
    {
        var winner = currentState.Winner();
        if (winner != null)
        { SetStatus($"{winner.playerName} THANG!"); return true; }

        // Phe hiện tại hết nước đi
        if (DodgemRules.GetChildren(currentState).Count == 0)
        {
            var stuck = currentState.CurrentPlayer;
            currentState.NextTurn();
            SetStatus($"{stuck.playerName} het nuoc di!");
            if (!CheckWinCondition()) ContinueTurn();
            return false;
        }
        return false;
    }

    string TurnStatus(PlayerData player)
    {
        if (player.type == PlayerType.Bot)
            return $"Luot {player.playerName} (Bot dang nghi...)";
        return $"Luot {player.playerName} (ban) — Click quan de chon";
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Dodgem] " + msg);
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        StartGame();
    }
}