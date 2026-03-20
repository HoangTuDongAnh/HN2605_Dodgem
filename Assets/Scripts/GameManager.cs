using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public BoardRenderer  boardRenderer;
    public BoardGenerator boardGenerator;
    public Camera         mainCamera;

    [Header("UI — Game Panel")]
    public TextMeshProUGUI statusText;
    public Button          restartButton;

    private GameState    currentState;
    private GamePreset   lastPreset;
    private PlayerType[] lastOverrideTypes;
    private int[]        lastOverrideDepths;
    private IGameAI[]    bots;
    private bool         isAnimating = false;

    private Vector2Int       selectedPiece = new Vector2Int(-1, -1);
    private List<Vector2Int> validMoves    = new List<Vector2Int>();

    void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    public void StartGameFromPreset(GamePreset preset,
                                    PlayerType[] overrideTypes,
                                    int[] overrideDepths)
    {
        lastPreset         = preset;
        lastOverrideTypes  = overrideTypes;
        lastOverrideDepths = overrideDepths;

        StopAllCoroutines();
        InitGame();
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        InitGame();
    }

    void InitGame()
    {
        if (lastPreset == null)
        {
            Debug.LogError("[GameManager] Chua co preset!");
            return;
        }

        currentState = GameState.CreateFromPreset(lastPreset, lastOverrideTypes, lastOverrideDepths);
        if (currentState == null) return;

        boardGenerator.Generate(currentState);
        boardRenderer.SetupFromGenerator(boardGenerator, currentState);

        bots = new IGameAI[currentState.NumPlayers];
        for (int i = 0; i < currentState.NumPlayers; i++)
        {
            var p = currentState.players[i];
            bots[i] = GameAIFactory.Create(p);
        }

        FitCamera(currentState.boardWidth, currentState.boardHeight);

        Deselect();
        isAnimating = false;
        boardRenderer.Render(currentState);

        StartCoroutine(HandleTurn());
    }

    void FitCamera(int width, int height)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || boardGenerator == null) return;

        float cs = boardGenerator.cellSize;
        float margin = cs * 1.8f;

        float halfH = height * cs * 0.5f;
        float orthoByHeight = halfH + margin;

        float aspect = mainCamera.aspect > 0.01f ? mainCamera.aspect : 1.7777f;
        float halfW = width * cs * 0.5f;
        float orthoByWidth = (halfW + margin) / aspect;

        mainCamera.orthographicSize = Mathf.Max(orthoByHeight, orthoByWidth);
        mainCamera.transform.position = new Vector3(0, 0, -10);
    }

    IEnumerator HandleTurn()
    {
        while (true)
        {
            var player = currentState.CurrentPlayer;
            SetStatus(TurnStatus(player));

            boardRenderer.Render(currentState);

            if (player.type == PlayerType.Bot)
            {
                yield return StartCoroutine(BotTurn(player.playerIndex));
                if (CheckWinCondition()) yield break;
            }
            else
            {
                yield break;
            }
        }
    }

    IEnumerator BotTurn(int idx)
    {
        isAnimating = true;

        var ai = bots != null && idx < bots.Length ? bots[idx] : null;
        string aiName = ai != null ? ai.DisplayName : "AI";
        string diff   = GameAIFactory.DifficultyLabel(currentState.players[idx].botDepth);

        SetStatus($"Luot {currentState.players[idx].playerName} (Bot - {aiName} / {diff})");
        yield return new WaitForSeconds(0.35f);

        GameState old  = currentState;
        GameState next = ai?.BestMove(currentState);

        if (next != null)
        {
            currentState = next;
            yield return StartCoroutine(boardRenderer.RenderAnimated(old, currentState));
        }

        isAnimating = false;
        ContinueTurn();
    }

    void ContinueTurn()
    {
        Deselect();

        if (CheckWinCondition()) return;
        StartCoroutine(HandleTurn());
    }

    public void OnCellClicked(Vector2Int pos)
    {
        if (isAnimating || currentState == null) return;

        var player = currentState.CurrentPlayer;
        if (player.type != PlayerType.Human) return;

        if (!DodgemRules.InBounds(pos, currentState)) return;
        if (!currentState.IsCellPlayable(pos)) return;

        if (selectedPiece.x != -1 && pos == selectedPiece)
        {
            if (CanEscape(selectedPiece, player))
            {
                var esc = TryFindEscapeMove(selectedPiece, player.playerIndex);
                if (esc != null)
                {
                    ExecutePlayerMove(esc);
                    return;
                }

                SetStatus("Khong the thoat!");
                return;
            }

            Deselect();
            boardRenderer.Render(currentState);
            SetStatus(TurnStatus(player));
            return;
        }

        if (selectedPiece.x == -1)
        {
            if (!player.HasPieceAt(pos)) return;

            var moves = DodgemRules.GetValidMovesForPiece(currentState, pos, player.playerIndex);
            if (moves.Count == 0)
            {
                SetStatus("Quan nay khong di duoc!");
                return;
            }

            selectedPiece = pos;
            validMoves    = moves;
            boardRenderer.HighlightSelected(selectedPiece, validMoves, player.playerIndex, currentState);

            if (CanEscape(selectedPiece, player))
                SetStatus($"Da chon ({pos.x},{pos.y}) — Click lai o nay de THOAT, hoac click o xanh de di");
            else
                SetStatus($"Da chon ({pos.x},{pos.y}) — Click o xanh de di");

            return;
        }

        if (player.HasPieceAt(pos))
        {
            var moves = DodgemRules.GetValidMovesForPiece(currentState, pos, player.playerIndex);
            if (moves.Count > 0)
            {
                selectedPiece = pos;
                validMoves    = moves;
                boardRenderer.HighlightSelected(selectedPiece, validMoves, player.playerIndex, currentState);

                if (CanEscape(selectedPiece, player))
                    SetStatus($"Da chon ({pos.x},{pos.y}) — Click lai o nay de THOAT");
                else
                    SetStatus($"Da chon ({pos.x},{pos.y})");
            }
            else
            {
                SetStatus("Quan do khong di duoc!");
            }
            return;
        }

        var chosen = TryFindMove(selectedPiece, pos, player.playerIndex);
        if (chosen == null)
        {
            SetStatus("Khong the di toi do!");
            return;
        }

        ExecutePlayerMove(chosen);
    }

    GameState TryFindMove(Vector2Int from, Vector2Int to, int pIdx)
    {
        foreach (var child in DodgemRules.GetChildren(currentState))
        {
            for (int i = 0; i < currentState.players[pIdx].pieces.Length; i++)
            {
                if (currentState.players[pIdx].pieces[i] == from &&
                    child.players[pIdx].pieces[i] == to)
                    return child;
            }
        }
        return null;
    }

    GameState TryFindEscapeMove(Vector2Int from, int pIdx)
    {
        foreach (var child in DodgemRules.GetChildren(currentState))
        {
            for (int i = 0; i < currentState.players[pIdx].pieces.Length; i++)
            {
                if (currentState.players[pIdx].pieces[i] == from &&
                    child.players[pIdx].pieces[i].x == -1 &&
                    child.players[pIdx].escaped > currentState.players[pIdx].escaped)
                    return child;
            }
        }
        return null;
    }

    bool CanEscape(Vector2Int pos, PlayerData player)
    {
        return player.CanEscapeFrom(pos);
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

        if (boardRenderer != null && currentState != null)
            boardRenderer.Render(currentState);
    }

    bool CheckWinCondition()
    {
        if (currentState == null) return false;

        var winner = currentState.Winner();
        if (winner != null)
        {
            SetStatus($"{winner.playerName} THANG!");
            return true;
        }

        return false;
    }

    string TurnStatus(PlayerData p)
    {
        if (p.type == PlayerType.Bot)
        {
            string diff = GameAIFactory.DifficultyLabel(p.botDepth);
            string aiName = bots != null && p.playerIndex < bots.Length && bots[p.playerIndex] != null
                ? bots[p.playerIndex].DisplayName
                : "AI";
            return $"Luot {p.playerName} — Bot ({aiName} / {diff})";
        }

        return $"Luot {p.playerName} — Human";
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[Dodgem] " + msg);
    }
}