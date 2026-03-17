using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardRenderer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;

    // 9 ô bàn cờ — thứ tự:
    // 0=(0,0) 1=(1,0) 2=(2,0)
    // 3=(0,1) 4=(1,1) 5=(2,1)
    // 6=(0,2) 7=(1,2) 8=(2,2)
    public GameObject[] cells = new GameObject[9];

    // 1 ô exit duy nhất phía trên bàn, CellClick.gridPos = (0,3)
    public GameObject blackExitCell;

    [Header("Colors")]
    public Color selectedColor   = new Color(1f,    0.85f, 0f,   1f);
    public Color validMoveColor  = new Color(0.3f,  0.9f,  0.3f, 1f);
    public Color exitColor       = new Color(0.2f,  0.7f,  1f,   1f);
    public Color normalCellColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color whitePieceColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color blackPieceColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Layout & Animation")]
    public float cellSize  = 2f;
    public float moveSpeed = 8f;

    // ── Private ───────────────────────────────────────────────────────
    private GameObject[]     whitePieceObjs = new GameObject[2];
    private GameObject[]     blackPieceObjs = new GameObject[2];
    private SpriteRenderer[] cellSR         = new SpriteRenderer[9];
    private SpriteRenderer   exitSR;
    private bool             initialized    = false;

    // ── Dùng Start thay Awake để đảm bảo Prefab đã được load trên WebGL ──
    void Start()
    {
        Init();
    }

    // Gọi thủ công từ GameManager nếu cần đảm bảo init trước Render()
    public void Init()
    {
        if (initialized) return;
        initialized = true;

        // Kiểm tra prefab trước khi Instantiate
        if (whitePiecePrefab == null || blackPiecePrefab == null)
        {
            Debug.LogError("[BoardRenderer] Prefab chưa gán trong Inspector!");
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            whitePieceObjs[i] = Instantiate(whitePiecePrefab);
            blackPieceObjs[i] = Instantiate(blackPiecePrefab);
            SetColor(whitePieceObjs[i], whitePieceColor);
            SetColor(blackPieceObjs[i], blackPieceColor);

            // Đặt tên để dễ debug
            whitePieceObjs[i].name = $"WhitePiece_{i}";
            blackPieceObjs[i].name = $"BlackPiece_{i}";
        }

        // Cache SpriteRenderer 9 ô
        for (int i = 0; i < 9; i++)
        {
            if (i < cells.Length && cells[i] != null)
                cellSR[i] = GetSR(cells[i]);
            else
                Debug.LogError($"[BoardRenderer] cells[{i}] chưa gán trong Inspector!");
        }

        // Exit cell — ẩn ngay
        if (blackExitCell != null)
        {
            exitSR = GetSR(blackExitCell);
            blackExitCell.SetActive(false);
        }

        ResetAllColors();
    }

    // ── Render tức thì ────────────────────────────────────────────────
    public void Render(GameState state)
    {
        Init(); // đảm bảo đã init

        for (int i = 0; i < 2; i++)
        {
            if (whitePieceObjs[i] == null || blackPieceObjs[i] == null) continue;

            bool wa = state.whitePieces[i].x != -1;
            whitePieceObjs[i].SetActive(wa);
            if (wa) whitePieceObjs[i].transform.position = GridToWorld(state.whitePieces[i]);

            bool ba = state.blackPieces[i].x != -1;
            blackPieceObjs[i].SetActive(ba);
            if (ba) blackPieceObjs[i].transform.position = GridToWorld(state.blackPieces[i]);
        }
        ResetAllColors();
    }

    // ── Render có animation ───────────────────────────────────────────
    public IEnumerator RenderAnimated(GameState oldState, GameState newState)
    {
        Init();
        ResetAllColors();
        var anims = new List<Coroutine>();

        for (int i = 0; i < 2; i++)
        {
            if (whitePieceObjs[i] == null || blackPieceObjs[i] == null) continue;

            bool wWas = oldState.whitePieces[i].x != -1;
            bool wNow = newState.whitePieces[i].x != -1;
            if (wWas && wNow && oldState.whitePieces[i] != newState.whitePieces[i])
                anims.Add(StartCoroutine(Slide(whitePieceObjs[i], GridToWorld(newState.whitePieces[i]))));
            else if (wWas && !wNow)
            {
                Vector3 ex = GridToWorld(oldState.whitePieces[i]) + new Vector3(cellSize * 1.5f, 0, 0);
                anims.Add(StartCoroutine(SlideAndHide(whitePieceObjs[i], ex)));
            }

            bool bWas = oldState.blackPieces[i].x != -1;
            bool bNow = newState.blackPieces[i].x != -1;
            if (bWas && bNow && oldState.blackPieces[i] != newState.blackPieces[i])
                anims.Add(StartCoroutine(Slide(blackPieceObjs[i], GridToWorld(newState.blackPieces[i]))));
            else if (bWas && !bNow)
            {
                Vector3 ex = GridToWorld(oldState.blackPieces[i]) + new Vector3(0, cellSize * 1.5f, 0);
                anims.Add(StartCoroutine(SlideAndHide(blackPieceObjs[i], ex)));
            }
        }

        foreach (var c in anims) yield return c;
        Render(newState);
    }

    // ── Highlight ─────────────────────────────────────────────────────
    public void HighlightSelected(Vector2Int selected, List<Vector2Int> validMoves)
    {
        ResetAllColors();

        int si = BoardIndex(selected);
        if (si >= 0 && cellSR[si] != null) cellSR[si].color = selectedColor;

        bool hasEscape = false;
        foreach (var mv in validMoves)
        {
            if (mv.y == 3) { hasEscape = true; continue; }
            int idx = BoardIndex(mv);
            if (idx >= 0 && cellSR[idx] != null) cellSR[idx].color = validMoveColor;
        }

        if (hasEscape && blackExitCell != null)
        {
            blackExitCell.SetActive(true);
            if (exitSR != null) exitSR.color = exitColor;
        }
    }

    public void ResetAllColors()
    {
        for (int i = 0; i < 9; i++)
            if (cellSR[i] != null) cellSR[i].color = normalCellColor;

        if (blackExitCell != null) blackExitCell.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────
    public Vector3 GridToWorld(Vector2Int grid)
    {
        float offset = (3 - 1) / 2f * cellSize;
        return new Vector3(grid.x * cellSize - offset,
                           grid.y * cellSize - offset, 0f);
    }

    public int BoardIndex(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x > 2 || pos.y < 0 || pos.y > 2) return -1;
        int idx = pos.y * 3 + pos.x;
        return idx < 9 ? idx : -1;
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