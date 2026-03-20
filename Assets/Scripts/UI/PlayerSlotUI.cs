using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlotUI : MonoBehaviour
{
    [Header("UI elements — keo vao Inspector")]
    public TextMeshProUGUI labelName;
    public Toggle          toggleHuman;
    public TMP_Dropdown    dropdownDiff;
    public TextMeshProUGUI labelType;

    static readonly int[]    DepthMap = { 2, 4, 6 };
    static readonly string[] Labels   = { "Easy", "Medium", "Hard" };

    private int  slotIndex;
    private bool isSetup = false;

    // ── Setup tu PlayerPresetConfig ───────────────────────────────
    public void SetupFromPreset(int idx, PlayerPresetConfig cfg)
    {
        slotIndex = idx;
        isSetup   = true;

        if (labelName != null)
        {
            labelName.text  = cfg.playerName;
            labelName.color = cfg.pieceColor;
        }

        InitDropdown();

        bool isHuman = (cfg.type == PlayerType.Human);
        if (toggleHuman  != null) toggleHuman.isOn  = isHuman;
        if (dropdownDiff != null) dropdownDiff.value = DepthToIdx(cfg.botDepth);

        RefreshUI(isHuman);
        AddListeners();
    }

    void InitDropdown()
    {
        if (dropdownDiff == null) return;
        dropdownDiff.ClearOptions();
        dropdownDiff.AddOptions(new System.Collections.Generic.List<string>(Labels));
    }

    void AddListeners()
    {
        if (toggleHuman  != null) toggleHuman.onValueChanged.RemoveAllListeners();
        if (dropdownDiff != null) dropdownDiff.onValueChanged.RemoveAllListeners();

        if (toggleHuman  != null) toggleHuman.onValueChanged.AddListener(OnToggleChanged);
        if (dropdownDiff != null) dropdownDiff.onValueChanged.AddListener(v => { });
    }

    void OnToggleChanged(bool isHuman) => RefreshUI(isHuman);

    void RefreshUI(bool isHuman)
    {
        if (dropdownDiff != null) dropdownDiff.gameObject.SetActive(!isHuman);
        if (labelType    != null) labelType.text = isHuman ? "Human" : "Bot";
    }

    public PlayerType GetPlayerType()
        => (toggleHuman != null && toggleHuman.isOn) ? PlayerType.Human : PlayerType.Bot;

    public int GetBotDepth()
    {
        if (GetPlayerType() == PlayerType.Human) return 0;
        if (dropdownDiff == null) return 4;
        return DepthMap[Mathf.Clamp(dropdownDiff.value, 0, DepthMap.Length - 1)];
    }

    static int DepthToIdx(int depth)
    {
        if (depth <= 2) return 0;
        if (depth <= 4) return 1;
        return 2;
    }
}