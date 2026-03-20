using UnityEngine;

public enum CellOwner { None, P0, P1, P2, P3 }

[System.Serializable]
public class PlayerPresetConfig
{
    public string          playerName  = "Player";
    public EscapeDirection escapeDir   = EscapeDirection.Right;
    public PlayerType      type        = PlayerType.Human;
    [Range(1, 6)]
    public int             botDepth    = 4;
    public Color           pieceColor  = Color.white;

    [Header("Cac o thoat hop le cua nguoi choi nay")]
    public Vector2Int[]    exitCells;
}

[System.Serializable]
public class BoardRow
{
    public CellOwner[] cells;
    public BoardRow(int width) { cells = new CellOwner[width]; }
}

[System.Serializable]
public class BoolRow
{
    public bool[] cells;
    public BoolRow(int width, bool defaultValue = true)
    {
        cells = new bool[width];
        for (int i = 0; i < width; i++) cells[i] = defaultValue;
    }
}

[CreateAssetMenu(fileName = "NewPreset", menuName = "Dodgem/GamePreset")]
public class GamePreset : ScriptableObject
{
    [Header("Thong tin hien thi trong menu")]
    public string presetName  = "2 Nguoi - 3x3";
    public string description = "Co dien";
    [TextArea(2, 3)]
    public string rules       = "";

    [Header("Board size")]
    [Range(3, 9)] public int boardWidth  = 3;
    [Range(3, 9)] public int boardHeight = 3;

    [Header("Ma tran xuat phat")]
    public BoardRow[] boardMatrix;

    [Header("Mask o hop le tren ban")]
    public BoolRow[] validMatrix;

    [Header("Cau hinh tung phe")]
    public PlayerPresetConfig[] playerConfigs;

    // Backward-compat helper
    public int boardSize => Mathf.Max(boardWidth, boardHeight);

    void OnValidate()
    {
        ResizeMatrix();
        if (playerConfigs == null) playerConfigs = new PlayerPresetConfig[0];
    }

    public void ResizeMatrix()
    {
        int W = boardWidth;
        int H = boardHeight;

        // boardMatrix
        if (boardMatrix == null || boardMatrix.Length != H)
        {
            var old = boardMatrix;
            boardMatrix = new BoardRow[H];
            for (int row = 0; row < H; row++)
            {
                boardMatrix[row] = new BoardRow(W);
                if (old != null && row < old.Length && old[row] != null && old[row].cells != null)
                {
                    for (int col = 0; col < W && col < old[row].cells.Length; col++)
                        boardMatrix[row].cells[col] = old[row].cells[col];
                }
            }
        }
        else
        {
            for (int row = 0; row < H; row++)
            {
                if (boardMatrix[row] == null)
                {
                    boardMatrix[row] = new BoardRow(W);
                }
                else if (boardMatrix[row].cells == null || boardMatrix[row].cells.Length != W)
                {
                    var old = boardMatrix[row].cells;
                    boardMatrix[row].cells = new CellOwner[W];
                    if (old != null)
                    {
                        for (int col = 0; col < W && col < old.Length; col++)
                            boardMatrix[row].cells[col] = old[col];
                    }
                }
            }
        }

        // validMatrix
        if (validMatrix == null || validMatrix.Length != H)
        {
            var old = validMatrix;
            validMatrix = new BoolRow[H];
            for (int row = 0; row < H; row++)
            {
                validMatrix[row] = new BoolRow(W, true);
                if (old != null && row < old.Length && old[row] != null && old[row].cells != null)
                {
                    for (int col = 0; col < W && col < old[row].cells.Length; col++)
                        validMatrix[row].cells[col] = old[row].cells[col];
                }
            }
        }
        else
        {
            for (int row = 0; row < H; row++)
            {
                if (validMatrix[row] == null)
                {
                    validMatrix[row] = new BoolRow(W, true);
                }
                else if (validMatrix[row].cells == null || validMatrix[row].cells.Length != W)
                {
                    var old = validMatrix[row].cells;
                    validMatrix[row].cells = new bool[W];
                    for (int col = 0; col < W; col++) validMatrix[row].cells[col] = true;

                    if (old != null)
                    {
                        for (int col = 0; col < W && col < old.Length; col++)
                            validMatrix[row].cells[col] = old[col];
                    }
                }
            }
        }

        // O invalid thi khong duoc co quan
        for (int row = 0; row < H; row++)
        {
            for (int col = 0; col < W; col++)
            {
                if (!validMatrix[row].cells[col])
                    boardMatrix[row].cells[col] = CellOwner.None;
            }
        }
    }

