using System;
using System.Collections.Generic;
using System.Linq;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour
{
    [Header("Header Text (From Firebase)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;

    [Header("UI References")]
    public GameObject gamePanel;
    public Button submitButton;

    [Header("Matching Board Elements")]
    public Transform rowsContainer;
    public GameObject matchRowPrefab;
    public Transform optionsContainer;
    public GameObject draggablePrefab;

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackTitleText;
    public TextMeshProUGUI feedbackMessageText;
    public Button feedbackActionButton;

    // ── Runtime ────────────────────────────────────────────────
    private PlaybookQuiz _quizData;
    private int _correctCount = 0;
    private Dictionary<string, string> _capturedAnswers = new();
    private Action<int, int, int> _onCompleteCallback;
    private List<GameObject> _spawnedObjects = new();

    private void Awake()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitAnswer);
    }

    public void StartGame(PlaybookQuiz runtimeQuiz, Action<int, int, int> onComplete)
    {
        if (runtimeQuiz == null || runtimeQuiz.questions == null || runtimeQuiz.questions.Count == 0) return;

        _onCompleteCallback = onComplete;
        _quizData = runtimeQuiz;
        _correctCount = 0;
        _capturedAnswers.Clear();

        if (titleText != null) titleText.text = _quizData.title;
        if (instructionText != null) instructionText.text = _quizData.instructionText;

        gameObject.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(true);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        BuildMatchingBoard();
    }

    private void BuildMatchingBoard()
    {
        ClearBoard();
        if (submitButton != null) submitButton.interactable = true;

        Dictionary<string, QuizChoice> choicePool = new Dictionary<string, QuizChoice>();

        foreach (var q in _quizData.questions)
        {
            GameObject newRow = Instantiate(matchRowPrefab, rowsContainer);
            _spawnedObjects.Add(newRow);

            DNDRowUI rowUI = newRow.GetComponent<DNDRowUI>();
            if (rowUI != null)
            {
                rowUI.poorFeedbackText.text = q.prompt;
                rowUI.dropSlot.questionId = q.id;
            }

            foreach (var choice in q.choices)
            {
                if (!choicePool.ContainsKey(choice.id))
                    choicePool.Add(choice.id, choice);
            }
        }

        var shuffledChoices = choicePool.Values.OrderBy(x => Guid.NewGuid()).ToList();

        foreach (var choice in shuffledChoices)
        {
            GameObject newOption = Instantiate(draggablePrefab, optionsContainer);
            _spawnedObjects.Add(newOption);

            DraggableOption dragScript = newOption.GetComponentInChildren<DraggableOption>();
            if (dragScript != null) dragScript.choiceId = choice.id;

            TextMeshProUGUI optionText = newOption.GetComponentInChildren<TextMeshProUGUI>();
            if (optionText != null) optionText.text = choice.text;
        }
    }

    private void ClearBoard()
    {
        foreach (var obj in _spawnedObjects) { if (obj != null) Destroy(obj); }
        _spawnedObjects.Clear();
        foreach (Transform child in rowsContainer) Destroy(child.gameObject);
        foreach (Transform child in optionsContainer) Destroy(child.gameObject);
    }

    private void OnSubmitAnswer()
    {
        if (submitButton != null) submitButton.interactable = false;
        _correctCount = 0;
        bool allCorrect = true;

        foreach (Transform row in rowsContainer)
        {
            DNDRowUI rowUI = row.GetComponent<DNDRowUI>();
            if (rowUI == null) continue;

            string questionId = rowUI.dropSlot != null ? rowUI.dropSlot.questionId : null;
            if (string.IsNullOrEmpty(questionId)) continue;

            var q = _quizData.questions.Find(x => x.id == questionId);
            string selectedChoiceId = "";

            DraggableOption droppedOption = rowUI.dropSlot.GetComponentInChildren<DraggableOption>();
            if (droppedOption != null) selectedChoiceId = droppedOption.choiceId;

            _capturedAnswers[questionId] = selectedChoiceId;
            bool isCorrect = (q != null && selectedChoiceId == q.correctAnswerId);

            if (isCorrect) _correctCount++;
            else allCorrect = false;

            Image slotImage = rowUI.dropSlot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.color = isCorrect
                    ? new Color(0.369f, 0.788f, 0.498f)
                    : new Color(0.906f, 0.255f, 0.259f);
            }
        }

        ShowFeedback(allCorrect);
    }

    private void ShowFeedback(bool allCorrect)
    {
        feedbackPanel.SetActive(true);
        feedbackActionButton.onClick.RemoveAllListeners();

        if (allCorrect)
        {
            feedbackTitleText.text = "✅ Excellent!";
            feedbackMessageText.text = _quizData.correctFeedbackText;

            feedbackActionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue";
            feedbackActionButton.onClick.AddListener(() => EndGame());
        }
        else
        {
            feedbackTitleText.text = "❌ Not quite.";
            feedbackMessageText.text = _quizData.incorrectFeedbackText;

            feedbackActionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Try Again";
            feedbackActionButton.onClick.AddListener(() =>
            {
                feedbackPanel.SetActive(false);
                BuildMatchingBoard();
            });
        }
    }

    private void EndGame()
    {
        ScoreManager.Instance.SubmitDragDropScore(
            _quizData.id, _correctCount, _quizData.questions.Count, _capturedAnswers,
            onSuccess: () =>
            {
                int ptsEach = ScoreManager.Instance.pointsPerCorrectDragDrop;
                bool perfect = _correctCount == _quizData.questions.Count;
                int pointsEarned = (_correctCount * ptsEach) + (perfect ? ScoreManager.Instance.perfectScoreBonus : 0);

                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, pointsEarned);
            },
            onError: err =>
            {
                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, 0);
            }
        );
    }
}