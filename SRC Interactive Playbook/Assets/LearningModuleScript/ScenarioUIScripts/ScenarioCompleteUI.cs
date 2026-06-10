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
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI correctAnswersText;
    [SerializeField] private GameObject perfectScoreBadge;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button replayButton;

    private Action _onHome;
    private Action _onReplay;

    private void Awake()
    {
        homeButton.onClick.AddListener(() => _onHome?.Invoke());
        replayButton.onClick.AddListener(() => _onReplay?.Invoke());
    }

    public void Show(UserScenarioProgress progress, PlaybookScenario scenario,
                     Action onHomeClicked, Action onReplayClicked)
    {
        _onHome = onHomeClicked;
        _onReplay = onReplayClicked;

        scenarioTitleText.text = scenario.title;
        scoreText.text = $"{progress.score} pts";
        correctAnswersText.text = $"{progress.correctAnswers} / {progress.totalQuestions} correct";

        bool perfect = progress.totalQuestions > 0 &&
                       progress.correctAnswers == progress.totalQuestions;
        if (perfectScoreBadge != null) perfectScoreBadge.SetActive(perfect);

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);
}