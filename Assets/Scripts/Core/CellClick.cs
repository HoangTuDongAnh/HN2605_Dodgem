using UnityEngine;

/// <summary>
/// Gan cho moi o ban co de gui su kien click ve handler.
/// </summary>
public class CellClick : MonoBehaviour
{
    #region Fields

    public Vector2Int gridPos;

    private ICellClickHandler clickHandler;

    #endregion

    #region Public API

    /// <summary>
    /// Khoi tao handler click cho o nay.
    /// </summary>
    public void Initialize(ICellClickHandler handler, Vector2Int position)
    {
        clickHandler = handler;
        gridPos = position;
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Gui su kien click ve handler da duoc gan truoc.
    /// </summary>
    void OnMouseDown()
    {
        if (clickHandler == null)
        {
            Debug.LogError("[CellClick] No click handler assigned.");
            return;
        }

        clickHandler.OnCellClicked(gridPos);
    }

    #endregion
}