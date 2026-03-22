using UnityEngine;

/// <summary>
/// Sinh cac o ban co theo kich thuoc runtime.
/// </summary>
public class BoardGenerator : MonoBehaviour
{
    #region Fields

    [Header("Prefabs")]
    public GameObject cellPrefab;

    [Header("Layout")]
    public float cellSize = 2f;

    public GameObject[] Cells { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    #endregion

    #region Public API

    /// <summary>
    /// Sinh toan bo ban co va gan click handler cho tung o.
    /// </summary>
    public void Generate(GameState state, ICellClickHandler clickHandler)
    {
        if (cellPrefab == null)
        {
            Debug.LogError("[BoardGenerator] Cell prefab is not assigned.");
            return;
        }

        ClearChildren();

        Width = state.boardWidth;
        Height = state.boardHeight;

        Cells = new GameObject[Width * Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var cell = Instantiate(cellPrefab, transform);
                cell.name = $"Cell_{x}_{y}";
                cell.transform.localPosition = GridToLocal(new Vector2Int(x, y), Width, Height);

                var click = cell.GetComponent<CellClick>();
                if (click != null)
                    click.Initialize(clickHandler, new Vector2Int(x, y));

                Cells[y * Width + x] = cell;
            }
        }
    }

    /// <summary>
    /// Chuyen toa do grid sang world.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int grid, int width, int height)
    {
        return transform.position + GridToLocal(grid, width, height);
    }

    /// <summary>
    /// Chuyen toa do grid sang local position.
    /// </summary>
    public Vector3 GridToLocal(Vector2Int grid, int width, int height)
    {
        float offsetX = (width - 1) * 0.5f * cellSize;
        float offsetY = (height - 1) * 0.5f * cellSize;
        return new Vector3(grid.x * cellSize - offsetX, grid.y * cellSize - offsetY, 0f);
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Don cac object con khi generator bi huy.
    /// </summary>
    void OnDestroy()
    {
        ClearChildren();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Xoa toan bo object con hien tai.
    /// </summary>
    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    #endregion
}