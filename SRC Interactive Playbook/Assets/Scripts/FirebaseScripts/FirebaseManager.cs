/*
 * Description: Central Firebase service layer.  Handles Authentication,
 *              Realtime-Database CRUD for User profiles, score updates
 *              from simulations / quizzes, and leaderboard fetching.
 */

using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using RedCross.Playbook.Data;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static FirebaseManager Instance { get; private set; }

    // ── Internal state ─────────────────────────────────────────────────────────
    private FirebaseAuth _auth;
    private DatabaseReference _db;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;
    public string CurrentUserId => _auth?.CurrentUser?.UserId;

    private bool IsAuthenticated =>
        _auth?.CurrentUser != null &&
        !string.IsNullOrEmpty(_auth.CurrentUser.UserId);

    // ══════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                _auth = FirebaseAuth.DefaultInstance;
                _db = FirebaseDatabase.DefaultInstance.RootReference;
                _isInitialized = true;
                Debug.Log("[FirebaseManager] Initialized.");
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Dependency error: {task.Result}");
            }
        });
    }

    // OnApplicationQuit: no longer calls SetUserOffline — isLoggedIn is removed.
    // Firebase Auth state is the source of truth for session presence.

    // ══════════════════════════════════════════════════════════════════════════
    //  AUTHENTICATION — SIGN-UP
    // ══════════════════════════════════════════════════════════════════════════

    public void SignUp(string username, string email, string password,
                       Action onSuccess, Action<string> onError)
    {
        if (!_isInitialized) { onError("Firebase not ready."); return; }

        FirebaseAuth.DefaultInstance
            .CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    onError(task.Exception?.GetBaseException().Message ?? "Sign-up failed.");
                    return;
                }
                CreateUserDocument(username, email, onSuccess, onError);
            });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  AUTHENTICATION — LOGIN
    // ══════════════════════════════════════════════════════════════════════════

    public void Login(string email, string password,
                      Action<User> onSuccess, Action<string> onError)
    {
        if (!_isInitialized) { onError("Firebase not ready."); return; }

        FirebaseAuth.DefaultInstance
            .SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    onError(task.Exception?.GetBaseException().Message ?? "Login failed.");
                    return;
                }

                UpdateUserField("lastLoginAt",
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds(), () => { }, _ => { });

                LoadUserDocument(onSuccess, onError);
                ProfileManager.profileManagerInstance.gameObject.SetActive(true); // Enable the ProfileManager script when the user logs in
                ProfileManager.profileManagerInstance.ShowProfile(); // Call ShowProfile to display the user's profile information
            });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  AUTHENTICATION — LOGOUT
    // ══════════════════════════════════════════════════════════════════════════

    public void Logout(Action onSuccess = null, Action<string> onError = null)
    {
        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("[FirebaseManager] User signed out.");
        onSuccess?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DATABASE — USER DOCUMENT
    // ══════════════════════════════════════════════════════════════════════════

    public void CreateUserDocument(string username, string email,
                                   Action onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        var newUser = new User(username, email)
        {
            lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            score = 0
        };

        WriteUserJson(newUser, onSuccess, onError);
    }

    public void LoadUserDocument(Action<User> onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        _db.Child("users").Child(CurrentUserId)
           .GetValueAsync()
           .ContinueWithOnMainThread(task =>
           {
               if (task.IsCanceled || task.IsFaulted)
               {
                   HandleTaskError(task, "LoadUserDocument", onError);
                   return;
               }

               DataSnapshot snap = task.Result;
               if (!snap.Exists) { onError("User data not found."); return; }

               User user = JsonUtility.FromJson<User>(snap.GetRawJsonValue());
               Debug.Log($"[FirebaseManager] Loaded: {user.username} | Score: {user.score}");
               onSuccess(user);
           });
    }

    public void SaveUserDocument(User user, Action onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;
        WriteUserJson(user, onSuccess, onError);
    }

    public void UpdateUserField(string fieldName, object value,
                                Action onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        _db.Child("users").Child(CurrentUserId).Child(fieldName)
           .SetValueAsync(value)
           .ContinueWithOnMainThread(task =>
           {
               if (task.IsCanceled || task.IsFaulted)
               {
                   HandleTaskError(task, $"UpdateUserField({fieldName})", onError);
                   return;
               }
               onSuccess();
           });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCORING — SIMULATION
    // ══════════════════════════════════════════════════════════════════════════

    public void RecordSimulationCompletion(string simulationId, int pointsEarned,
                                           User currentUser,
                                           Action<User> onSuccess,
                                           Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        currentUser.score += pointsEarned;

        var completionEntry = new Dictionary<string, object>
        {
            { "moduleId",     simulationId },
            { "moduleType",   "scenario" },
            { "completedAt",  DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "pointsEarned", pointsEarned }
        };

        // Push generates a unique key — safe append, safe delete, no read needed.
        DatabaseReference pushRef =
            _db.Child("users").Child(CurrentUserId)
               .Child("completedSimulations").Push();

        pushRef.SetValueAsync(completionEntry).ContinueWithOnMainThread(pushTask =>
        {
            if (pushTask.IsCanceled || pushTask.IsFaulted)
            {
                HandleTaskError(pushTask, "RecordSimulationCompletion (push)", onError);
                return;
            }

            // Update the single score field
            _db.Child("users").Child(CurrentUserId).Child("score")
               .SetValueAsync(currentUser.score)
               .ContinueWithOnMainThread(scoreTask =>
               {
                   if (scoreTask.IsCanceled || scoreTask.IsFaulted)
                   {
                       HandleTaskError(scoreTask, "RecordSimulationCompletion (score)", onError);
                       return;
                   }

                   Debug.Log($"[FirebaseManager] Simulation '{simulationId}' saved. " +
                             $"Score: {currentUser.score}");
                   onSuccess(currentUser);
               });
        });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCORING — QUIZ
    // ══════════════════════════════════════════════════════════════════════════

    public void RecordQuizCompletion(string quizId, int pointsEarned,
                                     int correctAnswers, int totalQuestions,
                                     Dictionary<string, string> answers,
                                     User currentUser,
                                     Action<User> onSuccess,
                                     Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        // writes to playbook/quiz_progress/{uid}/{quizId}
        currentUser.score += pointsEarned;

        var quizProgressEntry = new Dictionary<string, object>
        {
            { "completed",      true },
            { "completedAt",    DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "score",          pointsEarned },
            { "correctAnswers", correctAnswers },
            { "totalQuestions", totalQuestions },
            { "answers",        answers ?? new Dictionary<string, string>() }
        };

        // Write quiz_progress
        _db.Child("playbook").Child("quiz_progress")
           .Child(CurrentUserId).Child(quizId)
           .SetValueAsync(quizProgressEntry)
           .ContinueWithOnMainThread(qpTask =>
           {
               if (qpTask.IsCanceled || qpTask.IsFaulted)
               {
                   HandleTaskError(qpTask, "RecordQuizCompletion (quiz_progress)", onError);
                   return;
               }

               // Push completion record onto user (same pattern as simulation)
               var completionEntry = new Dictionary<string, object>
               {
                   { "moduleId",     quizId },
                   { "moduleType",   "quiz" },
                   { "completedAt",  DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                   { "pointsEarned", pointsEarned }
               };

               _db.Child("users").Child(CurrentUserId)
                  .Child("completedSimulations").Push()
                  .SetValueAsync(completionEntry)
                  .ContinueWithOnMainThread(pushTask =>
                  {
                      if (pushTask.IsCanceled || pushTask.IsFaulted)
                      {
                          HandleTaskError(pushTask, "RecordQuizCompletion (push)", onError);
                          return;
                      }

                      // Update single score field
                      _db.Child("users").Child(CurrentUserId).Child("score")
                         .SetValueAsync(currentUser.score)
                         .ContinueWithOnMainThread(scoreTask =>
                         {
                             if (scoreTask.IsCanceled || scoreTask.IsFaulted)
                             {
                                 HandleTaskError(scoreTask, "RecordQuizCompletion (score)", onError);
                                 return;
                             }

                             Debug.Log($"[FirebaseManager] Quiz '{quizId}' saved. " +
                                       $"Score: {currentUser.score}");
                             onSuccess(currentUser);
                         });
                  });
           });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CHEATSHEET ACCESS TRACKING (NEW)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Call this when a user scans an AR QR code and the cheatsheet opens.
    /// Writes to playbook/cheatsheet_access/{uid}/{cheatsheetId}.
    /// </summary>
    public void RecordCheatsheetAccess(string cheatsheetId,
                                       Action onSuccess = null,
                                       Action<string> onError = null)
    {
        if (!AssertAuthenticated(onError ?? (_ => { }))) return;

        DatabaseReference accessRef =
            _db.Child("playbook").Child("cheatsheet_access")
               .Child(CurrentUserId).Child(cheatsheetId);

        // Read existing entry to increment accessCount
        accessRef.GetValueAsync().ContinueWithOnMainThread(getTask =>
        {
            if (getTask.IsCanceled || getTask.IsFaulted)
            {
                HandleTaskError(getTask, "RecordCheatsheetAccess (read)", onError ?? (_ => { }));
                return;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int accessCount = 1;
            long firstAccess = now;

            if (getTask.Result.Exists)
            {
                var snap = getTask.Result;
                accessCount = (int)(long)(snap.Child("accessCount").Value ?? 0L) + 1;
                firstAccess = (long)(snap.Child("firstAccessedAt").Value ?? now);
            }

            var entry = new Dictionary<string, object>
            {
                { "firstAccessedAt", firstAccess },
                { "lastAccessedAt",  now },
                { "accessCount",     accessCount }
            };

            accessRef.SetValueAsync(entry).ContinueWithOnMainThread(setTask =>
            {
                if (setTask.IsCanceled || setTask.IsFaulted)
                {
                    HandleTaskError(setTask, "RecordCheatsheetAccess (write)", onError ?? (_ => { }));
                    return;
                }
                Debug.Log($"[FirebaseManager] Cheatsheet '{cheatsheetId}' access #{accessCount} recorded.");
                onSuccess?.Invoke();
            });
        });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  LEADERBOARD
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches the top N users ordered by score directly from users/.
    /// </summary>
    public void FetchLeaderboard(int limit, Action<List<User>> onSuccess,
                                 Action<string> onError)
    {
        _db.Child("users")
           .OrderByChild("score")
           .LimitToLast(limit)
           .GetValueAsync()
           .ContinueWithOnMainThread(task =>
           {
               if (task.IsCanceled || task.IsFaulted)
               {
                   HandleTaskError(task, "FetchLeaderboard", onError);
                   return;
               }

               var results = new List<User>();
               foreach (DataSnapshot child in task.Result.Children)
               {
                   try
                   {
                       User u = JsonUtility.FromJson<User>(child.GetRawJsonValue());
                       if (u != null && !string.IsNullOrEmpty(u.username))
                           results.Add(u);
                   }
                   catch (Exception ex)
                   {
                       Debug.LogWarning($"[FirebaseManager] Skipping user {child.Key}: {ex.Message}");
                   }
               }

               // OrderByChild returns ascending — reverse for rank display
               results.Reverse();
               Debug.Log($"[FirebaseManager] Leaderboard: {results.Count} entries.");
               onSuccess(results);
           });
    }

    // Keep FetchAllUsers for any callers that still need the full dict
    public void FetchAllUsers(Action<Dictionary<string, User>> onSuccess,
                              Action<string> onError)
    {
        _db.Child("users").GetValueAsync()
           .ContinueWithOnMainThread(task =>
           {
               if (task.IsCanceled || task.IsFaulted)
               {
                   HandleTaskError(task, "FetchAllUsers", onError);
                   return;
               }

               var users = new Dictionary<string, User>();
               foreach (DataSnapshot child in task.Result.Children)
               {
                   try
                   {
                       User u = JsonUtility.FromJson<User>(child.GetRawJsonValue());
                       if (u != null && !string.IsNullOrEmpty(u.username))
                           users[child.Key] = u;
                   }
                   catch (Exception ex)
                   {
                       Debug.LogError($"[FirebaseManager] Error parsing {child.Key}: {ex.Message}");
                   }
               }
               onSuccess(users);
           });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void WriteUserJson(User user, Action onSuccess, Action<string> onError)
    {
        _db.Child("users").Child(CurrentUserId)
           .SetRawJsonValueAsync(JsonUtility.ToJson(user))
           .ContinueWithOnMainThread(task =>
           {
               if (task.IsCanceled || task.IsFaulted)
               {
                   HandleTaskError(task, "WriteUserJson", onError);
                   return;
               }
               onSuccess();
           });
    }

    private bool AssertAuthenticated(Action<string> onError)
    {
        if (IsAuthenticated) return true;
        const string msg = "User is not authenticated.";
        Debug.LogWarning($"[FirebaseManager] {msg}");
        onError(msg);
        return false;
    }

    private void HandleTaskError(System.Threading.Tasks.Task task,
                                  string context, Action<string> onError)
    {
        string msg = task.Exception?.GetBaseException().Message ?? "Unknown error.";
        Debug.LogError($"[FirebaseManager] {context} failed: {msg}");
        onError(msg);
    }
}