    public int NumPlayers => playerConfigs != null ? playerConfigs.Length : 0;

    public bool IsValidCell(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= boardWidth || pos.y < 0 || pos.y >= boardHeight)
            return false;

        if (validMatrix == null || pos.y >= validMatrix.Length || validMatrix[pos.y] == null || validMatrix[pos.y].cells == null)
            return true;

        if (pos.x >= validMatrix[pos.y].cells.Length) return false;
        return validMatrix[pos.y].cells[pos.x];
    }

    public bool[,] BuildValidCellMap()
    {
        var map = new bool[boardWidth, boardHeight];
        for (int y = 0; y < boardHeight; y++)
            for (int x = 0; x < boardWidth; x++)
                map[x, y] = IsValidCell(new Vector2Int(x, y));
        return map;
    }

    public Vector2Int[] GetStartPositions(int playerIdx)
    {
        var list   = new System.Collections.Generic.List<Vector2Int>();
        var target = (CellOwner)(playerIdx + 1);

        if (boardMatrix == null) return list.ToArray();

        for (int row = 0; row < boardMatrix.Length; row++)
        {
            if (boardMatrix[row]?.cells == null) continue;
            for (int col = 0; col < boardMatrix[row].cells.Length; col++)
            {
                var pos = new Vector2Int(col, row);
                if (!IsValidCell(pos)) continue;
                if (boardMatrix[row].cells[col] == target)
                    list.Add(pos);
            }
        }
        return list.ToArray();
    }

    public int GetPieceCount(int playerIdx) => GetStartPositions(playerIdx).Length;

    public Vector2Int[] GetExitCellsSafe(int playerIdx)
    {
        if (playerConfigs == null || playerIdx < 0 || playerIdx >= playerConfigs.Length)
            return new Vector2Int[0];

        var cfg = playerConfigs[playerIdx];
        if (cfg == null || cfg.exitCells == null)
            return new Vector2Int[0];

        var list = new System.Collections.Generic.List<Vector2Int>();
        foreach (var c in cfg.exitCells)
        {
            if (IsValidCell(c))
                list.Add(c);
        }
        return list.ToArray();
    }

    public bool IsValid(out string error)
    {
        if (playerConfigs == null || playerConfigs.Length < 2)
        {
            error = "Can it nhat 2 playerConfigs";
            return false;
        }

        for (int i = 0; i < playerConfigs.Length; i++)
        {
            if (GetStartPositions(i).Length == 0)
            {
                error = $"P{i} chua co quan nao tren matrix hop le";
                return false;
            }
        }

        error = "";
        return true;
    }

    public PlayerPresetConfig GetConfigSafe(int idx)
    {
        var defaults = new (string name, EscapeDirection dir, Color color, PlayerType type, int depth)[]
        {
            ("Trang", EscapeDirection.Right,  Color.white,                   PlayerType.Bot,   6),
            ("Den",   EscapeDirection.Top,    new Color(0.15f,0.15f,0.15f), PlayerType.Human, 0),
            ("Do",    EscapeDirection.Left,   new Color(0.85f,0.2f,0.2f),   PlayerType.Bot,   4),
            ("Xanh",  EscapeDirection.Bottom, new Color(0.2f,0.6f,0.9f),    PlayerType.Bot,   4),
        };

        PlayerPresetConfig cfg = (playerConfigs != null && idx < playerConfigs.Length)
                                 ? playerConfigs[idx]
                                 : new PlayerPresetConfig();

        if (idx >= defaults.Length) return cfg;

        var def = defaults[idx];

        return new PlayerPresetConfig
        {
            playerName = string.IsNullOrEmpty(cfg.playerName) ? def.name : cfg.playerName,
            escapeDir  = (cfg.escapeDir == 0 && idx != 0) ? def.dir : cfg.escapeDir,
            type       = cfg.type,
            botDepth   = (cfg.botDepth == 0) ? def.depth : cfg.botDepth,
            pieceColor = (cfg.pieceColor.a < 0.01f) ? def.color : cfg.pieceColor,
            exitCells  = cfg.exitCells != null ? cfg.exitCells : new Vector2Int[0]
        };
    }
}