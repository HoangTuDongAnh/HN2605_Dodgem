using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Chiu trach nhiem ve render board, piece va highlight.
/// </summary>
public class BoardRenderer : MonoBehaviour
{
    #region Fields

    [Header("Piece Prefabs")]
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    [Header("Colors")]
    public Color selectedColor = new Color(1f, 0.85f, 0f, 1f);
    public Color validMoveColor = new Color(0.3f, 0.9f, 0.3f, 1f);
    public Color normalCellColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color blockedCellColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    public Color exitCellHintColor = new Color(1f, 0.85f, 0.2f, 0.35f);
    public Color escapeReadyColor = new Color(1f, 0.5f, 0.1f, 0.95f);

    [Header("Animation")]
    public float moveSpeed = 8f;

    private GameObject[][] pieceObjects;
    private SpriteRenderer[] cellRenderers;
    private int currentWidth;
    private int currentHeight;
    private bool isInitialized;

    private BoardGenerator cachedGenerator;
    private GameState cachedState;

    #endregion

    #region Public API

    /// <summary>
    /// Khoi tao renderer tu board generator va state hien tai.
    /// </summary>
    public void SetupFromGenerator(BoardGenerator generator, GameState state)
    {
        if (whitePiecePrefab == null)
        {
            Debug.LogError("[BoardRenderer] White piece prefab is not assigned.");
            return;
        }

        isInitialized = false;
        currentWidth = state.boardWidth;
        currentHeight = state.boardHeight;
        cachedGenerator = generator;
        cachedState = state;

        int totalCells = currentWidth * currentHeight;
        cellRenderers = new SpriteRenderer[totalCells];

        if (generator.Cells != null)
        {
            for (int i = 0; i < generator.Cells.Length && i < cellRenderers.Length; i++)
            {
                if (generator.Cells[i] != null)
                    cellRenderers[i] = GetSpriteRenderer(generator.Cells[i]);
            }
        }

        SetupPieces(state, generator);
        ResetAllColors();
        isInitialized = true;
    }

    /// <summary>
    /// Render trang thai board hien tai (snap truc tiep, khong co animation).
    /// </summary>
    public void Render(GameState state)
    {
        if (!isInitialized) return;

        cachedState = state;
        currentWidth = state.boardWidth;
        currentHeight = state.boardHeight;

        for (int p = 0; p < state.NumPlayers && p < pieceObjects.Length; p++)
        {
            if (pieceObjects[p] == null) continue;

            var player = state.players[p];
            for (int i = 0; i < player.pieces.Length && i < pieceObjects[p].Length; i++)
            {
                if (pieceObjects[p][i] == null) continue;

                bool isActive = player.pieces[i].x != -1;
                pieceObjects[p][i].SetActive(isActive);

                if (isActive)
                {
                    pieceObjects[p][i].transform.position =
                        GridToWorld(player.pieces[i], state.boardWidth, state.boardHeight);
                }
            }
        }

        ResetAllColors();
        HighlightExitCells(state.CurrentPlayer, state);
    }

