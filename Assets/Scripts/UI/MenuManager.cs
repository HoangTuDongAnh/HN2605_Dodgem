using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// ================================================================
// MenuManager — final fix
//
// Fix 1: Dung PresetCardUI.SetData() thay GetComponentsInChildren
//         → khong lẫn sang TMP cua PlayerSlot, Toggle, Dropdown
//
// Fix 2: Can giua card bang cach set Content width = ScrollView width
//         khi so card it (khong tran ra ngoai viewport)
//         → them CanvasGroup padding hoac dung script can chinh
// ================================================================

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject gamePanel;

    [Header("Danh sach preset")]
    public GamePreset[] presets;

    [Header("Preset card container")]
    public Transform    presetContainer;   // ScrollView > Viewport > Content
    public GameObject   presetCardPrefab;  // can co PresetCardUI tren root

    [Header("ScrollView (de can giua card)")]
    public RectTransform scrollViewRect;   // keo ScrollView vao day

    [Header("Player Slots (keo 4 slot theo thu tu P0..P3)")]
    public PlayerSlotUI[] playerSlots = new PlayerSlotUI[4];
    public GameObject     slotsPanel;

    [Header("Buttons")]
    public Button btnStart;
    public Button btnBackToMenu;

    // ── Private ──────────────────────────────────────────────────
    private GamePreset    selectedPreset = null;
    private int           selectedIndex  = -1;
    private PresetCardUI[] cardUIs       = new PresetCardUI[0];

    // Singleton guard
    static MenuManager instance;
    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }
    void OnDestroy()
    {
        if (instance == this) instance = null;
        cardUIs = new PresetCardUI[0];
    }

    // ── Init ─────────────────────────────────────────────────────
    void Start()
    {
        if (btnStart      != null) { btnStart.onClick.AddListener(OnStartClicked); btnStart.interactable = false; }
        if (btnBackToMenu != null) btnBackToMenu.onClick.AddListener(ShowMenu);

        BuildPresetCards();
        HideAllSlots();

        if (slotsPanel != null) slotsPanel.SetActive(false);
        ShowMenu();
    }

    // ── Build card ────────────────────────────────────────────────
    void BuildPresetCards()
    {
        if (presets == null || presetContainer == null || presetCardPrefab == null)
        {
            Debug.LogWarning("[MenuManager] Thieu presets / presetContainer / presetCardPrefab!");
            return;
        }

        // Xoa card cu
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
            if (preset == null) { Debug.LogWarning($"[MenuManager] presets[{i}] null!"); continue; }

            var cardGO  = Instantiate(presetCardPrefab, presetContainer);
            cardGO.name = "Card_" + i;

            // Lay PresetCardUI tren root — KHONG dung GetComponentsInChildren
            var cardUI = cardGO.GetComponent<PresetCardUI>();
            if (cardUI == null)
            {
                Debug.LogWarning(
                    $"[MenuManager] Card_{i}: PresetCard prefab thieu PresetCardUI script! " +
                    "Gang PresetCardUI.cs vao root cua PresetCard prefab.");
            }
            else
            {
                cardUI.SetData(preset.presetName, preset.description, BuildPieceInfo(preset));
            }
            cardUIs[i] = cardUI;

            int capturedIdx = i;
            var btn = cardGO.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => OnPresetSelected(capturedIdx));
        }

        // Can giua: neu tong chieu rong card < scrollview thi dat Content nho lai
        // va de HorizontalLayoutGroup tu can giua
        AlignCardsCenter();
    }

    // FIX 2: Can chinh Content de card luon nam giua ScrollView
    void AlignCardsCenter()
    {
        if (presetContainer == null) return;

        var contentRT = presetContainer as RectTransform;
        if (contentRT == null) return;

        var hlg = presetContainer.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            // Buoc 1: Tat Control Child Size de card giu size rieng
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            // Buoc 2: Can giua
            hlg.childAlignment = TextAnchor.MiddleCenter;
        }

        // Buoc 3: Dat Content pivot = (0.5, 0.5) de can giua
        contentRT.pivot = new Vector2(0.5f, 0.5f);

        // Buoc 4: Neu co ScrollView ref thi anchor Content vao giua ScrollView
        if (scrollViewRect != null)
        {
            contentRT.anchorMin = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax = new Vector2(0.5f, 0.5f);
        }
    }

    // Tao chuoi so quan tu playerConfigs — KHONG hardcode ten
    string BuildPieceInfo(GamePreset preset)
    {
        if (preset.playerConfigs == null || preset.playerConfigs.Length == 0) return "";
        var parts = new List<string>();
        for (int p = 0; p < preset.NumPlayers; p++)
        {
            string pName = (p < preset.playerConfigs.Length && preset.playerConfigs[p] != null)
                           ? preset.playerConfigs[p].playerName : $"P{p}";
            parts.Add($"{pName}:{preset.GetPieceCount(p)}q");
        }
        return string.Join("  ", parts);
    }

    // ── Chon preset ───────────────────────────────────────────────
    void OnPresetSelected(int idx)
    {
        if (presets == null || idx < 0 || idx >= presets.Length) return;

        selectedPreset = presets[idx];
        selectedIndex  = idx;

        for (int i = 0; i < cardUIs.Length; i++)
            if (cardUIs[i] != null) cardUIs[i].SetSelected(i == idx);

        RefreshSlots(selectedPreset);

        if (slotsPanel != null) slotsPanel.SetActive(true);
        if (btnStart   != null) btnStart.interactable = true;
    }

    void RefreshSlots(GamePreset preset)
    {
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] == null) continue;
            bool active = preset.playerConfigs != null && i < preset.playerConfigs.Length;
            playerSlots[i].gameObject.SetActive(active);
            if (active) playerSlots[i].SetupFromPreset(i, preset.playerConfigs[i]);
        }
    }

    void HideAllSlots()
    {
        foreach (var slot in playerSlots)
            if (slot != null) slot.gameObject.SetActive(false);
    }

    // ── Start game ────────────────────────────────────────────────
    void OnStartClicked()
    {
        if (selectedPreset == null) { Debug.LogWarning("[MenuManager] Chua chon preset!"); return; }

        int N      = selectedPreset.NumPlayers;
        var types  = new PlayerType[N];
        var depths = new int[N];

        for (int i = 0; i < N; i++)
        {
            bool hasSlot = i < playerSlots.Length
                           && playerSlots[i] != null
                           && playerSlots[i].gameObject.activeSelf;
            types[i]  = hasSlot ? playerSlots[i].GetPlayerType()  : selectedPreset.playerConfigs[i].type;
            depths[i] = hasSlot ? playerSlots[i].GetBotDepth()    : selectedPreset.playerConfigs[i].botDepth;
        }

        ShowGame();
        var gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.StartGameFromPreset(selectedPreset, types, depths);
        else Debug.LogError("[MenuManager] Khong tim thay GameManager!");
    }

    // ── Panel toggle ──────────────────────────────────────────────
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