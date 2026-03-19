using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardRenderer : MonoBehaviour
{
    [Header("Prefabs quan co — PHAI GAN TRONG INSPECTOR")]
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    [Header("Colors")]
    public Color selectedColor   = new Color(1f,    0.85f, 0f,   1f);
    public Color validMoveColor  = new Color(0.3f,  0.9f,  0.3f, 1f);
    public Color exitColor       = new Color(0.2f,  0.7f,  1f,   1f);
    public Color normalCellColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Animation")]
    public float moveSpeed = 8f;

    private GameObject[][]   pieceObjs;
    private SpriteRenderer[] cellSR;
    private SpriteRenderer[] exitSRs;
    private GameObject[]     exitCellObjs;
    private int              currentBoardSize;
    private bool             initialized = false;

    // Cache generator de tinh GridToWorld chinh xac
    private BoardGenerator cachedGen;

    public void SetupFromGenerator(BoardGenerator gen, GameState state)
    {
        if (whitePiecePrefab == null)
        {
            Debug.LogError("[BoardRenderer] whitePiecePrefab CHUA GAN! " +
                           "Chon object Board -> BoardRenderer -> keo prefab vao.");
            return;
        }

        initialized      = false;
        currentBoardSize = state.boardSize;
        cachedGen        = gen;
        int N            = state.boardSize;

        cellSR = new SpriteRenderer[N * N];
        if (gen.Cells != null)
            for (int i = 0; i < gen.Cells.Length && i < cellSR.Length; i++)
                if (gen.Cells[i] != null) cellSR[i] = GetSR(gen.Cells[i]);

        exitCellObjs = gen.ExitCells;
        exitSRs      = new SpriteRenderer[state.NumPlayers];
        if (exitCellObjs != null)
            for (int p = 0; p < state.NumPlayers && p < exitCellObjs.Length; p++)
                if (exitCellObjs[p] != null) exitSRs[p] = GetSR(exitCellObjs[p]);

        SetupPieces(state, gen);
        ResetAllColors();
        initialized = true;
    }

    void SetupPieces(GameState state, BoardGenerator gen)
    {
        // Destroy quan cu
        if (pieceObjs != null)
            foreach (var arr in pieceObjs)
                if (arr != null)
                    foreach (var go in arr)
                        if (go != null) Destroy(go);

        pieceObjs = new GameObject[state.NumPlayers][];

        // FIX: dung gen.transform lam parent de piece la con cua Board
        Transform pieceParent = gen != null ? gen.transform : null;

        for (int p = 0; p < state.NumPlayers; p++)
        {
            var player   = state.players[p];
            int nPieces  = player.pieces.Length;
            pieceObjs[p] = new GameObject[nPieces];

            GameObject basePrefab = whitePiecePrefab;
            if (p == 1 && blackPiecePrefab != null) basePrefab = blackPiecePrefab;
            if (basePrefab == null) { Debug.LogError("[BoardRenderer] Khong co prefab!"); continue; }

            for (int i = 0; i < nPieces; i++)
            {
                // Instantiate voi parent de khong spam root scene
                var go = pieceParent != null
                         ? Instantiate(basePrefab, pieceParent)
                         : Instantiate(basePrefab);
                go.name = $"Player{p}_Piece{i}";
                SetColor(go, player.pieceColor);
                pieceObjs[p][i] = go;
            }
        }
    }

    public void Render(GameState state)
    {
        if (!initialized) return;
        currentBoardSize = state.boardSize;

        for (int p = 0; p < state.NumPlayers && p < pieceObjs.Length; p++)
        {
            if (pieceObjs[p] == null) continue;
            var player = state.players[p];
            for (int i = 0; i < player.pieces.Length && i < pieceObjs[p].Length; i++)
            {
                if (pieceObjs[p][i] == null) continue;
                bool active = player.pieces[i].x != -1;
                pieceObjs[p][i].SetActive(active);
                if (active)
                    pieceObjs[p][i].transform.position =
                        GridToWorld(player.pieces[i], state.boardSize);
            }
        }
        ResetAllColors();
    }

    public IEnumerator RenderAnimated(GameState oldState, GameState newState)
    {
        if (!initialized) yield break;
        ResetAllColors();
        var anims = new List<Coroutine>();
        int N = newState.boardSize;

        for (int p = 0; p < newState.NumPlayers && p < pieceObjs.Length; p++)
        {
            if (pieceObjs[p] == null) continue;
            var oldP = oldState.players[p];
            var newP = newState.players[p];

            for (int i = 0; i < oldP.pieces.Length && i < pieceObjs[p].Length; i++)
            {
                if (pieceObjs[p][i] == null) continue;
                bool was = oldP.pieces[i].x != -1;
                bool now = newP.pieces[i].x != -1;

                if (was && now && oldP.pieces[i] != newP.pieces[i])
                    anims.Add(StartCoroutine(Slide(pieceObjs[p][i], GridToWorld(newP.pieces[i], N))));
                else if (was && !now)
                {
                    Vector3 ex = GridToWorld(oldP.pieces[i], N) + ExitOffset(newP.escapeDir, N);
                    anims.Add(StartCoroutine(SlideAndHide(pieceObjs[p][i], ex)));
                }
            }
        }

        foreach (var c in anims) yield return c;
        Render(newState);
    }

    public void HighlightSelected(Vector2Int selected, List<Vector2Int> validMoves,
                                  int playerIdx, GameState state)
    {
        ResetAllColors();
        int N = state.boardSize;

        int si = BoardIndex(selected, N);
        if (si >= 0 && si < cellSR.Length && cellSR[si] != null)
            cellSR[si].color = selectedColor;

        bool hasEscape = false;
        foreach (var mv in validMoves)
        {
            if (!DodgemRules.InBounds(mv, N)) { hasEscape = true; continue; }
            int idx = BoardIndex(mv, N);
            if (idx >= 0 && idx < cellSR.Length && cellSR[idx] != null)
                cellSR[idx].color = validMoveColor;
        }

        if (hasEscape && exitCellObjs != null &&
            playerIdx < exitCellObjs.Length && exitCellObjs[playerIdx] != null)
        {
            exitCellObjs[playerIdx].SetActive(true);
            if (playerIdx < exitSRs.Length && exitSRs[playerIdx] != null)
                exitSRs[playerIdx].color = exitColor;
        }
    }

    public void ResetAllColors()
    {
        // An exit cells truoc
        if (exitCellObjs != null)
            foreach (var go in exitCellObjs)
                if (go != null) go.SetActive(false);

        if (cellSR != null)
            foreach (var sr in cellSR)
                if (sr != null) sr.color = normalCellColor;
    }

    // FIX: Dung cachedGen.GridToWorld de tinh dung world position
    public Vector3 GridToWorld(Vector2Int grid, int boardSize)
    {
        if (cachedGen != null)
            return cachedGen.GridToWorld(grid, boardSize);

        // Fallback neu khong co generator
        float cs     = 2f;
        float offset = (boardSize - 1) / 2f * cs;
        return new Vector3(grid.x * cs - offset, grid.y * cs - offset, 0f);
    }

    public int BoardIndex(Vector2Int pos, int boardSize)
    {
        if (pos.x < 0 || pos.x >= boardSize || pos.y < 0 || pos.y >= boardSize) return -1;
        return pos.y * boardSize + pos.x;
    }

    Vector3 ExitOffset(EscapeDirection dir, int boardSize)
    {
        float cs = cachedGen != null ? cachedGen.cellSize : 2f;
        float d  = cs * 1.5f;
        switch (dir)
        {
            case EscapeDirection.Right:  return new Vector3( d,  0, 0);
            case EscapeDirection.Top:    return new Vector3( 0,  d, 0);
            case EscapeDirection.Left:   return new Vector3(-d,  0, 0);
            case EscapeDirection.Bottom: return new Vector3( 0, -d, 0);
            default: return Vector3.zero;
        }
    }

    SpriteRenderer GetSR(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        return sr != null ? sr : go.GetComponentInChildren<SpriteRenderer>();
    }

    void SetColor(GameObject go, Color c)
    {
        var sr = GetSR(go);
        if (sr != null) sr.color = c;
        else Debug.LogWarning($"[BoardRenderer] {go.name} khong co SpriteRenderer. " +
                               "Doi Material sang Sprites/Default.");
    }

    IEnumerator Slide(GameObject piece, Vector3 target)
    {
        while (Vector3.Distance(piece.transform.position, target) > 0.005f)
        {
            piece.transform.position = Vector3.MoveTowards(
                piece.transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        piece.transform.position = target;
    }

    IEnumerator SlideAndHide(GameObject piece, Vector3 exitPos)
    {
        yield return StartCoroutine(Slide(piece, exitPos));
        piece.SetActive(false);
    }
}