// ============================================================
// ScenarioManager.cs
// Drives the complete play session for one scenario.
// Attach to a persistent manager GameObject in the Scenario scene.
//
// Flow:
//   ScenarioListUI  →  (loads scene)  →  ScenarioManager.StartScenario()
//   ScenarioManager orchestrates:
//     NarrativePartUI  →  QuestionPartUI  →  FeedbackUI  →  repeat
//   At the end: ScenarioCompleteUI + saves progress + awards points.
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedCross.Playbook.Data;
using RedCross.Playbook.Firebase;
using RedCross.Playbook.UI;

namespace RedCross.Playbook.Scenario
{
    public class ScenarioManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────
        public static ScenarioManager Instance { get; private set; }

        // ── Inspector slots ────────────────────────────────────────
        [Header("UI Panels")]
        [SerializeField] private ScenarioIntroUI introUI;
        [SerializeField] private NarrativePartUI narrativeUI;
        [SerializeField] private QuestionPartUI questionUI;
        [SerializeField] private FeedbackUI feedbackUI;
        [SerializeField] private ScenarioCompleteUI completeUI;
        [SerializeField] private LoadingOverlayUI loadingUI;

        [Header("Background image (RawImage in Hierarchy)")]
        [SerializeField] private UnityEngine.UI.RawImage backgroundImage;

        // ── Runtime state ──────────────────────────────────────────
        private PlaybookScenario _scenario;
        private int _currentPartIndex = 0;
        private int _correctAnswers = 0;
        private int _totalQuestions = 0;
        private int _pointsEarned = 0;
        private List<string> _answeredChoiceIds = new();
        private bool _isRunning = false;

        // ── Events ─────────────────────────────────────────────────
        public event Action<int> OnPointsAwarded;
        public event Action<UserScenarioProgress> OnScenarioCompleted;

        // ══════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════

