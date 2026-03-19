using UnityEngine;

// ================================================================
// GameConfig — ScriptableObject lưu cấu hình ván chơi
//
// THÊM MỚI (Giai đoạn 2B):
//   - Save() / Load() qua PlayerPrefs để truyền cấu hình giữa Scene
//   - MenuManager gọi Save() → GameManager gọi Load() khi Start
// ================================================================

[CreateAssetMenu(fileName = "GameConfig", menuName = "Dodgem/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Board")]
    [Range(3, 6)]
    public int boardSize = 3;

    [Header("Players")]
    [Range(2, 4)]
    public int numPlayers = 2;

    // Luôn 4 phần tử — chỉ numPlayers đầu tiên được dùng
    public PlayerType[] playerTypes = {
        PlayerType.Bot,
        PlayerType.Human,
        PlayerType.Bot,
        PlayerType.Bot
    };

    // Độ sâu AI: 2=Easy, 4=Medium, 6=Hard; 0 = Human (không dùng)
    public int[] botDepths = { 6, 0, 4, 4 };

    // ── PlayerPrefs keys ──────────────────────────────────────────
    const string KEY_BOARD      = "cfg_boardSize";
    const string KEY_PLAYERS    = "cfg_numPlayers";
    const string KEY_TYPE_PRE   = "cfg_type_";    // + index
    const string KEY_DEPTH_PRE  = "cfg_depth_";   // + index

    // ── Save vào PlayerPrefs (gọi từ MenuManager trước LoadScene) ─
    public void Save()
    {
        PlayerPrefs.SetInt(KEY_BOARD,   boardSize);
        PlayerPrefs.SetInt(KEY_PLAYERS, numPlayers);
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.SetInt(KEY_TYPE_PRE  + i, (int)playerTypes[i]);
            PlayerPrefs.SetInt(KEY_DEPTH_PRE + i, botDepths[i]);
        }
        PlayerPrefs.Save();
    }

    // ── Load từ PlayerPrefs (gọi từ GameManager.Start) ────────────
    // Nếu chưa có dữ liệu thì giữ nguyên giá trị mặc định.
    public void Load()
    {
        if (!PlayerPrefs.HasKey(KEY_BOARD)) return; // chưa save lần nào

        boardSize  = PlayerPrefs.GetInt(KEY_BOARD,   boardSize);
        numPlayers = PlayerPrefs.GetInt(KEY_PLAYERS, numPlayers);
        for (int i = 0; i < 4; i++)
        {
            playerTypes[i] = (PlayerType)PlayerPrefs.GetInt(KEY_TYPE_PRE  + i, (int)playerTypes[i]);
            botDepths[i]   = PlayerPrefs.GetInt(KEY_DEPTH_PRE + i, botDepths[i]);
        }
    }

    // ── Xóa dữ liệu đã lưu ───────────────────────────────────────
    public void Clear()
    {
        PlayerPrefs.DeleteKey(KEY_BOARD);
        PlayerPrefs.DeleteKey(KEY_PLAYERS);
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.DeleteKey(KEY_TYPE_PRE  + i);
            PlayerPrefs.DeleteKey(KEY_DEPTH_PRE + i);
        }
    }

    // ── Validation ────────────────────────────────────────────────
    void OnValidate()
    {
        if (playerTypes == null || playerTypes.Length != 4)
            playerTypes = new PlayerType[] { PlayerType.Bot, PlayerType.Human, PlayerType.Bot, PlayerType.Bot };
        if (botDepths == null || botDepths.Length != 4)
            botDepths = new int[] { 6, 0, 4, 4 };
    }

    // ── Helpers ───────────────────────────────────────────────────
    public bool  IsBot(int i)         => i < playerTypes.Length && playerTypes[i] == PlayerType.Bot;
    public int   GetBotDepth(int i)   => i < botDepths.Length ? botDepths[i] : 4;
    public string DiffLabel(int depth) => depth <= 2 ? "De" : depth <= 4 ? "TB" : "Kho";
}