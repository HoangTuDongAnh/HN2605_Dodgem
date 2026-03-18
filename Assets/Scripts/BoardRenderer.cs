using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ================================================================
// BoardRenderer — hiển thị bàn cờ, quân, animation, highlight
//
// THAY ĐỔI SO VỚI BẢN CŨ:
//   - HighlightSelected nhận playerIdx + state để xác định exit marker
//   - GridToWorld dùng state.boardSize thay vì hardcode 3
//   - BoardIndex dùng state.boardSize thay vì hardcode 3
//   - Render dùng players[] thay vì whitePieces/blackPieces
//   - cells[] và pieceObjs[][] được sinh động theo boardSize (Init)
//
// Backward-compat: cells[] public vẫn dùng được cho 3x3 hand-setup.
// ================================================================

public class BoardRenderer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject whitePiecePrefab;   // Trắng (players[0])
    public GameObject blackPiecePrefab;   // Đen   (players[1])
    // Giai đoạn 3 sẽ thêm redPiecePrefab, bluePiecePrefab

    // 9 ô bàn cờ (3x3 hand-setup) — index: y*3+x
    // Khi Giai đoạn 2A xong sẽ được sinh động
    public GameObject[] cells = new GameObject[9];

    // 1 ô exit duy nhất phía trên bàn (dùng cho Đen 3x3)
    public GameObject blackExitCell;

    [Header("Colors")]
    public Color selectedColor   = new Color(1f,    0.85f, 0f,   1f);
    public Color validMoveColor  = new Color(0.3f,  0.9f,  0.3f, 1f);
    public Color exitColor       = new Color(0.2f,  0.7f,  1f,   1f);
    public Color normalCellColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Layout & Animation")]
    public float cellSize  = 2f;
    public float moveSpeed = 8f;

    // ── Private ───────────────────────────────────────────────────
    // pieceObjs[playerIdx][pieceIdx]
    private GameObject[][]   pieceObjs;
    private SpriteRenderer[] cellSR       = new SpriteRenderer[9];
    private SpriteRenderer   exitSR;
    private bool             initialized  = false;

    // Cache boardSize dùng khi không có state
    private int cachedBoardSize = 3;

    // ── Init ──────────────────────────────────────────────────────
    void Start() { Init(); }

    public void Init()
    {
        if (initialized) return;
        initialized = true;

        if (whitePiecePrefab == null || blackPiecePrefab == null)
        { Debug.LogError("[BoardRenderer] Prefab chưa gán!"); return; }

        // Với 3x3 2 người: tạo 2 phe, mỗi phe 2 quân
        // Giai đoạn 2 sẽ gọi InitForState(state) thay thế
        var prefabs = new[] { whitePiecePrefab, blackPiecePrefab };
        pieceObjs   = new GameObject[2][];
        for (int p = 0; p < 2; p++)
        {
            pieceObjs[p] = new GameObject[2];
            for (int i = 0; i < 2; i++)
            {
                pieceObjs[p][i] = Instantiate(prefabs[p]);
                pieceObjs[p][i].name = $"Player{p}_Piece{i}";
            }
        }

        // Cache SR 9 ô
        for (int i = 0; i < 9; i++)
        {
            if (i < cells.Length && cells[i] != null)
                cellSR[i] = GetSR(cells[i]);
            else if (i < cells.Length)
                Debug.LogError($"[BoardRenderer] cells[{i}] chưa gán!");
        }

        // Exit cell — ẩn ngay
        if (blackExitCell != null)
        {
            exitSR = GetSR(blackExitCell);
            blackExitCell.SetActive(false);
        }

        ResetAllColors();
    }

    // ── Render tức thì ────────────────────────────────────────────
    public void Render(GameState state)
    {
        Init();
        cachedBoardSize = state.boardSize;

        // Đảm bảo có đủ pieceObjs cho số phe hiện tại
        EnsurePieceObjs(state);

        for (int p = 0; p < state.NumPlayers && p < pieceObjs.Length; p++)
        {
            var player = state.players[p];
            for (int i = 0; i < player.pieces.Length && i < pieceObjs[p].Length; i++)
            {
                bool active = player.pieces[i].x != -1;
                pieceObjs[p][i].SetActive(active);
                if (active)
                    pieceObjs[p][i].transform.position = GridToWorld(player.pieces[i], state.boardSize);
            }
        }
        ResetAllColors();
    }

    // ── Render có animation ───────────────────────────────────────
    public IEnumerator RenderAnimated(GameState oldState, GameState newState)
    {
        Init();
        ResetAllColors();
        EnsurePieceObjs(newState);
        var anims = new List<Coroutine>();

        for (int p = 0; p < newState.NumPlayers && p < pieceObjs.Length; p++)
        {
            var oldP = oldState.players[p];
            var newP = newState.players[p];

            for (int i = 0; i < oldP.pieces.Length && i < pieceObjs[p].Length; i++)
            {
                bool was = oldP.pieces[i].x != -1;
                bool now = newP.pieces[i].x != -1;

                if (was && now && oldP.pieces[i] != newP.pieces[i])
                {
                    anims.Add(StartCoroutine(
                        Slide(pieceObjs[p][i], GridToWorld(newP.pieces[i], newState.boardSize))));
                }
                else if (was && !now)
                {
                    // Thoát theo hướng escape
                    Vector3 exit = GridToWorld(oldP.pieces[i], oldState.boardSize)
                                 + ExitOffset(newP.escapeDir, newState.boardSize);
                    anims.Add(StartCoroutine(SlideAndHide(pieceObjs[p][i], exit)));
                }
            }
        }

        foreach (var c in anims) yield return c;
        Render(newState);
    }

    // ── Highlight ─────────────────────────────────────────────────
    // Nhận playerIdx để biết escape marker của phe nào
    public void HighlightSelected(Vector2Int selected, List<Vector2Int> validMoves,
                                  int playerIdx, GameState state)
    {
        ResetAllColors();
        int N = state.boardSize;

        // Tô vàng ô quân chọn
        int si = BoardIndex(selected, N);
        if (si >= 0 && si < cellSR.Length && cellSR[si] != null)
            cellSR[si].color = selectedColor;

        bool hasEscape = false;
        foreach (var mv in validMoves)
        {
            // Ô thoát: nằm ngoài biên
            if (!DodgemRules.InBounds(mv, N))
            {
                hasEscape = true;
                continue;
            }
            int idx = BoardIndex(mv, N);
            if (idx >= 0 && idx < cellSR.Length && cellSR[idx] != null)
                cellSR[idx].color = validMoveColor;
        }

        // Hiện exit cell nếu có nước thoát
        if (hasEscape && blackExitCell != null)
        {
            blackExitCell.SetActive(true);
            if (exitSR != null) exitSR.color = exitColor;
        }
    }

    // Backward-compat (BoardRenderer cũ không có playerIdx)
    public void HighlightSelected(Vector2Int selected, List<Vector2Int> validMoves)
    {
        ResetAllColors();
        int N = cachedBoardSize;

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
        if (hasEscape && blackExitCell != null)
        {
            blackExitCell.SetActive(true);
            if (exitSR != null) exitSR.color = exitColor;
        }
    }

    public void ResetAllColors()
    {
        foreach (var sr in cellSR)
            if (sr != null) sr.color = normalCellColor;

        if (blackExitCell != null) blackExitCell.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────
    public Vector3 GridToWorld(Vector2Int grid, int boardSize)
    {
        float offset = (boardSize - 1) / 2f * cellSize;
        return new Vector3(grid.x * cellSize - offset,
                           grid.y * cellSize - offset, 0f);
    }

    // Backward-compat (dùng cachedBoardSize)
    public Vector3 GridToWorld(Vector2Int grid)
        => GridToWorld(grid, cachedBoardSize);

    public int BoardIndex(Vector2Int pos, int boardSize)
    {
        if (pos.x < 0 || pos.x >= boardSize || pos.y < 0 || pos.y >= boardSize) return -1;
        return pos.y * boardSize + pos.x;
    }

    public int BoardIndex(Vector2Int pos)
        => BoardIndex(pos, cachedBoardSize);

    // Offset animation khi quân thoát ra ngoài bàn
    Vector3 ExitOffset(EscapeDirection dir, int boardSize)
    {
        float d = cellSize * 1.5f;
        switch (dir)
        {
            case EscapeDirection.Right:  return new Vector3( d,  0, 0);
            case EscapeDirection.Top:    return new Vector3( 0,  d, 0);
            case EscapeDirection.Left:   return new Vector3(-d,  0, 0);
            case EscapeDirection.Bottom: return new Vector3( 0, -d, 0);
            default: return new Vector3(d, 0, 0);
        }
    }

    // Đảm bảo pieceObjs có đủ slot cho state.NumPlayers
    void EnsurePieceObjs(GameState state)
    {
        if (pieceObjs == null) pieceObjs = new GameObject[0][];
        if (pieceObjs.Length >= state.NumPlayers) return;

        // Mở rộng mảng (giữ phần tử cũ)
        var old = pieceObjs;
        pieceObjs = new GameObject[state.NumPlayers][];
        for (int i = 0; i < old.Length; i++) pieceObjs[i] = old[i];

        // Tạo thêm cho các phe mới (dùng màu từ PlayerData)
        for (int p = old.Length; p < state.NumPlayers; p++)
        {
            var player  = state.players[p];
            int nPieces = player.pieces.Length;
            pieceObjs[p] = new GameObject[nPieces];

            // Dùng whitePiecePrefab làm base, sau đó đổi màu
            for (int i = 0; i < nPieces; i++)
            {
                pieceObjs[p][i] = Instantiate(whitePiecePrefab);
                pieceObjs[p][i].name = $"Player{p}_Piece{i}";
                SetColor(pieceObjs[p][i], player.pieceColor);
            }
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