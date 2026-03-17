using UnityEngine;

public class CellClick : MonoBehaviour
{
    public Vector2Int gridPos;

    // KHÔNG dùng Start() + FindObjectOfType — trên WebGL thứ tự Awake/Start
    // không đảm bảo giống Editor, GameManager có thể chưa tồn tại khi Start() chạy.
    // Dùng lazy-init: chỉ tìm khi thực sự cần (lúc click).
    private GameManager gm;

    void OnMouseDown()
    {
        if (gm == null)
            gm = FindObjectOfType<GameManager>();

        if (gm != null)
            gm.OnCellClicked(gridPos);
        else
            Debug.LogError("[CellClick] Không tìm thấy GameManager!");
    }
}