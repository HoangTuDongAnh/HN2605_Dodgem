using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject gamePanel;

    [Header("Danh sach preset")]
    public GamePreset[] presets;

    [Header("Preset card container")]
    public Transform  presetContainer;
    public GameObject presetCardPrefab;

    [Header("ScrollView")]
    public RectTransform scrollViewRect;

    [Header("Player Slots")]
    public PlayerSlotUI[] playerSlots = new PlayerSlotUI[4];
    public GameObject     slotsPanel;

    [Header("Buttons")]
    public Button btnStart;
    public Button btnBackToMenu;

    private GamePreset     selectedPreset = null;
    private int            selectedIndex  = -1;
    private PresetCardUI[] cardUIs        = new PresetCardUI[0];

    static MenuManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        cardUIs = new PresetCardUI[0];
    }

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

        if (slotsPanel != null) slotsPanel.SetActive(false);
        ShowMenu();
    }

    void BuildPresetCards()
    {
        if (presets == null || presetContainer == null || presetCardPrefab == null)
        {
            Debug.LogWarning("[MenuManager] Thieu presets / presetContainer / presetCardPrefab!");
            return;
        }

        for (int i = presetContainer.childCount - 1; i >= 0; i--)
        {
            var child = presetContainer.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        selectedIndex  = -1;
        selectedPreset = null;
        cardUIs        = new PresetCardUI[presets.Length];

        for (int i = 0; i < presets.Length; i++)
        {
            var preset = presets[i];
            if (preset == null)
            {
                Debug.LogWarning($"[MenuManager] presets[{i}] null!");
                continue;
            }

            var cardGO = Instantiate(presetCardPrefab, presetContainer);
            cardGO.name = "Card_" + i;

            var cardUI = cardGO.GetComponent<PresetCardUI>();
            if (cardUI == null)
            {
                Debug.LogWarning($"[MenuManager] Card_{i}: thieu PresetCardUI!");
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

            int capturedIdx = i;
            var btn = cardGO.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnPresetSelected(capturedIdx));
        }

        AlignCardsCenter();
    }

    void AlignCardsCenter()
    {
        if (presetContainer == null) return;

        var contentRT = presetContainer as RectTransform;
        if (contentRT == null) return;

        var hlg = presetContainer.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childForceExpandWidth  = false;
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

    string BuildDescription(GamePreset preset)
    {
        string desc = string.IsNullOrWhiteSpace(preset.description) ? "Preset" : preset.description;
        return $"{desc}\nBoard: {preset.boardWidth}x{preset.boardHeight} | Players: {preset.NumPlayers}";
    }

    string BuildInfoLine(GamePreset preset)
    {
        if (preset.playerConfigs == null || preset.playerConfigs.Length == 0)
            return "";

        var parts = new List<string>();
        for (int p = 0; p < preset.NumPlayers; p++)
        {
            string pName = (p < preset.playerConfigs.Length && preset.playerConfigs[p] != null)
                ? preset.playerConfigs[p].playerName
                : $"P{p}";

            parts.Add($"{pName}:{preset.GetPieceCount(p)}q");
        }

        return string.Join("  |  ", parts);
    }

    void OnPresetSelected(int idx)
    {
        if (presets == null || idx < 0 || idx >= presets.Length) return;

        selectedPreset = presets[idx];
        selectedIndex  = idx;

        for (int i = 0; i < cardUIs.Length; i++)
            if (cardUIs[i] != null)
                cardUIs[i].SetSelected(i == idx);

        RefreshSlots(selectedPreset);

        if (slotsPanel != null) slotsPanel.SetActive(true);
        if (btnStart != null) btnStart.interactable = true;
    }

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

    void HideAllSlots()
    {
        foreach (var slot in playerSlots)
            if (slot != null) slot.gameObject.SetActive(false);
    }

    void OnStartClicked()
    {
        if (selectedPreset == null)
        {
            Debug.LogWarning("[MenuManager] Chua chon preset!");
            return;
        }

        int N = selectedPreset.NumPlayers;
        var types  = new PlayerType[N];
        var depths = new int[N];

        for (int i = 0; i < N; i++)
        {
            bool hasSlot = i < playerSlots.Length
                        && playerSlots[i] != null
                        && playerSlots[i].gameObject.activeSelf;

            types[i]  = hasSlot ? playerSlots[i].GetPlayerType() : selectedPreset.playerConfigs[i].type;
            depths[i] = hasSlot ? playerSlots[i].GetBotDepth()   : selectedPreset.playerConfigs[i].botDepth;
        }

        ShowGame();
        var gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.StartGameFromPreset(selectedPreset, types, depths);
        else Debug.LogError("[MenuManager] Khong tim thay GameManager!");
    }

    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(false);
    }

    public void ShowGame()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);
    }
}