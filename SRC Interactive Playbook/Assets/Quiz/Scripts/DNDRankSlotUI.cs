using UnityEngine;
using TMPro;

public class DNDRankSlotUI : MonoBehaviour
{
    [Tooltip("Displays the rank number (e.g., '1', '2', '3')")]
    public TextMeshProUGUI rankNumberText;

    [Tooltip("The slot where the user drops the card.")]
    public DNDOptionSlot dropSlot;
}