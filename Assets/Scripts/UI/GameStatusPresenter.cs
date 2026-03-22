using UnityEngine;
using TMPro;

/// <summary>
/// Phu trach hien thi status va format thong diep giao dien.
/// </summary>
public class GameStatusPresenter : MonoBehaviour
{
    #region Fields

    [Header("UI")]
    public TextMeshProUGUI statusText;

    [Header("Debug")]
    public bool logToConsole = false;

    #endregion

    #region Public API

    /// <summary>
    /// Hien thi thong diep bat ky len UI va log.
    /// </summary>
    public void ShowMessage(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (logToConsole)
            Debug.Log("[Dodgem] " + message);
    }
    
    public void ShowDraw(int repeatCount)
    {
        // Ví dụ đơn giản — tuỳ chỉnh theo UI của bạn
        statusText.text = $"Hòa! Trạng thái lặp lại {repeatCount} lần.";
    }

    /// <summary>
    /// Hien thi status cho luot hien tai.
    /// </summary>
    public void ShowTurnStatus(PlayerData player, IGameAI ai = null)
    {
        ShowMessage(BuildTurnStatus(player, ai));
    }

    /// <summary>
    /// Hien thi thong bao chien thang.
    /// </summary>
    public void ShowWinner(PlayerData winner)
    {
        if (winner == null) return;
        ShowMessage($"{winner.playerName} WINS!");
    }

    /// <summary>
    /// Hien thi thong bao quan khong di duoc.
    /// </summary>
    public void ShowNoLegalMovesForPiece()
    {
        ShowMessage("This piece has no legal moves.");
    }

    /// <summary>
    /// Hien thi thong bao nuoc di khong hop le.
    /// </summary>
    public void ShowInvalidMove()
    {
        ShowMessage("Invalid move.");
    }

    /// <summary>
    /// Hien thi thong bao khong the thoat.
    /// </summary>
    public void ShowCannotEscape()
    {
        ShowMessage("Cannot escape from this cell.");
    }

    /// <summary>
    /// Hien thi thong bao khi da chon quan.
    /// </summary>
    public void ShowPieceSelected(Vector2Int pos, bool canEscape)
    {
        if (canEscape)
            ShowMessage($"Selected ({pos.x}, {pos.y}) - Click again to escape, or click a green cell to move.");
        else
            ShowMessage($"Selected ({pos.x}, {pos.y}) - Click a green cell to move.");
    }

    /// <summary>
    /// Hien thi thong bao khi doi sang quan khac.
    /// </summary>
    public void ShowPieceReselected(Vector2Int pos, bool canEscape)
    {
        if (canEscape)
            ShowMessage($"Selected ({pos.x}, {pos.y}) - Click again to escape.");
        else
            ShowMessage($"Selected ({pos.x}, {pos.y}).");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Tao chuoi status cho player hien tai.
    /// </summary>
    string BuildTurnStatus(PlayerData player, IGameAI ai)
    {
        if (player == null)
            return "No active player.";

        if (player.type == PlayerType.Bot)
        {
            string difficulty = GameAIFactory.DifficultyLabel(player.botDepth);
            string aiName = ai != null ? ai.DisplayName : "AI";
            return $"Turn: {player.playerName} - Bot ({aiName} / {difficulty})";
        }

        return $"Turn: {player.playerName} - Human";
    }

    #endregion
}