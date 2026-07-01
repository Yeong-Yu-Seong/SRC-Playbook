// ============================================================
// ScenarioUI.cs
// All UI panel MonoBehaviours for the scenario player.
// Each class maps to one UI panel prefab in the scene.
// Split into separate files in production; kept here for clarity.
// ============================================================

/*
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;

namespace RedCross.Playbook.UI
{
    // ══════════════════════════════════════════════════════════════
    // LoadingOverlayUI
    // ══════════════════════════════════════════════════════════════
    public class LoadingOverlayUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI messageText;

        public void Show(string message = "Loading…")
        {
            messageText.text = message;
            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    // ScenarioIntroUI — Exhibit entry card (Image 1 style)
    // ══════════════════════════════════════════════════════════════
    public class ScenarioIntroUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI exhibitNumberText;
        [SerializeField] private RawImage thumbnailImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI outlineDescriptionText;
        [SerializeField] private Button enterButton;

        private Action _onEnterClicked;

        private void Awake()
        {
            enterButton.onClick.AddListener(() => _onEnterClicked?.Invoke());
        }

        public void Show(PlaybookScenario scenario, Action onEnterClicked)
        {
            _onEnterClicked = onEnterClicked;
            exhibitNumberText.text = scenario.exhibitNumber;
            titleText.text = scenario.title;
            outlineDescriptionText.text = scenario.outlineDescription;

            var tex = Resources.Load<Texture2D>(scenario.thumbnailUrl);
            if (tex != null) thumbnailImage.texture = tex;

            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    // NarrativePartUI — scene text with tap-to-continue or auto-advance
    // ══════════════════════════════════════════════════════════════
    public class NarrativePartUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI narrativeText;
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject tapToContinueHint;
        [SerializeField] private Button fullScreenTapZone;

        private Action _onContinue;
        private Coroutine _autoAdvanceCoroutine;

        private void Awake()
        {
            continueButton.onClick.AddListener(HandleContinue);
            if (fullScreenTapZone != null)
                fullScreenTapZone.onClick.AddListener(HandleContinue);
        }

        public void Show(ScenePart part, Action onContinue)
        {
            _onContinue = onContinue;
            narrativeText.text = part.narrativeText;

            bool autoAdvance = part.displayDurationSecs > 0;
            continueButton.gameObject.SetActive(!autoAdvance);
            if (tapToContinueHint != null) tapToContinueHint.SetActive(!autoAdvance);

            if (autoAdvance)
            {
                if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = StartCoroutine(AutoAdvance(part.displayDurationSecs));
            }

            panel.SetActive(true);
        }

        public void Hide()
        {
            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
                _autoAdvanceCoroutine = null;
            }
            panel.SetActive(false);
        }

        private void HandleContinue()
        {
            if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
            _onContinue?.Invoke();
        }

        private IEnumerator AutoAdvance(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _onContinue?.Invoke();
        }
    }

    // ══════════════════════════════════════════════════════════════
    // QuestionPartUI — question + 2 choice buttons (Image 2 style)
    // ══════════════════════════════════════════════════════════════
    public class QuestionPartUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI contextHintText;
        [SerializeField] private GameObject contextHintBubble;
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private Transform choicesContainer;

        [Header("Prefab")]
        [SerializeField] private GameObject choiceButtonPrefab;

        private Action<Choice> _onChoiceSelected;
        private readonly List<GameObject> _spawnedButtons = new();

        public void Show(ScenePart part, Action<Choice> onChoiceSelected)
        {
            _onChoiceSelected = onChoiceSelected;

            bool hasHint = !string.IsNullOrEmpty(part.contextHintText);
            contextHintBubble.SetActive(hasHint);
            if (hasHint) contextHintText.text = part.contextHintText;

            questionText.text = part.questionText;

            foreach (var b in _spawnedButtons) Destroy(b);
            _spawnedButtons.Clear();

            foreach (var choice in part.choices)
            {
                var go = Instantiate(choiceButtonPrefab, choicesContainer);
                var btn = go.GetComponent<ChoiceButtonUI>();
                btn.Initialise(choice, OnChoiceButtonClicked);
                _spawnedButtons.Add(go);
            }

            panel.SetActive(true);
        }

        public void Hide() => panel.SetActive(false);

        private void OnChoiceButtonClicked(Choice choice)
        {
            foreach (var go in _spawnedButtons)
                go.GetComponent<ChoiceButtonUI>()?.SetInteractable(false);

            _onChoiceSelected?.Invoke(choice);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // ChoiceButtonUI — one answer button inside QuestionPartUI
    // ══════════════════════════════════════════════════════════════
    public class ChoiceButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI choiceText;

        private Choice _choice;
        private Action<Choice> _onClicked;

        private void Awake()
        {
            button.onClick.AddListener(() => _onClicked?.Invoke(_choice));
        }

        public void Initialise(Choice choice, Action<Choice> onClicked)
        {
            _choice = choice;
            _onClicked = onClicked;
            labelText.text = choice.label;
            choiceText.text = choice.text;
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;
    }

    // ══════════════════════════════════════════════════════════════
    // FeedbackUI — result overlay after a choice (Image 3 style)
    // ══════════════════════════════════════════════════════════════
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

    // ══════════════════════════════════════════════════════════════
    // ScenarioCompleteUI — summary screen at scenario end
    // ══════════════════════════════════════════════════════════════
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
}
*/