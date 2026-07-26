/*
    Author: Yeong Yu Seong
    Date Created: 26 May 2026
    Last Edited: 26 July 2026 (by Kwek Sin En)
    Description: This script is used to manage the Multiple Choice Questions game.
*/
using RedCross.Playbook.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MCQ : MonoBehaviour
{
    // ── Inspector: UI References ──────────────────────────────
    [Header("Header Text (From Firebase)")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;

    [Header("Question UI")]
    public TextMeshProUGUI statementText;
    public TextMeshProUGUI questionNumberText;
    public Image timerCountdown;        // fill image, 0–1
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public Button submitButton;         // for MultiMCQ only

    [Header("Panels")]
    public GameObject gamePanel;

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackTitleText;
    public TextMeshProUGUI feedbackMessageText;
    public Button feedbackActionButton;

    // ── Runtime ────────────────────────────────────────────────
    private PlaybookQuiz _quizData;
    private int _questionIndex = 0;
    private int _correctCount = 0;
    private float _timer;
    private const float TimerDuration = 60f;
    private bool _isGameActive = false;
    private Dictionary<string, string> _capturedAnswers = new();
    private string _quizType;
    private Action<int, int, int> _onCompleteCallback;

    private List<string> _currentMultiSelections = new();

    private ColorBlock[] _originalColorBlocks;

    private void Awake()
    {
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }
    private void Update()
    {
        if (!_isGameActive) return;
        _timer -= Time.deltaTime;
        if (timerCountdown != null) timerCountdown.fillAmount = _timer / TimerDuration;
        if (_timer <= 0f) EndGame();
    }

    public void StartGame(PlaybookQuiz runtimeQuiz, Action<int, int, int> onComplete)
    {
        if (runtimeQuiz == null || runtimeQuiz.questions == null || runtimeQuiz.questions.Count == 0) return;

        _onCompleteCallback = onComplete;
        _quizData = runtimeQuiz;
        _correctCount = 0;
        _questionIndex = 0;
        _timer = TimerDuration;
        _capturedAnswers.Clear();
        _quizType = runtimeQuiz.type;

        // Map Firebase Headers
        if (titleText != null) titleText.text = _quizData.title;
        if (instructionText != null) instructionText.text = _quizData.instructionText;

        gameObject.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(true);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
        if (submitButton != null) submitButton.gameObject.SetActive(_quizType == "MultiMCQ");

        _isGameActive = true;
        DisplayQuestion();
    }

    private void DisplayQuestion()
    {
        _currentMultiSelections.Clear();
        if (submitButton != null) submitButton.interactable = true;

        var q = _quizData.questions[_questionIndex];
        if (statementText != null) statementText.text = q.prompt;
        if (questionNumberText != null) questionNumberText.text = $"Q{_questionIndex + 1}/{_quizData.questions.Count}";
        if (timerCountdown != null) timerCountdown.fillAmount = 1f;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < q.choices.Count)
            {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                optionTexts[i].text = q.choices[i].text;

                // Reset button to its beautiful original color
                if (_originalColorBlocks != null && i < _originalColorBlocks.Length)
                {
                    optionButtons[i].colors = _originalColorBlocks[i];
                }

                optionButtons[i].onClick.RemoveAllListeners();
                string choiceId = q.choices[i].id;
                Button btnRef = optionButtons[i];
                int btnIndex = i; // Safely capture the index for the listener

                optionButtons[i].onClick.AddListener(() => OnOptionSelected(choiceId, btnRef, btnIndex));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionSelected(string selectedChoiceId, Button clickedBtn, int btnIndex)
    {
        if (_quizType == "MultiMCQ")
        {
            ColorBlock cb = clickedBtn.colors;
            ColorBlock originalCb = _originalColorBlocks[btnIndex];

            // Toggle selection
            if (_currentMultiSelections.Contains(selectedChoiceId))
            {
                _currentMultiSelections.Remove(selectedChoiceId);
                // Restore your original bright color
                clickedBtn.colors = originalCb;
            }
            else
            {
                _currentMultiSelections.Add(selectedChoiceId);

                // Darken your original color by 50% so it looks "pressed in"
                Color darkenedColor = originalCb.normalColor * 0.5f;
                darkenedColor.a = 1f; // Ensure it stays fully opaque
                cb.normalColor = darkenedColor;
                cb.selectedColor = darkenedColor;

                clickedBtn.colors = cb;
            }
        }
        else
        {
            // Single MCQ mode
            _isGameActive = false;
            foreach (var btn in optionButtons) btn.interactable = false;

            var q = _quizData.questions[_questionIndex];
            _capturedAnswers[q.id] = selectedChoiceId;

            bool isCorrect = selectedChoiceId == q.correctAnswerId;
            if (isCorrect) _correctCount++;

            ShowFeedback(isCorrect, q);
        }
    }

    private void OnSubmitClicked()
    {
        if (_quizType != "MultiMCQ") return;

        _isGameActive = false;
        if (submitButton != null) submitButton.interactable = false;
        foreach (var btn in optionButtons) btn.interactable = false;

        var q = _quizData.questions[_questionIndex];

        // Split the correct string and sort both lists to compare them accurately 
        // (So if they select 2 then 1, it still matches "1,2")
        var correctList = q.correctAnswerId.Split(',').Select(s => s.Trim()).OrderBy(s => s).ToList();
        var userList = _currentMultiSelections.OrderBy(s => s).ToList();

        bool isCorrect = correctList.SequenceEqual(userList);
        if (isCorrect) _correctCount++;

        string finalAnswerStr = string.Join(",", userList);
        _capturedAnswers[q.id] = finalAnswerStr;

        ShowFeedback(isCorrect, q);
    }

    private void ShowFeedback(bool isCorrect, QuizQuestion q)
    {
        feedbackPanel.SetActive(true);
        feedbackActionButton.onClick.RemoveAllListeners();

        feedbackTitleText.text = isCorrect ? "✅ Correct!" : "❌ Incorrect.";

        string fallbackText = isCorrect ? "Well done!" : "Not quite.";
        feedbackMessageText.text = !string.IsNullOrEmpty(q.feedbackText) ? q.feedbackText : fallbackText;

        feedbackActionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Continue";
        feedbackActionButton.onClick.AddListener(() =>
        {
            feedbackPanel.SetActive(false);
            if (_questionIndex < _quizData.questions.Count - 1)
            {
                _questionIndex++;
                _timer = TimerDuration;
                _isGameActive = true;
                DisplayQuestion();
            }
            else
            {
                EndGame();
            }
        });
    }

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
                int pointsEarned = (_correctCount * ptsEach) + (perfect ? ScoreManager.Instance.perfectScoreBonus : 0);

                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, pointsEarned);
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