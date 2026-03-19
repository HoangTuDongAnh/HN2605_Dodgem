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
}

[System.Serializable]
public class BoardRow
{
    public CellOwner[] cells;
    public BoardRow(int width) { cells = new CellOwner[width]; }
}

[CreateAssetMenu(fileName = "NewPreset", menuName = "Dodgem/GamePreset")]
public class GamePreset : ScriptableObject
{
    [Header("Thong tin hien thi trong menu")]
    public string presetName  = "2 Nguoi - 3x3";
    public string description = "Co dien, 2 nguoi, ban co 3x3";
    [TextArea(2, 3)]
    public string rules       = "";

    [Header("Board size")]
    [Range(3, 7)]
    public int boardSize = 3;

    [Header("Ma tran xuat phat")]
    public BoardRow[] boardMatrix;

    [Header("Cau hinh tung phe")]
    public PlayerPresetConfig[] playerConfigs;

    void OnValidate()
    {
        ResizeMatrix();
        if (playerConfigs == null) playerConfigs = new PlayerPresetConfig[0];
    }

    public void ResizeMatrix()
    {
        int N = boardSize;
        if (boardMatrix == null || boardMatrix.Length != N)
        {
            var old = boardMatrix;
            boardMatrix = new BoardRow[N];
            for (int row = 0; row < N; row++)
            {
                boardMatrix[row] = new BoardRow(N);
                if (old != null && row < old.Length && old[row] != null)
                    for (int col = 0; col < N && col < old[row].cells.Length; col++)
                        boardMatrix[row].cells[col] = old[row].cells[col];
            }
        }
        else
        {
            for (int row = 0; row < N; row++)
            {
                if (boardMatrix[row] == null)
                    boardMatrix[row] = new BoardRow(N);
                else if (boardMatrix[row].cells == null || boardMatrix[row].cells.Length != N)
                {
                    var old = boardMatrix[row].cells;
                    boardMatrix[row].cells = new CellOwner[N];
                    if (old != null)
                        for (int col = 0; col < N && col < old.Length; col++)
                            boardMatrix[row].cells[col] = old[col];
                }
            }
        }
    }

    public int NumPlayers => playerConfigs != null ? playerConfigs.Length : 0;

    public Vector2Int[] GetStartPositions(int playerIdx)
    {
        var list   = new System.Collections.Generic.List<Vector2Int>();
        var target = (CellOwner)(playerIdx + 1);

        if (boardMatrix == null) return list.ToArray();

        for (int row = 0; row < boardMatrix.Length; row++)
        {
            if (boardMatrix[row]?.cells == null) continue;
            for (int col = 0; col < boardMatrix[row].cells.Length; col++)
                if (boardMatrix[row].cells[col] == target)
                    list.Add(new Vector2Int(col, row));
        }
        return list.ToArray();
    }

    public int GetPieceCount(int playerIdx) => GetStartPositions(playerIdx).Length;

    // FIX: IsValid chi kiem tra co du quan khong, khong check playerName
    public bool IsValid(out string error)
    {
        if (playerConfigs == null || playerConfigs.Length < 2)
        { error = "Can it nhat 2 playerConfigs"; return false; }

        for (int i = 0; i < playerConfigs.Length; i++)
        {
            if (GetStartPositions(i).Length == 0)
            { error = $"P{i} chua co quan nao tren matrix"; return false; }
        }
        error = "";
        return true;
    }

    // Tra ve PlayerPresetConfig da duoc fill default neu thieu data
    public PlayerPresetConfig GetConfigSafe(int idx)
    {
        // Default cho tung phe
        var defaults = new (string name, EscapeDirection dir, Color color, PlayerType type, int depth)[]
        {
            ("Trang", EscapeDirection.Right,  Color.white,                     PlayerType.Bot,   6),
            ("Den",   EscapeDirection.Top,    new Color(0.15f,0.15f,0.15f),   PlayerType.Human, 0),
            ("Do",    EscapeDirection.Left,   new Color(0.85f,0.2f, 0.2f),    PlayerType.Bot,   4),
            ("Xanh",  EscapeDirection.Bottom, new Color(0.2f, 0.6f, 0.9f),    PlayerType.Bot,   4),
        };

        PlayerPresetConfig cfg = (playerConfigs != null && idx < playerConfigs.Length)
                                 ? playerConfigs[idx] : new PlayerPresetConfig();

        if (idx >= defaults.Length) return cfg;

        var def = defaults[idx];

        // Neu cac field trong asset bi de mac dinh (empty/zero) thi dung fallback
        return new PlayerPresetConfig
        {
            playerName = string.IsNullOrEmpty(cfg.playerName) ? def.name : cfg.playerName,
            escapeDir  = (cfg.escapeDir == 0 && idx != 0)     ? def.dir  : cfg.escapeDir,
            type       = cfg.type,
            botDepth   = (cfg.botDepth == 0)                  ? def.depth : cfg.botDepth,
            // pieceColor: neu la {0,0,0,0} (alpha=0) thi dung fallback
            pieceColor = (cfg.pieceColor.a < 0.01f)           ? def.color : cfg.pieceColor,
        };
    }
}