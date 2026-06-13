/*
    Author: Yeong Yu Seong
    Date Created: 12 June 2026
    Last Edited: 13 June 2026
    Description: This script is used to manage the UI elements for the AR.
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ARUIHandler : MonoBehaviour
{
    [Header("Bubble Settings")]
    public TextMeshProUGUI bubbleText; // Reference to the TextMeshProUGUI
    public string[] bubbleMessages; // Array to store the messages for the speech bubble
    private int currentMessageIndex = 0; // Index to track the current message being displayed

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>
    /// This method is called to update the speech bubble text to the next message in the bubbleMessages array.
    /// It checks if the currentMessageIndex is within the bounds of the bubbleMessages array and updates the bubbleText accordingly. If the currentMessageIndex exceeds the bounds of the array, it does not update the text and can optionally log a message or reset the index to loop through the messages again.
    /// </summary>
    public void UpdateBubbleText()
    {
        if (currentMessageIndex >= 0 && currentMessageIndex < bubbleMessages.Length-1)
        {
            currentMessageIndex++; // Increment the message index to show the next message in the array
            bubbleText.text = bubbleMessages[currentMessageIndex]; // Update the speech bubble text based on the current message index
        }
    }
}
