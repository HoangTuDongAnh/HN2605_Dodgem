using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quan ly mot dong setup Human/Bot trong menu.
/// </summary>
public class PlayerSlotUI : MonoBehaviour
{
    #region Fields

    [Header("UI Elements")]
    public TextMeshProUGUI labelName;
    public Toggle toggleHuman;
    public TMP_Dropdown dropdownDiff;
    public TextMeshProUGUI labelType;

    private static readonly int[] DepthMap = { 2, 4, 6 };
    private static readonly string[] DifficultyLabels = { "Easy", "Medium", "Hard" };

    private int slotIndex;
    private bool isSetup;

    #endregion

    #region Public API

    /// <summary>
    /// Khoi tao slot tu du lieu preset.
    /// </summary>
    public void SetupFromPreset(int idx, PlayerPresetConfig cfg)
    {
        slotIndex = idx;
        isSetup = true;

        if (labelName != null)
        {
            labelName.text = cfg.playerName;
            labelName.color = cfg.pieceColor;
        }

        InitDropdown();

        bool isHuman = cfg.type == PlayerType.Human;

        if (toggleHuman != null)
            toggleHuman.isOn = isHuman;

        if (dropdownDiff != null)
            dropdownDiff.value = DepthToIndex(cfg.botDepth);

        RefreshUI(isHuman);
        AddListeners();
    }

    /// <summary>
    /// Lay loai nguoi choi hien tai.
    /// </summary>
    public PlayerType GetPlayerType()
    {
        return (toggleHuman != null && toggleHuman.isOn) ? PlayerType.Human : PlayerType.Bot;
    }

    /// <summary>
    /// Lay depth bot tu dropdown hien tai.
    /// </summary>
    public int GetBotDepth()
    {
        if (GetPlayerType() == PlayerType.Human)
            return 0;

        if (dropdownDiff == null)
            return 4;

        return DepthMap[Mathf.Clamp(dropdownDiff.value, 0, DepthMap.Length - 1)];
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Khoi tao dropdown do kho.
    /// </summary>
    void InitDropdown()
    {
        if (dropdownDiff == null) return;

        dropdownDiff.ClearOptions();
        dropdownDiff.AddOptions(new System.Collections.Generic.List<string>(DifficultyLabels));
    }

    /// <summary>
    /// Gan lai listener cho UI.
    /// </summary>
    void AddListeners()
    {
        if (toggleHuman != null)
            toggleHuman.onValueChanged.RemoveAllListeners();

        if (dropdownDiff != null)
            dropdownDiff.onValueChanged.RemoveAllListeners();

        if (toggleHuman != null)
            toggleHuman.onValueChanged.AddListener(OnToggleChanged);

        if (dropdownDiff != null)
            dropdownDiff.onValueChanged.AddListener(_ => { });
    }

    /// <summary>
    /// Xu ly khi doi qua Human/Bot.
    /// </summary>
    void OnToggleChanged(bool isHuman)
    {
        RefreshUI(isHuman);
    }

    /// <summary>
    /// Cap nhat giao dien theo loai nguoi choi.
    /// </summary>
    void RefreshUI(bool isHuman)
    {
        if (dropdownDiff != null)
            dropdownDiff.gameObject.SetActive(!isHuman);

        if (labelType != null)
            labelType.text = isHuman ? "Human" : "Bot";
    }

    /// <summary>
    /// Chuyen bot depth sang index dropdown.
    /// </summary>
    static int DepthToIndex(int depth)
    {
        if (depth <= 2) return 0;
        if (depth <= 4) return 1;
        return 2;
    }

    #endregion
}