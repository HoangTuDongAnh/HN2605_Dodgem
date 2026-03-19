#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GamePreset))]
public class GamePresetEditor : Editor
{
    const int CELL_SIZE    = 36;
    const int CELL_PADDING = 3;
    const int LABEL_WIDTH  = 20;

    static readonly Color ColorNone = new Color(0.25f, 0.25f, 0.25f);
    static readonly Color ColorP0   = new Color(0.95f, 0.95f, 0.95f);
    static readonly Color ColorP1   = new Color(0.15f, 0.15f, 0.15f);
    static readonly Color ColorP2   = new Color(0.85f, 0.2f,  0.2f);
    static readonly Color ColorP3   = new Color(0.2f,  0.6f,  0.9f);

    // Default data cho tung phe
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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("boardSize"));
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

        // NUT MOI: Apply Defaults — fix asset thieu data
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Neu playerName trong / pieceColor den (alpha=0) → nhan 'Apply Defaults' de tu dong dien.",
            MessageType.Info);

        if (GUILayout.Button("Apply Defaults cho playerConfigs", GUILayout.Height(28)))
        {
            Undo.RecordObject(preset, "Apply Defaults");
            ApplyDefaults(preset);
            EditorUtility.SetDirty(preset);
            serializedObject.Update();
        }
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("Ma tran xuat phat (click de toggle)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Gray=None | Mau P0/P1/P2/P3 = quan cua phe\nClick: None→P0→P1→P2→P3→None",
            MessageType.Info);
        EditorGUILayout.Space(4);

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

        EditorGUILayout.Space(4);
        string err;
        if (preset.IsValid(out err))
        {
            EditorGUILayout.HelpBox("Preset hop le!", MessageType.None);
            for (int i = 0; i < preset.NumPlayers; i++)
            {
                var cfg = preset.GetConfigSafe(i);
                EditorGUILayout.LabelField($"  {cfg.playerName}: {preset.GetPieceCount(i)} quan | {cfg.escapeDir} | {cfg.type} | color alpha={cfg.pieceColor.a:F2}");
            }
        }
        else
        {
            EditorGUILayout.HelpBox("LOI: " + err, MessageType.Error);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Dien default vao cac field trong/zero
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

            if (string.IsNullOrEmpty(cfg.playerName))  cfg.playerName = def.name;
            if (cfg.escapeDir == 0 && i != 0)          cfg.escapeDir  = def.dir;
            if (cfg.pieceColor.a < 0.01f)              cfg.pieceColor = def.color;
            if (cfg.botDepth == 0)                     cfg.botDepth   = def.depth;

            preset.playerConfigs[i] = cfg;
        }
    }

    void DrawMatrix(GamePreset preset)
    {
        if (preset.boardMatrix == null) return;
        int N = preset.boardSize;
        int totalW = LABEL_WIDTH + N * (CELL_SIZE + CELL_PADDING);
        int totalH = N * (CELL_SIZE + CELL_PADDING) + LABEL_WIDTH;

        Rect layoutRect = GUILayoutUtility.GetRect(totalW, totalH + 8);
        float startX    = layoutRect.x + (layoutRect.width - totalW) / 2f;
        float startY    = layoutRect.y + 4;

        for (int col = 0; col < N; col++)
        {
            float cx = startX + LABEL_WIDTH + col * (CELL_SIZE + CELL_PADDING);
            GUI.Label(new Rect(cx, startY, CELL_SIZE, LABEL_WIDTH - 2), col.ToString(),
                new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter });
        }

        for (int row = 0; row < N; row++)
        {
            int displayRow = N - 1 - row;
            float cy = startY + LABEL_WIDTH + displayRow * (CELL_SIZE + CELL_PADDING);

            GUI.Label(new Rect(startX, cy, LABEL_WIDTH - 2, CELL_SIZE), row.ToString(),
                new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });

            if (preset.boardMatrix[row] == null) continue;

            for (int col = 0; col < N; col++)
            {
                float cx     = startX + LABEL_WIDTH + col * (CELL_SIZE + CELL_PADDING);
                var cellRect = new Rect(cx, cy, CELL_SIZE, CELL_SIZE);
                var owner    = preset.boardMatrix[row].cells[col];
                Color bg     = GetOwnerColor(owner, preset);
                EditorGUI.DrawRect(cellRect, bg);
                DrawBorder(cellRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));

                string lbl = owner == CellOwner.None ? "" : owner.ToString();
                GUI.Label(cellRect, lbl, new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = Luminance(bg) > 0.5f ? Color.black : Color.white }
                });

                if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
                {
                    Undo.RecordObject(preset, "Toggle Cell");
                    preset.boardMatrix[row].cells[col] = NextOwner(owner, preset.NumPlayers);
                    EditorUtility.SetDirty(preset);
                    Event.current.Use();
                    Repaint();
                }
            }
        }
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

    float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

    void DrawBorder(Rect r, Color c)
    {
        EditorGUI.DrawRect(new Rect(r.x,           r.y,            r.width, 1),     c);
        EditorGUI.DrawRect(new Rect(r.x,           r.y+r.height-1, r.width, 1),     c);
        EditorGUI.DrawRect(new Rect(r.x,           r.y,            1,       r.height), c);
        EditorGUI.DrawRect(new Rect(r.x+r.width-1, r.y,            1,       r.height), c);
    }

    void ClearMatrix(GamePreset preset)
    {
        if (preset.boardMatrix == null) return;
        foreach (var row in preset.boardMatrix)
            if (row?.cells != null)
                for (int i = 0; i < row.cells.Length; i++)
                    row.cells[i] = CellOwner.None;
    }

    void AutoFill2Player(GamePreset preset)
    {
        ClearMatrix(preset);
        int N = preset.boardSize;
        for (int i = 1; i < N; i++) preset.boardMatrix[i].cells[0] = CellOwner.P0;
        for (int i = 1; i < N; i++) preset.boardMatrix[0].cells[i] = CellOwner.P1;
        if (preset.playerConfigs == null || preset.playerConfigs.Length < 2)
            ApplyDefaults(preset);
    }

    void AutoFill4Player(GamePreset preset)
    {
        ClearMatrix(preset);
        int N = preset.boardSize;
        for (int i = 1; i < N-1; i++) preset.boardMatrix[i].cells[0]   = CellOwner.P0;
        for (int i = 1; i < N-1; i++) preset.boardMatrix[0].cells[i]   = CellOwner.P1;
        for (int i = 1; i < N-1; i++) preset.boardMatrix[i].cells[N-1] = CellOwner.P2;
        for (int i = 1; i < N-1; i++) preset.boardMatrix[N-1].cells[i] = CellOwner.P3;
        if (preset.playerConfigs == null || preset.playerConfigs.Length < 4)
            ApplyDefaults(preset);
    }
}
#endif