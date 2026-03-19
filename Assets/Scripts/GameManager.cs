using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// ================================================================
// GameManager — phien ban preset
// THAY DOI: them StartGameFromPreset(), bo GameConfig dependency
// ================================================================

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public BoardRenderer  boardRenderer;
    public BoardGenerator boardGenerator;
    public Camera         mainCamera;

    [Header("UI — Game Panel")]
    public TextMeshProUGUI statusText;
    public Button          restartButton;

    // ── Private ───────────────────────────────────────────────────
    private GameState        currentState;
    private GamePreset       lastPreset;
    private PlayerType[]     lastOverrideTypes;
    private int[]            lastOverrideDepths;
    private AlphaBetaAI[]    bots;
    private bool             isAnimating  = false;
    private Vector2Int       selectedPiece = new Vector2Int(-1, -1);
    private List<Vector2Int> validMoves    = new List<Vector2Int>();

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    // ── Goi tu MenuManager voi preset da chon ────────────────────
    public void StartGameFromPreset(GamePreset preset,
                                    PlayerType[] overrideTypes,
                                    int[]        overrideDepths)
    {
        lastPreset         = preset;
        lastOverrideTypes  = overrideTypes;
        lastOverrideDepths = overrideDepths;
        StopAllCoroutines();
        InitGame();
    }

    // ── Restart: dung lai preset lan truoc ───────────────────────
    public void RestartGame()
    {
        StopAllCoroutines();
        InitGame();
    }

    // ── Khoi tao game ─────────────────────────────────────────────
    void InitGame()
    {
        if (lastPreset == null)
        { Debug.LogError("[GameManager] Chua co preset!"); return; }

        // 1. Tao GameState tu preset
        currentState = GameState.CreateFromPreset(
            lastPreset, lastOverrideTypes, lastOverrideDepths);
        if (currentState == null) return;

        // 2. Sinh ban co
        boardGenerator.Generate(currentState);

        // 3. Setup renderer
        boardRenderer.SetupFromGenerator(boardGenerator, currentState);

        // 4. Tao bots
        bots = new AlphaBetaAI[currentState.NumPlayers];
        for (int i = 0; i < currentState.NumPlayers; i++)
        {
            var p = currentState.players[i];
            if (p.type == PlayerType.Bot)
                // Truyen myPlayerIndex = i de moi bot biet minh la phe nao
                // Bot se maximize diem cua chinh minh, khong phai cua Trang
                bots[i] = new AlphaBetaAI(depth: p.botDepth, myPlayerIndex: i);
        }

        // 5. Camera fit
        FitCamera(currentState.boardSize);

        // 6. Reset
        Deselect();
        isAnimating = false;
        boardRenderer.Render(currentState);

        StartCoroutine(HandleTurn());
    }

    void FitCamera(int boardSize)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || boardGenerator == null) return;
        float cs        = boardGenerator.cellSize;
        float halfBoard = (boardSize - 1) / 2f * cs;
        mainCamera.orthographicSize  = halfBoard + cs * 1.8f;
        mainCamera.transform.position = new Vector3(0, 0, -10);
    }

    // ── Vong lap luot ─────────────────────────────────────────────
    IEnumerator HandleTurn()
    {
        while (true)
        {
            var player = currentState.CurrentPlayer;
            SetStatus(TurnStatus(player));

            if (player.type == PlayerType.Bot)
            {
                yield return StartCoroutine(BotTurn(player.playerIndex));
                if (CheckWinCondition()) yield break;
            }
            else
                yield break; // cho input nguoi choi
        }
    }

    IEnumerator BotTurn(int idx)
    {
        isAnimating = true;
        SetStatus($"Luot {currentState.players[idx].playerName} (Bot dang nghi...)");
        yield return new WaitForSeconds(0.4f);

        GameState old  = currentState;
        GameState next = bots[idx]?.BestMove(currentState);

        if (next != null)
        {
            currentState = next;
            yield return StartCoroutine(boardRenderer.RenderAnimated(old, currentState));
        }
        isAnimating = false;
    }

    void ContinueTurn()
    {
        if (CheckWinCondition()) return;
        StartCoroutine(HandleTurn());
    }

    // ── Player Input ──────────────────────────────────────────────
    public void OnCellClicked(Vector2Int pos)
    {
        if (isAnimating || currentState == null) return;
        var player = currentState.CurrentPlayer;
        if (player.type != PlayerType.Human) return;

        int N = currentState.boardSize;

        // Click Exit
        if (IsEscapeClick(pos, player, N))
        {
            if (selectedPiece.x == -1) { SetStatus("Chon quan truoc!"); return; }
            if (!CanEscape(selectedPiece, player, N))
            { SetStatus("Quan phai o sat bien de thoat!"); return; }
            var esc = TryFindEscapeMove(selectedPiece, player.playerIndex);
            if (esc != null) { ExecutePlayerMove(esc); return; }
            SetStatus("Khong the thoat!");
            return;
        }

        if (!DodgemRules.InBounds(pos, N)) return;

        if (selectedPiece.x == -1)
        {
            if (!player.HasPieceAt(pos)) return;
            var moves = DodgemRules.GetValidMovesForPiece(currentState, pos, player.playerIndex);
            if (moves.Count == 0) { SetStatus("Quan nay khong di duoc!"); return; }
            selectedPiece = pos;
            validMoves    = moves;
            boardRenderer.HighlightSelected(selectedPiece, validMoves, player.playerIndex, currentState);
            SetStatus($"Da chon ({pos.x},{pos.y}) — Click o xanh de di");
            return;
        }

        if (pos == selectedPiece)
        { Deselect(); SetStatus(TurnStatus(player)); return; }

        if (player.HasPieceAt(pos))
        {
            var moves = DodgemRules.GetValidMovesForPiece(currentState, pos, player.playerIndex);
            if (moves.Count > 0)
            {
                selectedPiece = pos;
                validMoves    = moves;
                boardRenderer.HighlightSelected(selectedPiece, validMoves, player.playerIndex, currentState);
                SetStatus($"Da chon ({pos.x},{pos.y})");
            }
            else SetStatus("Quan do khong di duoc!");
            return;
        }

        var chosen = TryFindMove(selectedPiece, pos, player.playerIndex);
        if (chosen == null) { SetStatus("Khong the di toi do!"); return; }
        ExecutePlayerMove(chosen);
    }

    GameState TryFindMove(Vector2Int from, Vector2Int to, int pIdx)
    {
        foreach (var child in DodgemRules.GetChildren(currentState))
            for (int i = 0; i < currentState.players[pIdx].pieces.Length; i++)
                if (currentState.players[pIdx].pieces[i] == from &&
                    child.players[pIdx].pieces[i] == to)
                    return child;
        return null;
    }

    GameState TryFindEscapeMove(Vector2Int from, int pIdx)
    {
        foreach (var child in DodgemRules.GetChildren(currentState))
            for (int i = 0; i < currentState.players[pIdx].pieces.Length; i++)
                if (currentState.players[pIdx].pieces[i] == from &&
                    child.players[pIdx].pieces[i].x == -1 &&
                    child.players[pIdx].escaped > currentState.players[pIdx].escaped)
                    return child;
        return null;
    }

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

    void ExecutePlayerMove(GameState next)
    {
        isAnimating   = true;
        GameState old = currentState;
        currentState  = next;
        Deselect();
        StartCoroutine(PlayerMoveCoroutine(old, next));
    }

    IEnumerator PlayerMoveCoroutine(GameState old, GameState next)
    {
        yield return StartCoroutine(boardRenderer.RenderAnimated(old, next));
        isAnimating = false;
        ContinueTurn();
    }

    void Deselect()
    {
        selectedPiece = new Vector2Int(-1, -1);
        validMoves.Clear();
        if (boardRenderer != null) boardRenderer.ResetAllColors();
    }

    bool CheckWinCondition()
    {
        if (currentState == null) return false;
        var winner = currentState.Winner();
        if (winner != null) { SetStatus($"{winner.playerName} THANG!"); return true; }
        if (DodgemRules.GetChildren(currentState).Count == 0)
        {
            var stuck = currentState.CurrentPlayer.playerName;
            SetStatus($"{stuck} het nuoc di!");
            currentState.NextTurn();
            ContinueTurn();
        }
        return false;
    }

    string TurnStatus(PlayerData p)
        => p.type == PlayerType.Bot
           ? $"Luot {p.playerName} (Bot)..."
           : $"Luot {p.playerName} — Click quan";

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Dodgem] " + msg);
    }
}