    /// <summary>
    /// Render co animation khi state thay doi.
    /// FIX: Tat ca piece slide DONG THOI thay vi tuan tu.
    /// </summary>
    public IEnumerator RenderAnimated(GameState oldState, GameState newState)
    {
        if (!isInitialized) yield break;

        cachedState = newState;
        ResetAllColors();

        // FIX: dem so luong animation dang chay de cho tat ca ket thuc cung luc
        int runningCount = 0;
        bool anyAnimation = false;

        for (int p = 0; p < newState.NumPlayers && p < pieceObjects.Length; p++)
        {
            if (pieceObjects[p] == null) continue;

            var oldPlayer = oldState.players[p];
            var newPlayer = newState.players[p];

            for (int i = 0; i < oldPlayer.pieces.Length && i < pieceObjects[p].Length; i++)
            {
                if (pieceObjects[p][i] == null) continue;

                bool wasActive = oldPlayer.pieces[i].x != -1;
                bool isActive = newPlayer.pieces[i].x != -1;

                if (wasActive && isActive && oldPlayer.pieces[i] != newPlayer.pieces[i])
                {
                    anyAnimation = true;
                    runningCount++;
                    Vector3 target = GridToWorld(newPlayer.pieces[i], newState.boardWidth, newState.boardHeight);
                    StartCoroutine(SlideAndNotify(pieceObjects[p][i], target, () => runningCount--));
                }
                else if (wasActive && !isActive)
                {
                    anyAnimation = true;
                    runningCount++;
                    Vector3 exitPos =
                        GridToWorld(oldPlayer.pieces[i], newState.boardWidth, newState.boardHeight) +
                        ExitOffset(newPlayer.escapeDir);
                    StartCoroutine(SlideHideAndNotify(pieceObjects[p][i], exitPos, () => runningCount--));
                }
            }
        }

        // FIX: cho tat ca animation ket thuc cung luc (dong thoi), khong yield tung cai mot
        if (anyAnimation)
            yield return new WaitUntil(() => runningCount <= 0);

        // Render snap cuoi de dam bao vi tri chinh xac sau anim
        Render(newState);
    }

    /// <summary>
    /// Highlight quan dang chon va cac nuoc di hop le.
    /// </summary>
    public void HighlightSelected(Vector2Int selected, List<Vector2Int> validMoves, int playerIdx, GameState state)
    {
        cachedState = state;
        ResetAllColors();
        HighlightExitCells(state.players[playerIdx], state);

        int selectedIndex = BoardIndex(selected, state.boardWidth);
        if (selectedIndex >= 0 && selectedIndex < cellRenderers.Length && cellRenderers[selectedIndex] != null)
            cellRenderers[selectedIndex].color = selectedColor;

        foreach (var move in validMoves)
        {
            if (!DodgemRules.InBounds(move, state)) continue;

            int index = BoardIndex(move, state.boardWidth);
            if (index >= 0 && index < cellRenderers.Length && cellRenderers[index] != null)
                cellRenderers[index].color = validMoveColor;
        }

        if (state.players[playerIdx].CanEscapeFrom(selected))
        {
            if (selectedIndex >= 0 && selectedIndex < cellRenderers.Length && cellRenderers[selectedIndex] != null)
                cellRenderers[selectedIndex].color = escapeReadyColor;
        }
    }

    /// <summary>
    /// Highlight cac exit cell cua player hien tai.
    /// </summary>
    public void HighlightExitCells(PlayerData player, GameState state)
    {
        if (player == null || player.exitCells == null) return;

        foreach (var cell in player.exitCells)
        {
            if (!DodgemRules.InBounds(cell, state)) continue;
            if (!state.IsCellPlayable(cell)) continue;

            int index = BoardIndex(cell, state.boardWidth);
            if (index >= 0 && index < cellRenderers.Length && cellRenderers[index] != null)
            {
                cellRenderers[index].color =
                    Color.Lerp(cellRenderers[index].color, exitCellHintColor, exitCellHintColor.a);
            }
        }
    }

    /// <summary>
    /// Reset mau toan bo o board ve trang thai mac dinh.
    /// </summary>
    public void ResetAllColors()
    {
        if (cellRenderers == null) return;

        for (int i = 0; i < cellRenderers.Length; i++)
        {
            if (cellRenderers[i] == null) continue;

            int x = i % currentWidth;
            int y = i / currentWidth;
            var pos = new Vector2Int(x, y);

            bool playable = true;
            if (cachedState != null)
                playable = cachedState.IsCellPlayable(pos);

            cellRenderers[i].color = playable ? normalCellColor : blockedCellColor;
        }
    }

    /// <summary>
    /// Chuyen toa do grid sang world.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int grid, int width, int height)
    {
        if (cachedGenerator != null)
            return cachedGenerator.GridToWorld(grid, width, height);

        float offsetX = (width - 1) * 0.5f * 2f;
        float offsetY = (height - 1) * 0.5f * 2f;
        return new Vector3(grid.x * 2f - offsetX, grid.y * 2f - offsetY, 0f);
    }

