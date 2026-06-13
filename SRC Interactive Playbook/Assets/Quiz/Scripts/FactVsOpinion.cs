/*
    Author: Yeong Yu Seong
    Date Created: 25 May 2026
    Last Edited: 12 June 2026
    Description: This script is used to manage the Fact vs Opinion game.
*/
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class FactVsOpinion : MonoBehaviour
{
    [Header("Game Settings")]
    public float score; // Player's score
    public string[] statements; // Array to store statements
    public bool[] isFactArray; // Array to store whether each statement is a fact or opinion
    [SerializeField]
    public Sprite[] mascotSprites; // Array to store the mascot sprites
    public int questionIndex = 0; // Index to track the current statement
    public float timer; // Time limit for the game
    public bool isGameActive = false; // Flag to check if the game is active

    /// <summary>
    /// Template for scenario-specific settings. Each scenario can have its own set of statements and corresponding fact/opinion arrays.
    /// You can expand this template to include more scenarios as needed, ensuring that each scenario has its own unique statements and fact/opinion arrays.
    /// </summary>
    [Header("Scenario Id 1 Settings")]
    public string[] scenario1Statements; // Array to store statements for scenario 1
    public bool[] scenario1IsFactArray; // Array to store whether each statement in scenario 1 is a fact or opinion
    [Header("Scenario Id 2 Settings")]
    public string[] scenario2Statements; // Array to store statements for scenario 2
    public bool[] scenario2IsFactArray; // Array to store whether each statement in scenario 2 is a fact or opinion
    
    [Header("UI References")]
    public TextMeshProUGUI statementText; // Reference to the UI Text component to display statements
    public TextMeshProUGUI questionNumberText; // Reference to the TextMeshProUGUI component to display the question number
    public TextMeshProUGUI timerText; // Reference to the TextMeshProUGUI component to display the timer
    public Image mascotImage; // Reference to the Image component to display the mascot
    public Button[] optionButtons; // Array to store the option buttons for fact and opinion
    public GameObject gamePanel; // Reference to the game panel to show/hide during the game
    public GameObject resultPanel; // Reference to the result panel to display the final score
    public TextMeshProUGUI finalScoreText; // Reference to the TextMeshProUGUI component to display the final score
    public TextMeshProUGUI pointText; // Reference to the TextMeshProUGUI component to display the points earned
    public TextMeshProUGUI endMessageText; // Reference to the TextMeshProUGUI component to display the end message
    public TextMeshProUGUI endMessageSubText; // Reference to the TextMeshProUGUI component to display the end message subtext
    public QuizManager quizManagerInstance; // Reference to the QuizManager instance to access the high score variables

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isGameActive)
        {
            timer -= Time.deltaTime; // Decrease the timer by the time elapsed since the last frame
            timerText.text = $"Time: {Mathf.Ceil(timer)}"; // Update the timer text to show the remaining time, rounded up to the nearest whole number
            if (timer <= 0)
            {
                EndGame(); // End the game when the timer runs out
            }
        }
    }

    /// <summary>
    /// Starts the Fact vs Opinion game by initializing the score, question index, and displaying the first statement.
    /// It also checks if the statements and isFactArray have the same length to avoid errors during gameplay.
    /// </summary>
    public void StartGame()
    {
        Debug.Log("Starting Fact vs Opinion Game..."); // Log a message to indicate that the game is starting

        // Initialize the statements and isFactArray based on the scenario ID from the QuizManager
        // Template for setting up the statements and isFactArray based on the scenario ID.
        // You can expand this switch statement to include more scenarios as needed.
        switch (quizManagerInstance.scenarioId)
        {
            case 1:
                statements = scenario1Statements;
                isFactArray = scenario1IsFactArray;
                break;
            case 2:
                statements = scenario2Statements;
                isFactArray = scenario2IsFactArray;
                break;
            default:
                Debug.LogError("Invalid scenario ID! Please set a valid scenario ID in the QuizManager.");
                break;
        }

        // Check if the statements and isFactArray have the same length to avoid errors during gameplay
        if (statements.Length != isFactArray.Length)
        {
            Debug.LogError("Statements and isFactArray must have the same length!");
            return;
        }

        // Reset the game state
        score = 0;
        questionIndex = 0;
        timer = 60f; // Reset the timer
        isGameActive = true;
        timerText.text = $"Time: {timer}"; // Display the initial timer value to the player

        // Display the first statement to the player
        statementText.text = statements[questionIndex];
        questionNumberText.text = $"Question: {questionIndex + 1}/{statements.Length}";
        gamePanel.SetActive(true); // Show the game panel
        resultPanel.SetActive(false); // Hide the result panel
    }
    
    /// <summary>
    /// Ends the Fact vs Opinion game by setting the game as inactive, hiding the game panel, showing the result panel, and displaying the final score and points earned to the player.
    /// It calculates the final score as a percentage based on the player's score and the total number of statements.
    /// </summary>
    public void EndGame()
    {
        timer = 0; // Ensure the timer is set to zero when the game ends
        isGameActive = false; // Set the game as inactive
        gamePanel.SetActive(false); // Hide the game panel
        resultPanel.SetActive(true); // Show the result panel
        finalScoreText.text = $"{score / statements.Length * 100}%"; // Display the final score to the player
        pointText.text = $"+{score}"; // Display the points earned to the player
        endMessageText.text = ""; // Clear the end message text
        endMessageSubText.text = ""; // Clear the end message subtext

        // Set the end message based on the player's score. If the player scored at least half of the statements correctly, display a congratulatory message. Otherwise, encourage the player to keep practicing.
        if (score >= (statements.Length / 2))
        {
            endMessageText.text = "Congratulations, you did it!"; // Set the end message for a high score
            endMessageSubText.text = "You may head back to the learning page and carry on to other topics. Happy learning!"; // Set the end message subtext for a high score
        }
        else
        {
            endMessageText.text = "Keep practicing!"; // Set the end message for a low score
            endMessageSubText.text = "Don't worry, you'll do better next time!"; // Set the end message subtext for a low score
        }

        if (score > quizManagerInstance.FactVsOpinionHighScores[quizManagerInstance.scenarioId-1])
        {
            quizManagerInstance.FactVsOpinionHighScores[quizManagerInstance.scenarioId-1] = score; // Update the high score for the Fact vs Opinion quiz in the QuizManager
        }
    }
}