        private void Awake()
        {
            // Simple singleton — ScenarioManager lives only in ScenarioScene.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Validate all slots are wired to scene instances
            ValidateReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ══════════════════════════════════════════════════════════
        // Reference validation — catches the prefab-asset mistake
        // ══════════════════════════════════════════════════════════

        private void ValidateReferences()
        {
            CheckRef(introUI, "Intro UI");
            CheckRef(narrativeUI, "Narrative UI");
            CheckRef(questionUI, "Question UI");
            CheckRef(feedbackUI, "Feedback UI");
            CheckRef(completeUI, "Complete UI");
            CheckRef(loadingUI, "Loading UI");
        }

        private void CheckRef(MonoBehaviour mb, string label)
        {
            if (mb == null)
            {
                Debug.LogError($"[ScenarioManager] {label} is not assigned. " +
                               "Drag the GameObject from the HIERARCHY (not the Project panel) " +
                               "into the ScenarioManager Inspector slot.");
                return;
            }
            if (!mb.gameObject.scene.IsValid())
            {
                Debug.LogError($"[ScenarioManager] {label} looks like a prefab asset, " +
                               "not a scene instance. Open the ScenarioScene Hierarchy, " +
                               $"find the {mb.gameObject.name} GameObject, and drag THAT " +
                               "into the slot — not the asset in the Project panel.");
            }
        }

        // ══════════════════════════════════════════════════════════
        // Entry points
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Called by ScenarioSceneBootstrapper with the scenario id.
        /// </summary>
        public void StartScenario(string scenarioId)
        {
            if (_isRunning)
            {
                Debug.LogWarning("[ScenarioManager] StartScenario called while already running. Ignoring.");
                return;
            }

            if (string.IsNullOrEmpty(scenarioId))
            {
                Debug.LogError("[ScenarioManager] StartScenario called with empty scenarioId.");
                return;
            }

            ResetState();
            loadingUI.Show("Loading scenario…");

            FirebaseScenarioService.Instance.FetchScenario(
                scenarioId,
                onComplete: scenario =>
                {
                    _scenario = scenario;
                    CountQuestions();
                    loadingUI.Hide();
                    ShowIntro();
                },
                onError: err =>
                {
                    loadingUI.Hide();
                    Debug.LogError($"[ScenarioManager] Failed to load '{scenarioId}': {err}");
                }
            );
        }

        /// <summary>
        /// No-argument version — reads the pending ID from the bootstrapper.
        /// Assign this to ScenarioIntroUI's Enter button OnClick() in the Inspector
        /// if you need a direct Inspector binding (not recommended — use the code path).
        /// </summary>
        public void StartScenarioFromPending()
        {
            StartScenario(ScenarioSceneBootstrapper.PendingScenarioId);
        }

        // ══════════════════════════════════════════════════════════
        // Intro screen
        // ══════════════════════════════════════════════════════════

        private void ShowIntro()
        {
            UIManager.Instance.GetNavBar()?.ShowNavBar();
            introUI.Show(_scenario, onEnterClicked: () =>
            {
                introUI.Hide();
                _isRunning = true;
                PlayCurrentPart();
            });
        }

        // ══════════════════════════════════════════════════════════
        // Scene part playback
        // ══════════════════════════════════════════════════════════

        private void PlayCurrentPart()
        {
            if (_currentPartIndex >= _scenario.sceneParts.Count)
            {
                FinishScenario();
                return;
            }

            var part = _scenario.sceneParts[_currentPartIndex];
            LoadBackground(part.backgroundImageUrl);

            switch (part.type)
            {
                case ScenePartType.Narrative: PlayNarrativePart(part); break;
                case ScenePartType.Question: PlayQuestionPart(part); break;
                default:
                    Debug.LogWarning($"[ScenarioManager] Unknown part type at index {_currentPartIndex}. Skipping.");
                    _currentPartIndex++;
                    PlayCurrentPart();
                    break;
            }
        }

        private void PlayNarrativePart(ScenePart part)
        {
            narrativeUI.Show(part, onContinue: () =>
            {
                narrativeUI.Hide();
                _currentPartIndex++;
                PlayCurrentPart();
            });
        }

        private void PlayQuestionPart(ScenePart part)
        {
            questionUI.Show(part, onChoiceSelected: choice =>
            {
                questionUI.Hide();
                HandleChoiceSelected(choice);
            });
        }

        private void HandleChoiceSelected(Choice choice)
        {
            _answeredChoiceIds.Add(choice.id);

            if (choice.isCorrect)
            {
                _correctAnswers++;
                _pointsEarned += _scenario.pointsPerCorrect;
                OnPointsAwarded?.Invoke(_scenario.pointsPerCorrect);
            }

            feedbackUI.Show(choice, onDismissed: () =>
            {
                feedbackUI.Hide();
                _currentPartIndex++;
                PlayCurrentPart();
            });
        }

        // ══════════════════════════════════════════════════════════
        // Completion
        // ══════════════════════════════════════════════════════════

        private void FinishScenario()
        {
            _isRunning = false;
            _pointsEarned += _scenario.pointsOnCompletion;
            OnPointsAwarded?.Invoke(_scenario.pointsOnCompletion);

            var progress = new UserScenarioProgress
            {
                scenarioId = _scenario.id,
                completed = true,
                score = _pointsEarned,
                correctAnswers = _correctAnswers,
                totalQuestions = _totalQuestions,
                completedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                answeredChoiceIds = _answeredChoiceIds
            };

            string userId = PlayerPrefs.GetString("userId", "guest");
            FirebaseScenarioService.Instance.SaveUserProgress(userId, progress);
            OnScenarioCompleted?.Invoke(progress);

            completeUI.Show(progress, _scenario,
                onHomeClicked: () => UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene"),
                onReplayClicked: () =>
                {
                    string id = _scenario.id;
                    ResetState();
                    StartScenario(id);
                });
        }

        // ══════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════

        private void ResetState()
        {
            _currentPartIndex = 0;
            _correctAnswers = 0;
            _totalQuestions = 0;
            _pointsEarned = 0;
            _isRunning = false;
            _answeredChoiceIds = new List<string>();
            completeUI.Hide();
        }

        private void CountQuestions()
        {
            _totalQuestions = 0;
            foreach (var p in _scenario.sceneParts)
                if (p.type == ScenePartType.Question) _totalQuestions++;
        }

        private void LoadBackground(string url)
        {
            if (string.IsNullOrEmpty(url) || backgroundImage == null) return;
            var tex = Resources.Load<Texture2D>(url);
            if (tex != null) { backgroundImage.texture = tex; return; }
            StartCoroutine(LoadBackgroundFromUrl(url));
        }

        private IEnumerator LoadBackgroundFromUrl(string url)
        {
            using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                backgroundImage.texture =
                    UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
        }
    }
}