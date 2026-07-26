using UnityEngine;
using TMPro;

public class DNDBinUI : MonoBehaviour
{
    [Tooltip("The title text for the bin (e.g., Evidence Archive)")]
    public TextMeshProUGUI binTitleText;

    [Tooltip("The actual drop zone where items go")]
    public DNDBinSlot dropSlot;
}
