/// <summary>
/// Interface nhan su kien click o tren ban co.
/// </summary>
public interface ICellClickHandler
{
    /// <summary>
    /// Xu ly khi mot o tren ban co duoc click.
    /// </summary>
    void OnCellClicked(UnityEngine.Vector2Int pos);
}