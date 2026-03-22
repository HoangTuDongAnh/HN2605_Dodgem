using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Xu ly input cua nguoi choi human tren ban co.
/// </summary>
public class HumanInputController
{
    #region Fields

    private readonly GameStatusPresenter statusPresenter;
    private readonly MoveResolver moveResolver;

    private Vector2Int selectedPiece = new Vector2Int(-1, -1);
    private readonly List<Vector2Int> validMoves = new List<Vector2Int>();

    #endregion

    #region Constructor

    /// <summary>
    /// Khoi tao controller input voi presenter va move resolver.
    /// </summary>
    public HumanInputController(GameStatusPresenter statusPresenter, MoveResolver moveResolver)
    {
        this.statusPresenter = statusPresenter;
        this.moveResolver = moveResolver;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Xu ly click cua human va tra ve state tiep theo neu co nuoc di hop le.
    /// </summary>
    public GameState HandleCellClick(GameState currentState, BoardRenderer boardRenderer, Vector2Int pos)
    {
        if (currentState == null) return null;

        var player = currentState.CurrentPlayer;
        if (player.type != PlayerType.Human) return null;

        if (!DodgemRules.InBounds(pos, currentState)) return null;
        if (!currentState.IsCellPlayable(pos)) return null;

        // Click lai o dang chon
        if (HasSelection() && pos == selectedPiece)
        {
            if (CanEscape(player))
            {
                var escapeState = moveResolver.ResolveEscape(currentState, player.playerIndex, selectedPiece);
                if (escapeState != null)
                {
                    ClearSelection();
                    return escapeState;
                }

                statusPresenter?.ShowCannotEscape();
                return null;
            }

            ClearSelection();
            boardRenderer?.Render(currentState);
            statusPresenter?.ShowTurnStatus(player);
            return null;
        }

        // Chua chon quan
        if (!HasSelection())
        {
            if (!player.HasPieceAt(pos))
                return null;

            var moves = DodgemRules.GetValidMovesForPiece(currentState, pos, player.playerIndex);
            if (moves.Count == 0)
            {
                statusPresenter?.ShowNoLegalMovesForPiece();
                return null;
            }

            selectedPiece = pos;
            validMoves.Clear();
            validMoves.AddRange(moves);

            boardRenderer?.HighlightSelected(selectedPiece, validMoves, player.playerIndex, currentState);
            statusPresenter?.ShowPieceSelected(pos, CanEscape(player));
            return null;
        }

        // Dang chon mot quan, click vao quan khac cua minh
        if (player.HasPieceAt(pos))
        {
            var moves = DodgemRules.GetValidMovesForPiece(currentState, pos, player.playerIndex);
            if (moves.Count > 0)
            {
                selectedPiece = pos;
                validMoves.Clear();
                validMoves.AddRange(moves);

                boardRenderer?.HighlightSelected(selectedPiece, validMoves, player.playerIndex, currentState);
                statusPresenter?.ShowPieceReselected(pos, CanEscape(player));
            }
            else
            {
                statusPresenter?.ShowNoLegalMovesForPiece();
            }

            return null;
        }

        // Thu di chuyen thuong
        var chosenState = moveResolver.ResolveMove(currentState, player.playerIndex, selectedPiece, pos);
        if (chosenState == null)
        {
            statusPresenter?.ShowInvalidMove();
            return null;
        }

        ClearSelection();
        return chosenState;
    }

    /// <summary>
    /// Bo chon quan hien tai.
    /// </summary>
    public void ClearSelection()
    {
        selectedPiece = new Vector2Int(-1, -1);
        validMoves.Clear();
    }

    /// <summary>
    /// Kiem tra hien tai co dang chon quan hay khong.
    /// </summary>
    public bool HasSelection()
    {
        return selectedPiece.x != -1;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Kiem tra quan dang chon co the thoat hay khong.
    /// </summary>
    bool CanEscape(PlayerData player)
    {
        return HasSelection() && player != null && player.CanEscapeFrom(selectedPiece);
    }

    #endregion
}