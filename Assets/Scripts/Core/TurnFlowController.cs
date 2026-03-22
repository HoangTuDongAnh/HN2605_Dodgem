using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Dieu phoi flow luot choi, state hien tai va dieu kien ket thuc.
/// </summary>
public class TurnFlowController
{
    #region Fields

    private readonly MonoBehaviour coroutineHost;
    private readonly BoardRenderer boardRenderer;
    private readonly GameStatusPresenter statusPresenter;
    private readonly HumanInputController humanInputController;
    private readonly BotTurnController botTurnController;
    private readonly IGameAI[] bots;

    private GameState currentState;
    private bool isAnimating;
    private int sessionVersion;

    // Coroutine duy nhat dang chay - de co the stop sach
    private Coroutine activeCoroutine;

    // Theo doi so lan lap lai trang thai de phong draw vo tan
    private readonly Dictionary<string, int> stateHistory = new Dictionary<string, int>();
    private const int MaxRepeatCount = 3;

    // Dem so luot pass lien tiep: neu tat ca player deu pass lien tuc = deadlock that
    private int consecutivePassCount;
    private GameState lastStateBeforePass;

    #endregion

    #region Properties

    public GameState CurrentState => currentState;
    public bool IsAnimating => isAnimating;

    #endregion

    #region Constructor

