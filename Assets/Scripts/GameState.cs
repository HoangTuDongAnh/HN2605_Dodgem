using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameState
{
    // Bàn cờ 3x3: (row, col) - row 0 là hàng dưới, row 2 là hàng trên
    public Vector2Int[] whitePieces;   // Vị trí 2 quân Trắng
    public Vector2Int[] blackPieces;   // Vị trí 2 quân Đen
    public bool isWhiteTurn;           // true = đến lượt Trắng (máy tính)
    public int whiteEscaped = 0;       // Số quân Trắng đã thoát
    public int blackEscaped = 0;       // Số quân Đen đã thoát

    public GameState Clone()
    {
        return new GameState {
            whitePieces = (Vector2Int[])whitePieces.Clone(),
            blackPieces  = (Vector2Int[])blackPieces.Clone(),
            isWhiteTurn  = isWhiteTurn,
            whiteEscaped = whiteEscaped,
            blackEscaped = blackEscaped
        };
    }

    // Kiểm tra trạng thái kết thúc
    public bool IsTerminal()
    {
        if (whiteEscaped == 2) return true;  // Trắng thắng
        if (blackEscaped == 2) return true;  // Đen thắng
        // Kiểm tra bên nào không còn nước đi
        return false;
    }
}
