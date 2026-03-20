using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject cellPrefab;

    [Header("Layout")]
    public float cellSize = 2f;

    public GameObject[] Cells  { get; private set; }
    public int          Width  { get; private set; }
    public int          Height { get; private set; }

    public void Generate(GameState state)
    {
        if (cellPrefab == null)
        {
            Debug.LogError("[BoardGenerator] cellPrefab chua duoc gan!");
            return;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        Width  = state.boardWidth;
        Height = state.boardHeight;

        Cells = new GameObject[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var cell = Instantiate(cellPrefab, transform);
                cell.name = $"Cell_{x}_{y}";
                cell.transform.localPosition = GridToLocal(new Vector2Int(x, y), Width, Height);

                var cc = cell.GetComponent<CellClick>();
                if (cc != null) cc.gridPos = new Vector2Int(x, y);

                Cells[y * Width + x] = cell;
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int grid, int width, int height)
        => transform.position + GridToLocal(grid, width, height);

    public Vector3 GridToLocal(Vector2Int grid, int width, int height)
    {
        float offsetX = (width - 1) * 0.5f * cellSize;
        float offsetY = (height - 1) * 0.5f * cellSize;
        return new Vector3(grid.x * cellSize - offsetX, grid.y * cellSize - offsetY, 0f);
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