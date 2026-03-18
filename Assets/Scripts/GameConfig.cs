using UnityEngine;

// ================================================================
// GameConfig — ScriptableObject lưu cấu hình ván chơi
//
// MenuManager ghi vào đây, GameManager đọc từ đây.
// Dùng ScriptableObject để dữ liệu tồn tại giữa các Scene.
//
// Tạo asset: chuột phải trong Project → Create → Dodgem → GameConfig
// ================================================================

[CreateAssetMenu(fileName = "GameConfig", menuName = "Dodgem/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Board")]
    [Range(3, 6)]
    public int boardSize = 3;          // kích thước bàn cờ NxN

    [Header("Players")]
    [Range(2, 4)]
    public int numPlayers = 2;

    // Loại mỗi người chơi (index 0..3)
    public PlayerType[] playerTypes = {
        PlayerType.Bot,
        PlayerType.Human,
        PlayerType.Bot,
        PlayerType.Bot
    };

    // Độ sâu AI cho từng Bot (0 = không dùng nếu là Human)
    // 2 = Easy, 4 = Medium, 6 = Hard
    public int[] botDepths = { 6, 0, 4, 4 };

    // ── Validation ────────────────────────────────────────────────
    // Gọi khi save trong Inspector để đảm bảo mảng đúng kích thước
    void OnValidate()
    {
        // Đảm bảo mảng luôn có đúng 4 phần tử
        if (playerTypes == null || playerTypes.Length != 4)
        {
            playerTypes = new PlayerType[] {
                PlayerType.Bot, PlayerType.Human, PlayerType.Bot, PlayerType.Bot
            };
        }
        if (botDepths == null || botDepths.Length != 4)
        {
            botDepths = new int[] { 6, 0, 4, 4 };
        }
    }

    // ── Helpers ───────────────────────────────────────────────────
    public bool IsBot(int playerIdx)
    {
        if (playerIdx < 0 || playerIdx >= playerTypes.Length) return false;
        return playerTypes[playerIdx] == PlayerType.Bot;
    }

    public int GetBotDepth(int playerIdx)
    {
        if (playerIdx < 0 || playerIdx >= botDepths.Length) return 4;
        return botDepths[playerIdx];
    }

    public string GetDifficultyLabel(int depth)
    {
        if (depth <= 2) return "Easy";
        if (depth <= 4) return "Medium";
        return "Hard";
    }
}