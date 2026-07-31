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

// ============================================================
// ScenarioManager.cs
// Drives the complete play session for one scenario.
// Attach to a persistent manager GameObject in the Scenario scene.
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
        private ScenarioIntroUI introUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileIntroUI : desktopIntroUI;
        private NarrativePartUI narrativeUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileNarrativeUI : desktopNarrativeUI;
        private QuestionPartUI questionUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileQuestionUI : desktopQuestionUI;
        private FeedbackUI feedbackUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileFeedbackUI : desktopFeedbackUI;
        private ScenarioCompleteUI completeUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileCompleteUI : desktopCompleteUI;
        private LoadingOverlayUI loadingUI => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileLoadingUI : desktopLoadingUI;

        [Header("Background")]
        [SerializeField] private UnityEngine.UI.RawImage backgroundImage;

        [Header("Video System")]
        [SerializeField] private UnityEngine.Video.VideoPlayer videoPlayer;
        [SerializeField] private UnityEngine.UI.RawImage videoSurface;

        [Header("Audio System")]
        [SerializeField] private AudioSource audioSource;

        // ── Runtime state ──────────────────────────────────────────
        private PlaybookScenario _scenario;
        private int _currentPartIndex;
        private int _correctAnswers;
        private int _totalQuestions;
        private int _pointsEarned;
        private List<string> _answeredChoiceIds = new();
        private bool _isRunning;

        // Prevents mid-scenario saves from overwriting a completed high score
        private bool _isReplayingCompletedScenario;

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
        [SerializeField] private DNDRanking mobileRankingSystem;
        [SerializeField] private DNDSorting mobileSortingSystem;

        // ── Desktop Quiz Elements ──────────────────────────────────
        [Header("Desktop Quiz Transition UI")]
        [SerializeField] private GameObject desktopQuizStartPanel;
        [SerializeField] private UnityEngine.UI.Button desktopStartQuizButton;

        [Header("Desktop Quiz Systems")]
        [SerializeField] private MCQ desktopMcqSystem;
        [SerializeField] private FactVsOpinion desktopFactsSystem;
        [SerializeField] private DragAndDrop desktopDragDropSystem;
        [SerializeField] private DNDRanking desktopRankingSystem;
        [SerializeField] private DNDSorting desktopSortingSystem;

        // ── Dynamic Quiz Routers ───────────────────────────────────
        private GameObject quizStartPanel => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileQuizStartPanel : desktopQuizStartPanel;
        private UnityEngine.UI.Button startQuizButton => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileStartQuizButton : desktopStartQuizButton;
        private MCQ mcqSystem => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileMcqSystem : desktopMcqSystem;
        private FactVsOpinion factsSystem => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileFactsSystem : desktopFactsSystem;
        private DragAndDrop dragDropSystem => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileDragDropSystem : desktopDragDropSystem;
        private DNDRanking rankingSystem => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileRankingSystem : desktopRankingSystem;
        private DNDSorting sortingSystem => ResponsiveLayoutManager.Instance.IsMobileActive ? mobileSortingSystem : desktopSortingSystem;

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
            if (_isRunning) return;

            ResetState();
            loadingUI.Show("Loading scenario…");

            FirebaseScenarioService.Instance.FetchScenario(scenarioId,
                onComplete: scenario =>
                {
                    _scenario = scenario;
                    CountQuestions();

                    string userId = FirebaseManager.Instance?.CurrentUserId;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        FirebaseScenarioService.Instance.FetchUserProgress(userId, scenarioId, progress =>
                        {
                            if (progress != null && progress.completed)
                            {
                                // User is replaying. Protect their high score from mid-scenario overwrites.
                                _isReplayingCompletedScenario = true;
                                loadingUI.Hide();
                                ShowIntro();
                            }
                            else if (progress != null && !progress.completed && progress.answeredChoiceIds.Count > 0)
                            {
                                _isReplayingCompletedScenario = false;
                                _answeredChoiceIds = progress.answeredChoiceIds;
                                _correctAnswers = progress.correctAnswers;
                                _pointsEarned = progress.score;

                                FastForwardToResumePoint();
                                loadingUI.Hide();
                            }
                            else
                            {
                                _isReplayingCompletedScenario = false;
                                loadingUI.Hide();
                                ShowIntro();
                            }
                        });
                    }
                    else
                    {
                        loadingUI.Hide();
                        ShowIntro();
                    }
                },
                onError: err =>
                {
                    loadingUI.Hide();
                    Debug.LogError($"[ScenarioManager] Load failed '{scenarioId}': {err}");
                });
        }

        public void StartScenarioFromPending() =>
            StartScenario(ScenarioSceneBootstrapper.PendingScenarioId);

        private void FastForwardToResumePoint()
        {
            _isRunning = true;
            _currentPartIndex = 0;
            int questionsPassed = 0;
            bool foundResumePoint = false;

            for (int i = 0; i < _scenario.sceneParts.Count; i++)
            {
                if (_scenario.sceneParts[i].type == ScenePartType.Question || _scenario.sceneParts[i].type == ScenePartType.Activity)
                {
                    if (questionsPassed < _answeredChoiceIds.Count)
                    {
                        questionsPassed++;
                    }
                    else
                    {
                        // We found the unanswered question/activity at index 'i'.
                        // Let's rewind to the nearest Narrative part immediately preceding this question for context.
                        int startIndex = i;
                        while (startIndex > 0 && _scenario.sceneParts[startIndex - 1].type == ScenePartType.Narrative)
                        {
                            startIndex--;
                        }

                        _currentPartIndex = startIndex;
                        foundResumePoint = true;
                        break;
                    }
                }
            }

            // If they answered all questions but dropped out at the final assessment
            if (!foundResumePoint && questionsPassed == _answeredChoiceIds.Count)
            {
                _currentPartIndex = _scenario.sceneParts.Count;
            }

            PlayCurrentPart();
        }

        // ══════════════════════════════════════════════════════════
        //  PLAYBACK & BRANCH ROUTING
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
            LoadVideo(part.videoUrl);
            LoadAudio(part.audioUrl);

            switch (part.type)
            {
                case ScenePartType.Narrative: PlayNarrativePart(part); break;
                case ScenePartType.Question: PlayQuestionPart(part); break;
                case ScenePartType.Activity: PlayInteractiveActivityPart(part); break;
                default:
                    _currentPartIndex++;
                    PlayCurrentPart();
                    break;
            }
        }

        private void PlayInteractiveActivityPart(ScenePart part)
        {
            LoadVideo("");
            if (loadingUI != null) loadingUI.Show("Loading activity...");

            FirebaseScenarioService.Instance.FetchQuizById(part.linkedActivityId, fetchedQuiz =>
            {
                if (loadingUI != null) loadingUI.Hide();

                if (fetchedQuiz == null)
                {
                    _currentPartIndex++;
                    PlayCurrentPart();
                    return;
                }
                LaunchMidScenarioQuizSystem(fetchedQuiz);
            });
        }

        private void LaunchMidScenarioQuizSystem(PlaybookQuiz fetchedQuiz)
        {
            Action<int, int, int> onActivityFinished = (correct, total, points) =>
            {
                _pointsEarned += points;
                OnPointsAwarded?.Invoke(points);

                // CRITICAL FIX: Track that this activity was completed so the resume logic
                // knows to skip past it, preventing double-counting of points/answers!
                _answeredChoiceIds.Add($"activity_{fetchedQuiz.id}");

                SaveMidScenarioProgress();

                _currentPartIndex++;
                PlayCurrentPart();
            };

            switch (fetchedQuiz.type)
            {
                case "MCQ":
                    if (mcqSystem != null) mcqSystem.StartGame(fetchedQuiz, onActivityFinished);
                    break;
                case "MultiMCQ":
                    if (mcqSystem != null) mcqSystem.StartGame(fetchedQuiz, onActivityFinished);
                    break;
                case "FactsVsOpinions":
                    if (factsSystem != null) factsSystem.StartGame(fetchedQuiz, onActivityFinished);
                    break;
                case "DragAndDrop":
                    if (dragDropSystem != null) dragDropSystem.StartGame(fetchedQuiz, onActivityFinished);
                    break;
                case "Ranking":
                    if (rankingSystem != null) rankingSystem.StartGame(fetchedQuiz, onActivityFinished);
                    break;
                case "Sorting":
                    if (sortingSystem != null) sortingSystem.StartGame(fetchedQuiz, onActivityFinished);
                    break;
                default:
                    onActivityFinished(0, 0, 0);
                    break;
            }
        }

        private void CheckForQuizAssessment()
        {
            LoadVideo("");

            if (loadingUI != null) loadingUI.Show("Checking for assessment...");

            FirebaseScenarioService.Instance.FetchQuizForScenario(_scenario.id, (fetchedQuiz) =>
            {
                if (loadingUI != null) loadingUI.Hide();

                if (fetchedQuiz == null || string.IsNullOrEmpty(fetchedQuiz.type) || fetchedQuiz.type == "None" || fetchedQuiz.questions == null || fetchedQuiz.questions.Count == 0)
                {
                    FinishScenario(0, 0, 0);
                    return;
                }

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
                    LaunchQuizSystem(fetchedQuiz);
                }
            });
        }

        private void LaunchQuizSystem(PlaybookQuiz fetchedQuiz)
        {
            switch (fetchedQuiz.type)
            {
                case "MCQ":
                    if (mcqSystem != null) mcqSystem.StartGame(fetchedQuiz, FinishScenario);
                    else FinishScenario(0, 0, 0);
                    break;
                case "MultiMCQ":
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
                case "Ranking":
                    if (rankingSystem != null) rankingSystem.StartGame(fetchedQuiz, FinishScenario);
                    else FinishScenario(0, 0, 0);
                    break;
                case "Sorting":
                    if (sortingSystem != null) sortingSystem.StartGame(fetchedQuiz, FinishScenario);
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

            // Award points only if they got it right on this single attempt
            if (choice.isCorrect)
            {
                _correctAnswers++;
                _pointsEarned += _scenario.pointsPerCorrect;
                OnPointsAwarded?.Invoke(_scenario.pointsPerCorrect);
            }

            SaveMidScenarioProgress();

            feedbackUI.Show(choice, onDismissed: () =>
            {
                feedbackUI.Hide();

                // Advance to the next part regardless of whether they were right or wrong
                _currentPartIndex++;
                PlayCurrentPart();
            });
        }

        private void SaveMidScenarioProgress()
        {
            // CRITICAL FIX: If they are replaying a completed scenario, do NOT save 
            // mid-progress. This prevents overwriting a high score with "completed: false".
            if (_isReplayingCompletedScenario) return;

            string userId = FirebaseManager.Instance?.CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return;

            var progress = new UserScenarioProgress
            {
                scenarioId = _scenario.id,
                completed = false,
                score = _pointsEarned,
                correctAnswers = _correctAnswers,
                totalQuestions = _totalQuestions,
                completedTimestamp = 0,
                answeredChoiceIds = _answeredChoiceIds
            };

            FirebaseScenarioService.Instance.SaveUserProgress(userId, _scenario.id, progress, null);
        }

        // ══════════════════════════════════════════════════════════
        //  COMPLETION
        // ══════════════════════════════════════════════════════════

        private void FinishScenario(int quizCorrect, int quizTotal, int quizPointsEarned)
        {
            _isRunning = false;

            int currentRunScore = _pointsEarned + _scenario.pointsOnCompletion;
            OnPointsAwarded?.Invoke(_scenario.pointsOnCompletion);

            var progress = new UserScenarioProgress
            {
                scenarioId = _scenario.id,
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
                FirebaseScenarioService.Instance.FetchUserProgress(userId, _scenario.id, previousProgress =>
                {
                    int previousBestScore = (previousProgress != null && previousProgress.completed) ? previousProgress.score : 0;
                    int scoreDelta = currentRunScore - previousBestScore;
                    bool isNewHighScore = scoreDelta > 0;

                    if (isNewHighScore && UserManager.Instance != null)
                    {
                        UserManager.Instance.AwardSimulationPoints(_scenario.id, scoreDelta);
                    }

                    if (currentRunScore >= previousBestScore || previousProgress == null || !previousProgress.completed)
                    {
                        FirebaseScenarioService.Instance.SaveUserProgress(userId, _scenario.id, progress,
                            onComplete: () => CheckPostSurveyEligibility(userId));
                    }
                    else
                    {
                        Debug.Log($"[ScenarioManager] Score {currentRunScore} did not beat high score {previousBestScore}. Discarding lower attempt.");
                        CheckPostSurveyEligibility(userId);
                    }

                    OnScenarioCompleted?.Invoke(progress);

                    completeUI.Show(progress, _scenario, quizCorrect, quizTotal, quizPointsEarned,
                        isNewHighScore,
                        onHomeClicked: () => UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene"),
                        onReplayClicked: () => { string id = _scenario.id; ResetState(); StartScenario(id); }
                    );
                });
            }
            else
            {
                OnScenarioCompleted?.Invoke(progress);
                completeUI.Show(progress, _scenario, quizCorrect, quizTotal, quizPointsEarned,
                    true,
                    onHomeClicked: () => UnityEngine.SceneManagement.SceneManager.LoadScene("HomeScene"),
                    onReplayClicked: () => { string id = _scenario.id; ResetState(); StartScenario(id); }
                );
            }
        }

        private void CheckPostSurveyEligibility(string userId)
        {
            if (UserManager.Instance == null || UserManager.Instance.CurrentUser == null || UserManager.Instance.CurrentUser.hasCompletedPostSurvey) return;

            // Get the user's selected track (e.g., "Manager" or "Employee")
            string userTrack = UserManager.Instance.CurrentUser.selectedTrack;

            FirebaseScenarioService.Instance.FetchScenarioIndex(index =>
            {
                // Filter scenarios to ONLY include "Main" scenarios that match the user's track
                var mainScenarios = index.FindAll(s => s.category == "Main" && (s.track == userTrack || string.IsNullOrEmpty(s.track) || s.track == "All"));

                int mainCount = mainScenarios.Count;
                if (mainCount == 0) return; // Saftey check if no scenarios exist for this track yet

                int completedCount = 0;
                int processedCount = 0; // Tracks how many Firebase callbacks have finished

                foreach (var scenario in mainScenarios)
                {
                    FirebaseScenarioService.Instance.FetchUserProgress(userId, scenario.id, userProgress =>
                    {
                        processedCount++; // Mark this scenario check as finished

                        if (userProgress != null && userProgress.completed)
                        {
                            completedCount++;
                        }

                        // Only evaluate the final result once ALL async Firebase checks have returned
                        if (processedCount == mainCount)
                        {
                            if (completedCount == mainCount)
                            {
                                Debug.Log($"[ScenarioManager] All {mainCount} main scenarios for track '{userTrack}' completed! Flagging post-survey.");
                                PlayerPrefs.SetInt("ShowPostSurvey", 1);
                                PlayerPrefs.Save();
                            }
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
            _isReplayingCompletedScenario = false;
            _answeredChoiceIds = new List<string>();
            completeUI.Hide();
            LoadVideo("");
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

        private void LoadVideo(string url)
        {
            if (string.IsNullOrEmpty(url) || videoPlayer == null)
            {
                if (videoSurface != null) videoSurface.gameObject.SetActive(false);
                if (videoPlayer != null) videoPlayer.Stop();
                return;
            }

            if (videoSurface != null) videoSurface.gameObject.SetActive(true);
            videoPlayer.source = UnityEngine.Video.VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.Prepare();

            videoPlayer.prepareCompleted += (vp) => { vp.Play(); };
        }

        private void LoadAudio(string url)
        {
            if (string.IsNullOrEmpty(url) || audioSource == null)
            {
                if (audioSource != null) audioSource.Stop();
                return;
            }

            if (url.StartsWith("http"))
            {
                StartCoroutine(LoadAudioFromUrl(url));
            }
            else
            {
                var clip = Resources.Load<AudioClip>(url);
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }
        }

        private IEnumerator LoadAudioFromUrl(string url)
        {
            AudioType type = url.Contains(".wav") ? AudioType.WAV : AudioType.MPEG;
            using var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, type);
            yield return req.SendWebRequest();
            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                audioSource.clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
                audioSource.Play();
            }
            else
            {
                Debug.LogError($"[ScenarioManager] Audio load failed: {req.error}");
            }
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