    /// <summary>
    /// Khoi tao flow controller voi cac dependency can thiet.
    /// </summary>
    public TurnFlowController(
        MonoBehaviour coroutineHost,
        BoardRenderer boardRenderer,
        GameStatusPresenter statusPresenter,
        HumanInputController humanInputController,
        BotTurnController botTurnController,
        IGameAI[] bots)
    {
        this.coroutineHost = coroutineHost;
        this.boardRenderer = boardRenderer;
        this.statusPresenter = statusPresenter;
        this.humanInputController = humanInputController;
        this.botTurnController = botTurnController;
        this.bots = bots;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Bat dau van dau moi voi state ban dau.
    /// </summary>
    public void StartGame(GameState initialState)
    {
        sessionVersion++;
        currentState = initialState;
        isAnimating = false;

        humanInputController?.ClearSelection();

        // Reset lich su state khi bat dau van moi
        stateHistory.Clear();
        consecutivePassCount = 0;
        lastStateBeforePass = null;

        // Render lan duy nhat khi bat dau game
        boardRenderer?.Render(currentState);

        StartTurnLoop(sessionVersion);
    }

    /// <summary>
    /// Xu ly click o board cua human.
    /// </summary>
    public void HandleHumanClick(Vector2Int pos)
    {
        if (isAnimating || currentState == null) return;
        if (currentState.CurrentPlayer.type != PlayerType.Human) return;

        var nextState = humanInputController.HandleCellClick(currentState, boardRenderer, pos);
        if (nextState == null) return;

        ApplyHumanMove(nextState, sessionVersion);
    }

    #endregion

    #region Turn Loop

    /// <summary>
    /// Khoi chay turn loop chinh - chi co 1 coroutine duy nhat chay tai 1 thoi diem.
    /// </summary>
    void StartTurnLoop(int version)
    {
        if (activeCoroutine != null)
            coroutineHost.StopCoroutine(activeCoroutine);

        activeCoroutine = coroutineHost.StartCoroutine(TurnLoop(version));
    }

    /// <summary>
    /// Vong lap turn chinh - chay lien tuc cac luot bot, dung lai khi gap human hoac game ket thuc.
    /// </summary>
    IEnumerator TurnLoop(int version)
    {
        while (true)
        {
            if (version != sessionVersion || currentState == null)
                yield break;

            var player = currentState.CurrentPlayer;
            var ai = (bots != null && player.playerIndex < bots.Length) ? bots[player.playerIndex] : null;

            statusPresenter?.ShowTurnStatus(player, ai);

            if (player.type == PlayerType.Bot)
            {
                isAnimating = true;

                // Cap nhat history cho AI truoc moi turn
                botTurnController.SetRepetitionHistory(stateHistory);

                GameState appliedState = currentState;
                bool stateApplied = false;

                yield return coroutineHost.StartCoroutine(
                    botTurnController.ExecuteBotTurn(
                        currentState,
                        nextState =>
                        {
                            appliedState = nextState;
                            stateApplied = true;
                        }
                    )
                );

                if (version != sessionVersion)
                    yield break;

                if (stateApplied)
                    currentState = appliedState;

                isAnimating = false;
                humanInputController?.ClearSelection();

                if (CheckWinCondition())
                    yield break;

                // Phat hien pass: board khong doi (chi currentPlayerIndex thay doi)
                if (IsPassState(appliedState))
                {
                    consecutivePassCount++;
                    // Neu tat ca player deu pass lien tiep = deadlock that -> hoa
                    if (consecutivePassCount >= NumPlayers)
                    {
                        statusPresenter?.ShowDraw(0); // 0 = deadlock, khac voi repetition
                        yield break;
                    }
                }
                else
                {
                    consecutivePassCount = 0;
                }
                lastStateBeforePass = appliedState;

                if (CheckRepetitionDraw())
                    yield break;

                // Tiep tuc while(true) - khong tao coroutine moi
            }
            else
            {
                // Luot human: render board va dung loop, cho input tu HandleHumanClick
                boardRenderer?.Render(currentState);
                yield break;
            }
        }
    }

    #endregion

    #region Human Move

    /// <summary>
    /// Thuc thi nuoc di cua human va chay animation.
    /// </summary>
    void ApplyHumanMove(GameState nextState, int version)
    {
        if (version != sessionVersion) return;

        isAnimating = true;

        GameState oldState = currentState;
        currentState = nextState;

        humanInputController?.ClearSelection();

        if (activeCoroutine != null)
            coroutineHost.StopCoroutine(activeCoroutine);

        activeCoroutine = coroutineHost.StartCoroutine(HumanMoveCoroutine(oldState, nextState, version));
    }

    /// <summary>
    /// Animation sau nuoc di cua human, sau do khoi dong lai turn loop.
    /// </summary>
    IEnumerator HumanMoveCoroutine(GameState oldState, GameState nextState, int version)
    {
        yield return coroutineHost.StartCoroutine(boardRenderer.RenderAnimated(oldState, nextState));

        if (version != sessionVersion)
            yield break;

        isAnimating = false;

        if (CheckWinCondition())
            yield break;

        if (CheckRepetitionDraw())
            yield break;

        StartTurnLoop(version);
    }

    #endregion

    #region Win Check

    int NumPlayers => currentState?.NumPlayers ?? 0;

    /// <summary>
    /// Phat hien pass turn: board y het nhau chi khac currentPlayerIndex.
    /// </summary>
    bool IsPassState(GameState newState)
    {
        if (lastStateBeforePass == null || newState == null) return false;
        if (lastStateBeforePass.NumPlayers != newState.NumPlayers) return false;

        for (int p = 0; p < newState.NumPlayers; p++)
        {
            var oldPlayer = lastStateBeforePass.players[p];
            var newPlayer = newState.players[p];
            if (oldPlayer.escaped != newPlayer.escaped) return false;
            if (oldPlayer.pieces.Length != newPlayer.pieces.Length) return false;
            for (int i = 0; i < oldPlayer.pieces.Length; i++)
                if (oldPlayer.pieces[i] != newPlayer.pieces[i]) return false;
        }

        return true; // board giong het, chi doi luot = pass
    }

    /// <summary>
    /// Kiem tra dieu kien ket thuc van dau.
    /// </summary>
    bool CheckWinCondition()
    {
        if (currentState == null) return false;

        var winner = currentState.Winner();
        if (winner != null)
        {
            isAnimating = false;
            statusPresenter?.ShowWinner(winner);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kiem tra trang thai hien tai co bi lap lai qua nhieu lan khong (hoa).
    /// </summary>
    bool CheckRepetitionDraw()
    {
        if (currentState == null) return false;

        string key = currentState.StateKey();

        if (!stateHistory.TryGetValue(key, out int count))
            count = 0;

        count++;
        stateHistory[key] = count;

        if (count >= MaxRepeatCount)
        {
            isAnimating = false;
            statusPresenter?.ShowDraw(MaxRepeatCount);
            return true;
        }

        return false;
    }

    #endregion
}
