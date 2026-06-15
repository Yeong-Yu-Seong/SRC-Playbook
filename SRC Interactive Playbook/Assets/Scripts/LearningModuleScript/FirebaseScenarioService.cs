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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using RedCross.Playbook.Data;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

namespace RedCross.Playbook.Firebase
{
    public class FirebaseScenarioService : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────
        public static FirebaseScenarioService Instance { get; private set; }

        [Header("Firebase Config")]
        [Tooltip("Must match the root node in Firebase and in the Admin Dashboard setting.")]
        [SerializeField] private string dbRootNode = "playbook";   // ← KEY FIX

        [Tooltip("Uncheck to force local JSON fallback during development.")]
        [SerializeField] private bool useFirebase = true;

        private bool _firebaseReady = false;
        private DatabaseReference _dbRef;

        public event Action OnFirebaseReady;
        public event Action<string> OnFirebaseError;

        // ══════════════════════════════════════════════════════════
        // Lifecycle
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
                    _dbRef         = FirebaseDatabase.DefaultInstance.RootReference;
                    _firebaseReady = true;
                    OnFirebaseReady?.Invoke();
                    Debug.Log("[Firebase] Initialised successfully.");
                }
                else
                {
                    Debug.LogWarning($"[Firebase] Dependency error: {task.Result}. Using local fallback.");
                    _firebaseReady = false;
                    OnFirebaseError?.Invoke(task.Result.ToString());
                }
            });

            Debug.Log("[FirebaseScenarioService] Running in local-JSON fallback mode. " +
                      "Import Firebase SDK and uncomment the InitFirebase block to use live data.");
            _firebaseReady = false;
        }

        // ══════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════
        public void FetchScenarioIndex(Action<List<ScenarioIndexEntry>> onComplete)
        {
            if (_firebaseReady) FetchIndexFromFirebase(onComplete);
            else FetchIndexFromLocal(onComplete);
        }

        // Reads from: playbook/scenarios/{scenarioId}
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


        public void SaveUserProgress(string userId, UserScenarioProgress progress,
                                     Action onComplete = null)
        {
            if (_firebaseReady) SaveProgressToFirebase(userId, progress, onComplete);
            else SaveProgressToLocal(userId, progress, onComplete);
        }

        public void FetchUserProgress(string userId, string scenarioId,
                                      Action<UserScenarioProgress> onComplete)
        {
            if (_firebaseReady) FetchProgressFromFirebase(userId, scenarioId, onComplete);
            else FetchProgressFromLocal(userId, scenarioId, onComplete);
        }

        // ══════════════════════════════════════════════════════════
        // Firebase implementations
        // ══════════════════════════════════════════════════════════

        private void FetchIndexFromFirebase(Action<List<ScenarioIndexEntry>> onComplete)
        {
            _dbRef.Child(dbRootNode).Child("scenarios_index")
                  .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully) { FetchIndexFromLocal(onComplete); return; }
                var list = new List<ScenarioIndexEntry>();
                foreach (var child in task.Result.Children)
                {
                    var e = JsonConvert.DeserializeObject<ScenarioIndexEntry>(child.GetRawJsonValue());
                    if (e != null) list.Add(e);
                }
                onComplete?.Invoke(list);
            });
            FetchIndexFromLocal(onComplete);
        }

        private void FetchScenarioFromFirebase(string id,
                                               Action<PlaybookScenario> onComplete,
                                               Action<string> onError)
        {
            _dbRef.Child(dbRootNode).Child("scenarios").Child(id)
                  .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully || !task.Result.Exists)
                {
                    FetchScenarioFromLocal(id, onComplete, onError);
                    return;
                }
                var s = JsonConvert.DeserializeObject<PlaybookScenario>(task.Result.GetRawJsonValue());
                onComplete?.Invoke(s);
            });
            FetchScenarioFromLocal(id, onComplete, onError);
        }

        private void FetchAllFromFirebase(Action<List<PlaybookScenario>> onComplete,
                                          Action<string> onError)
        {
            _dbRef.Child(dbRootNode).Child("scenarios")
                  .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully) { onError?.Invoke(task.Exception?.Message); return; }
                var list = new List<PlaybookScenario>();
                foreach (var child in task.Result.Children)
                {
                    var s = JsonConvert.DeserializeObject<PlaybookScenario>(child.GetRawJsonValue());
                    if (s != null) list.Add(s);
                }
                onComplete?.Invoke(list);
            });
            FetchAllFromLocal(onComplete, onError);
        }

        private void SaveProgressToFirebase(string userId, UserScenarioProgress progress,
                                            Action onComplete)
        {
            string json = JsonConvert.SerializeObject(progress);
            _dbRef.Child(dbRootNode).Child("user_progress").Child(userId)
                  .Child(progress.scenarioId).SetRawJsonValueAsync(json)
                  .ContinueWithOnMainThread(_ => onComplete?.Invoke());
            SaveProgressToLocal(userId, progress, onComplete);
        }

        private void FetchProgressFromFirebase(string userId, string scenarioId,
                                               Action<UserScenarioProgress> onComplete)
        {
            _dbRef.Child(dbRootNode).Child("user_progress").Child(userId)
                  .Child(scenarioId).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompletedSuccessfully || !task.Result.Exists)
                { onComplete?.Invoke(null); return; }
                var p = JsonConvert.DeserializeObject<UserScenarioProgress>(
                            task.Result.GetRawJsonValue());
                onComplete?.Invoke(p);
            });
            FetchProgressFromLocal(userId, scenarioId, onComplete);
        }

        // ══════════════════════════════════════════════════════════
        // Local JSON fallback
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
                    title = s.title,
                    exhibitNumber = s.exhibitNumber,
                    outlineDescription = s.outlineDescription,
                    thumbnailUrl = s.thumbnailUrl,
                    isPublished = s.isPublished,
                    totalQuestions = q,
                    pointsOnCompletion = s.pointsOnCompletion
                });
            }
            onComplete?.Invoke(list);
        }

        private void FetchScenarioFromLocal(string id,
                                            Action<PlaybookScenario> onComplete,
                                            Action<string> onError)
        {
            // Local JSON files live at: Assets/Resources/Scenarios/{id}.json
            var asset = Resources.Load<TextAsset>($"Scenarios/{id}");
            if (asset == null)
            {
                string msg = $"[FirebaseScenarioService] Local file not found: Resources/Scenarios/{id}.json\n" +
                             "Either add the JSON file, or connect Firebase so live data is used instead.";
                Debug.LogError(msg);
                onError?.Invoke(msg);
                return;
            }
            try
            {
                var scenario = JsonConvert.DeserializeObject<PlaybookScenario>(asset.text);
                onComplete?.Invoke(scenario);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseScenarioService] JSON parse error in {id}: {e.Message}");
                onError?.Invoke(e.Message);
            }
        }

        private void FetchAllFromLocal(Action<List<PlaybookScenario>> onComplete,
                                       Action<string> onError)
        {
            try { onComplete?.Invoke(LoadAllLocalScenarios()); }
            catch (Exception e) { onError?.Invoke(e.Message); }
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
                catch (Exception e)
                {
                    Debug.LogWarning($"[FirebaseScenarioService] Skipping {a.name}: {e.Message}");
                }
            }
            return list;
        }

        // ── Local progress ─────────────────────────────────────────

        private static string ProgressKey(string userId, string scenarioId) =>
            $"progress_{userId}_{scenarioId}";

        private static void SaveProgressToLocal(string userId,
                                                UserScenarioProgress progress,
                                                Action onComplete)
        {
            PlayerPrefs.SetString(ProgressKey(userId, progress.scenarioId),
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
    }
}