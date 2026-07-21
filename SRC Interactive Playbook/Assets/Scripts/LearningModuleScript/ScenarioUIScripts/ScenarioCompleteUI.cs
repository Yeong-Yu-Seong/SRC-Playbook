using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;
public class ScenarioCompleteUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI scenarioTitleText;
    [SerializeField] private TextMeshProUGUI scenarioStatsText;
    [SerializeField] private TextMeshProUGUI quizStatsText;
    [SerializeField] private TextMeshProUGUI totalPointsText;
    [SerializeField] private GameObject perfectScoreBadge;
    [SerializeField] private GameObject newHighScoreBadge;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button replayButton;

    private Action _onHome;
    private Action _onReplay;

    private void Awake()
    {
        // Validate and wire homeButton
        if (homeButton != null)
            homeButton.onClick.AddListener(() => _onHome?.Invoke());
        else
            Debug.LogError("[ScenarioCompleteUI] homeButton is not assigned in the Inspector. " +
                           "Drag the Home button from this panel's Hierarchy into the slot.");

        // Validate and wire replayButton
        if (replayButton != null)
            replayButton.onClick.AddListener(() => _onReplay?.Invoke());
        else
            Debug.LogError("[ScenarioCompleteUI] replayButton is not assigned in the Inspector. " +
                           "Drag the Retry/Replay button from this panel's Hierarchy into the slot.");

    }

    public void Show(UserScenarioProgress scenarioProgress, PlaybookScenario scenario, int quizCorrect, int quizTotal, int quizPoints, bool isNewHighScore,
                     Action onHomeClicked, Action onReplayClicked)
    {
        _onHome = onHomeClicked;
        _onReplay = onReplayClicked;

        scenarioTitleText.text = scenario.title;

        // 1. Scenario Breakdown
        scenarioStatsText.text = $"Scenario Score: {scenarioProgress.correctAnswers}/{scenarioProgress.totalQuestions} (+{scenarioProgress.score} pts)";

        // 2. Quiz Breakdown
        if (quizTotal > 0)
        {
            quizStatsText.gameObject.SetActive(true);
            quizStatsText.text = $"Quiz Score: {quizCorrect}/{quizTotal} (+{quizPoints} pts)";
        }
        else
        {
            quizStatsText.gameObject.SetActive(false); // Hide if no quiz was played
        }

        // 3. Total Points
        int totalEarned = scenarioProgress.score + quizPoints;
        totalPointsText.text = $"Total Earned: +{totalEarned} pts";

        // Badge Logic
        bool perfectScenario = scenarioProgress.totalQuestions > 0 && scenarioProgress.correctAnswers == scenarioProgress.totalQuestions;
        bool perfectQuiz = quizTotal == 0 || (quizTotal > 0 && quizCorrect == quizTotal);
        if (perfectScoreBadge != null) perfectScoreBadge.SetActive(perfectScenario && perfectQuiz);
        if (newHighScoreBadge != null) newHighScoreBadge.SetActive(isNewHighScore);

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);
}