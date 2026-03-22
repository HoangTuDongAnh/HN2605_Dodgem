using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Quan ly menu chon preset, player setup va chuyen panel.
/// </summary>
public class MenuManager : MonoBehaviour
{
    #region Fields

    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject gamePanel;

    [Header("References")]
    public GameManager gameManager;

    [Header("Preset List")]
    public GamePreset[] presets;

    [Header("Preset Card Container")]
    public Transform presetContainer;
    public GameObject presetCardPrefab;

    [Header("Scroll View")]
    public RectTransform scrollViewRect;

    [Header("Player Slots")]
    public PlayerSlotUI[] playerSlots = new PlayerSlotUI[4];
    public GameObject slotsPanel;

    [Header("Buttons")]
    public Button btnStart;
    public Button btnBackToMenu;

    private GamePreset selectedPreset;
    private PresetCardUI[] cardUIs = new PresetCardUI[0];

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Gan su kien nut va khoi tao giao dien menu.
    /// </summary>
    void Start()
    {
        if (btnStart != null)
        {
            btnStart.onClick.AddListener(OnStartClicked);
            btnStart.interactable = false;
        }

        if (btnBackToMenu != null)
            btnBackToMenu.onClick.AddListener(ShowMenu);

        BuildPresetCards();
        HideAllSlots();

        if (slotsPanel != null)
            slotsPanel.SetActive(false);

        ShowMenu();
    }

    /// <summary>
    /// Giai phong mang card UI khi object bi huy.
    /// </summary>
    void OnDestroy()
    {
        cardUIs = new PresetCardUI[0];
    }

    #endregion

    #region Menu Flow

