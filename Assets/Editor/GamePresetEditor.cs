#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(GamePreset))]
public class GamePresetEditor : Editor
{
    const int CELL_SIZE    = 36;
    const int CELL_PADDING = 3;
    const int LABEL_WIDTH  = 20;

    static readonly Color ColorNone    = new Color(0.25f, 0.25f, 0.25f);
    static readonly Color ColorP0      = new Color(0.95f, 0.95f, 0.95f);
    static readonly Color ColorP1      = new Color(0.15f, 0.15f, 0.15f);
    static readonly Color ColorP2      = new Color(0.85f, 0.2f,  0.2f);
    static readonly Color ColorP3      = new Color(0.2f,  0.6f,  0.9f);
    static readonly Color InvalidColor = new Color(0.08f, 0.08f, 0.08f);

    enum EditMode
    {
        Matrix,
        ExitCells,
        ValidCells
    }

    EditMode editMode = EditMode.Matrix;
    int selectedExitPlayer = 0;

    static readonly (string name, EscapeDirection dir, Color color, PlayerType type, int depth)[] Defaults =
    {
        ("Trang", EscapeDirection.Right,  new Color(0.95f,0.95f,0.95f),  PlayerType.Bot,   6),
        ("Den",   EscapeDirection.Top,    new Color(0.15f,0.15f,0.15f),  PlayerType.Human, 0),
        ("Do",    EscapeDirection.Left,   new Color(0.85f,0.2f, 0.2f),   PlayerType.Bot,   4),
        ("Xanh",  EscapeDirection.Bottom, new Color(0.2f, 0.6f, 0.9f),   PlayerType.Bot,   4),
    };

