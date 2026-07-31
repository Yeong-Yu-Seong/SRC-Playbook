using System;
using System.Collections.Generic;
using System.Linq;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DNDRanking : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gamePanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;
    public Button submitButton;

    [Header("Ranking Board Elements")]
    public Transform slotsContainer;
    public GameObject rankSlotPrefab;
    public Transform optionsContainer;
    public GameObject draggablePrefab;

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackTitleText;
    public TextMeshProUGUI feedbackMessageText;
    public Button feedbackActionButton; // "Try Again" or "Continue"

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
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        BuildRankingBoard();
    }

    private void BuildRankingBoard()
    {
        ClearBoard();
        if (submitButton != null) submitButton.interactable = true;

        // 1. Build the Slots (1, 2, 3, 4)
        for (int i = 0; i < _quizData.questions.Count; i++)
        {
            var q = _quizData.questions[i];
            GameObject newSlot = Instantiate(rankSlotPrefab, slotsContainer);
            _spawnedObjects.Add(newSlot);

            DNDRankSlotUI slotUI = newSlot.GetComponent<DNDRankSlotUI>();
            if (slotUI != null)
            {
                slotUI.rankNumberText.text = (i + 1).ToString();
                slotUI.dropSlot.questionId = q.id; // Assign ID to validate later
            }
        }

        // 2. Build the Draggable Options (Shuffled)
        var shuffledQuestions = _quizData.questions.OrderBy(x => Guid.NewGuid()).ToList();

        foreach (var q in shuffledQuestions)
        {
            // For ranking, we just map the correct answer text directly from the prompt or a choice
            GameObject newOption = Instantiate(draggablePrefab, optionsContainer);
            _spawnedObjects.Add(newOption);

            DraggableOption dragScript = newOption.GetComponentInChildren<DraggableOption>();
            if (dragScript != null)
            {
                dragScript.choiceId = q.correctAnswerId;
            }

            TextMeshProUGUI optionText = newOption.GetComponentInChildren<TextMeshProUGUI>();
            if (optionText != null)
            {
                // Assuming the text we want to drag is stored in the first choice
                optionText.text = q.choices[0].text;
            }
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

        foreach (Transform slotTransform in slotsContainer)
        {
            DNDRankSlotUI slotUI = slotTransform.GetComponent<DNDRankSlotUI>();
            if (slotUI == null) continue;

            string questionId = slotUI.dropSlot.questionId;
            var q = _quizData.questions.Find(x => x.id == questionId);

            DraggableOption droppedOption = slotUI.dropSlot.GetComponentInChildren<DraggableOption>();
            string selectedChoiceId = droppedOption != null ? droppedOption.choiceId : "";

            capturedAnswers[questionId] = selectedChoiceId;

            if (q != null && selectedChoiceId == q.correctAnswerId)
            {
                correctCount++;
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
            feedbackTitleText.text = "Excellent!";
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
        feedbackActionButton.onClick.AddListener(() => EndGame(capturedAnswers, correctCount));
    }

    private void EndGame(Dictionary<string, string> capturedAnswers, int correctCount)
    {
        int totalCount = _quizData.questions.Count;

        ScoreManager.Instance.SubmitDragDropScore(
            _quizData.id, correctCount, totalCount, capturedAnswers,
            onSuccess: () =>
            {
                _pointsEarned = (correctCount * ScoreManager.Instance.pointsPerCorrectDragDrop);
                if (correctCount == totalCount) _pointsEarned += ScoreManager.Instance.perfectScoreBonus;

                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(correctCount, totalCount, _pointsEarned);
            },
            onError: err =>
            {
                Debug.LogError($"[DNDRanking] Score save failed: {err}");
                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(correctCount, totalCount, 0);
            }
        );
    }
}