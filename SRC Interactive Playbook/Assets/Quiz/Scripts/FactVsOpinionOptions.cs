/*
    Author: Yeong Yu Seong
    Date Created: 25 May 2026
    Last Edited: 16 June 2026
    Description: This script is used to manage the options for the Fact vs Opinion game.
*/
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FactVsOpinionOptions : MonoBehaviour
{
    [Header("Option Settings")]
    public bool isFact; // Flag to indicate whether the option is a fact or an opinion
    private Coroutine timeToNextQuestionCoroutine; // Coroutine to handle the delay before moving to the next question
    private float timeToNextQuestion = 2f; // Time to wait before moving to the next question

    [Header("Reference to FactVsOpinion Script")]
    private FactVsOpinion factVsOpinionScript; // Reference to the FactVsOpinion script
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        factVsOpinionScript = FindObjectOfType<FactVsOpinion>(); // Get the reference to the FactVsOpinion script
        timeToNextQuestionCoroutine = null; // Initialize the coroutine reference to null
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Update the game state everytime the players answer a question.
    /// For testing purposes, correct or wrong, the question moves on.
    /// </summary>
    public void NextQuestion()
    {
        CheckAnswer(isFact); // Check the player's answer before moving to the next question
        foreach (Button button in factVsOpinionScript.optionButtons)
        {
            button.interactable = false; // Disable the option buttons to prevent multiple answers
        }
        if (timeToNextQuestionCoroutine == null)
        {
            timeToNextQuestionCoroutine = StartCoroutine(TimeToNextQuestion()); // Start the coroutine to move to the next question after a delay
        }
    }

    /// <summary>
    /// Checks the player's answer and updates the score accordingly.
    /// It compares the player's choice (isFact) with the correct answer from the FactVsOpinion script's isFactArray using the current question index.
    /// If the player's answer is correct, it increments the score; otherwise, it simply logs that the answer is wrong. This method is called every time the player selects an option and moves to the next question.
    /// </summary>
    /// <param name="isFact"></param>
    private void CheckAnswer(bool isFact)
    {
        factVsOpinionScript.mascotImage.sprite = factVsOpinionScript.mascotSprites[1]; // Set the mascot image

        if (isFact == factVsOpinionScript.isFactArray[factVsOpinionScript.questionIndex])
        {
            ColorUtility.TryParseHtmlString("#5EC97F", out Color correctColor); // Change the color of the button to hex 5EC97F if the answer is correct
            GetComponent<Image>().color = correctColor;
            Debug.Log("Correct!");
            factVsOpinionScript.score++;
        }
        else
        {
            ColorUtility.TryParseHtmlString("#E74142", out Color wrongColor); // Change the color of the button to hex E74142 if the answer is wrong
            GetComponent<Image>().color = wrongColor;
            Debug.Log("Wrong!");
        }
    }

    /// <summary>
    /// Coroutine to handle the delay before moving to the next question.
    /// It waits for a specified time (timeToNextQuestion) before checking if there are more questions to display.
    /// If there are more questions, it updates the statement text and question number text to show the next question.
    /// If there are no more questions, it calls the EndGame method from the FactVsOpinion script to end the game.
    /// After moving to the next question, it resets the mascot image and re-enables the option buttons for the next question.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TimeToNextQuestion()
    {
        yield return new WaitForSeconds(timeToNextQuestion); // Wait for the specified time before moving to the next question
        if (factVsOpinionScript.questionIndex < factVsOpinionScript.statements.Length-1)
        {
            // Display the current statement to the player
            factVsOpinionScript.questionIndex++;
            factVsOpinionScript.statementText.text = factVsOpinionScript.statements[factVsOpinionScript.questionIndex];
            factVsOpinionScript.questionNumberText.text = $"Question: {factVsOpinionScript.questionIndex+1}/{factVsOpinionScript.statements.Length}";
        }
        else
        {
            factVsOpinionScript.EndGame(); // End the game if there are no more questions
        }
        timeToNextQuestionCoroutine = null; // Reset the coroutine reference to null after moving to the next question
        factVsOpinionScript.mascotImage.sprite = factVsOpinionScript.mascotSprites[0]; // Set the mascot image to the default sprite
        
        foreach (Button button in factVsOpinionScript.optionButtons)
        {
            button.interactable = true; // Re-enable the option buttons for the next question
            button.GetComponent<Image>().color = Color.white; // Reset the color of the buttons to white for the next question
        }
    }
}
