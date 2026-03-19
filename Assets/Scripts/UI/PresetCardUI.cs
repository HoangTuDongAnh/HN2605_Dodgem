using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ================================================================
// PresetCardUI — gắn vào root của PresetCard prefab
//
// Kéo 3 TMP vào Inspector của component này.
// MenuManager gọi SetData() để điền nội dung — không dùng
// GetComponentsInChildren nữa nên sẽ không lẫn sang Slot/Toggle.
//
// Setup trong prefab:
//   PresetCard (root)
//   ├── Button component  ← trên root
//   ├── Image component   ← trên root (để highlight)
//   ├── PresetCardUI      ← script này, trên root
//   └── Layout (Vertical)
//       ├── Txt_Name      ← kéo vào txtName
//       ├── Txt_Desc      ← kéo vào txtDesc
//       └── Txt_Info      ← kéo vào txtInfo
// ================================================================

public class PresetCardUI : MonoBehaviour
{
    [Header("Keo 3 TMP vao day (khong dung GetComponentsInChildren)")]
    public TextMeshProUGUI txtName;   // ten preset
    public TextMeshProUGUI txtDesc;   // mo ta
    public TextMeshProUGUI txtInfo;   // so quan: "Trang:2q  Den:2q"

    private Image cardImage;

    void Awake()
    {
        cardImage = GetComponent<Image>();
    }

    public void SetData(string name, string desc, string info)
    {
        if (txtName != null) txtName.text = name;
        if (txtDesc != null) txtDesc.text = desc;
        if (txtInfo != null) txtInfo.text = info;
    }

    public void SetSelected(bool selected)
    {
        if (cardImage == null) cardImage = GetComponent<Image>();
        if (cardImage != null)
            cardImage.color = selected ? new Color(0.7f, 0.9f, 1f) : Color.white;
    }
}