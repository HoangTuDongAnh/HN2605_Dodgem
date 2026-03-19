using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject cellPrefab;
    public GameObject exitCellPrefab;  // co the de null, se dung cellPrefab lam fallback

    [Header("Layout")]
    public float cellSize      = 2f;
    public float exitThickness = 1.8f;

    public GameObject[] Cells     { get; private set; }
    public GameObject[] ExitCells { get; private set; }
    public int          BoardSize { get; private set; }

    public void Generate(GameState state)
    {
        if (cellPrefab == null)
        {
            Debug.LogError("[BoardGenerator] cellPrefab chua duoc gan!");
            return;
        }

        // FIX: DestroyImmediate dam bao xoa trong cung frame, khong defer
        // Phai xoa truoc khi spawn de tranh cell cu con ton tai song song
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        int N = state.boardSize;
        BoardSize = N;

        Cells = new GameObject[N * N];
        for (int y = 0; y < N; y++)
        {
            for (int x = 0; x < N; x++)
            {
                var cell = Instantiate(cellPrefab, transform);
                cell.name = $"Cell_{x}_{y}";
                cell.transform.localPosition = GridToLocal(new Vector2Int(x, y), N);

                var cc = cell.GetComponent<CellClick>();
                if (cc != null) cc.gridPos = new Vector2Int(x, y);

                Cells[y * N + x] = cell;
            }
        }

        ExitCells = new GameObject[state.NumPlayers];
        for (int p = 0; p < state.NumPlayers; p++)
        {
            var player = state.players[p];
            if (player.type != PlayerType.Human) continue;

            // FIX: neu exitCellPrefab null hoac trung cellPrefab thi van ok
            // BoardRenderer se dung exitColor de to mau phan biet
            var prefab = (exitCellPrefab != null) ? exitCellPrefab : cellPrefab;
            ExitCells[p] = CreateExitCell(player, N, prefab);
        }
    }

    GameObject CreateExitCell(PlayerData player, int N, GameObject prefab)
    {
        var exit = Instantiate(prefab, transform);
        exit.name = $"ExitCell_P{player.playerIndex}";
        exit.SetActive(false);

        float half = (N - 1) / 2f * cellSize;
        float dist = half + cellSize;

        Vector3 pos   = Vector3.zero;
        Vector3 scale = Vector3.one;

        switch (player.escapeDir)
        {
            case EscapeDirection.Top:
                pos = new Vector3(0, dist, 0); scale = new Vector3(N * cellSize, exitThickness, 1f); break;
            case EscapeDirection.Right:
                pos = new Vector3(dist, 0, 0); scale = new Vector3(exitThickness, N * cellSize, 1f); break;
            case EscapeDirection.Left:
                pos = new Vector3(-dist, 0, 0); scale = new Vector3(exitThickness, N * cellSize, 1f); break;
            case EscapeDirection.Bottom:
                pos = new Vector3(0, -dist, 0); scale = new Vector3(N * cellSize, exitThickness, 1f); break;
        }

        exit.transform.localPosition = pos;
        exit.transform.localScale    = scale;

        var cc = exit.GetComponent<CellClick>();
        if (cc != null)
        {
            switch (player.escapeDir)
            {
                case EscapeDirection.Top:    cc.gridPos = new Vector2Int(0,  N);  break;
                case EscapeDirection.Right:  cc.gridPos = new Vector2Int(N,  0);  break;
                case EscapeDirection.Left:   cc.gridPos = new Vector2Int(-1, 0);  break;
                case EscapeDirection.Bottom: cc.gridPos = new Vector2Int(0, -1);  break;
            }
        }

        return exit;
    }

    public Vector3 GridToWorld(Vector2Int grid, int boardSize)
        => transform.position + GridToLocal(grid, boardSize);

    public Vector3 GridToLocal(Vector2Int grid, int boardSize)
    {
        float offset = (boardSize - 1) / 2f * cellSize;
        return new Vector3(grid.x * cellSize - offset, grid.y * cellSize - offset, 0f);
    }

    void OnDestroy()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }
}