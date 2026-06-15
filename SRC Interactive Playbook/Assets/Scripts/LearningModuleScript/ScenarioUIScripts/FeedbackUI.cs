using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;

public class FeedbackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject correctBanner;
    [SerializeField] private GameObject incorrectBanner;
    [SerializeField] private GameObject urgentBanner;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private Transform tagsContainer;
    [SerializeField] private GameObject tagPillPrefab;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button fullScreenTapZone;

    private Action _onDismissed;
    private readonly List<GameObject> _spawnedTags = new();

    private void Awake()
    {
        continueButton.onClick.AddListener(Dismiss);
        if (fullScreenTapZone != null) fullScreenTapZone.onClick.AddListener(Dismiss);
    }

    public void Show(Choice choice, Action onDismissed)
    {
        _onDismissed = onDismissed;

        bool isCorrect = choice.isCorrect;
        bool isUrgent = choice.feedbackType == FeedbackType.Urgent;

        correctBanner.SetActive(isCorrect && !isUrgent);
        incorrectBanner.SetActive(!isCorrect && !isUrgent);
        urgentBanner.SetActive(isUrgent);

        tipText.text = choice.feedbackText;

        foreach (var t in _spawnedTags) Destroy(t);
        _spawnedTags.Clear();

        foreach (var tag in choice.feedbackTags)
        {
            var go = Instantiate(tagPillPrefab, tagsContainer);
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = tag;
            _spawnedTags.Add(go);
        }

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    private void Dismiss() => _onDismissed?.Invoke();
}