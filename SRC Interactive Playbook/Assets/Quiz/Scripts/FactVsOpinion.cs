/*
    Author: Yeong Yu Seong
    Date Created: 25 May 2026
    Last Edited: 12 June 2026
    Description: This script is used to manage the Fact vs Opinion game.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FactVsOpinion : MonoBehaviour
{
    // ── Inspector: Game Panel ──────────────────────────────────
    [Header("UI References")]
    public TextMeshProUGUI statementText;
    public TextMeshProUGUI questionNumberText;
    public TextMeshProUGUI timerText;
    public Image mascotImage;
    public Button[] optionButtons;     // [0]=Fact, [1]=Opinion
    public GameObject gamePanel;

    // ── Inspector: Complete Panel ──────────────────────────────
    [Header("Quiz Complete Panel")]
    [SerializeField] private GameObject quizCompletePanel;
    [SerializeField] private TextMeshProUGUI completeScoreText;
    [SerializeField] private TextMeshProUGUI completePointsText;

    // ── Inspector: Mascot sprites ──────────────────────────────
    [Header("Mascot Assets")]
    [SerializeField] private Sprite[] mascotSprites;  // [0]=neutral, [1]=feedback

    // ── Runtime ────────────────────────────────────────────────
    private PlaybookQuiz _quizData;
    private int _questionIndex = 0;
    private int _correctCount = 0;
    private float _timer;
    private bool _isGameActive = false;
    private Dictionary<string, string> _capturedAnswers = new();
    private int _pointsEarned = 0;

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
        if (timerText != null) timerText.text = $"Time: {Mathf.Ceil(_timer)}";
        if (_timer <= 0f) EndGame();
    }

    // ══════════════════════════════════════════════════════════
    // Entry point — called by ScenarioManager.CheckForQuizAssessment()
    // ══════════════════════════════════════════════════════════
    private Action<int, int, int> _onCompleteCallback; // Update delegate type at the top of your script

    public void StartGame(PlaybookQuiz runtimeQuiz, Action<int, int, int> onComplete)
    {
        if (runtimeQuiz == null || runtimeQuiz.questions == null || runtimeQuiz.questions.Count == 0)
        {
            Debug.LogError("[FactVsOpinion] Invalid quiz data passed to StartGame.");
            //onComplete?.Invoke();
            return;
        }

        _onCompleteCallback = onComplete;
        gameObject.SetActive(true);

        _quizData = runtimeQuiz;
        _correctCount = 0;
        _questionIndex = 0;
        _timer = 60f;
        _pointsEarned = 0;
        _capturedAnswers.Clear();

        if (quizCompletePanel != null) quizCompletePanel.SetActive(false);
        if (gamePanel != null) gamePanel.SetActive(true);

        _isGameActive = true;
        DisplayQuestion();

        Debug.Log($"[FactVsOpinion] Started quiz '{runtimeQuiz.id}' with {runtimeQuiz.questions.Count} questions.");
    }

    // ══════════════════════════════════════════════════════════
    // Question display
    // ══════════════════════════════════════════════════════════

    private void DisplayQuestion()
    {
        var q = _quizData.questions[_questionIndex];
        if (statementText != null) statementText.text = q.prompt;
        if (questionNumberText != null) questionNumberText.text =
            $"Question: {_questionIndex + 1}/{_quizData.questions.Count}";

        if (mascotImage != null && mascotSprites.Length > 0)
            mascotImage.sprite = mascotSprites[0];

        foreach (Button btn in optionButtons)
        {
            btn.interactable = true;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = Color.white;
        }
    }

    // ══════════════════════════════════════════════════════════
    // Answer selection — wire Fact button: OnOptionSelected(true)
    //                    wire Opinion button: OnOptionSelected(false)
    // ══════════════════════════════════════════════════════════

    public void OnOptionSelected(bool selectedIsFact)
    {
        foreach (Button btn in optionButtons) btn.interactable = false;
        _isGameActive = false;

        var q = _quizData.questions[_questionIndex];
        string selectedChoiceId = selectedIsFact ? "choice_fact" : "choice_opinion";
        _capturedAnswers[q.id] = selectedChoiceId;

        bool isCorrect = selectedChoiceId == q.correctAnswerId;
        if (isCorrect) _correctCount++;

        // Visual feedback
        if (mascotImage != null && mascotSprites.Length > 1)
            mascotImage.sprite = mascotSprites[1];

        Color feedbackColor = isCorrect
            ? new Color(0.369f, 0.788f, 0.498f)   // #5EC97F
            : new Color(0.906f, 0.255f, 0.259f);   // #E74142

        foreach (Button btn in optionButtons)
        {
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = feedbackColor;
        }

        StartCoroutine(AdvanceAfterDelay());
    }

    private IEnumerator AdvanceAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        if (_questionIndex < _quizData.questions.Count - 1)
        {
            _questionIndex++;
            _isGameActive = true;
            DisplayQuestion();
        }
        else
        {
            EndGame();
        }
    }

    // ══════════════════════════════════════════════════════════
    // End game — submit score then show complete panel
    // ══════════════════════════════════════════════════════════

    private void EndGame()
    {
        _timer = 0f;
        _isGameActive = false;

        ScoreManager.Instance.SubmitFactsOpinionsScore(
            _quizData.id, _correctCount, _quizData.questions.Count, _capturedAnswers,
            onSuccess: () =>
            {
                int ptsPerCorrect = ScoreManager.Instance.pointsPerCorrectFactsOpinions;
                bool perfect = _correctCount == _quizData.questions.Count;
                _pointsEarned = (_correctCount * ptsPerCorrect) + (perfect ? ScoreManager.Instance.perfectScoreBonus : 0);

                gameObject.SetActive(false); // Hide the quiz panel
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, _pointsEarned); // Send data back!
            },
            onError: err =>
            {
                Debug.LogError($"[FactVsOpinion] Score save failed: {err}");
                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, 0); // Send data back even on error
            }
        );
    }
}