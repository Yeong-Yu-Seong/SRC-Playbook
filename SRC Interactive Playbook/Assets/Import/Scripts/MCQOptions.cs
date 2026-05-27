/*
    Author: Yeong Yu Seong
    Date Created: 26 May 2026
    Description: This script is used to manage the game state for the Multiple Choice Questions game by checking the player's answer and updating the score accordingly. It also handles moving to the next question and ending the game when all questions have been answered.
*/
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MCQOptions : MonoBehaviour
{
    [Header("Option Settings")]
    public string optionType; // Type of option ("A", "B", "C", "D")
    private Coroutine timeToNextQuestionCoroutine; // Coroutine to handle the delay before moving to the next question
    private float timeToNextQuestion = 2f; // Time to wait before moving to the next question

    [Header("Reference to MCQ Script")]
    private MCQ mcqScript; // Reference to the MCQ script
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mcqScript = FindObjectOfType<MCQ>(); // Get the reference to the MCQ script
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
    private void NextQuestion()
    {
        CheckAnswer(optionType); // Check the player's answer before moving to the next question
        foreach (Button button in mcqScript.optionButtons)
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
    /// It compares the player's choice (optionType) with the correct answer from the MCQ script's answerArray using the current question index.
    /// If the player's answer is correct, it increments the score; otherwise, it simply logs that the answer is wrong. This method is called every time the player selects an option and moves to the next question.
    /// </summary>
    /// <param name="optionType"></param>
    public void CheckAnswer(string optionType)
    {

        if (optionType == mcqScript.answerArray[mcqScript.questionIndex])
        {
            Debug.Log("Correct!");
            mcqScript.score++;
        }
        else
        {
            Debug.Log("Wrong!");
        }
    }

    /// <summary>
    /// Coroutine to handle the delay before moving to the next question.
    /// It waits for a specified time (timeToNextQuestion) before checking if there are more questions to display.
    /// If there are more questions, it updates the statement text and question number text to show the next question.
    /// If there are no more questions, it calls the EndGame method from the MCQ script to end the game.
    /// After moving to the next question, it resets the mascot image and re-enables the option buttons for the next question.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TimeToNextQuestion()
    {
        yield return new WaitForSeconds(timeToNextQuestion); // Wait for the specified time before moving to the next question
        if (mcqScript.questionIndex < mcqScript.statements.Length-1)
        {
            // Display the current statement to the player
            mcqScript.questionIndex++;
            mcqScript.statementText.text = mcqScript.statements[mcqScript.questionIndex];
            mcqScript.questionNumberText.text = $"Q{mcqScript.questionIndex+1}/{mcqScript.statements.Length}";
            mcqScript.optionAText.text = ""; // Clear the option text for the next question
            mcqScript.optionBText.text = ""; // Clear the option text for the next question
            mcqScript.optionCText.text = ""; // Clear the option text for the next question
            mcqScript.optionDText.text = ""; // Clear the option text for the next question
            mcqScript.optionAText.text = mcqScript.optionAArray[mcqScript.questionIndex];
            mcqScript.optionBText.text = mcqScript.optionBArray[mcqScript.questionIndex];
            mcqScript.optionCText.text = mcqScript.optionCArray[mcqScript.questionIndex];
            mcqScript.optionDText.text = mcqScript.optionDArray[mcqScript.questionIndex];
        }
        else
        {
            mcqScript.EndGame(); // End the game if there are no more questions
        }
        timeToNextQuestionCoroutine = null; // Reset the coroutine reference to null after moving to the next question
        foreach (Button button in mcqScript.optionButtons)
        {
            button.interactable = true; // Re-enable the option buttons for the next question
        }
        mcqScript.timer = 60f; // Reset the timer for the next question
        mcqScript.timerCountdown.fillAmount = 1f; // Reset the timer countdown image for the next question
    }
}