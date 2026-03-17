using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public BoardRenderer   boardRenderer;
    public TextMeshProUGUI statusText;
    public Button          restartButton;

    private GameState        currentState;
    private AlphaBetaAI      ai;
    private bool             isPlayerTurn = false;
    private bool             isAnimating  = false;
    private Vector2Int       selectedPiece = new Vector2Int(-1, -1);
    private List<Vector2Int> validMoves    = new List<Vector2Int>();

    // Dùng Start (không phải Awake) để đảm bảo BoardRenderer.Start đã chạy trước
    void Start()
    {
        if (boardRenderer == null)
        {
            Debug.LogError("[GameManager] boardRenderer chưa gán trong Inspector!");
            return;
        }

        ai = new AlphaBetaAI(depth: 6);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        // Gọi Init tường minh trước khi dùng bất kỳ hàm nào của BoardRenderer
        boardRenderer.Init();

        StartGame();
    }

    void StartGame()
    {
        currentState = CreateInitialState();
        Deselect();
        isPlayerTurn = false;
        isAnimating  = false;

        boardRenderer.Render(currentState);
        SetStatus("Lượt Trắng (AI đang nghĩ...)");
        StartCoroutine(AITurn());
    }

    GameState CreateInitialState()
    {
        return new GameState
        {
            whitePieces = new[] { new Vector2Int(0, 1), new Vector2Int(0, 2) },
            blackPieces = new[] { new Vector2Int(1, 0), new Vector2Int(2, 0) },
            isWhiteTurn = true
        };
    }

    // ── AI Turn ───────────────────────────────────────────────────────
    IEnumerator AITurn()
    {
        isAnimating = true;
        SetStatus("Lượt Trắng (AI đang nghĩ...)");
        yield return new WaitForSeconds(0.4f);

        GameState old  = currentState;
        GameState next = ai.BestMove(currentState);

        if (next != null)
        {
            currentState = next;
            yield return StartCoroutine(boardRenderer.RenderAnimated(old, currentState));
        }

        isAnimating = false;
        if (CheckWinCondition()) yield break;

        isPlayerTurn = true;
        SetStatus("Lượt Đen (bạn) — Click quân để chọn");
    }

    // ── Player Input ──────────────────────────────────────────────────
    public void OnCellClicked(Vector2Int pos)
    {
        if (!isPlayerTurn || isAnimating) return;

        // Click ô exit (y=3) khi đang có quân chọn → thoát
        if (pos.y == 3)
        {
            if (selectedPiece.x == -1)
            { SetStatus("Chọn quân trước!"); return; }

            GameState escapeState = TryFindEscapeMove(selectedPiece);
            if (escapeState != null)
                ExecutePlayerMove(escapeState);
            else
                SetStatus("Quân phải ở hàng trên cùng (y=2) để thoát!");
            return;
        }

        // Chỉ xử lý ô trong bàn (0-2, 0-2)
        if (pos.x < 0 || pos.x > 2 || pos.y < 0 || pos.y > 2) return;

        // ── Chưa chọn quân ──
        if (selectedPiece.x == -1)
        {
            if (!IsBlackPiece(pos)) return;

            var moves = DodgemRules.GetValidMovesForPiece(currentState, pos, isWhite: false);
            if (moves.Count == 0)
            { SetStatus("Quân này không đi được, chọn quân khác!"); return; }

            selectedPiece = pos;
            validMoves    = moves;
            boardRenderer.HighlightSelected(selectedPiece, validMoves);
            SetStatus($"Đã chọn ({pos.x},{pos.y}) — Click ô xanh để đi");
            return;
        }

        // ── Click lại chính quân → bỏ chọn ──
        if (pos == selectedPiece)
        {
            Deselect();
            SetStatus("Lượt Đen (bạn) — Click quân để chọn");
            return;
        }

        // ── Click quân Đen khác → chuyển chọn ──
        if (IsBlackPiece(pos))
        {
            var moves = DodgemRules.GetValidMovesForPiece(currentState, pos, isWhite: false);
            if (moves.Count > 0)
            {
                selectedPiece = pos;
                validMoves    = moves;
                boardRenderer.HighlightSelected(selectedPiece, validMoves);
                SetStatus($"Đã chọn ({pos.x},{pos.y}) — Click ô xanh để đi");
            }
            else SetStatus("Quân đó không đi được!");
            return;
        }

        // ── Click ô đích trong bàn ──
        GameState chosen = TryFindMove(selectedPiece, pos);
        if (chosen == null)
        {
            SetStatus("Không thể đi tới đó! Chọn ô xanh.");
            return;
        }
        ExecutePlayerMove(chosen);
    }

    GameState TryFindMove(Vector2Int from, Vector2Int to)
    {
        foreach (var child in DodgemRules.GetChildren(currentState))
            for (int i = 0; i < currentState.blackPieces.Length; i++)
                if (currentState.blackPieces[i] == from && child.blackPieces[i] == to)
                    return child;
        return null;
    }

    GameState TryFindEscapeMove(Vector2Int from)
    {
        if (from.y != 2) return null;

        foreach (var child in DodgemRules.GetChildren(currentState))
            for (int i = 0; i < currentState.blackPieces.Length; i++)
                if (currentState.blackPieces[i] == from
                    && child.blackPieces[i].x == -1
                    && child.blackEscaped > currentState.blackEscaped)
                    return child;
        return null;
    }

    void ExecutePlayerMove(GameState next)
    {
        isPlayerTurn  = false;
        GameState old = currentState;
        currentState  = next;
        Deselect();
        StartCoroutine(PlayerMoveCoroutine(old, next));
    }

    IEnumerator PlayerMoveCoroutine(GameState old, GameState next)
    {
        isAnimating = true;
        yield return StartCoroutine(boardRenderer.RenderAnimated(old, next));
        isAnimating = false;
        if (CheckWinCondition()) yield break;
        StartCoroutine(AITurn());
    }

    void Deselect()
    {
        selectedPiece = new Vector2Int(-1, -1);
        validMoves.Clear();
        if (boardRenderer != null) boardRenderer.ResetAllColors();
    }

    bool IsBlackPiece(Vector2Int pos)
    {
        foreach (var bp in currentState.blackPieces)
            if (bp == pos) return true;
        return false;
    }

    bool CheckWinCondition()
    {
        if (currentState.whiteEscaped == 2)
        { SetStatus("TRẮNG THẮNG!"); return true; }

        if (currentState.blackEscaped == 2)
        { SetStatus("ĐEN THẮNG!"); return true; }

        var moves = DodgemRules.GetChildren(currentState);
        if (moves.Count == 0)
        {
            string stuck  = currentState.isWhiteTurn ? "Trắng" : "Đen";
            string winner = currentState.isWhiteTurn ? "Đen"   : "Trắng";
            SetStatus($"{stuck} hết nước đi — {winner} THẮNG!");
            return true;
        }
        return false;
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