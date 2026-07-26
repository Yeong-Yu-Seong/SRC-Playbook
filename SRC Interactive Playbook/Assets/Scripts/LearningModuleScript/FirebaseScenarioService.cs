// ============================================================
// FirebaseScenarioService.cs
// Handles all Firebase Realtime Database reads and writes.
// Falls back to local Resources/Scenarios JSON files when
// Firebase is unavailable or during offline play.
//
// SETUP: Add the Firebase Unity SDK to your project and set
// your project's google-services.json / GoogleService-Info.plist.
// Firebase package: com.google.firebase.database (v12+)
// ============================================================
using System;
// ============================================================
// FirebaseScenarioService.cs
// Refactored for WebGL deployment using FirebaseWebGL bridge.
// Handles all Firebase Realtime Database reads and writes.
// Falls back to local Resources/Scenarios JSON files when
// Firebase is unavailable or during offline play.
// ============================================================

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RedCross.Playbook.Data;
using UnityEngine;
using FirebaseWebGL.Scripts.FirebaseBridge; // Uses WebGL Bridge

namespace RedCross.Playbook.Firebase
{
    public class FirebaseScenarioService : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────
        public static FirebaseScenarioService Instance { get; private set; }
        public bool IsInitialized { get; internal set; }

        [Header("Firebase Config")]
        [SerializeField] private string dbRootNode = "playbook";
        [SerializeField] private bool useFirebase = true;

        private bool _firebaseReady;

        public event Action OnFirebaseReady;
        public event Action<string> OnFirebaseError;

        // --- Temporary Callback Storage for WebGL ---
        private Action<List<ScenarioIndexEntry>> _onFetchIndexComplete;
        private Action<PlaybookScenario> _onFetchScenarioComplete;
        private Action<string> _onFetchScenarioError;
        private Action<List<PlaybookScenario>> _onFetchAllComplete;
        private Action<string> _onFetchAllError;
        private Action _onSaveProgressComplete;
        private Action<UserScenarioProgress> _onFetchProgressComplete;

        private Action<PlaybookQuiz> _onFetchQuizComplete;
        private Action<List<PlaybookQuiz>> _onFetchAllQuizzesComplete;
        private Action<PlaybookQuiz> _onFetchQuizByIdComplete;

        private string _pendingScenarioId; // Used for finding linked quizzes
        // ------------------------------------------

