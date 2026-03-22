using UnityEngine;

public enum CellOwner
{
    None,
    P0,
    P1,
    P2,
    P3
}

[System.Serializable]
public class PlayerPresetConfig
{
    public string playerName = "Player";
    public EscapeDirection escapeDir = EscapeDirection.Right;
    public PlayerType type = PlayerType.Human;

    [Range(1, 6)]
    public int botDepth = 4;

    public Color pieceColor = Color.white;

    [Header("Cac o thoat hop le cua nguoi choi nay")]
    public Vector2Int[] exitCells;
}

[System.Serializable]
public class BoardRow
{
    public CellOwner[] cells;

    public BoardRow(int width)
    {
        cells = new CellOwner[width];
    }
}

[System.Serializable]
public class BoolRow
{
    public bool[] cells;

    public BoolRow(int width, bool defaultValue = true)
    {
        cells = new bool[width];
        for (int i = 0; i < width; i++)
            cells[i] = defaultValue;
    }
}

/// <summary>
/// Luu du lieu preset cho board, player va vi tri khoi tao.
/// </summary>
[CreateAssetMenu(fileName = "NewPreset", menuName = "Dodgem/GamePreset")]
public class GamePreset : ScriptableObject
{
    #region Fields

    [Header("Display Info")]
    public string presetName = "2 Players - 3x3";
    public string description = "Classic";
    [TextArea(2, 3)]
    public string rules = "";

    [Header("Board Size")]
    [Range(3, 9)] public int boardWidth = 3;
    [Range(3, 9)] public int boardHeight = 3;

    [Header("Initial Board Matrix")]
    public BoardRow[] boardMatrix;

    [Header("Playable Cell Mask")]
    public BoolRow[] validMatrix;

    [Header("Player Configurations")]
    public PlayerPresetConfig[] playerConfigs;

    #endregion

    #region Properties

    public int NumPlayers => playerConfigs != null ? playerConfigs.Length : 0;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Dong bo kich thuoc matrix khi asset thay doi.
    /// </summary>
    void OnValidate()
    {
        ResizeMatrix();

        if (playerConfigs == null)
            playerConfigs = new PlayerPresetConfig[0];
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Dong bo kich thuoc matrix theo width va height hien tai.
    /// </summary>
    public void ResizeMatrix()
    {
        int width = boardWidth;
        int height = boardHeight;

        ResizeBoardMatrix(width, height);
        ResizeValidMatrix(width, height);
        ClearPiecesOnInvalidCells(width, height);
    }

    /// <summary>
    /// Kiem tra o co hop le theo valid matrix hay khong.
    /// </summary>
    public bool IsValidCell(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= boardWidth || pos.y < 0 || pos.y >= boardHeight)
            return false;

        if (validMatrix == null || pos.y >= validMatrix.Length || validMatrix[pos.y] == null || validMatrix[pos.y].cells == null)
            return true;

        if (pos.x >= validMatrix[pos.y].cells.Length)
            return false;

        return validMatrix[pos.y].cells[pos.x];
    }

    /// <summary>
    /// Tao mask playable cell cho runtime state.
    /// </summary>
    public bool[,] BuildValidCellMap()
    {
        var map = new bool[boardWidth, boardHeight];

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
                map[x, y] = IsValidCell(new Vector2Int(x, y));
        }

