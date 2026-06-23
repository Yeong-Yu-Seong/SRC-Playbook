/*
    Author: Yeong Yu Seong
    Date Created: 12 June 2026
    Last Edited: 12 June 2026
    Description: This script is used to manage the quiz games. It handles the selection of the quiz game and tracks the high scores for each quiz type.
*/
using UnityEngine;

public class QuizManager : MonoBehaviour
{
    [Header("Quiz Settings")]
    public static QuizManager quizManagerInstance; // Singleton instance of the QuizManager
    public int scenarioId; // Variable to track the current scenario ID
    private int maxScenarios = 2; // Maximum number of scenarios available. This can be adjusted based on the number of scenarios you have in your game.
    public Canvas[] quizzes; // Array to hold references to the quiz canvases
    public float[] FactVsOpinionHighScores; // Variable to track the highest score for the Fact vs Opinion quiz
    public float[] MCQHighScores; // Variable to track the highest score for the MCQ quiz

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (quizManagerInstance != null)
        {
            quizManagerInstance = this; // Ensure that the instance is assigned to this QuizManager
        }
        if (scenarioId == 0)
        {
            Debug.LogError("Scenario ID is not set. Please set the scenario ID in the QuizManager to a value between 1 and " + maxScenarios + "."); // Log an error if the scenario ID is not set
        }
        // Initialize the high score arrays for both quizzes. Each index in the array corresponds to a specific scenario, allowing you to track high scores for multiple scenarios.
        FactVsOpinionHighScores = new float[maxScenarios];
        MCQHighScores = new float[maxScenarios];
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    /// <summary>
    /// This method is called to randomly select and start a quiz game.
    /// It randomly selects a quiz canvas from the quizzes array and activates it.
    /// It then checks which quiz script is attached to the selected canvas and calls the StartGame method on the corresponding script to begin the game.
    /// If the selected canvas does not have a valid quiz script attached, it logs an error message.
    /// </summary>
    public void ChooseGame()
    {
        int i = Random.Range(0, quizzes.Length); // Randomly select an index for the quiz canvases
        quizzes[i].gameObject.SetActive(true); // Activate the selected quiz canvas
        if (quizzes[i].GetComponent<MCQ>() != null)
        {
            quizzes[i].GetComponent<MCQ>().StartGame(); // Start the game by calling the StartGame method on the corresponding quiz script
        }
        else if (quizzes[i].GetComponent<FactVsOpinion>() != null)
        {
            quizzes[i].GetComponent<FactVsOpinion>().StartGame(); // Start the game by calling the StartGame method on the corresponding quiz script
        }
        else
        {
            Debug.LogError("The selected quiz canvas does not have a valid quiz script attached."); // Log an error if the selected quiz canvas does not have a valid quiz script attached
        }
    }
}
