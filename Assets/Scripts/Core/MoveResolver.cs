using UnityEngine;

/// <summary>
/// Tim state con tu mot hanh dong di chuyen hoac thoat.
/// </summary>
public class MoveResolver
{
    #region Public API

    /// <summary>
    /// Tim state con sau nuoc di thuong.
    /// </summary>
    public GameState ResolveMove(GameState currentState, int playerIndex, Vector2Int from, Vector2Int to)
    {
        if (currentState == null) return null;

        foreach (var child in DodgemRules.GetChildren(currentState))
        {
            for (int i = 0; i < currentState.players[playerIndex].pieces.Length; i++)
            {
                if (currentState.players[playerIndex].pieces[i] == from &&
                    child.players[playerIndex].pieces[i] == to)
                {
                    return child;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Tim state con sau nuoc thoat.
    /// </summary>
    public GameState ResolveEscape(GameState currentState, int playerIndex, Vector2Int from)
    {
        if (currentState == null) return null;

        foreach (var child in DodgemRules.GetChildren(currentState))
        {
            for (int i = 0; i < currentState.players[playerIndex].pieces.Length; i++)
            {
                if (currentState.players[playerIndex].pieces[i] == from &&
                    child.players[playerIndex].pieces[i].x == -1 &&
                    child.players[playerIndex].escaped > currentState.players[playerIndex].escaped)
                {
                    return child;
                }
            }
        }

        return null;
    }

    #endregion
}