/*
    Author: Yeong Yu Seong
    Date Created: 26 May 2026
    Last Edited: 16 June 2026
    Description: This script is used to manage the Multiple Choice Questions game.
*/
using System;
using System.Collections.Generic;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MCQ : MonoBehaviour
{
    // ── Inspector: Game Panel ──────────────────────────────────
    [Header("UI References")]
    public TextMeshProUGUI statementText;
    public TextMeshProUGUI questionNumberText;
    public TextMeshProUGUI answerPanelQuestionNumber;
    public Image timerCountdown;        // fill image, 0–1
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;

    [Header("Panels")]
    public GameObject gamePanel;
    public GameObject answerPanel;

    [Header("Answer Panel UI")]
    public TextMeshProUGUI answerQuestionText;
    public TextMeshProUGUI answerText;

    // ── Inspector: Complete Panel ──────────────────────────────
    [Header("Quiz Complete Panel")]
    [SerializeField] private GameObject quizCompletePanel;
    [SerializeField] private TextMeshProUGUI completeScoreText;
    [SerializeField] private TextMeshProUGUI completePointsText;
    // ── Runtime ────────────────────────────────────────────────
    private PlaybookQuiz _quizData;
    private int _questionIndex = 0;
    private int _correctCount = 0;
    private float _timer;
    private const float TimerDuration = 60f;
    private bool _isGameActive = false;
    private Dictionary<string, string> _capturedAnswers = new();
    private int _pointsEarned = 0;
    private Action<int, int, int> _onCompleteCallback;

    // ══════════════════════════════════════════════════════════
    // Lifecycle
    // ══════════════════════════════════════════════════════════

    private void Awake()
    {
        if (quizCompletePanel != null) quizCompletePanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(false);
    }

    private void Update()
    {
        if (!_isGameActive) return;
        _timer -= Time.deltaTime;
        if (timerCountdown != null) timerCountdown.fillAmount = _timer / TimerDuration;
        if (_timer <= 0f) EndGame();
    }

    // ══════════════════════════════════════════════════════════
    // Entry point
    // ══════════════════════════════════════════════════════════

    public void StartGame(PlaybookQuiz runtimeQuiz, Action<int, int, int> onComplete)
    {
        if (runtimeQuiz == null || runtimeQuiz.questions == null || runtimeQuiz.questions.Count == 0)
        {
            Debug.LogError("[MCQ] Invalid quiz data passed to StartGame.");
            //onComplete?.Invoke();
            return;
        }
        _onCompleteCallback = onComplete;
        gameObject.SetActive(true);
        _quizData = runtimeQuiz;
        _correctCount = 0;
        _questionIndex = 0;
        _timer = TimerDuration;
        _pointsEarned = 0;
        _capturedAnswers.Clear();

        if (quizCompletePanel != null) quizCompletePanel.SetActive(false);
        if (answerPanel != null) answerPanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);

        _isGameActive = true;
        DisplayQuestion();

        Debug.Log($"[MCQ] Started quiz '{runtimeQuiz.id}' with {runtimeQuiz.questions.Count} questions.");
    }

    // ══════════════════════════════════════════════════════════
    // Question display
    // ══════════════════════════════════════════════════════════

    private void DisplayQuestion()
    {
        var q = _quizData.questions[_questionIndex];

        if (statementText != null) statementText.text = q.prompt;
        if (questionNumberText != null) questionNumberText.text =
            $"Q{_questionIndex + 1}/{_quizData.questions.Count}";
        if (timerCountdown != null) timerCountdown.fillAmount = 1f;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.choices.Count)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                if (optionTexts.Length > i) optionTexts[i].text = q.choices[i].text;

                int captured = i; // capture loop variable
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(
                    () => OnOptionSelected(q.choices[captured].id));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    // Answer selection
    // ══════════════════════════════════════════════════════════

    public void OnOptionSelected(string selectedChoiceId)
    {
        _isGameActive = false;
        foreach (Button btn in optionButtons) btn.interactable = false;

        var q = _quizData.questions[_questionIndex];
        bool isCorrect = selectedChoiceId == q.correctAnswerId;

        _capturedAnswers[q.id] = selectedChoiceId;
        if (isCorrect) _correctCount++;

        ShowAnswerPanel(q, isCorrect);
    }

    private void ShowAnswerPanel(QuizQuestion question, bool isCorrect)
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (answerPanel != null) answerPanel.SetActive(true);

        if (answerQuestionText != null) answerQuestionText.text = question.prompt;
        if (answerPanelQuestionNumber != null) answerPanelQuestionNumber.text =
            $"Q{_questionIndex + 1}/{_quizData.questions.Count}";

        string correctText = question.choices.Find(c => c.id == question.correctAnswerId)?.text ?? "—";
        string resultPrefix = isCorrect
            ? "<color=#5EC97F>Correct!</color>"
            : "<color=#E74142>Incorrect!</color>";

        if (answerText != null)
            answerText.text = $"{resultPrefix}\nCorrect answer: {correctText}" +
                              (string.IsNullOrEmpty(question.feedbackText)
                                   ? ""
                                   : $"\n{question.feedbackText}");
    }

    // Wire this to the "Next" button on the Answer Panel in the Inspector
    public void NextQuestion()
    {
        if (answerPanel != null) answerPanel.SetActive(false);

        if (_questionIndex < _quizData.questions.Count - 1)
        {
            _questionIndex++;
            _timer = TimerDuration;
            _isGameActive = true;
            if (gamePanel != null) gamePanel.SetActive(true);
            DisplayQuestion();
        }
        else
        {
            EndGame();
        }
    }

    // ══════════════════════════════════════════════════════════
    // End game
    // ══════════════════════════════════════════════════════════

    private void EndGame()
    {
        _timer = 0f;
        _isGameActive = false;

        ScoreManager.Instance.SubmitMCQScore(
            _quizData.id, _correctCount, _quizData.questions.Count, _capturedAnswers,
            onSuccess: () =>
            {
                int ptsEach = ScoreManager.Instance.pointsPerCorrectMCQ;
                bool perfect = _correctCount == _quizData.questions.Count;
                _pointsEarned = (_correctCount * ptsEach) + (perfect ? ScoreManager.Instance.perfectScoreBonus : 0);

                gameObject.SetActive(false); // Hide the quiz panel
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, _pointsEarned); // Send data back!
            },
            onError: err =>
            {
                Debug.LogError($"[MCQ] Score save failed: {err}");
                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, 0);
            }
        );
    }
}