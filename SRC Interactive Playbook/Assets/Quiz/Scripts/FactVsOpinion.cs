/*
    Author: Yeong Yu Seong
    Date Created: 25 May 2026
    Last Edited: 23 July 2026 (Kwek Sin En)
    Description: This script is used to manage the Fact vs Opinion game.
*/
using System;
using System.Collections.Generic;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FactVsOpinion : MonoBehaviour
{
    // ── Inspector: UI References ──────────────────────────────
    [Header("Header Text (From Firebase)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;

    [Header("Question UI")]
    public TextMeshProUGUI statementText;
    public TextMeshProUGUI questionNumberText;
    public Image mascotImage;
    public Button[] optionButtons;     // [0]=Fact/Effective, [1]=Opinion/NeedsImprovement
    public GameObject gamePanel;

    [Header("Mascot Assets")]
    [SerializeField] private Sprite[] mascotSprites;  // [0]=neutral, [1]=feedback

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackTitleText;
    public TextMeshProUGUI feedbackMessageText;
    public Button feedbackActionButton;

    // ── Runtime ────────────────────────────────────────────────
    private PlaybookQuiz _quizData;
    private int _questionIndex = 0;
    private int _correctCount = 0;
    private Dictionary<string, string> _capturedAnswers = new();
    private bool _isGameActive = false;
    private Action<int, int, int> _onCompleteCallback;

    private void Awake()
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    public void StartGame(PlaybookQuiz runtimeQuiz, Action<int, int, int> onComplete)
    {
        if (runtimeQuiz == null || runtimeQuiz.questions == null || runtimeQuiz.questions.Count == 0) return;

        _onCompleteCallback = onComplete;
        _quizData = runtimeQuiz;
        _correctCount = 0;
        _questionIndex = 0;
        _capturedAnswers.Clear();

        // Map Firebase Headers
        if (titleText != null) titleText.text = _quizData.title;
        if (instructionText != null) instructionText.text = _quizData.instructionText;

        gameObject.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(true);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        _isGameActive = true;
        NextQuestion();
    }

    private void DisplayQuestion()
    {
        if (mascotImage != null && mascotSprites.Length > 0)
            mascotImage.sprite = mascotSprites[0]; // Neutral

        var q = _quizData.questions[_questionIndex];
        if (statementText != null) statementText.text = q.prompt;
        if (questionNumberText != null) questionNumberText.text = $"Q{_questionIndex + 1}/{_quizData.questions.Count}";

        for (int i = 0; i < optionButtons.Length; i++)
        {
            // Reset disabled color to default before re-enabling
            ColorBlock cb = optionButtons[i].colors;
            cb.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            optionButtons[i].colors = cb;

            optionButtons[i].interactable = true;

            optionButtons[i].onClick.RemoveAllListeners();
            string choiceId = q.choices[i].id;
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(choiceId));
        }
    }

    private void OnOptionSelected(string selectedChoiceId)
    {
        if (!_isGameActive) return;
        _isGameActive = false; // Pause interactions

        var q = _quizData.questions[_questionIndex];
        bool isCorrect = (selectedChoiceId == q.correctAnswerId);
        _capturedAnswers[q.id] = selectedChoiceId;

        if (isCorrect) _correctCount++;

        Color feedbackColor = isCorrect
            ? new Color(0.369f, 0.788f, 0.498f)
            : new Color(0.906f, 0.255f, 0.259f);

        foreach (Button btn in optionButtons)
        {
            btn.interactable = false;
            ColorBlock cb = btn.colors;
            cb.disabledColor = feedbackColor;
            btn.colors = cb;
        }

        if (mascotImage != null && mascotSprites.Length > 1)
            mascotImage.sprite = mascotSprites[1];

        ShowFeedback(isCorrect, q);
    }

    private void ShowFeedback(bool isCorrect, QuizQuestion q)
    {
        feedbackPanel.SetActive(true);
        feedbackActionButton.onClick.RemoveAllListeners();

        feedbackTitleText.text = isCorrect ? "Correct!" : "Incorrect.";

        string fallbackText = isCorrect ? "Well done!" : "Not quite.";
        feedbackMessageText.text = !string.IsNullOrEmpty(q.feedbackText) ? q.feedbackText : fallbackText;

        feedbackActionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue";
        feedbackActionButton.onClick.AddListener(() =>
        {
            feedbackPanel.SetActive(false);
            if (_questionIndex < _quizData.questions.Count - 1)
            {
                _questionIndex++;
                NextQuestion();
            }
            else
            {
                EndGame();
            }
        });
    }

    private void NextQuestion()
    {
        _isGameActive = true;
        DisplayQuestion();
    }

    private void EndGame()
    {
        _isGameActive = false;

        ScoreManager.Instance.SubmitFactsOpinionsScore(
            _quizData.id, _correctCount, _quizData.questions.Count, _capturedAnswers,
            onSuccess: () =>
            {
                int ptsPerCorrect = ScoreManager.Instance.pointsPerCorrectFactsOpinions;
                bool perfect = _correctCount == _quizData.questions.Count;
                int pointsEarned = (_correctCount * ptsPerCorrect) + (perfect ? ScoreManager.Instance.perfectScoreBonus : 0);

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