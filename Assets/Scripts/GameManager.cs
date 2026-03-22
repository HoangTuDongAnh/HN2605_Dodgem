using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Khoi tao van dau, ket noi cac controller va nhan input tu board.
/// </summary>
public class GameManager : MonoBehaviour, ICellClickHandler
{
    #region Fields

    [Header("References")]
    public BoardRenderer boardRenderer;
    public BoardGenerator boardGenerator;
    public Camera mainCamera;
    public GameStatusPresenter statusPresenter;

    [Header("UI - Game Panel")]
    public Button restartButton;

    [Header("Bot Timing")]
    public float botPreThinkDelay = 0.15f;
    public float botPostMoveDelay = 0.1f;

    [Header("Simulation")]
    public bool fastSimulationMode = false;
    public bool animateBotMoves = true;

    private GamePreset lastPreset;
    private PlayerType[] lastOverrideTypes;
    private int[] lastOverrideDepths;
    private IGameAI[] bots;

    private MoveResolver moveResolver;
    private HumanInputController humanInputController;
    private BotTurnController botTurnController;
    private TurnFlowController turnFlowController;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Khoi tao dependency co ban va gan su kien restart.
    /// </summary>
    void Start()
    {
        moveResolver = new MoveResolver();
        humanInputController = new HumanInputController(statusPresenter, moveResolver);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Bat dau van dau tu preset da chon.
    /// </summary>
    public void StartGameFromPreset(GamePreset preset, PlayerType[] overrideTypes, int[] overrideDepths)
    {
        lastPreset = preset;
        lastOverrideTypes = overrideTypes;
        lastOverrideDepths = overrideDepths;

        StopAllCoroutines();
        InitGame();
    }

    /// <summary>
    /// Khoi dong lai van dau hien tai.
    /// </summary>
    public void RestartGame()
    {
        StopAllCoroutines();
        InitGame();
    }

    /// <summary>
    /// Nhan su kien click o ban co va chuyen cho flow controller.
    /// </summary>
    public void OnCellClicked(Vector2Int pos)
    {
        if (turnFlowController == null) return;
        turnFlowController.HandleHumanClick(pos);
    }

    #endregion

    #region Game Setup

    /// <summary>
    /// Khoi tao state, board va cac controller cho van dau moi.
    /// </summary>
    void InitGame()
    {
        if (lastPreset == null)
        {
            Debug.LogError("[GameManager] No preset selected.");
            return;
        }

        var initialState = GameStateFactory.CreateFromPreset(lastPreset, lastOverrideTypes, lastOverrideDepths);
        if (initialState == null) return;

        boardGenerator.Generate(initialState, this);
        boardRenderer.SetupFromGenerator(boardGenerator, initialState);

        bots = new IGameAI[initialState.NumPlayers];
        for (int i = 0; i < initialState.NumPlayers; i++)
            bots[i] = GameAIFactory.Create(initialState.players[i]);

        botTurnController = new BotTurnController(
            statusPresenter,
            boardRenderer,
            bots,
            fastSimulationMode ? 0f : botPreThinkDelay,
            fastSimulationMode ? 0f : botPostMoveDelay,
            fastSimulationMode,
            animateBotMoves
        );

        turnFlowController = new TurnFlowController(
            this,
            boardRenderer,
            statusPresenter,
            humanInputController,
            botTurnController,
            bots
        );

        FitCamera(initialState.boardWidth, initialState.boardHeight);
        turnFlowController.StartGame(initialState);
    }

    /// <summary>
    /// Can camera theo kich thuoc board hien tai.
    /// </summary>
    void FitCamera(int width, int height)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || boardGenerator == null) return;

        float cellSize = boardGenerator.cellSize;
        float margin = cellSize * 1.8f;

        float halfHeight = height * cellSize * 0.5f;
        float orthoByHeight = halfHeight + margin;

        float aspect = mainCamera.aspect > 0.01f ? mainCamera.aspect : 1.7777f;
        float halfWidth = width * cellSize * 0.5f;
        float orthoByWidth = (halfWidth + margin) / aspect;

        mainCamera.orthographicSize = Mathf.Max(orthoByHeight, orthoByWidth);
        mainCamera.transform.position = new Vector3(0, 0, -10);
    }

    #endregion
}