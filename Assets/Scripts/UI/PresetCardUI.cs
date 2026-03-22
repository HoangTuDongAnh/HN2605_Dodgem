using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hien thi mot card preset trong menu.
/// </summary>
public class PresetCardUI : MonoBehaviour
{
    #region Fields

    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI infoText;
    public Image backgroundImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.85f, 0.95f, 1f, 1f);

    #endregion

    #region Public API

    /// <summary>
    /// Gan du lieu hien thi cho card.
    /// </summary>
    public void SetData(string title, string description, string info)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        if (infoText != null) infoText.text = info;
    }

    /// <summary>
    /// Cap nhat trang thai dang chon cua card.
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    #endregion
}