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
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using RedCross.Playbook.Data;
using UnityEngine;

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
        private DatabaseReference _dbRef;

        public event Action OnFirebaseReady;
        public event Action<string> OnFirebaseError;

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
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                    _firebaseReady = true;
                    OnFirebaseReady?.Invoke();
                    Debug.Log("[FirebaseScenarioService] Initialised.");
                }
                else
                {
                    Debug.LogWarning($"[FirebaseScenarioService] Dependency error: {task.Result}. Using local fallback.");
                    _firebaseReady = false;
                    OnFirebaseError?.Invoke(task.Result.ToString());
                }
            });
        }

        // ══════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════

        public void FetchScenarioIndex(Action<List<ScenarioIndexEntry>> onComplete)
        {
            if (_firebaseReady) FetchIndexFromFirebase(onComplete);
            else FetchIndexFromLocal(onComplete);
        }

        public void FetchScenario(string scenarioId,
                                  Action<PlaybookScenario> onComplete,
                                  Action<string> onError = null)
        {
            if (_firebaseReady) FetchScenarioFromFirebase(scenarioId, onComplete, onError);
            else FetchScenarioFromLocal(scenarioId, onComplete, onError);
        }

        public void FetchAllScenarios(Action<List<PlaybookScenario>> onComplete,
                                      Action<string> onError = null)
        {
            if (_firebaseReady) FetchAllFromFirebase(onComplete, onError);
            else FetchAllFromLocal(onComplete, onError);
        }

        /// <summary>
        /// </summary>
        public void SaveUserProgress(string userId, string scenarioId,
                                     UserScenarioProgress progress,
                                     Action onComplete = null)
        {
            if (_firebaseReady) SaveProgressToFirebase(userId, scenarioId, progress, onComplete);
            else SaveProgressToLocal(userId, scenarioId, progress, onComplete);
        }

        public void FetchUserProgress(string userId, string scenarioId,
                                      Action<UserScenarioProgress> onComplete)
        {
            if (_firebaseReady) FetchProgressFromFirebase(userId, scenarioId, onComplete);
            else FetchProgressFromLocal(userId, scenarioId, onComplete);
        }

        // ══════════════════════════════════════════════════════════
        //  FIREBASE IMPLEMENTATIONS
        // ══════════════════════════════════════════════════════════

        private void FetchIndexFromFirebase(Action<List<ScenarioIndexEntry>> onComplete)
        {
            _dbRef.Child(dbRootNode).Child("scenarios_index")
                  .OrderByChild("sortOrder")
                  .GetValueAsync()
                  .ContinueWithOnMainThread(task =>
                  {
                      if (!task.IsCompletedSuccessfully)
                      {
                          Debug.LogWarning("[FirebaseScenarioService] Index fetch failed — using local fallback.");
                          FetchIndexFromLocal(onComplete);
                          return;
                      }

                      var list = new List<ScenarioIndexEntry>();
                      foreach (DataSnapshot child in task.Result.Children)
                      {
                          try
                          {
                              var e = JsonConvert.DeserializeObject<ScenarioIndexEntry>(
                                          child.GetRawJsonValue());
                              if (e != null && e.isPublished) list.Add(e);
                          }
                          catch (Exception ex)
                          {
                              Debug.LogWarning($"[FirebaseScenarioService] Skipping index entry {child.Key}: {ex.Message}");
                          }
                      }

                      onComplete?.Invoke(list);
                  });
        }

        private void FetchScenarioFromFirebase(string id,
                                               Action<PlaybookScenario> onComplete,
                                               Action<string> onError)
        {
            _dbRef.Child(dbRootNode).Child("scenarios").Child(id)
                  .GetValueAsync()
                  .ContinueWithOnMainThread(task =>
                  {
                      if (!task.IsCompletedSuccessfully || !task.Result.Exists)
                      {
                          Debug.LogWarning($"[FirebaseScenarioService] Scenario '{id}' not found in Firebase — using local fallback.");
                          FetchScenarioFromLocal(id, onComplete, onError);
                          return;
                      }

                      try
                      {
                          var s = JsonConvert.DeserializeObject<PlaybookScenario>(
                                      task.Result.GetRawJsonValue());
                          onComplete?.Invoke(s);
                      }
                      catch (Exception ex)
                      {
                          Debug.LogError($"[FirebaseScenarioService] Parse error for '{id}': {ex.Message}");
                          onError?.Invoke(ex.Message);
                      }
                  });
        }

        private void FetchAllFromFirebase(Action<List<PlaybookScenario>> onComplete,
                                          Action<string> onError)
        {
            // FIXED: same double-fire bug — local fallback was unconditional.
            _dbRef.Child(dbRootNode).Child("scenarios")
                  .GetValueAsync()
                  .ContinueWithOnMainThread(task =>
                  {
                      if (!task.IsCompletedSuccessfully)
                      {
                          Debug.LogWarning("[FirebaseScenarioService] FetchAll failed — using local fallback.");
                          FetchAllFromLocal(onComplete, onError);
                          return;
                      }

                      var list = new List<PlaybookScenario>();
                      foreach (DataSnapshot child in task.Result.Children)
                      {
                          try
                          {
                              var s = JsonConvert.DeserializeObject<PlaybookScenario>(
                                          child.GetRawJsonValue());
                              if (s != null) list.Add(s);
                          }
                          catch (Exception ex)
                          {
                              Debug.LogWarning($"[FirebaseScenarioService] Skipping scenario {child.Key}: {ex.Message}");
                          }
                      }

                      onComplete?.Invoke(list);
                  });
        }

        private void SaveProgressToFirebase(string userId, string scenarioId,
                                            UserScenarioProgress progress,
                                            Action onComplete)
        {
            string json = JsonConvert.SerializeObject(progress);

            _dbRef.Child(dbRootNode).Child("user_progress")
                  .Child(userId).Child(scenarioId)
                  .SetRawJsonValueAsync(json)
                  .ContinueWithOnMainThread(task =>
                  {
                      if (task.IsCanceled || task.IsFaulted)
                          Debug.LogError($"[FirebaseScenarioService] SaveProgress failed for '{scenarioId}': " +
                                         task.Exception?.GetBaseException().Message);
                      else
                          Debug.Log($"[FirebaseScenarioService] Progress saved: {userId}/{scenarioId}");

                      onComplete?.Invoke();
                  });
        }

        private void FetchProgressFromFirebase(string userId, string scenarioId,
                                               Action<UserScenarioProgress> onComplete)
        {
            _dbRef.Child(dbRootNode).Child("user_progress")
                  .Child(userId).Child(scenarioId)
                  .GetValueAsync()
                  .ContinueWithOnMainThread(task =>
                  {
                      if (!task.IsCompletedSuccessfully || !task.Result.Exists)
                      {
                          onComplete?.Invoke(null);
                          return;
                      }

                      try
                      {
                          var p = JsonConvert.DeserializeObject<UserScenarioProgress>(
                                      task.Result.GetRawJsonValue());
                          onComplete?.Invoke(p);
                      }
                      catch (Exception ex)
                      {
                          Debug.LogWarning($"[FirebaseScenarioService] Progress parse error: {ex.Message}");
                          onComplete?.Invoke(null);
                      }
                  });
        }

        // ══════════════════════════════════════════════════════════
        //  LOCAL JSON FALLBACK
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
                    // sortOrder, wallX, wallY, cardWidth, cardHeight default to 0/300/240
                    // — fine for local dev; real values come from Firebase in production.
                });
            }

            // Sort by sortOrder for consistent local display
            list.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            onComplete?.Invoke(list);
        }

        private void FetchScenarioFromLocal(string id,
                                            Action<PlaybookScenario> onComplete,
                                            Action<string> onError)
        {
            var asset = Resources.Load<TextAsset>($"Scenarios/{id}");
            if (asset == null)
            {
                string msg = $"[FirebaseScenarioService] Local file not found: Resources/Scenarios/{id}.json";
                Debug.LogError(msg);
                onError?.Invoke(msg);
                return;
            }

            try
            {
                var scenario = JsonConvert.DeserializeObject<PlaybookScenario>(asset.text);
                onComplete?.Invoke(scenario);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseScenarioService] JSON parse error in {id}: {ex.Message}");
                onError?.Invoke(ex.Message);
            }
        }

        private void FetchAllFromLocal(Action<List<PlaybookScenario>> onComplete,
                                       Action<string> onError)
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
                try
                {
                    var s = JsonConvert.DeserializeObject<PlaybookScenario>(a.text);
                    if (s != null) list.Add(s);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FirebaseScenarioService] Skipping {a.name}: {ex.Message}");
                }
            }

            return list;
        }

        // ── Local progress ──────────────────────────────────────────

        private static string ProgressKey(string userId, string scenarioId) =>
            $"progress_{userId}_{scenarioId}";

        private static void SaveProgressToLocal(string userId, string scenarioId,
                                                UserScenarioProgress progress,
                                                Action onComplete)
        {
            PlayerPrefs.SetString(ProgressKey(userId, scenarioId),
                                  JsonConvert.SerializeObject(progress));
            PlayerPrefs.Save();
            onComplete?.Invoke();
        }

        private static void FetchProgressFromLocal(string userId, string scenarioId,
                                                   Action<UserScenarioProgress> onComplete)
        {
            string json = PlayerPrefs.GetString(ProgressKey(userId, scenarioId), "");
            if (string.IsNullOrEmpty(json)) { onComplete?.Invoke(null); return; }

            try { onComplete?.Invoke(JsonConvert.DeserializeObject<UserScenarioProgress>(json)); }
            catch { onComplete?.Invoke(null); }
        }
        public void FetchQuizForScenario(string scenarioId, Action<PlaybookQuiz> onComplete)
        {
            if (!_firebaseReady)
            {
                // Route to local fallback instead of returning null
                FetchQuizFromLocal(scenarioId, onComplete);
                return;
            }

            Debug.Log($"[FirebaseScenarioService] Fetching quiz for scenario: {scenarioId}");

            _dbRef.Child(dbRootNode).Child("quizzes")
                  .GetValueAsync()
                  .ContinueWithOnMainThread(task =>
                  {
                      if (!task.IsCompletedSuccessfully || !task.Result.Exists)
                      {
                          onComplete?.Invoke(null);
                          return;
                      }

                      foreach (DataSnapshot child in task.Result.Children)
                      {
                          try
                          {
                              var quiz = JsonConvert.DeserializeObject<PlaybookQuiz>(child.GetRawJsonValue());
                              if (quiz != null && quiz.linkedScenarioId == scenarioId)
                              {
                                  onComplete?.Invoke(quiz);
                                  return;
                              }
                          }
                          catch (Exception ex)
                          {
                              Debug.LogWarning($"[Firebase] Quiz parse error: {ex.Message}");
                          }
                      }
                      onComplete?.Invoke(null);
                  });
        }
        private void FetchQuizFromLocal(string scenarioId, Action<PlaybookQuiz> onComplete)
        {
            Debug.Log("[FirebaseScenarioService] Using Local Fallback for Quiz.");

            // Loads any JSON files placed in a "Resources/Quizzes" folder
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
                catch
                {
                    // Skip invalid JSONs
                }
            }

            Debug.LogWarning($"[FirebaseScenarioService] No local quiz found linked to {scenarioId}.");
            onComplete?.Invoke(null);
        }
    }
}