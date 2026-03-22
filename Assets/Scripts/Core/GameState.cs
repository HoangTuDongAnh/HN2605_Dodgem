using UnityEngine;

/// <summary>
/// Luu tru trang thai runtime cua van dau.
/// </summary>
[System.Serializable]
public class GameState
{
    #region Fields

    public int boardWidth;
    public int boardHeight;
    public PlayerData[] players;
    public int currentPlayerIndex;

    /// <summary>
    /// Index nguoi choi vua thuc hien nuoc di (truoc khi NextTurn).
    /// Dung de Winner() biet chinh xac ai vua di khi doi phuong bi pass.
    /// </summary>
    public int lastMoverIndex;

    [Header("Mask o hop le tren ban")]
    public bool[,] validCells;

    #endregion

    #region Properties

    public int NumPlayers => players != null ? players.Length : 0;
    public PlayerData CurrentPlayer => players[currentPlayerIndex];

    #endregion

    #region Public Methods

    /// <summary>
    /// Tao ban sao state hien tai de phuc vu AI/search.
    /// </summary>
    public GameState Clone()
    {
        var clonedPlayers = new PlayerData[players.Length];
        for (int i = 0; i < players.Length; i++)
            clonedPlayers[i] = players[i].Clone();

        bool[,] clonedValidCells = null;
        if (validCells != null)
        {
            clonedValidCells = new bool[boardWidth, boardHeight];
            for (int x = 0; x < boardWidth; x++)
            {
                for (int y = 0; y < boardHeight; y++)
                    clonedValidCells[x, y] = validCells[x, y];
            }
        }

        return new GameState
        {
            boardWidth = boardWidth,
            boardHeight = boardHeight,
            players = clonedPlayers,
            currentPlayerIndex = currentPlayerIndex,
            lastMoverIndex = lastMoverIndex,
            validCells = clonedValidCells
        };
    }

    /// <summary>
    /// Kiem tra state da ket thuc hay chua.
    /// Ket thuc khi: co nguoi da thoat het quan, hoac tat ca nguoi choi deu bi block cung luc.
    /// Pass don thuan (1 player bi block) khong phai la terminal.
    /// </summary>
    public bool IsTerminal()
    {
        foreach (var player in players)
        {
            if (player.HasWon())
                return true;
        }

        // Chi terminal khi TAT CA nguoi choi deu khong co nuoc di
        for (int i = 0; i < NumPlayers; i++)
        {
            if (DodgemRules.HasAnyLegalMove(this, i))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Lay nguoi thang neu state da ket thuc.
    /// - Co nguoi thoat het quan: nguoi do thang.
    /// - Tat ca bi block: nguoi thoat nhieu nhat thang (hoac hoa).
    /// Pass don thuan khong bao gio tra ve winner.
    /// </summary>
    public PlayerData Winner()
    {
        // Truong hop 1: co nguoi da thoat het quan
        foreach (var player in players)
        {
            if (player.HasWon())
                return player;
        }

        // Truong hop 2: tat ca deu bi block, tinh theo so quan da thoat
        bool allBlocked = true;
        for (int i = 0; i < NumPlayers; i++)
        {
            if (DodgemRules.HasAnyLegalMove(this, i))
            {
                allBlocked = false;
                break;
            }
        }

        if (allBlocked)
        {
            // Tim nguoi thoat nhieu nhat
            PlayerData best = players[0];
            for (int i = 1; i < NumPlayers; i++)
            {
                if (players[i].escaped > best.escaped)
                    best = players[i];
            }
            return best;
        }

        return null;
    }

    /// <summary>
    /// Kiem tra co quan nao dang chiem o nay hay khong.
    /// </summary>
    public bool IsOccupied(Vector2Int pos)
    {
        foreach (var player in players)
        {
            if (player.HasPieceAt(pos))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Kiem tra o nay co hop le de dung/di chuyen hay khong.
    /// </summary>
    public bool IsCellPlayable(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= boardWidth || pos.y < 0 || pos.y >= boardHeight)
            return false;

        if (validCells == null)
            return true;

        return validCells[pos.x, pos.y];
    }

    /// <summary>
    /// Chuyen sang luot nguoi choi tiep theo.
    /// </summary>
    public void NextTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % NumPlayers;
    }

    /// <summary>
    /// Tao key duy nhat dai dien cho trang thai ban co hien tai.
    /// Gom: index luot hien tai + vi tri moi quan + so quan da thoat cua moi player.
    /// Dung de phat hien lap lai trang thai (threefold repetition).
    /// </summary>
    public string StateKey()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append('t');
        sb.Append(currentPlayerIndex);

        for (int p = 0; p < players.Length; p++)
        {
            sb.Append("|p");
            sb.Append(p);
            sb.Append(':');

            var pieces = players[p].pieces;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (i > 0) sb.Append(':');
                sb.Append(pieces[i].x);
                sb.Append(',');
                sb.Append(pieces[i].y);
            }

            sb.Append("|esc");
            sb.Append(players[p].escaped);
        }

        return sb.ToString();
    }

    #endregion
}