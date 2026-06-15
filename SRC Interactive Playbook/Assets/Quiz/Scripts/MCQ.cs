/*
    Author: Yeong Yu Seong
    Date Created: 26 May 2026
    Last Edited: 12 June 2026
    Description: This script is used to manage the Multiple Choice Questions game.
*/
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class MCQ : MonoBehaviour
{
    [Header("Game Settings")]
    public float score; // Player's score
    public string[] statements; // Array to store statements
    public string[] answerArray; // Array to store the correct answers for each statement
    public string[] answerExplanationArray; // Array to store the explanations for each answer
    private string learningPoints; // String to store the learning points for the current scenario
    public string[] optionAArray; // Array to store the text for option A for each statement
    public string[] optionBArray; // Array to store the text for option B for each statement
    public string[] optionCArray; // Array to store the text for option C for each statement
    public string[] optionDArray; // Array to store the text for option D for each statement

    /// <summary>
    /// Template for scenario-specific settings. Each scenario can have its own set of statements and corresponding answer arrays.
    /// </summary>
    [Header("Scenario Id 1 Settings")]
    public string[] scenario1Statements; // Array to store statements for scenario 1
    public string[] scenario1AnswerArray; // Array to store the correct answers for each statement in scenario 1
    public string[] scenario1AnswerExplanationArray; // Array to store the explanations for each answer in scenario 1
    public string[] scenario1OptionAArray; // Array to store the text for option A for each statement in scenario 1
    public string[] scenario1OptionBArray; // Array to store the text for option B for each statement in scenario 1
    public string[] scenario1OptionCArray; // Array to store the text for option C for each statement in scenario 1
    public string[] scenario1OptionDArray; // Array to store the text for option D for each statement in scenario 1
    public string scenario1LearningPoints; // String to store the learning points for scenario 1
    [Header("Scenario Id 2 Settings")]
    public string[] scenario2Statements; // Array to store statements for scenario 2
    public string[] scenario2AnswerArray; // Array to store the correct answers for each statement in scenario 2
    public string[] scenario2AnswerExplanationArray; // Array to store the explanations for each answer in scenario 2
    public string[] scenario2OptionAArray; // Array to store the text for option A for each statement in scenario 2
    public string[] scenario2OptionBArray; // Array to store the text for option B for each statement in scenario 2
    public string[] scenario2OptionCArray; // Array to store the text for option C for each statement in scenario 2
    public string[] scenario2OptionDArray; // Array to store the text for option D for each statement in scenario 2
    public string scenario2LearningPoints; // String to store the learning points for scenario 2

    [SerializeField]
    public int questionIndex = 0; // Index to track the current statement
    public float timer; // Time limit for the game
    private float timeToNextQuestion = 2f; // Time to wait before moving to the next question
    public bool isGameActive = false; // Flag to check if the game is active

    [Header("UI References")]
    public TextMeshProUGUI statementText; // Reference to the UI Text component to display statements
    public TextMeshProUGUI questionNumberText; // Reference to the TextMeshProUGUI component to display the question number
    public Image timerCountdown; // Reference to the Image component to display the timer countdown
    public Button[] optionButtons; // Array to store the option buttons for fact and opinion
    public GameObject gamePanel; // Reference to the game panel to show/hide during the game
    public GameObject answerPanel; // Reference to the answer panel to show when the player selects an answer
    public GameObject resultPanel; // Reference to the result panel to display the final score
    public TextMeshProUGUI finalScoreText; // Reference to the TextMeshProUGUI component to display the final score
    public TextMeshProUGUI pointText; // Reference to the TextMeshProUGUI component to display the points earned
    public TextMeshProUGUI endMessageText; // Reference to the TextMeshProUGUI component to display the end message
    public TextMeshProUGUI endMessageSubText; // Reference to the TextMeshProUGUI component to display the end message subtext
    public TextMeshProUGUI optionAText; // Reference to the TextMeshProUGUI component to display the text for option A
    public TextMeshProUGUI optionBText; // Reference to the TextMeshProUGUI component to display the text for option B
    public TextMeshProUGUI optionCText; // Reference to the TextMeshProUGUI component to display the text for option C
    public TextMeshProUGUI optionDText; // Reference to the TextMeshProUGUI component to display the text for option D
    public TextMeshProUGUI answerText; // Reference to the TextMeshProUGUI component to display the answer message
    public TextMeshProUGUI answerQuestionText; // Reference to the TextMeshProUGUI component to display the question for the answer question
    public Button nextQuestionButton; // Reference to the Button component for the next question button
    public TextMeshProUGUI learningPointsText; // Reference to the TextMeshProUGUI component to display the learning points at the end of the game

    private Coroutine timeToNextQuestionCoroutine; // Coroutine to handle the delay before moving to the next question
    public QuizManager quizManagerInstance; // Reference to the QuizManager instance to access the high score variables

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeToNextQuestionCoroutine = null; // Initialize the coroutine reference to null
    }

    // Update is called once per frame
    void Update()
    {
        if (isGameActive)
        {
            timer -= Time.deltaTime; // Decrease the timer by the time elapsed since the last frame
            timerCountdown.fillAmount = timer / 60f; // Update the timer countdown image to show the remaining time
            if (timer <= 0)
            {
                EndGame(); // End the game when the timer runs out
            }
        }
    }

    /// <summary>
    /// Starts the MCQ game by initializing the score, question index, and displaying the first statement.
    /// It also checks if the statements and answerArray have the same length to avoid errors during gameplay.
    /// Also acts as a reset function to reset the game state when the player starts a new game after finishing a game.
    /// </summary>
    public void StartGame()
    {
        Debug.Log("Starting MCQ Game..."); // Log a message to indicate that the game is starting

        // Initialize the statements, answerArray, and other arrays based on the scenario ID from the QuizManager
        // Template for setting up the statements, answerArray, and other arrays based on the scenario ID.
        // You can expand this switch statement to include more scenarios as needed.
        switch (quizManagerInstance.scenarioId)
        {
            case 1:
                statements = scenario1Statements;
                answerArray = scenario1AnswerArray;
                answerExplanationArray = scenario1AnswerExplanationArray;
                optionAArray = scenario1OptionAArray;
                optionBArray = scenario1OptionBArray;
                optionCArray = scenario1OptionCArray;
                optionDArray = scenario1OptionDArray;
                learningPoints = scenario1LearningPoints;
                break;
            case 2:
                statements = scenario2Statements;
                answerArray = scenario2AnswerArray;
                answerExplanationArray = scenario2AnswerExplanationArray;
                optionAArray = scenario2OptionAArray;
                optionBArray = scenario2OptionBArray;
                optionCArray = scenario2OptionCArray;
                optionDArray = scenario2OptionDArray;
                learningPoints = scenario2LearningPoints;
                break;
            default:
                Debug.LogError("Invalid scenario ID! Please set a valid scenario ID in the QuizManager."); // Log an error if the scenario ID is invalid
                break;
        }

        // Check if learning points are provided for the current scenario.
        if (learningPoints == null)
        {
            Debug.LogError("No learning points available for this scenario."); // Set a default message if learning points are not provided for the scenario
        }

        // Check if the statements, answerArray, and answerExplanationArray have the same length to avoid errors during gameplay
        if (statements.Length != answerArray.Length || statements.Length != answerExplanationArray.Length)
        {
            Debug.LogError("Statements, answerArray, and answerExplanationArray must have the same length! Please check the lengths of these arrays for the current scenario."); // Log an error if the statements, answerArray, and answerExplanationArray do not have the same length
            return;
        }

        score = 0;
        questionIndex = 0;
        timer = 60f; // Reset the timer
        isGameActive = true;
        timerCountdown.fillAmount = timer / 60f; // Display the initial timer value to the player

        // Display the first statement to the player
        statementText.text = statements[questionIndex];
        optionAText.text = optionAArray[questionIndex];
        optionBText.text = optionBArray[questionIndex];
        optionCText.text = optionCArray[questionIndex];
        optionDText.text = optionDArray[questionIndex];

        foreach (Button button in optionButtons)
        {
            button.interactable = true; // Enable the option buttons for the player to select an answer
        }

        nextQuestionButton.interactable = true; // Enable the next question button
        learningPointsText.text = ""; // Clear the learning points text at the start of the game
        questionNumberText.text = $"Q{questionIndex + 1}/{statements.Length}";
        gamePanel.SetActive(true); // Show the game panel
        resultPanel.SetActive(false); // Hide the result panel
    }
    
    /// <summary>
    /// Ends the MCQ game by setting the game as inactive, hiding the game panel, showing the result panel, and displaying the final score and points earned to the player.
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
        if (score > quizManagerInstance.MCQHighScores[quizManagerInstance.scenarioId-1])
        {
            quizManagerInstance.MCQHighScores[quizManagerInstance.scenarioId-1] = score; // Update the high score for the MCQ quiz in the QuizManager
        }
        learningPointsText.text = learningPoints; // Display the learning points to the player at the end of the game
    }
    
    /// <summary>
    /// Moves to the next question after a delay when the player selects an answer.
    /// It checks if there are more questions to display and updates the game state accordingly.
    /// If there are no more questions, it ends the game by calling the EndGame method.
    /// This method is called every time the player selects an answer and moves to the next question after a short delay to allow the player to see the correct answer and explanation before moving on.
    /// </summary>
    public void NextQuestion()
    {
        if (timeToNextQuestionCoroutine == null)
        {
            timeToNextQuestionCoroutine = StartCoroutine(TimeToNextQuestion()); // Start the coroutine to handle the delay before moving to the next question
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
        answerPanel.SetActive(false); // Hide the answer panel after the delay
        if (questionIndex < statements.Length-1)
        {
            // Display the current statement to the player
            questionIndex++;
            statementText.text = statements[questionIndex];
            questionNumberText.text = $"Q{questionIndex+1}/{statements.Length}";
            optionAText.text = ""; // Clear the option text for the next question
            optionBText.text = ""; // Clear the option text for the next question
            optionCText.text = ""; // Clear the option text for the next question
            optionDText.text = ""; // Clear the option text for the next question
            optionAText.text = optionAArray[questionIndex];
            optionBText.text = optionBArray[questionIndex];
            optionCText.text = optionCArray[questionIndex];
            optionDText.text = optionDArray[questionIndex];
            foreach (Button button in optionButtons)
            {
                button.interactable = true; // Re-enable the option buttons for the next question
            }
            gamePanel.SetActive(true); // Show the game panel for the next question
            timer = 60f; // Reset the timer for the next question
            timerCountdown.fillAmount = 1f; // Reset the timer countdown image for the next question
            isGameActive = true; // Set the game as active for the next question
            nextQuestionButton.interactable = true; // Re-enable the next question button for the next question
        }
        else
        {
            EndGame(); // End the game if there are no more questions
        }
        timeToNextQuestionCoroutine = null; // Reset the coroutine reference to null after moving to the next question
    }
}