    public override void OnInspectorGUI()
    {
        var preset = (GamePreset)target;
        serializedObject.Update();

        EditorGUILayout.LabelField("Thong tin", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("presetName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("description"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rules"));
        EditorGUILayout.Space(8);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("boardWidth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("boardHeight"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            preset.ResizeMatrix();
            EditorUtility.SetDirty(preset);
            serializedObject.Update();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Cau hinh nguoi choi", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("playerConfigs"), true);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Apply Defaults cho playerConfigs", GUILayout.Height(28)))
        {
            Undo.RecordObject(preset, "Apply Defaults");
            ApplyDefaults(preset);
            EditorUtility.SetDirty(preset);
            serializedObject.Update();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Che do chinh sua tren luoi", EditorStyles.boldLabel);
        editMode = (EditMode)GUILayout.Toolbar((int)editMode, new[] { "Matrix", "Exit Cells", "Valid Cells" });

        if (editMode == EditMode.ExitCells)
            DrawExitCellControls(preset);

        EditorGUILayout.Space(6);
        DrawMatrix(preset);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Tien ich", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Xoa matrix"))
        {
            Undo.RecordObject(preset, "Clear Matrix");
            ClearMatrix(preset);
            EditorUtility.SetDirty(preset);
        }

        if (GUILayout.Button("Tat ca o hop le"))
        {
            Undo.RecordObject(preset, "All Cells Valid");
            SetAllCellsValid(preset, true);
            EditorUtility.SetDirty(preset);
        }

        if (GUILayout.Button("Tat ca o invalid"))
        {
            Undo.RecordObject(preset, "All Cells Invalid");
            SetAllCellsValid(preset, false);
            EditorUtility.SetDirty(preset);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto-fill 2 phe"))
        {
            Undo.RecordObject(preset, "Auto Fill 2P");
            AutoFill2Player(preset);
            EditorUtility.SetDirty(preset);
        }

        if (GUILayout.Button("Auto-fill 4 phe"))
        {
            Undo.RecordObject(preset, "Auto Fill 4P");
            AutoFill4Player(preset);
            EditorUtility.SetDirty(preset);
        }
        EditorGUILayout.EndHorizontal();

        if (editMode == EditMode.ExitCells)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Xoa exit cells cua player dang chon"))
            {
                Undo.RecordObject(preset, "Clear Exit Cells");
                ClearExitCellsOfPlayer(preset, selectedExitPlayer);
                EditorUtility.SetDirty(preset);
                Repaint();
            }

            if (GUILayout.Button("Gan nhanh exit theo bien huong escape"))
            {
                Undo.RecordObject(preset, "Auto Fill Exit");
                AutoFillExitCellsByDirection(preset, selectedExitPlayer);
                EditorUtility.SetDirty(preset);
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);
        string err;
        if (preset.IsValid(out err))
        {
            EditorGUILayout.HelpBox("Preset hop le!", MessageType.None);
            for (int i = 0; i < preset.NumPlayers; i++)
            {
                var cfg = preset.GetConfigSafe(i);
                int exits = cfg.exitCells != null ? cfg.exitCells.Length : 0;
                EditorGUILayout.LabelField(
                    $"{cfg.playerName}: {preset.GetPieceCount(i)} quan | {cfg.escapeDir} | {cfg.type} | exits={exits}");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("LOI: " + err, MessageType.Error);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawExitCellControls(GamePreset preset)
    {
        if (preset.playerConfigs == null || preset.playerConfigs.Length == 0)
        {
            EditorGUILayout.HelpBox("Chua co playerConfigs.", MessageType.Warning);
            return;
        }

        string[] names = new string[preset.playerConfigs.Length];
        for (int i = 0; i < names.Length; i++)
        {
            var cfg = preset.GetConfigSafe(i);
            names[i] = $"P{i} - {cfg.playerName}";
        }

        selectedExitPlayer = EditorGUILayout.Popup("Player dang chinh exit", selectedExitPlayer, names);
        selectedExitPlayer = Mathf.Clamp(selectedExitPlayer, 0, Mathf.Max(0, preset.playerConfigs.Length - 1));
    }

    void ApplyDefaults(GamePreset preset)
    {
        if (preset.playerConfigs == null)
        {
            preset.playerConfigs = new PlayerPresetConfig[0];
            return;
        }

        for (int i = 0; i < preset.playerConfigs.Length && i < Defaults.Length; i++)
        {
            var cfg = preset.playerConfigs[i];
            var def = Defaults[i];

            if (string.IsNullOrEmpty(cfg.playerName)) cfg.playerName = def.name;
            if (cfg.escapeDir == 0 && i != 0)         cfg.escapeDir  = def.dir;
            if (cfg.pieceColor.a < 0.01f)             cfg.pieceColor = def.color;
            if (cfg.botDepth == 0)                    cfg.botDepth   = def.depth;
            if (cfg.exitCells == null)                cfg.exitCells  = new Vector2Int[0];

            preset.playerConfigs[i] = cfg;
        }
    }

    void DrawMatrix(GamePreset preset)
    {
        if (preset.boardMatrix == null) return;

        int W = preset.boardWidth;
        int H = preset.boardHeight;

        int totalW = LABEL_WIDTH + W * (CELL_SIZE + CELL_PADDING);
        int totalH = H * (CELL_SIZE + CELL_PADDING) + LABEL_WIDTH;

        Rect layoutRect = GUILayoutUtility.GetRect(totalW, totalH + 8);
        float startX = layoutRect.x + (layoutRect.width - totalW) / 2f;
        float startY = layoutRect.y + 4;

        for (int col = 0; col < W; col++)
        {
            float cx = startX + LABEL_WIDTH + col * (CELL_SIZE + CELL_PADDING);
            GUI.Label(new Rect(cx, startY, CELL_SIZE, LABEL_WIDTH - 2), col.ToString(),
                new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
        }

        for (int row = 0; row < H; row++)
        {
            int displayRow = H - 1 - row;
            float cy = startY + LABEL_WIDTH + displayRow * (CELL_SIZE + CELL_PADDING);

            GUI.Label(new Rect(startX, cy, LABEL_WIDTH - 2, CELL_SIZE), row.ToString(),
                new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });

            if (preset.boardMatrix[row] == null) continue;

            for (int col = 0; col < W; col++)
            {
                float cx = startX + LABEL_WIDTH + col * (CELL_SIZE + CELL_PADDING);
                var cellRect = new Rect(cx, cy, CELL_SIZE, CELL_SIZE);

                bool isValid = preset.IsValidCell(new Vector2Int(col, row));
                var owner = preset.boardMatrix[row].cells[col];
                Color bg = isValid ? GetOwnerColor(owner, preset) : InvalidColor;

                EditorGUI.DrawRect(cellRect, bg);
                DrawBorder(cellRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));

                string lbl = !isValid ? "X" : owner == CellOwner.None ? "" : owner.ToString();
                GUI.Label(cellRect, lbl, new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Luminance(bg) > 0.5f ? Color.black : Color.white }
                });

                DrawExitOverlayIfNeeded(preset, row, col, cellRect, isValid);

                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    if (editMode == EditMode.Matrix)
                    {
                        if (!isValid)
                        {
                            Event.current.Use();
                            return;
                        }

                        Undo.RecordObject(preset, "Toggle Cell");
                        preset.boardMatrix[row].cells[col] = NextOwner(owner, preset.NumPlayers);
                        EditorUtility.SetDirty(preset);
                    }
                    else if (editMode == EditMode.ExitCells)
                    {
                        if (!isValid)
                        {
                            Event.current.Use();
                            return;
                        }

                        Undo.RecordObject(preset, "Toggle Exit Cell");
                        ToggleExitCell(preset, selectedExitPlayer, new Vector2Int(col, row));
                        EditorUtility.SetDirty(preset);
                    }
                    else
                    {
                        Undo.RecordObject(preset, "Toggle Valid Cell");
                        ToggleValidCell(preset, new Vector2Int(col, row));
                        EditorUtility.SetDirty(preset);
                    }

                    Event.current.Use();
                    Repaint();
                }
            }
        }
    }

    void DrawExitOverlayIfNeeded(GamePreset preset, int row, int col, Rect cellRect, bool isValid)
    {
        if (!isValid) return;
        if (preset.playerConfigs == null) return;

        for (int i = 0; i < preset.playerConfigs.Length; i++)
        {
            var cfg = preset.playerConfigs[i];
            if (cfg == null || cfg.exitCells == null) continue;

            bool isExit = false;
            foreach (var p in cfg.exitCells)
            {
                if (p.x == col && p.y == row)
                {
                    isExit = true;
                    break;
                }
            }

            if (!isExit) continue;

            Color c = GetPlayerColor(i, preset);
            c.a = 1f;

            float pad = 3f + i * 2f;
            var r = new Rect(cellRect.x + pad, cellRect.y + pad,
                             cellRect.width - pad * 2f, cellRect.height - pad * 2f);

            DrawBorder(r, c);

            if (editMode == EditMode.ExitCells && i == selectedExitPlayer)
            {
                var dot = new Rect(cellRect.center.x - 4, cellRect.center.y - 4, 8, 8);
                EditorGUI.DrawRect(dot, c);
            }
        }
    }

    void ToggleValidCell(GamePreset preset, Vector2Int cell)
    {
        if (preset.validMatrix == null || cell.y < 0 || cell.y >= preset.validMatrix.Length) return;
        if (preset.validMatrix[cell.y] == null || preset.validMatrix[cell.y].cells == null) return;
        if (cell.x < 0 || cell.x >= preset.validMatrix[cell.y].cells.Length) return;

        bool next = !preset.validMatrix[cell.y].cells[cell.x];
        preset.validMatrix[cell.y].cells[cell.x] = next;

        if (!next)
        {
            preset.boardMatrix[cell.y].cells[cell.x] = CellOwner.None;

            if (preset.playerConfigs != null)
            {
                for (int i = 0; i < preset.playerConfigs.Length; i++)
                {
                    var cfg = preset.playerConfigs[i];
                    if (cfg.exitCells == null) continue;

                    var list = new List<Vector2Int>(cfg.exitCells);
                    list.RemoveAll(v => v == cell);
                    cfg.exitCells = list.ToArray();
                    preset.playerConfigs[i] = cfg;
                }
            }
        }
    }

    void SetAllCellsValid(GamePreset preset, bool value)
    {
        int W = preset.boardWidth;
        int H = preset.boardHeight;

        if (preset.validMatrix == null || preset.validMatrix.Length != H)
            preset.ResizeMatrix();

        for (int row = 0; row < H; row++)
            for (int col = 0; col < W; col++)
                preset.validMatrix[row].cells[col] = value;

        if (!value)
        {
            ClearMatrix(preset);
            if (preset.playerConfigs != null)
            {
                for (int i = 0; i < preset.playerConfigs.Length; i++)
                {
                    var cfg = preset.playerConfigs[i];
                    cfg.exitCells = new Vector2Int[0];
                    preset.playerConfigs[i] = cfg;
                }
            }
        }
    }

    void ToggleExitCell(GamePreset preset, int playerIdx, Vector2Int cell)
    {
        if (preset.playerConfigs == null) return;
        if (playerIdx < 0 || playerIdx >= preset.playerConfigs.Length) return;
        if (!preset.IsValidCell(cell)) return;

        var cfg = preset.playerConfigs[playerIdx];
        if (cfg.exitCells == null)
            cfg.exitCells = new Vector2Int[0];

        var list = new List<Vector2Int>(cfg.exitCells);
        int found = -1;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == cell)
            {
                found = i;
                break;
            }
        }

        if (found >= 0) list.RemoveAt(found);
        else list.Add(cell);

        cfg.exitCells = list.ToArray();
        preset.playerConfigs[playerIdx] = cfg;
    }

    void ClearExitCellsOfPlayer(GamePreset preset, int playerIdx)
    {
        if (preset.playerConfigs == null) return;
        if (playerIdx < 0 || playerIdx >= preset.playerConfigs.Length) return;

        var cfg = preset.playerConfigs[playerIdx];
        cfg.exitCells = new Vector2Int[0];
        preset.playerConfigs[playerIdx] = cfg;
    }

    void AutoFillExitCellsByDirection(GamePreset preset, int playerIdx)
    {
        if (preset.playerConfigs == null) return;
        if (playerIdx < 0 || playerIdx >= preset.playerConfigs.Length) return;

        int W = preset.boardWidth;
        int H = preset.boardHeight;
        var cfg = preset.playerConfigs[playerIdx];
        var list = new List<Vector2Int>();

        switch (cfg.escapeDir)
        {
            case EscapeDirection.Right:
                for (int row = 0; row < H; row++)
                {
                    var p = new Vector2Int(W - 1, row);
                    if (preset.IsValidCell(p)) list.Add(p);
                }
                break;

            case EscapeDirection.Left:
                for (int row = 0; row < H; row++)
                {
                    var p = new Vector2Int(0, row);
                    if (preset.IsValidCell(p)) list.Add(p);
                }
                break;

            case EscapeDirection.Top:
                for (int col = 0; col < W; col++)
                {
                    var p = new Vector2Int(col, H - 1);
                    if (preset.IsValidCell(p)) list.Add(p);
                }
                break;

            case EscapeDirection.Bottom:
                for (int col = 0; col < W; col++)
                {
                    var p = new Vector2Int(col, 0);
                    if (preset.IsValidCell(p)) list.Add(p);
                }
                break;
        }

        cfg.exitCells = list.ToArray();
        preset.playerConfigs[playerIdx] = cfg;
    }

    CellOwner NextOwner(CellOwner cur, int numPlayers)
    {
        int next = (int)cur + 1;
        if (next > Mathf.Min(numPlayers, 4)) next = 0;
        return (CellOwner)next;
    }

    Color GetOwnerColor(CellOwner owner, GamePreset preset)
    {
        int idx = (int)owner - 1;
        if (idx >= 0 && preset.playerConfigs != null && idx < preset.playerConfigs.Length)
        {
            var c = preset.playerConfigs[idx].pieceColor;
            if (c.a > 0.01f) return c;
        }

        switch (owner)
        {
            case CellOwner.P0: return ColorP0;
            case CellOwner.P1: return ColorP1;
            case CellOwner.P2: return ColorP2;
            case CellOwner.P3: return ColorP3;
            default:           return ColorNone;
        }
    }

    Color GetPlayerColor(int playerIdx, GamePreset preset)
    {
        if (preset.playerConfigs != null && playerIdx >= 0 && playerIdx < preset.playerConfigs.Length)
        {
            var c = preset.playerConfigs[playerIdx].pieceColor;
            if (c.a > 0.01f) return c;
        }

        switch (playerIdx)
        {
            case 0: return ColorP0;
            case 1: return ColorP1;
            case 2: return ColorP2;
            case 3: return ColorP3;
            default: return Color.yellow;
        }
    }

    float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

    void DrawBorder(Rect r, Color c)
    {
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y + r.height - 1, r.width, 1), c);
        EditorGUI.DrawRect(new Rect(r.x, r.y, 1, r.height), c);
        EditorGUI.DrawRect(new Rect(r.x + r.width - 1, r.y, 1, r.height), c);
    }

    void ClearMatrix(GamePreset preset)
    {
        if (preset.boardMatrix == null) return;

        foreach (var row in preset.boardMatrix)
        {
            if (row?.cells == null) continue;
            for (int i = 0; i < row.cells.Length; i++)
                row.cells[i] = CellOwner.None;
        }
    }

    void AutoFill2Player(GamePreset preset)
    {
        ClearMatrix(preset);

        int W = preset.boardWidth;
        int H = preset.boardHeight;

        for (int y = 1; y < H; y++)
            if (preset.IsValidCell(new Vector2Int(0, y)))
                preset.boardMatrix[y].cells[0] = CellOwner.P0;

        for (int x = 1; x < W; x++)
            if (preset.IsValidCell(new Vector2Int(x, 0)))
                preset.boardMatrix[0].cells[x] = CellOwner.P1;

        if (preset.playerConfigs == null || preset.playerConfigs.Length < 2)
            ApplyDefaults(preset);
    }

    void AutoFill4Player(GamePreset preset)
    {
        ClearMatrix(preset);

        int W = preset.boardWidth;
        int H = preset.boardHeight;

        for (int y = 1; y < H - 1; y++)
            if (preset.IsValidCell(new Vector2Int(0, y)))
                preset.boardMatrix[y].cells[0] = CellOwner.P0;

        for (int x = 1; x < W - 1; x++)
            if (preset.IsValidCell(new Vector2Int(x, 0)))
                preset.boardMatrix[0].cells[x] = CellOwner.P1;

        for (int y = 1; y < H - 1; y++)
            if (preset.IsValidCell(new Vector2Int(W - 1, y)))
                preset.boardMatrix[y].cells[W - 1] = CellOwner.P2;

        for (int x = 1; x < W - 1; x++)
            if (preset.IsValidCell(new Vector2Int(x, H - 1)))
                preset.boardMatrix[H - 1].cells[x] = CellOwner.P3;

        if (preset.playerConfigs == null || preset.playerConfigs.Length < 4)
            ApplyDefaults(preset);
    }
}
#endif