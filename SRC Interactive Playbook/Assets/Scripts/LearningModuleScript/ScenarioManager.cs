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

        [Header("Mobile UI Panels")]
        [SerializeField] private ScenarioIntroUI mobileIntroUI;
        [SerializeField] private NarrativePartUI mobileNarrativeUI;
        [SerializeField] private QuestionPartUI mobileQuestionUI;
        [SerializeField] private FeedbackUI mobileFeedbackUI;
        [SerializeField] private ScenarioCompleteUI mobileCompleteUI;
        [SerializeField] private LoadingOverlayUI mobileLoadingUI;

        [Header("Desktop UI Panels")]
        [SerializeField] private ScenarioIntroUI desktopIntroUI;
        [SerializeField] private NarrativePartUI desktopNarrativeUI;
        [SerializeField] private QuestionPartUI desktopQuestionUI;
        [SerializeField] private FeedbackUI desktopFeedbackUI;
        [SerializeField] private ScenarioCompleteUI desktopCompleteUI;
        [SerializeField] private LoadingOverlayUI desktopLoadingUI;

        // ── Dynamic UI Routers ─────────────────────────────────────
        // These magically return the correct panel based on screen size!
        private ScenarioIntroUI introUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileIntroUI : desktopIntroUI;
        private NarrativePartUI narrativeUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileNarrativeUI : desktopNarrativeUI;
        private QuestionPartUI questionUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileQuestionUI : desktopQuestionUI;
        private FeedbackUI feedbackUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileFeedbackUI : desktopFeedbackUI;
        private ScenarioCompleteUI completeUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileCompleteUI : desktopCompleteUI;
        private LoadingOverlayUI loadingUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileLoadingUI : desktopLoadingUI;

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

        // ── Mobile Quiz Elements ───────────────────────────────────
        [Header("Mobile Quiz Transition UI")]
        [SerializeField] private GameObject mobileQuizStartPanel;
        [SerializeField] private UnityEngine.UI.Button mobileStartQuizButton;

        [Header("Mobile Quiz Systems")]
        [SerializeField] private MCQ mobileMcqSystem;
        [SerializeField] private FactVsOpinion mobileFactsSystem;
        [SerializeField] private DragAndDrop mobileDragDropSystem;

        // ── Desktop Quiz Elements ──────────────────────────────────
        [Header("Desktop Quiz Transition UI")]
        [SerializeField] private GameObject desktopQuizStartPanel;
        [SerializeField] private UnityEngine.UI.Button desktopStartQuizButton;

        [Header("Desktop Quiz Systems")]
        [SerializeField] private MCQ desktopMcqSystem;
        [SerializeField] private FactVsOpinion desktopFactsSystem;
        [SerializeField] private DragAndDrop desktopDragDropSystem;

        // ── Dynamic Quiz Routers ───────────────────────────────────
        private GameObject quizStartPanel => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileQuizStartPanel : desktopQuizStartPanel;
        private UnityEngine.UI.Button startQuizButton => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileStartQuizButton : desktopStartQuizButton;

        private MCQ mcqSystem => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileMcqSystem : desktopMcqSystem;
        private FactVsOpinion factsSystem => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileFactsSystem : desktopFactsSystem;
        private DragAndDrop dragDropSystem => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileDragDropSystem : desktopDragDropSystem;

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
            foreach (var nav in UIManager.Instance.GetAllNavBars()) nav.ShowNavBar();
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
                CheckForQuizAssessment();
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
        private void FinishScenario(int quizCorrect, int quizTotal, int quizPointsEarned)
        {
            _isRunning = false;

            // Calculate the total points earned in THIS specific run
            int currentRunScore = _pointsEarned + _scenario.pointsOnCompletion;
            OnPointsAwarded?.Invoke(_scenario.pointsOnCompletion);

            var progress = new UserScenarioProgress
            {
                completed = true,
                score = currentRunScore,
                correctAnswers = _correctAnswers,
                totalQuestions = _totalQuestions,
                completedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                answeredChoiceIds = _answeredChoiceIds
            };

            string userId = FirebaseManager.Instance?.CurrentUserId;
            if (!string.IsNullOrEmpty(userId))
            {
                // 1. Fetch previous progress to calculate the score delta
                FirebaseScenarioService.Instance.FetchUserProgress(userId, _scenario.id, previousProgress =>
                {
                    int previousBestScore = previousProgress != null ? previousProgress.score : 0;

                    // Calculate how many NEW points they earned (if any)
                    int scoreDelta = currentRunScore - previousBestScore;

                    // Check if this run is a new high score
                    bool isNewHighScore = scoreDelta > 0;

                    // 2. Only award points to their global total if they beat their high score
                    if (isNewHighScore && UserManager.Instance != null)
                    {
                        UserManager.Instance.AwardSimulationPoints(_scenario.id, scoreDelta);
                    }

                    // 3. Only overwrite the scenario progress in the database if it's a new high score
                    if (currentRunScore >= previousBestScore)
                    {
                        FirebaseScenarioService.Instance.SaveUserProgress(userId, _scenario.id, progress,
                            onComplete: () => CheckPostSurveyEligibility(userId));
                    }
                    else
                    {
                        CheckPostSurveyEligibility(userId);
                    }

                    // 4. Trigger UI and Events INSIDE the callback so it waits for the calculation
                    OnScenarioCompleted?.Invoke(progress);

                    completeUI.Show(progress, _scenario, quizCorrect, quizTotal, quizPointsEarned,
                        isNewHighScore, // Now safely in scope!
                        onHomeClicked: () => UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene"),
                        onReplayClicked: () => { string id = _scenario.id; ResetState(); StartScenario(id); }
                    );
                });
            }
            else
            {
                // Fallback in case the user is somehow not logged in (e.g., testing offline)
                OnScenarioCompleted?.Invoke(progress);

                completeUI.Show(progress, _scenario, quizCorrect, quizTotal, quizPointsEarned,
                    true, // Default to true if no previous data exists
                    onHomeClicked: () => UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene"),
                    onReplayClicked: () => { string id = _scenario.id; ResetState(); StartScenario(id); }
                );
            }
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
            CheckRef(mobileIntroUI, "Mobile Intro UI");
            CheckRef(desktopIntroUI, "Desktop Intro UI");
            CheckRef(mobileNarrativeUI, "Mobile Narrative UI");
            CheckRef(desktopNarrativeUI, "Desktop Narrative UI");
            CheckRef(mobileQuestionUI, "Mobile Question UI");
            CheckRef(desktopQuestionUI, "Desktop Question UI");
            CheckRef(mobileFeedbackUI, "Mobile Feedback UI");
            CheckRef(desktopFeedbackUI, "Desktop Feedback UI");
            CheckRef(mobileCompleteUI, "Mobile Complete UI");
            CheckRef(desktopCompleteUI, "Desktop Complete UI");
            CheckRef(mobileLoadingUI, "Mobile Loading UI");
            CheckRef(desktopLoadingUI, "Desktop Loading UI");
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