    /// <summary>
    /// Tinh index 1 chieu cua o trong mang cell.
    /// </summary>
    public int BoardIndex(Vector2Int pos, int width)
    {
        if (pos.x < 0 || pos.x >= currentWidth || pos.y < 0 || pos.y >= currentHeight)
            return -1;

        return pos.y * width + pos.x;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Tao va gan cac object quan co.
    /// </summary>
    void SetupPieces(GameState state, BoardGenerator generator)
    {
        if (pieceObjects != null)
        {
            foreach (var arr in pieceObjects)
            {
                if (arr == null) continue;

                foreach (var go in arr)
                {
                    if (go != null)
                        Destroy(go);
                }
            }
        }

        pieceObjects = new GameObject[state.NumPlayers][];
        Transform pieceParent = generator != null ? generator.transform : null;

        for (int p = 0; p < state.NumPlayers; p++)
        {
            var player = state.players[p];
            pieceObjects[p] = new GameObject[player.pieces.Length];

            GameObject basePrefab = whitePiecePrefab;
            if (p == 1 && blackPiecePrefab != null)
                basePrefab = blackPiecePrefab;

            if (basePrefab == null)
            {
                Debug.LogError("[BoardRenderer] Missing piece prefab.");
                continue;
            }

            for (int i = 0; i < player.pieces.Length; i++)
            {
                var piece =
                    pieceParent != null
                        ? Instantiate(basePrefab, pieceParent)
                        : Instantiate(basePrefab);

                piece.name = $"Player{p}_Piece{i}";
                SetPieceColor(piece, player.pieceColor);
                pieceObjects[p][i] = piece;
            }
        }
    }

    /// <summary>
    /// Tinh vector offset khi quan thoat khoi board.
    /// </summary>
    Vector3 ExitOffset(EscapeDirection dir)
    {
        float cellSize = cachedGenerator != null ? cachedGenerator.cellSize : 2f;
        float distance = cellSize * 1.5f;

        switch (dir)
        {
            case EscapeDirection.Right:  return new Vector3(distance, 0, 0);
            case EscapeDirection.Top:    return new Vector3(0, distance, 0);
            case EscapeDirection.Left:   return new Vector3(-distance, 0, 0);
            case EscapeDirection.Bottom: return new Vector3(0, -distance, 0);
            default: return Vector3.zero;
        }
    }

    /// <summary>
    /// Lay SpriteRenderer tu object hoac object con.
    /// </summary>
    SpriteRenderer GetSpriteRenderer(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        return sr != null ? sr : go.GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Gan mau cho prefab quan.
    /// </summary>
    void SetPieceColor(GameObject go, Color color)
    {
        var sr = GetSpriteRenderer(go);
        if (sr != null)
            sr.color = color;
        else
            Debug.LogWarning($"[BoardRenderer] Object {go.name} has no SpriteRenderer.");
    }

    /// <summary>
    /// Slide den vi tri dich roi goi callback khi xong.
    /// FIX: dung callback thay vi yield, de co the chay dong thoi nhieu piece.
    /// </summary>
    IEnumerator SlideAndNotify(GameObject piece, Vector3 target, System.Action onDone)
    {
        while (piece != null && Vector3.Distance(piece.transform.position, target) > 0.005f)
        {
            piece.transform.position =
                Vector3.MoveTowards(piece.transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (piece != null)
            piece.transform.position = target;

        onDone?.Invoke();
    }

    /// <summary>
    /// Slide den vi tri exit, an di, roi goi callback khi xong.
    /// </summary>
    IEnumerator SlideHideAndNotify(GameObject piece, Vector3 exitPos, System.Action onDone)
    {
        while (piece != null && Vector3.Distance(piece.transform.position, exitPos) > 0.005f)
        {
            piece.transform.position =
                Vector3.MoveTowards(piece.transform.position, exitPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        if (piece != null)
            piece.SetActive(false);

        onDone?.Invoke();
    }

    #endregion
}
