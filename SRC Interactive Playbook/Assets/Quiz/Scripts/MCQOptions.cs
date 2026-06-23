/*
    Author: Yeong Yu Seong
    Date Created: 26 May 2026
    Last Edited: 16 June 2026
    Description: This script is used to manage the Multiple Choice Questions game.
*/
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class MCQOptions : MonoBehaviour
{
    [Header("Option Settings")]
    public string optionType; // Type of option ("A", "B", "C", "D")

    [Header("Reference to MCQ Script")]
    private MCQ mcqScript; // Reference to the MCQ script
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mcqScript = FindObjectOfType<MCQ>(); // Get the reference to the MCQ script
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Update the game state everytime the players answer a question.
    /// For testing purposes, correct or wrong, the question moves on.
    /// </summary>
    public void AnswerQuestion()
    {
        CheckAnswer(optionType); // Check the player's answer before moving to the next question
        foreach (Button button in mcqScript.optionButtons)
        {
            button.interactable = false; // Disable the option buttons to prevent multiple answers
        }
        mcqScript.isGameActive = false; // Set the game as inactive when the player selects an answer
        mcqScript.answerQuestionText.text = ""; // Clear the question text for the answer panel
        mcqScript.answerText.text = ""; // Clear the answer text for the answer panel
        mcqScript.gamePanel.SetActive(false); // Hide the game panel to show the answer panel
        mcqScript.answerPanel.SetActive(true); // Show the answer panel to display the correct answer or wrong answer message
        mcqScript.answerQuestionText.text = $"{mcqScript.statements[mcqScript.questionIndex]}"; // Set the question text for the answer panel
        mcqScript.answerPanelQuestionNumber.text = $"Q{mcqScript.questionIndex+1}/{mcqScript.statements.Length}"; // Set the question number text for the answer panel
        mcqScript.answerText.text = $"Correct answer: {mcqScript.answerArray[mcqScript.questionIndex]}\nExplanation: {mcqScript.answerExplanationArray[mcqScript.questionIndex]}"; // Set the answer text to display the correct answer and explanation
    }

    /// <summary>
    /// Checks the player's answer and updates the score accordingly.
    /// It compares the player's choice (optionType) with the correct answer from the MCQ script's answerArray using the current question index.
    /// If the player's answer is correct, it increments the score; otherwise, it simply logs that the answer is wrong. This method is called every time the player selects an option and moves to the next question.
    /// </summary>
    /// <param name="optionType"></param>
    public void CheckAnswer(string optionType)
    {
        mcqScript.isGameActive = false; // Set the game as inactive when the player selects an answer
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
}