    /// <summary>
    /// Hien panel menu va an panel game.
    /// </summary>
    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
    }

    /// <summary>
    /// Hien panel game va an panel menu.
    /// </summary>
    public void ShowGame()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
    }

    #endregion

    #region Preset Cards

    /// <summary>
    /// Tao danh sach card preset tren menu.
    /// </summary>
    void BuildPresetCards()
    {
        if (presets == null || presetContainer == null || presetCardPrefab == null)
        {
            Debug.LogWarning("[MenuManager] Missing presets, container, or card prefab.");
            return;
        }

        ClearPresetCards();

        selectedPreset = null;
        cardUIs = new PresetCardUI[presets.Length];

        for (int i = 0; i < presets.Length; i++)
        {
            var preset = presets[i];
            if (preset == null)
            {
                Debug.LogWarning($"[MenuManager] Preset at index {i} is null.");
                continue;
            }

            var cardGO = Instantiate(presetCardPrefab, presetContainer);
            cardGO.name = $"Card_{i}";

            var cardUI = cardGO.GetComponent<PresetCardUI>();
            if (cardUI == null)
            {
                Debug.LogWarning($"[MenuManager] Preset card {i} is missing PresetCardUI.");
            }
            else
            {
                cardUI.SetData(
                    preset.presetName,
                    BuildDescription(preset),
                    BuildInfoLine(preset)
                );
            }

            cardUIs[i] = cardUI;

            int capturedIndex = i;
            var button = cardGO.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnPresetSelected(capturedIndex));
            }
        }

        AlignCardsCenter();
    }

    /// <summary>
    /// Can giua danh sach card preset trong scroll view.
    /// </summary>
    void AlignCardsCenter()
    {
        if (presetContainer == null) return;

        var contentRT = presetContainer as RectTransform;
        if (contentRT == null) return;

        var hlg = presetContainer.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;
        }

        contentRT.pivot = new Vector2(0.5f, 0.5f);

        if (scrollViewRect != null)
        {
            contentRT.anchorMin = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    /// <summary>
    /// Xu ly khi nguoi dung chon mot preset.
    /// </summary>
    void OnPresetSelected(int index)
    {
        if (presets == null || index < 0 || index >= presets.Length) return;

        selectedPreset = presets[index];

        for (int i = 0; i < cardUIs.Length; i++)
        {
            if (cardUIs[i] != null)
                cardUIs[i].SetSelected(i == index);
        }

        RefreshSlots(selectedPreset);

        if (slotsPanel != null) slotsPanel.SetActive(true);
        if (btnStart != null) btnStart.interactable = true;
    }

    /// <summary>
    /// Xoa toan bo card preset hien tai.
    /// </summary>
    void ClearPresetCards()
    {
#if UNITY_EDITOR
        ClearEditorSelectionIfInsideContainer();
#endif

        for (int i = presetContainer.childCount - 1; i >= 0; i--)
        {
            var child = presetContainer.GetChild(i).gameObject;

            var button = child.GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveAllListeners();

            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Bo chon object trong Inspector neu no nam trong preset container sap bi xoa.
    /// </summary>
    void ClearEditorSelectionIfInsideContainer()
    {
        if (Selection.activeGameObject == null || presetContainer == null) return;

        Transform t = Selection.activeGameObject.transform;
        while (t != null)
        {
            if (t == presetContainer)
            {
                Selection.activeObject = null;
                break;
            }
            t = t.parent;
        }
    }
#endif

    #endregion

    #region Player Slots

    /// <summary>
    /// Cap nhat cac slot nguoi choi theo preset dang chon.
    /// </summary>
    void RefreshSlots(GamePreset preset)
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] == null) continue;

            bool active = preset.playerConfigs != null && i < preset.playerConfigs.Length;
            playerSlots[i].gameObject.SetActive(active);

            if (active)
                playerSlots[i].SetupFromPreset(i, preset.playerConfigs[i]);
        }
    }

    /// <summary>
    /// An toan bo player slot.
    /// </summary>
    void HideAllSlots()
    {
        foreach (var slot in playerSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Start Game

    /// <summary>
    /// Bat dau van dau voi preset va player setup hien tai.
    /// </summary>
    void OnStartClicked()
    {
        if (selectedPreset == null)
        {
            Debug.LogWarning("[MenuManager] No preset selected.");
            return;
        }

        if (gameManager == null)
        {
            Debug.LogError("[MenuManager] GameManager reference is missing.");
            return;
        }

        int count = selectedPreset.NumPlayers;
        var types = new PlayerType[count];
        var depths = new int[count];

        for (int i = 0; i < count; i++)
        {
            bool hasSlot =
                i < playerSlots.Length &&
                playerSlots[i] != null &&
                playerSlots[i].gameObject.activeSelf;

            types[i] = hasSlot
                ? playerSlots[i].GetPlayerType()
                : selectedPreset.playerConfigs[i].type;

            depths[i] = hasSlot
                ? playerSlots[i].GetBotDepth()
                : selectedPreset.playerConfigs[i].botDepth;
        }

        ShowGame();
        gameManager.StartGameFromPreset(selectedPreset, types, depths);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Tao mo ta ngan cho card preset.
    /// </summary>
    string BuildDescription(GamePreset preset)
    {
        string desc = string.IsNullOrWhiteSpace(preset.description) ? "Preset" : preset.description;
        return $"{desc}\nBoard: {preset.boardWidth}x{preset.boardHeight} | Players: {preset.NumPlayers}";
    }

    /// <summary>
    /// Tao dong thong tin so quan cua tung phe.
    /// </summary>
    string BuildInfoLine(GamePreset preset)
    {
        if (preset.playerConfigs == null || preset.playerConfigs.Length == 0)
            return string.Empty;

        var parts = new List<string>();
        for (int p = 0; p < preset.NumPlayers; p++)
        {
            string playerName = (p < preset.playerConfigs.Length && preset.playerConfigs[p] != null)
                ? preset.playerConfigs[p].playerName
                : $"P{p}";

            parts.Add($"{playerName}:{preset.GetPieceCount(p)}q");
        }

        return string.Join("  |  ", parts);
    }

    #endregion
}