        return map;
    }

    /// <summary>
    /// Lay vi tri xuat phat cua mot nguoi choi.
    /// </summary>
    public Vector2Int[] GetStartPositions(int playerIdx)
    {
        var result = new System.Collections.Generic.List<Vector2Int>();
        var target = (CellOwner)(playerIdx + 1);

        if (boardMatrix == null)
            return result.ToArray();

        for (int row = 0; row < boardMatrix.Length; row++)
        {
            if (boardMatrix[row]?.cells == null) continue;

            for (int col = 0; col < boardMatrix[row].cells.Length; col++)
            {
                var pos = new Vector2Int(col, row);
                if (!IsValidCell(pos)) continue;

                if (boardMatrix[row].cells[col] == target)
                    result.Add(pos);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Lay so quan khoi tao cua mot nguoi choi.
    /// </summary>
    public int GetPieceCount(int playerIdx)
    {
        return GetStartPositions(playerIdx).Length;
    }

    /// <summary>
    /// Lay danh sach exit cell hop le cua mot nguoi choi.
    /// </summary>
    public Vector2Int[] GetExitCellsSafe(int playerIdx)
    {
        if (playerConfigs == null || playerIdx < 0 || playerIdx >= playerConfigs.Length)
            return new Vector2Int[0];

        var cfg = playerConfigs[playerIdx];
        if (cfg == null || cfg.exitCells == null)
            return new Vector2Int[0];

        var result = new System.Collections.Generic.List<Vector2Int>();
        foreach (var cell in cfg.exitCells)
        {
            if (IsValidCell(cell))
                result.Add(cell);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Kiem tra preset hien tai co hop le de start game hay khong.
    /// </summary>
    public bool IsValid(out string error)
    {
        if (playerConfigs == null || playerConfigs.Length < 2)
        {
            error = "At least 2 player configurations are required.";
            return false;
        }

        for (int i = 0; i < playerConfigs.Length; i++)
        {
            if (GetStartPositions(i).Length == 0)
            {
                error = $"Player {i} has no valid starting piece.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Lay config an toan cua mot player, co ap dung default neu can.
    /// </summary>
    public PlayerPresetConfig GetConfigSafe(int idx)
    {
        var defaults = new (string name, EscapeDirection dir, Color color, PlayerType type, int depth)[]
        {
            ("White", EscapeDirection.Right,  Color.white,                   PlayerType.Bot,   6),
            ("Black", EscapeDirection.Top,    new Color(0.15f,0.15f,0.15f), PlayerType.Human, 0),
            ("Red",   EscapeDirection.Left,   new Color(0.85f,0.2f,0.2f),   PlayerType.Bot,   4),
            ("Blue",  EscapeDirection.Bottom, new Color(0.2f,0.6f,0.9f),    PlayerType.Bot,   4),
        };

        PlayerPresetConfig cfg =
            (playerConfigs != null && idx < playerConfigs.Length)
                ? playerConfigs[idx]
                : new PlayerPresetConfig();

        if (idx >= defaults.Length)
            return cfg;

        var def = defaults[idx];

        return new PlayerPresetConfig
        {
            playerName = string.IsNullOrEmpty(cfg.playerName) ? def.name : cfg.playerName,
            escapeDir = (cfg.escapeDir == 0 && idx != 0) ? def.dir : cfg.escapeDir,
            type = cfg.type,
            botDepth = (cfg.botDepth == 0) ? def.depth : cfg.botDepth,
            pieceColor = (cfg.pieceColor.a < 0.01f) ? def.color : cfg.pieceColor,
            exitCells = cfg.exitCells != null ? cfg.exitCells : new Vector2Int[0]
        };
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Dong bo board matrix theo kich thuoc moi.
    /// </summary>
    void ResizeBoardMatrix(int width, int height)
    {
        if (boardMatrix == null || boardMatrix.Length != height)
        {
            var old = boardMatrix;
            boardMatrix = new BoardRow[height];

            for (int row = 0; row < height; row++)
            {
                boardMatrix[row] = new BoardRow(width);

                if (old != null && row < old.Length && old[row] != null && old[row].cells != null)
                {
                    for (int col = 0; col < width && col < old[row].cells.Length; col++)
                        boardMatrix[row].cells[col] = old[row].cells[col];
                }
            }
        }
        else
        {
            for (int row = 0; row < height; row++)
            {
                if (boardMatrix[row] == null)
                {
                    boardMatrix[row] = new BoardRow(width);
                }
                else if (boardMatrix[row].cells == null || boardMatrix[row].cells.Length != width)
                {
                    var old = boardMatrix[row].cells;
                    boardMatrix[row].cells = new CellOwner[width];

                    if (old != null)
                    {
                        for (int col = 0; col < width && col < old.Length; col++)
                            boardMatrix[row].cells[col] = old[col];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Dong bo valid matrix theo kich thuoc moi.
    /// </summary>
    void ResizeValidMatrix(int width, int height)
    {
        if (validMatrix == null || validMatrix.Length != height)
        {
            var old = validMatrix;
            validMatrix = new BoolRow[height];

            for (int row = 0; row < height; row++)
            {
                validMatrix[row] = new BoolRow(width, true);

                if (old != null && row < old.Length && old[row] != null && old[row].cells != null)
                {
                    for (int col = 0; col < width && col < old[row].cells.Length; col++)
                        validMatrix[row].cells[col] = old[row].cells[col];
                }
            }
        }
        else
        {
            for (int row = 0; row < height; row++)
            {
                if (validMatrix[row] == null)
                {
                    validMatrix[row] = new BoolRow(width, true);
                }
                else if (validMatrix[row].cells == null || validMatrix[row].cells.Length != width)
                {
                    var old = validMatrix[row].cells;
                    validMatrix[row].cells = new bool[width];

                    for (int col = 0; col < width; col++)
                        validMatrix[row].cells[col] = true;

                    if (old != null)
                    {
                        for (int col = 0; col < width && col < old.Length; col++)
                            validMatrix[row].cells[col] = old[col];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Xoa quan nam tren cac o invalid.
    /// </summary>
    void ClearPiecesOnInvalidCells(int width, int height)
    {
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                if (!validMatrix[row].cells[col])
                    boardMatrix[row].cells[col] = CellOwner.None;
            }
        }
    }

    #endregion
}