using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// Xu ly luot di cua bot theo huong think o background, render o main thread.
/// </summary>
public class BotTurnController
{
    #region Fields

    private readonly GameStatusPresenter statusPresenter;
    private readonly BoardRenderer boardRenderer;
    private readonly IGameAI[] bots;

    private readonly float preThinkDelay;
    private readonly float postMoveDelay;
    private readonly bool fastSimulationMode;
    private readonly bool animateBotMoves;

    // Tham chieu den history de truyen vao AI
    private System.Collections.Generic.Dictionary<string, int> repetitionHistory;

    #endregion

    #region Constructor

    /// <summary>
    /// Khoi tao controller bot turn voi cac dependency can thiet.
    /// </summary>
    public BotTurnController(
        GameStatusPresenter statusPresenter,
        BoardRenderer boardRenderer,
        IGameAI[] bots,
        float preThinkDelay,
        float postMoveDelay,
        bool fastSimulationMode,
        bool animateBotMoves)
    {
        this.statusPresenter = statusPresenter;
        this.boardRenderer = boardRenderer;
        this.bots = bots;
        this.preThinkDelay = Mathf.Max(0f, preThinkDelay);
        this.postMoveDelay = Mathf.Max(0f, postMoveDelay);
        this.fastSimulationMode = fastSimulationMode;
        this.animateBotMoves = animateBotMoves;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Cap nhat tham chieu history truoc moi turn de AI co the tranh lap.
    /// </summary>
    public void SetRepetitionHistory(System.Collections.Generic.Dictionary<string, int> history)
    {
        repetitionHistory = history;
    }

    /// <summary>
    /// Xu ly coroutine cho luot di cua bot.
    /// </summary>
    public IEnumerator ExecuteBotTurn(
        GameState currentState,
        Action<GameState> onStateApplied)
    {
        if (currentState == null) yield break;

        var player = currentState.CurrentPlayer;
        if (player == null || player.type != PlayerType.Bot) yield break;

        var ai = (bots != null && player.playerIndex < bots.Length)
            ? bots[player.playerIndex]
            : null;

        if (ai == null)
        {
            onStateApplied?.Invoke(currentState);
            yield break;
        }

        if (!fastSimulationMode && preThinkDelay > 0f)
            yield return new WaitForSeconds(preThinkDelay);

        // Clone snapshot de tranh bi sua state trong luc bot dang tinh
        GameState snapshot = currentState.Clone();

        // FIX: Think o background thread, dung WaitUntil thay vi busy-wait while loop
        var historySnapshot = repetitionHistory;
        Task<GameState> thinkTask = Task.Run(() => ai.BestMove(snapshot, historySnapshot));

        yield return new WaitUntil(() => thinkTask.IsCompleted);

        if (thinkTask.IsFaulted)
        {
            Debug.LogError("[BotTurnController] Bot think task failed: " + thinkTask.Exception);
            onStateApplied?.Invoke(currentState);
            yield break;
        }

        GameState nextState = thinkTask.Result;
        if (nextState == null)
        {
            onStateApplied?.Invoke(currentState);
            yield break;
        }

        // FIX: chi render/anim o day - TurnFlowController khong goi Render() them
        if (fastSimulationMode || !animateBotMoves)
        {
            boardRenderer.Render(nextState);
        }
        else
        {
            yield return boardRenderer.RenderAnimated(currentState, nextState);
        }

        if (!fastSimulationMode && postMoveDelay > 0f)
            yield return new WaitForSeconds(postMoveDelay);

        onStateApplied?.Invoke(nextState);
    }

    #endregion
}
