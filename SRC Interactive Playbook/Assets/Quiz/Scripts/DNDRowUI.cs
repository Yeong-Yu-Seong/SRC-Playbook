using UnityEngine;
using TMPro;

public class DNDRowUI : MonoBehaviour
{
    [Tooltip("The text component that will display the 'Poor Feedback' from Firebase.")]
    public TextMeshProUGUI poorFeedbackText;
    
    [Tooltip("The slot where the user will drop the answer.")]
    public DNDOptionSlot dropSlot; 
}