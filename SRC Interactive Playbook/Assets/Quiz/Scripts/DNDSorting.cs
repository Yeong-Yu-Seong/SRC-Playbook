using System;
using System.Collections.Generic;
using System.Linq;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DNDSorting : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gamePanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;
    public Button submitButton;

    [Header("Sorting Board Elements")]
    public Transform binsContainer;     // Where the 2 big bins spawn
    public GameObject binPrefab;        // Prefab with DNDBinUI attached
    public Transform itemsContainer;    // Layout group at the bottom for cards
    public GameObject draggablePrefab;  // Your existing DraggableOption prefab

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackTitleText;
    public TextMeshProUGUI feedbackMessageText;
    public Button feedbackActionButton;

    // ── Runtime ────────────────────────────────────────────────
    private PlaybookQuiz _quizData;
    private Action<int, int, int> _onCompleteCallback;
    private List<GameObject> _spawnedObjects = new();
    private int _pointsEarned = 0;

    private void Awake()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitAnswer);
    }

    public void StartGame(PlaybookQuiz runtimeQuiz, Action<int, int, int> onComplete)
    {
        _onCompleteCallback = onComplete;
        _quizData = runtimeQuiz;

        if (titleText != null) titleText.text = _quizData.title;
        if (instructionText != null) instructionText.text = _quizData.instructionText;

        gameObject.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(true);

        BuildSortingBoard();
    }

    private void BuildSortingBoard()
    {
        ClearBoard();
        if (submitButton != null) submitButton.interactable = true;

        // 1. Build the Bins (Derived from the choices of the first question)
        if (_quizData.questions.Count > 0)
        {
            var binCategories = _quizData.questions[0].choices;
            foreach (var category in binCategories)
            {
                GameObject newBin = Instantiate(binPrefab, binsContainer);
                _spawnedObjects.Add(newBin);

                DNDBinUI binUI = newBin.GetComponent<DNDBinUI>();
                if (binUI != null)
                {
                    binUI.binTitleText.text = category.text;
                    binUI.dropSlot.binChoiceId = category.id; // Assigns ID (e.g. choice_archive)
                }
            }
        }

        // 2. Build the Draggable Cards (Shuffled)
        var shuffledQuestions = _quizData.questions.OrderBy(x => Guid.NewGuid()).ToList();

        foreach (var q in shuffledQuestions)
        {
            GameObject newCard = Instantiate(draggablePrefab, itemsContainer);
            _spawnedObjects.Add(newCard);

            DraggableOption dragScript = newCard.GetComponentInChildren<DraggableOption>();
            if (dragScript != null)
            {
                // Repurposing choiceId to hold the question ID so we can track it
                dragScript.choiceId = q.id;
            }

            TextMeshProUGUI cardText = newCard.GetComponentInChildren<TextMeshProUGUI>();
            if (cardText != null) cardText.text = q.prompt;
        }
    }

    private void ClearBoard()
    {
        foreach (var obj in _spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        _spawnedObjects.Clear();
    }

    private void OnSubmitAnswer()
    {
        if (submitButton != null) submitButton.interactable = false;

        int correctCount = 0;
        Dictionary<string, string> capturedAnswers = new();
        if (_quizData == null || _quizData.questions == null) return;

        // Loop through the bins to see what items are inside them
        foreach (Transform binTransform in binsContainer)
        {
            DNDBinUI binUI = binTransform.GetComponent<DNDBinUI>();
            if (binUI == null) continue;

            string currentBinId = binUI.dropSlot.binChoiceId;

            // Find all dropped cards sitting inside this bin's drop zone
            DraggableOption[] droppedCards = binUI.dropSlot.GetComponentsInChildren<DraggableOption>();

            foreach (var card in droppedCards)
            {
                string questionId = card.choiceId; // The ID of the specific evidence card
                capturedAnswers[questionId] = currentBinId;

                // Validate
                var q = _quizData.questions.Find(x => x.id == questionId);
                bool isCorrect = (q != null && currentBinId == q.correctAnswerId);

                if (isCorrect) correctCount++;

                Image cardImg = card.GetComponent<Image>();
                if (cardImg == null) cardImg = card.GetComponentInChildren<Image>();

                // Only apply colors if the Image component actually exists!
                if (cardImg != null)
                {
                    cardImg.color = isCorrect
                        ? new Color(0.369f, 0.788f, 0.498f)   // Green
                        : new Color(0.906f, 0.255f, 0.259f);  // Red
                }
            }
        }

        bool allCorrect = correctCount == _quizData.questions.Count;
        ShowFeedback(allCorrect, capturedAnswers, correctCount);
    }

    private void ShowFeedback(bool isCorrect, Dictionary<string, string> capturedAnswers, int correctCount)
    {
        feedbackPanel.SetActive(true);
        feedbackActionButton.onClick.RemoveAllListeners();

        if (isCorrect)
        {
            feedbackTitleText.text = "Great job!";
        }
        else
        {
            feedbackTitleText.text = "Not quite.";
        }

        // Calculate total points earned for the board
        int ptsEach = ScoreManager.Instance.pointsPerCorrectDragDrop;
        bool perfect = correctCount == _quizData.questions.Count;
        int pointsEarned = (correctCount * ptsEach) + (perfect ? ScoreManager.Instance.perfectScoreBonus : 0);

        string pointsString = pointsEarned > 0 ? $"<color=#2CA060><b>+{pointsEarned} Points</b></color>" : $"<color=#808080><b>+0 Points</b></color>";
        string baseMessage = isCorrect ? _quizData.correctFeedbackText : _quizData.incorrectFeedbackText;

        feedbackMessageText.text = baseMessage + $"\n\n{pointsString}";

        feedbackActionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue";
        feedbackActionButton.onClick.AddListener(() => EndGame(correctCount, capturedAnswers));
    }

    private void EndGame(int correctCount, Dictionary<string, string> capturedAnswers)
    {
        ScoreManager.Instance.SubmitDragDropScore(
            _quizData.id, correctCount, _quizData.questions.Count, capturedAnswers,
            onSuccess: () =>
            {
                _pointsEarned = (correctCount * ScoreManager.Instance.pointsPerCorrectDragDrop);
                if (correctCount == _quizData.questions.Count) _pointsEarned += ScoreManager.Instance.perfectScoreBonus;

                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(correctCount, _quizData.questions.Count, _pointsEarned);
            },
            onError: err =>
            {
                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(correctCount, _quizData.questions.Count, 0);
            }
        );
    }

    // Class-level variable for delayed callback
    private Dictionary<string, string> capturedAnswers = new();
}