        // ══════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ══════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (useFirebase) InitFirebase();
        }

        private void InitFirebase()
        {
            // WebGL doesn't require native dependency checks
            _firebaseReady = true;
            OnFirebaseReady?.Invoke();
            Debug.Log("[FirebaseScenarioService] Initialised for WebGL.");
        }

        // ══════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════

        public void FetchScenarioIndex(Action<List<ScenarioIndexEntry>> onComplete)
        {
            if (_firebaseReady) FetchIndexFromFirebase(onComplete);
            else FetchIndexFromLocal(onComplete);
        }

        public void FetchScenario(string scenarioId, Action<PlaybookScenario> onComplete, Action<string> onError = null)
        {
            if (_firebaseReady) FetchScenarioFromFirebase(scenarioId, onComplete, onError);
            else FetchScenarioFromLocal(scenarioId, onComplete, onError);
        }

        public void FetchAllScenarios(Action<List<PlaybookScenario>> onComplete, Action<string> onError = null)
        {
            if (_firebaseReady) FetchAllFromFirebase(onComplete, onError);
            else FetchAllFromLocal(onComplete, onError);
        }

        public void SaveUserProgress(string userId, string scenarioId, UserScenarioProgress progress, Action onComplete = null)
        {
            if (_firebaseReady) SaveProgressToFirebase(userId, scenarioId, progress, onComplete);
            else SaveProgressToLocal(userId, scenarioId, progress, onComplete);
        }

        public void FetchUserProgress(string userId, string scenarioId, Action<UserScenarioProgress> onComplete)
        {
            if (_firebaseReady) FetchProgressFromFirebase(userId, scenarioId, onComplete);
            else FetchProgressFromLocal(userId, scenarioId, onComplete);
        }

        // ══════════════════════════════════════════════════════════
        //  FIREBASE IMPLEMENTATIONS (WebGL)
        // ══════════════════════════════════════════════════════════

        private void FetchIndexFromFirebase(Action<List<ScenarioIndexEntry>> onComplete)
        {
            _onFetchIndexComplete = onComplete;
            FirebaseDatabase.GetJSON($"{dbRootNode}/scenarios_index", gameObject.name, "OnFetchIndexSuccess", "OnFetchIndexFailed");
        }

        public void OnFetchIndexSuccess(string json)
        {
            var list = new List<ScenarioIndexEntry>();
            if (!string.IsNullOrEmpty(json) && json != "null")
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, ScenarioIndexEntry>>(json);
                    foreach (var kvp in dict)
                    {
                        if (kvp.Value != null && kvp.Value.isPublished) list.Add(kvp.Value);
                    }
                    // Sort locally since WebGL GetJSON doesn't support OrderByChild natively
                    list.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FirebaseScenarioService] Skipping index entry: {ex.Message}");
                }
            }
            _onFetchIndexComplete?.Invoke(list);
        }

        public void OnFetchIndexFailed(string err)
        {
            Debug.LogWarning("[FirebaseScenarioService] Index fetch failed — using local fallback.");
            FetchIndexFromLocal(_onFetchIndexComplete);
        }

        private void FetchScenarioFromFirebase(string id, Action<PlaybookScenario> onComplete, Action<string> onError)
        {
            _onFetchScenarioComplete = onComplete;
            _onFetchScenarioError = onError;
            FirebaseDatabase.GetJSON($"{dbRootNode}/scenarios/{id}", gameObject.name, "OnFetchScenarioSuccess", "OnFetchScenarioFailed");
        }

        public void OnFetchScenarioSuccess(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                Debug.LogWarning("[FirebaseScenarioService] Scenario not found in Firebase — using local fallback.");
                FetchScenarioFromLocal("fallback_id", _onFetchScenarioComplete, _onFetchScenarioError);
                return;
            }
            try
            {
                var s = JsonConvert.DeserializeObject<PlaybookScenario>(json);
                _onFetchScenarioComplete?.Invoke(s);
            }
            catch (Exception ex)
            {
                _onFetchScenarioError?.Invoke(ex.Message);
            }
        }
        public void OnFetchScenarioFailed(string err) { _onFetchScenarioError?.Invoke(err); }

        private void FetchAllFromFirebase(Action<List<PlaybookScenario>> onComplete, Action<string> onError)
        {
            _onFetchAllComplete = onComplete;
            _onFetchAllError = onError;
            FirebaseDatabase.GetJSON($"{dbRootNode}/scenarios", gameObject.name, "OnFetchAllSuccess", "OnFetchAllFailed");
        }

        public void OnFetchAllSuccess(string json)
        {
            var list = new List<PlaybookScenario>();
            if (!string.IsNullOrEmpty(json) && json != "null")
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, PlaybookScenario>>(json);
                    foreach (var kvp in dict) if (kvp.Value != null) list.Add(kvp.Value);
                }
                catch (Exception ex) { Debug.LogWarning($"Parse error: {ex.Message}"); }
            }
            _onFetchAllComplete?.Invoke(list);
        }
        public void OnFetchAllFailed(string err)
        {
            Debug.LogWarning("[FirebaseScenarioService] FetchAll failed — using local fallback.");
            FetchAllFromLocal(_onFetchAllComplete, _onFetchAllError);
        }

        private void SaveProgressToFirebase(string userId, string scenarioId, UserScenarioProgress progress, Action onComplete)
        {
            _onSaveProgressComplete = onComplete;
            string json = JsonConvert.SerializeObject(progress);

            // Using UpdateJSON for deeper nesting
            FirebaseDatabase.UpdateJSON($"{dbRootNode}/user_progress/{userId}", $"{{\"{scenarioId}\": {json}}}", gameObject.name, "OnSaveProgressSuccess", "OnSaveProgressFailed");
        }

        public void OnSaveProgressSuccess(string info) { _onSaveProgressComplete?.Invoke(); }
        public void OnSaveProgressFailed(string err) { Debug.LogError("SaveProgress failed: " + err); _onSaveProgressComplete?.Invoke(); }

        private void FetchProgressFromFirebase(string userId, string scenarioId, Action<UserScenarioProgress> onComplete)
        {
            _onFetchProgressComplete = onComplete;
            FirebaseDatabase.GetJSON($"{dbRootNode}/user_progress/{userId}/{scenarioId}", gameObject.name, "OnFetchProgressSuccess", "OnFetchProgressFailed");
        }

        public void OnFetchProgressSuccess(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                _onFetchProgressComplete?.Invoke(null);
                return;
            }
            try
            {
                var p = JsonConvert.DeserializeObject<UserScenarioProgress>(json);
                _onFetchProgressComplete?.Invoke(p);
            }
            catch { _onFetchProgressComplete?.Invoke(null); }
        }
        public void OnFetchProgressFailed(string err) { _onFetchProgressComplete?.Invoke(null); }

        // ══════════════════════════════════════════════════════════
        //  QUIZ FETCHING
        // ══════════════════════════════════════════════════════════

        public void FetchQuizForScenario(string scenarioId, Action<PlaybookQuiz> onComplete)
        {
            if (!_firebaseReady)
            {
                FetchQuizFromLocal(scenarioId, onComplete);
                return;
            }
            _pendingScenarioId = scenarioId;
            _onFetchQuizComplete = onComplete;
            FirebaseDatabase.GetJSON($"{dbRootNode}/quizzes", gameObject.name, "OnQuizGetSuccess", "OnQuizGetFailed");
        }

        public void OnQuizGetSuccess(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                _onFetchQuizComplete?.Invoke(null);
                return;
            }

            PlaybookQuiz finalAssessment = null;
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, PlaybookQuiz>>(json);
                foreach (var kvp in dict)
                {
                    var quiz = kvp.Value;
                    if (quiz != null && quiz.linkedScenarioId == _pendingScenarioId)
                    {
                        if (finalAssessment == null || quiz.sortOrder > finalAssessment.sortOrder)
                        {
                            finalAssessment = quiz;
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.LogWarning($"[Firebase] Quiz parse error: {ex.Message}"); }

            _onFetchQuizComplete?.Invoke(finalAssessment);
        }
        public void OnQuizGetFailed(string err) { _onFetchQuizComplete?.Invoke(null); }


        public void FetchAllQuizzesForScenario(string scenarioId, Action<List<PlaybookQuiz>> onComplete)
        {
            if (!_firebaseReady)
            {
                FetchAllQuizzesFromLocal(scenarioId, onComplete);
                return;
            }

            Debug.Log($"[FirebaseScenarioService] Fetching ALL quizzes for scenario: {scenarioId}");
            _pendingScenarioId = scenarioId;
            _onFetchAllQuizzesComplete = onComplete;

            FirebaseDatabase.GetJSON($"{dbRootNode}/quizzes", gameObject.name, "OnAllQuizzesGetSuccess", "OnAllQuizzesGetFailed");
        }

        public void OnAllQuizzesGetSuccess(string json)
        {
            List<PlaybookQuiz> scenarioQuizzes = new List<PlaybookQuiz>();
            if (!string.IsNullOrEmpty(json) && json != "null")
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, PlaybookQuiz>>(json);
                    foreach (var kvp in dict)
                    {
                        var quiz = kvp.Value;
                        if (quiz != null && quiz.linkedScenarioId == _pendingScenarioId && quiz.isPublished)
                        {
                            scenarioQuizzes.Add(quiz);
                        }
                    }
                }
                catch (Exception ex) { Debug.LogWarning($"[Firebase] Quiz parse error: {ex.Message}"); }
            }

            scenarioQuizzes.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            _onFetchAllQuizzesComplete?.Invoke(scenarioQuizzes);
        }
        public void OnAllQuizzesGetFailed(string err) { _onFetchAllQuizzesComplete?.Invoke(new List<PlaybookQuiz>()); }


        public void FetchQuizById(string quizId, Action<PlaybookQuiz> onComplete)
        {
            if (!useFirebase || !_firebaseReady) { onComplete?.Invoke(null); return; }
            _onFetchQuizByIdComplete = onComplete;
            FirebaseDatabase.GetJSON($"{dbRootNode}/quizzes/{quizId}", gameObject.name, "OnQuizByIdSuccess", "OnQuizByIdFailed");
        }

        public void OnQuizByIdSuccess(string json)
        {
            if (string.IsNullOrEmpty(json) || json == "null") { _onFetchQuizByIdComplete?.Invoke(null); return; }
            try { _onFetchQuizByIdComplete?.Invoke(JsonConvert.DeserializeObject<PlaybookQuiz>(json)); }
            catch { _onFetchQuizByIdComplete?.Invoke(null); }
        }
        public void OnQuizByIdFailed(string err) { _onFetchQuizByIdComplete?.Invoke(null); }


        // ══════════════════════════════════════════════════════════
        //  LOCAL JSON FALLBACK (Unchanged)
        // ══════════════════════════════════════════════════════════

        private void FetchIndexFromLocal(Action<List<ScenarioIndexEntry>> onComplete)
        {
            var list = new List<ScenarioIndexEntry>();
            foreach (var s in LoadAllLocalScenarios())
            {
                if (!s.isPublished) continue;
                int q = 0;
                foreach (var p in s.sceneParts)
                    if (p.type == ScenePartType.Question) q++;

                list.Add(new ScenarioIndexEntry
                {
                    id = s.id,
                    exhibitNumber = s.exhibitNumber,
                    thumbnailUrl = s.thumbnailUrl,
                    isPublished = s.isPublished,
                    totalQuestions = q,
                    pointsOnCompletion = s.pointsOnCompletion
                });
            }
            list.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            onComplete?.Invoke(list);
        }

        private void FetchScenarioFromLocal(string id, Action<PlaybookScenario> onComplete, Action<string> onError)
        {
            var asset = Resources.Load<TextAsset>($"Scenarios/{id}");
            if (asset == null)
            {
                onError?.Invoke($"[FirebaseScenarioService] Local file not found: Resources/Scenarios/{id}.json");
                return;
            }
            try { onComplete?.Invoke(JsonConvert.DeserializeObject<PlaybookScenario>(asset.text)); }
            catch (Exception ex) { onError?.Invoke(ex.Message); }
        }

        private void FetchAllFromLocal(Action<List<PlaybookScenario>> onComplete, Action<string> onError)
        {
            try { onComplete?.Invoke(LoadAllLocalScenarios()); }
            catch (Exception ex) { onError?.Invoke(ex.Message); }
        }

        private List<PlaybookScenario> LoadAllLocalScenarios()
        {
            var list = new List<PlaybookScenario>();
            var assets = Resources.LoadAll<TextAsset>("Scenarios");
            foreach (var a in assets)
            {
                try { list.Add(JsonConvert.DeserializeObject<PlaybookScenario>(a.text)); }
                catch (Exception ex) { Debug.LogWarning($"[FirebaseScenarioService] Skipping {a.name}: {ex.Message}"); }
            }
            return list;
        }

        // ── Local progress ──────────────────────────────────────────

        private static string ProgressKey(string userId, string scenarioId) => $"progress_{userId}_{scenarioId}";

        private static void SaveProgressToLocal(string userId, string scenarioId, UserScenarioProgress progress, Action onComplete)
        {
            PlayerPrefs.SetString(ProgressKey(userId, scenarioId), JsonConvert.SerializeObject(progress));
            PlayerPrefs.Save();
            onComplete?.Invoke();
        }

        private static void FetchProgressFromLocal(string userId, string scenarioId, Action<UserScenarioProgress> onComplete)
        {
            string json = PlayerPrefs.GetString(ProgressKey(userId, scenarioId), "");
            if (string.IsNullOrEmpty(json)) { onComplete?.Invoke(null); return; }
            try { onComplete?.Invoke(JsonConvert.DeserializeObject<UserScenarioProgress>(json)); }
            catch { onComplete?.Invoke(null); }
        }

        private void FetchQuizFromLocal(string scenarioId, Action<PlaybookQuiz> onComplete)
        {
            var assets = Resources.LoadAll<TextAsset>("Quizzes");
            foreach (var asset in assets)
            {
                try
                {
                    var quiz = JsonConvert.DeserializeObject<PlaybookQuiz>(asset.text);
                    if (quiz != null && quiz.linkedScenarioId == scenarioId)
                    {
                        onComplete?.Invoke(quiz);
                        return;
                    }
                }
                catch { }
            }
            onComplete?.Invoke(null);
        }

        private void FetchAllQuizzesFromLocal(string scenarioId, Action<List<PlaybookQuiz>> onComplete)
        {
            List<PlaybookQuiz> scenarioQuizzes = new List<PlaybookQuiz>();
            var assets = Resources.LoadAll<TextAsset>("Quizzes");
            foreach (var asset in assets)
            {
                try
                {
                    var quiz = JsonConvert.DeserializeObject<PlaybookQuiz>(asset.text);
                    if (quiz != null && quiz.linkedScenarioId == scenarioId && quiz.isPublished) scenarioQuizzes.Add(quiz);
                }
                catch { }
            }
            scenarioQuizzes.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            onComplete?.Invoke(scenarioQuizzes);
        }
    }
}