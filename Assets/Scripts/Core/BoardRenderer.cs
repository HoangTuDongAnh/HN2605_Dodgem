using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardRenderer : MonoBehaviour
{
    [Header("Prefabs quan co")]
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    [Header("Colors")]
    public Color selectedColor     = new Color(1f, 0.85f, 0f, 1f);
    public Color validMoveColor    = new Color(0.3f, 0.9f, 0.3f, 1f);
    public Color normalCellColor   = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color blockedCellColor  = new Color(0.12f, 0.12f, 0.12f, 1f);
    public Color exitCellHintColor = new Color(1f, 0.85f, 0.2f, 0.35f);
    public Color escapeReadyColor  = new Color(1f, 0.5f, 0.1f, 0.95f);

    [Header("Animation")]
    public float moveSpeed = 8f;

    private GameObject[][]   pieceObjs;
    private SpriteRenderer[] cellSR;
    private int              currentWidth;
    private int              currentHeight;
    private bool             initialized = false;

    private BoardGenerator cachedGen;
    private GameState      cachedState;

    public void SetupFromGenerator(BoardGenerator gen, GameState state)
    {
        if (whitePiecePrefab == null)
        {
            Debug.LogError("[BoardRenderer] whitePiecePrefab CHUA GAN!");
            return;
        }

        initialized   = false;
        currentWidth  = state.boardWidth;
        currentHeight = state.boardHeight;
        cachedGen     = gen;
        cachedState   = state;

        int total = currentWidth * currentHeight;
        cellSR = new SpriteRenderer[total];

        if (gen.Cells != null)
        {
            for (int i = 0; i < gen.Cells.Length && i < cellSR.Length; i++)
                if (gen.Cells[i] != null)
                    cellSR[i] = GetSR(gen.Cells[i]);
        }

        SetupPieces(state, gen);
        ResetAllColors();
        initialized = true;
    }

    void SetupPieces(GameState state, BoardGenerator gen)
    {
        if (pieceObjs != null)
        {
            foreach (var arr in pieceObjs)
            {
                if (arr == null) continue;
                foreach (var go in arr)
                    if (go != null) Destroy(go);
            }
        }

        pieceObjs = new GameObject[state.NumPlayers][];
        Transform pieceParent = gen != null ? gen.transform : null;

        for (int p = 0; p < state.NumPlayers; p++)
        {
            var player = state.players[p];
            pieceObjs[p] = new GameObject[player.pieces.Length];

            GameObject basePrefab = whitePiecePrefab;
            if (p == 1 && blackPiecePrefab != null) basePrefab = blackPiecePrefab;
            if (basePrefab == null)
            {
                Debug.LogError("[BoardRenderer] Khong co prefab!");
                continue;
            }

            for (int i = 0; i < player.pieces.Length; i++)
            {
                var go = pieceParent != null ? Instantiate(basePrefab, pieceParent) : Instantiate(basePrefab);
                go.name = $"Player{p}_Piece{i}";
                SetColor(go, player.pieceColor);
                pieceObjs[p][i] = go;
            }
        }
    }

    public void Render(GameState state)
    {
        if (!initialized) return;

        cachedState   = state;
        currentWidth  = state.boardWidth;
        currentHeight = state.boardHeight;

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
                        GridToWorld(player.pieces[i], state.boardWidth, state.boardHeight);
            }
        }

        ResetAllColors();
        HighlightExitCells(state.CurrentPlayer, state);
    }

    public IEnumerator RenderAnimated(GameState oldState, GameState newState)
    {
        if (!initialized) yield break;

        cachedState = newState;
        ResetAllColors();

        var anims = new List<Coroutine>();

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
                {
                    anims.Add(StartCoroutine(
                        Slide(pieceObjs[p][i], GridToWorld(newP.pieces[i], newState.boardWidth, newState.boardHeight))));
                }
                else if (was && !now)
                {
                    Vector3 ex = GridToWorld(oldP.pieces[i], newState.boardWidth, newState.boardHeight) + ExitOffset(newP.escapeDir);
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
        cachedState = state;
        ResetAllColors();
        HighlightExitCells(state.players[playerIdx], state);

        int si = BoardIndex(selected, state.boardWidth);
        if (si >= 0 && si < cellSR.Length && cellSR[si] != null)
            cellSR[si].color = selectedColor;

        foreach (var mv in validMoves)
        {
            if (!DodgemRules.InBounds(mv, state)) continue;

            int idx = BoardIndex(mv, state.boardWidth);
            if (idx >= 0 && idx < cellSR.Length && cellSR[idx] != null)
                cellSR[idx].color = validMoveColor;
        }

        if (state.players[playerIdx].CanEscapeFrom(selected))
        {
            if (si >= 0 && si < cellSR.Length && cellSR[si] != null)
                cellSR[si].color = escapeReadyColor;
        }
    }

    public void HighlightExitCells(PlayerData player, GameState state)
    {
        if (player == null || player.exitCells == null) return;

        foreach (var cell in player.exitCells)
        {
            if (!DodgemRules.InBounds(cell, state)) continue;
            if (!state.IsCellPlayable(cell)) continue;

            int idx = BoardIndex(cell, state.boardWidth);
            if (idx >= 0 && idx < cellSR.Length && cellSR[idx] != null)
                cellSR[idx].color = Color.Lerp(cellSR[idx].color, exitCellHintColor, exitCellHintColor.a);
        }
    }

    public void ResetAllColors()
    {
        if (cellSR == null) return;

        for (int i = 0; i < cellSR.Length; i++)
        {
            if (cellSR[i] == null) continue;

            int x = i % currentWidth;
            int y = i / currentWidth;
            var pos = new Vector2Int(x, y);

            bool playable = true;
            if (cachedState != null)
                playable = cachedState.IsCellPlayable(pos);

            cellSR[i].color = playable ? normalCellColor : blockedCellColor;
        }
    }

    public Vector3 GridToWorld(Vector2Int grid, int width, int height)
    {
        if (cachedGen != null)
            return cachedGen.GridToWorld(grid, width, height);

        float offsetX = (width - 1) * 0.5f * 2f;
        float offsetY = (height - 1) * 0.5f * 2f;
        return new Vector3(grid.x * 2f - offsetX, grid.y * 2f - offsetY, 0f);
    }

    public int BoardIndex(Vector2Int pos, int width)
    {
        if (pos.x < 0 || pos.x >= currentWidth || pos.y < 0 || pos.y >= currentHeight) return -1;
        return pos.y * width + pos.x;
    }

    Vector3 ExitOffset(EscapeDirection dir)
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
        else Debug.LogWarning($"[BoardRenderer] {go.name} khong co SpriteRenderer.");
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