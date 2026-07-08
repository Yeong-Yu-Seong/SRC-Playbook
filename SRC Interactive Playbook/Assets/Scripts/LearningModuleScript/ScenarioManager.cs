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

using RedCross.Playbook.Data;
using RedCross.Playbook.Firebase;
using RedCross.Playbook.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        [Header("Background")]
        [SerializeField] private UnityEngine.UI.RawImage backgroundImage;

        // ── Runtime state ──────────────────────────────────────────
        private PlaybookScenario _scenario;
        private int _currentPartIndex;
        private int _correctAnswers;
        private int _totalQuestions;
        private int _pointsEarned;
        private List<string> _answeredChoiceIds = new();
        private bool _isRunning;

        public event Action<int> OnPointsAwarded;
        public event Action<UserScenarioProgress> OnScenarioCompleted;

        [Header("Quiz Transition UI")]
        [SerializeField] private GameObject quizStartPanel;
        [SerializeField] private UnityEngine.UI.Button startQuizButton;

        [Header("Quiz Systems")]
        [SerializeField] private MCQ mcqSystem;
        [SerializeField] private FactVsOpinion factsSystem;
        [SerializeField] private DragAndDrop dragDropSystem;

        // ══════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ══════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ValidateReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ══════════════════════════════════════════════════════════
        //  ENTRY POINTS
        // ══════════════════════════════════════════════════════════

        public void StartScenario(string scenarioId)
        {
            if (_isRunning) { Debug.LogWarning("[ScenarioManager] Already running."); return; }
            if (string.IsNullOrEmpty(scenarioId)) { Debug.LogError("[ScenarioManager] Empty scenarioId."); return; }

            ResetState();
            loadingUI.Show("Loading scenario…");

            FirebaseScenarioService.Instance.FetchScenario(scenarioId,
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
                    Debug.LogError($"[ScenarioManager] Load failed '{scenarioId}': {err}");
                });
        }

        public void StartScenarioFromPending() =>
            StartScenario(ScenarioSceneBootstrapper.PendingScenarioId);

        // ══════════════════════════════════════════════════════════
        //  PLAYBACK
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

        private void PlayCurrentPart()
        {
            if (_currentPartIndex >= _scenario.sceneParts.Count)
            {
                CheckForQuizAssessment(); // Replaced FinishScenario()
                return;
            }

            var part = _scenario.sceneParts[_currentPartIndex];
            LoadBackground(part.backgroundImageUrl);

            switch (part.type)
            {
                case ScenePartType.Narrative: PlayNarrativePart(part); break;
                case ScenePartType.Question: PlayQuestionPart(part); break;
                default:
                    _currentPartIndex++;
                    PlayCurrentPart();
                    break;
            }
        }

        private void CheckForQuizAssessment()
        {
            if (loadingUI != null) loadingUI.Show("Checking for assessment...");

            FirebaseScenarioService.Instance.FetchQuizForScenario(_scenario.id, (fetchedQuiz) =>
            {
                if (loadingUI != null) loadingUI.Hide();

                if (fetchedQuiz == null || string.IsNullOrEmpty(fetchedQuiz.type) || fetchedQuiz.type == "None" || fetchedQuiz.questions == null || fetchedQuiz.questions.Count == 0)
                {
                    FinishScenario(0, 0, 0); // No quiz available, finish immediately
                    return;
                }

                // Show the "Now let's test your knowledge" transition screen
                if (quizStartPanel != null)
                {
                    quizStartPanel.SetActive(true);
                    startQuizButton.onClick.RemoveAllListeners();
                    startQuizButton.onClick.AddListener(() =>
                    {
                        quizStartPanel.SetActive(false);
                        LaunchQuizSystem(fetchedQuiz);
                    });
                }
                else
                {
                    // Fallback just in case the panel isn't assigned
                    LaunchQuizSystem(fetchedQuiz);
                }
            });
        }

        private void LaunchQuizSystem(PlaybookQuiz fetchedQuiz)
        {
            // Route to the correct Quiz Panel based on the DB type
            switch (fetchedQuiz.type)
            {
                case "MCQ":
                    if (mcqSystem != null) mcqSystem.StartGame(fetchedQuiz, FinishScenario);
                    else FinishScenario(0, 0, 0);
                    break;
                case "FactsVsOpinions":
                    if (factsSystem != null) factsSystem.StartGame(fetchedQuiz, FinishScenario);
                    else FinishScenario(0, 0, 0);
                    break;
                case "DragAndDrop":
                    if (dragDropSystem != null) dragDropSystem.StartGame(fetchedQuiz, FinishScenario);
                    else FinishScenario(0, 0, 0);
                    break;
                default:
                    FinishScenario(0, 0, 0);
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
        //  COMPLETION
        // ══════════════════════════════════════════════════════════

        // Now accepts the three integers passed back by the Quiz Action callback
        private void FinishScenario(int quizCorrect, int quizTotal, int quizPointsEarned)
        {
            _isRunning = false;
            _pointsEarned += _scenario.pointsOnCompletion;
            OnPointsAwarded?.Invoke(_scenario.pointsOnCompletion);

            var progress = new UserScenarioProgress
            {
                completed = true,
                score = _pointsEarned,
                correctAnswers = _correctAnswers,
                totalQuestions = _totalQuestions,
                completedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                answeredChoiceIds = _answeredChoiceIds
            };

            string userId = FirebaseManager.Instance?.CurrentUserId;
            if (!string.IsNullOrEmpty(userId))
            {
                FirebaseScenarioService.Instance.SaveUserProgress(userId, _scenario.id, progress,
                onComplete: () => CheckPostSurveyEligibility(userId));
            }

            OnScenarioCompleted?.Invoke(progress);

            // Pass the newly acquired quiz stats directly into the unified Complete UI
            completeUI.Show(progress, _scenario, quizCorrect, quizTotal, quizPointsEarned,
                onHomeClicked: () => UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene"),
                onReplayClicked: () => { string id = _scenario.id; ResetState(); StartScenario(id); }
            );
        }

        private void CheckPostSurveyEligibility(string userId)
        {
            // Skip if they've already done the post-survey
            if (UserManager.Instance.CurrentUser.hasCompletedPostSurvey) return;

            FirebaseScenarioService.Instance.FetchScenarioIndex(index =>
            {
                // Filter for only "Main" scenarios
                var mainScenarios = index.FindAll(s => s.category == "Main");
                int mainCount = mainScenarios.Count;
                int completedCount = 0;

                // Check user progress for each main scenario
                foreach (var scenario in mainScenarios)
                {
                    FirebaseScenarioService.Instance.FetchUserProgress(userId, scenario.id, userProgress =>
                    {
                        if (userProgress != null && userProgress.completed)
                        {
                            completedCount++;
                        }

                        // If all main scenarios are checked and completed, flag the survey
                        if (completedCount == mainCount)
                        {
                            Debug.Log("[ScenarioManager] All main scenarios completed! Flagging post-survey.");
                            // You can set a PlayerPref here to tell HomeScene to show the survey on load
                            PlayerPrefs.SetInt("ShowPostSurvey", 1);
                        }
                    });
                }
            });
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
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
                Debug.LogError($"[ScenarioManager] {label} not assigned — drag from Hierarchy.");
            else if (!mb.gameObject.scene.IsValid())
                Debug.LogError($"[ScenarioManager] {label} looks like a prefab asset, not a scene instance.");
        }
    }
}