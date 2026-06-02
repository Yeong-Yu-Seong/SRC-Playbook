/*
 * Description: Central Firebase service layer.  Handles Authentication,
 *              Realtime-Database CRUD for User profiles, score updates
 *              from simulations / quizzes, and leaderboard fetching.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;          // ContinueWithOnMainThread

public class FirebaseManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static FirebaseManager Instance { get; private set; }

    // ── Internal state ─────────────────────────────────────────────────────────
    private FirebaseAuth      _auth;
    private DatabaseReference _db;
    private bool              _isInitialized;

    // ── Convenience accessors ──────────────────────────────────────────────────
    public bool   IsInitialized => _isInitialized;
    public string CurrentUserId => _auth?.CurrentUser?.UserId;

    private bool IsAuthenticated =>
        _auth?.CurrentUser != null &&
        !string.IsNullOrEmpty(_auth.CurrentUser.UserId);

    // ══════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Verify Firebase dependencies before touching any Firebase API
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                _auth           = FirebaseAuth.DefaultInstance;
                _db             = FirebaseDatabase.DefaultInstance.RootReference;
                _isInitialized  = true;
                Debug.Log("[FirebaseManager] Initialized successfully.");
            }
            else
            {
                Debug.LogError($"[FirebaseManager] Could not resolve Firebase dependencies: {task.Result}");
            }
        });
    }

    private void OnApplicationQuit() => SetUserOffline();

    // ══════════════════════════════════════════════════════════════════════════
    //  AUTHENTICATION — SIGN-UP
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a Firebase Auth account and writes a new User document to the database.
    /// </summary>
    /// <param name="username">Display name chosen by the learner.</param>
    /// <param name="email">Email address used as the auth credential.</param>
    /// <param name="password">Password (min 6 chars, enforced by Firebase).</param>
    /// <param name="onSuccess">Invoked after both auth and database writes succeed.</param>
    /// <param name="onError">Invoked with a human-readable message on any failure.</param>
    public void SignUp(string username, string email, string password,
                       Action onSuccess, Action<string> onError)
    {
        if (!_isInitialized) { onError("Firebase not ready. Please wait."); return; }

        FirebaseAuth.DefaultInstance
            .CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCanceled || authTask.IsFaulted)
                {
                    string msg = authTask.Exception?.GetBaseException().Message ?? "Sign-up failed.";
                    Debug.LogError($"[FirebaseManager] SignUp auth error: {msg}");
                    onError(msg);
                    return;
                }

                // Auth account created — now write the User profile
                CreateUserDocument(username, email, onSuccess, onError);
            });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  AUTHENTICATION — LOGIN
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Signs the user in via Firebase Auth and loads their User document.
    /// </summary>
    public void Login(string email, string password,
                      Action<User> onSuccess, Action<string> onError)
    {
        if (!_isInitialized) { onError("Firebase not ready. Please wait."); return; }

        FirebaseAuth.DefaultInstance
            .SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCanceled || authTask.IsFaulted)
                {
                    string msg = authTask.Exception?.GetBaseException().Message ?? "Login failed.";
                    Debug.LogError($"[FirebaseManager] Login error: {msg}");
                    onError(msg);
                    return;
                }

                // Mark user online and refresh lastLoginAt
                UpdateUserField("isLoggedIn", true,  () => { }, _ => { });
                UpdateUserField("lastLoginAt",
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    () => { }, _ => { });

                // Load full profile
                LoadUserDocument(onSuccess, onError);
            });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  AUTHENTICATION — LOGOUT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Signs the current user out of Firebase Auth.</summary>
    public void Logout(Action onSuccess = null, Action<string> onError = null)
    {
        SetUserOffline();
        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("[FirebaseManager] User signed out.");
        onSuccess?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DATABASE — USER DOCUMENT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Writes a brand-new User document for the currently authenticated user.</summary>
    public void CreateUserDocument(string username, string email,
                                   Action onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        User newUser       = new User(username, email);
        newUser.isLoggedIn = true;
        newUser.lastLoginAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        WriteUserJson(newUser, onSuccess, onError);
    }

    /// <summary>Loads the full User document for the currently authenticated user.</summary>
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
               if (!snap.Exists)
               {
                   onError("User data not found in database.");
                   return;
               }

               User user = JsonUtility.FromJson<User>(snap.GetRawJsonValue());
               Debug.Log($"[FirebaseManager] Loaded user: {user.username} | Score: {user.score}");
               onSuccess(user);
           });
    }

    /// <summary>Overwrites the entire User document with the supplied object.</summary>
    public void SaveUserDocument(User user, Action onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;
        WriteUserJson(user, onSuccess, onError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DATABASE — FIELD-LEVEL UPDATES
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Updates a single top-level field on the current user's document.</summary>
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
               Debug.Log($"[FirebaseManager] Field '{fieldName}' updated.");
               onSuccess();
           });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCORING — SIMULATION (Choose Your Next Step)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Records the completion of a branching-story simulation, adds the earned
    /// points to simulationScore and total score, and persists everything.
    /// Call this at the end of each simulation branch resolution.
    /// </summary>
    /// <param name="simulationId">Unique ID of the simulation (e.g. "sim_feedback_01").</param>
    /// <param name="pointsEarned">Points the player earned in this run.</param>
    /// <param name="currentUser">The in-memory User object (will be mutated and saved).</param>
    /// <param name="onSuccess">Returns the updated User after save.</param>
    /// <param name="onError">Error callback.</param>
    public void RecordSimulationCompletion(string simulationId, int pointsEarned,
                                           User currentUser,
                                           Action<User> onSuccess,
                                           Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        // Mutate in-memory object
        currentUser.simulationScore += pointsEarned;
        currentUser.score           += pointsEarned;
        currentUser.completedSimulations.Add(new CompletedModule(simulationId, pointsEarned));

        Debug.Log($"[FirebaseManager] Simulation '{simulationId}' completed. " +
                  $"+{pointsEarned} pts → total: {currentUser.score}");

        // Persist
        WriteUserJson(currentUser,
            onSuccess: () => onSuccess(currentUser),
            onError:   onError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCORING — QUIZ (Facts vs Opinions / MCQ / Drag-and-Drop)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Records the completion of any assessment quiz format, awards points,
    /// and persists the updated User document.
    /// </summary>
    /// <param name="quizId">Unique ID of the quiz (e.g. "quiz_mcq_module2").</param>
    /// <param name="pointsEarned">Points based on correct answers.</param>
    /// <param name="currentUser">The in-memory User object (will be mutated and saved).</param>
    /// <param name="onSuccess">Returns the updated User after save.</param>
    /// <param name="onError">Error callback.</param>
    public void RecordQuizCompletion(string quizId, int pointsEarned,
                                     User currentUser,
                                     Action<User> onSuccess,
                                     Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        currentUser.quizScore += pointsEarned;
        currentUser.score     += pointsEarned;
        currentUser.completedQuizzes.Add(new CompletedModule(quizId, pointsEarned));

        Debug.Log($"[FirebaseManager] Quiz '{quizId}' completed. " +
                  $"+{pointsEarned} pts → total: {currentUser.score}");

        WriteUserJson(currentUser,
            onSuccess: () => onSuccess(currentUser),
            onError:   onError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  LEADERBOARD
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches all user documents and returns them as a dictionary keyed by UID.
    /// Used by LeaderboardManager to render the ranked list.
    /// </summary>
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

               DataSnapshot snap = task.Result;
               Dictionary<string, User> users = new Dictionary<string, User>();

               if (!snap.Exists || !snap.HasChildren)
               {
                   Debug.Log("[FirebaseManager] No users found in database.");
                   onSuccess(users);
                   return;
               }

               foreach (DataSnapshot child in snap.Children)
               {
                   try
                   {
                       string json = child.GetRawJsonValue();
                       User   u    = JsonUtility.FromJson<User>(json);

                       if (u != null && !string.IsNullOrEmpty(u.username))
                           users[child.Key] = u;
                       else
                           Debug.LogWarning($"[FirebaseManager] Skipping invalid user entry: {child.Key}");
                   }
                   catch (Exception ex)
                   {
                       Debug.LogError($"[FirebaseManager] Error parsing user {child.Key}: {ex.Message}");
                   }
               }

               Debug.Log($"[FirebaseManager] Fetched {users.Count} users for leaderboard.");
               onSuccess(users);
           });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRESENCE
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Marks the authenticated user as offline in the database.</summary>
    public void SetUserOffline()
    {
        if (!IsAuthenticated) return;

        _db.Child("users").Child(CurrentUserId).Child("isLoggedIn")
           .SetValueAsync(false)
           .ContinueWithOnMainThread(task =>
           {
               if (task.IsCompleted)
                   Debug.Log("[FirebaseManager] User set offline.");
           });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void WriteUserJson(User user, Action onSuccess, Action<string> onError)
    {
        string json   = JsonUtility.ToJson(user);
        string userId = CurrentUserId;

        _db.Child("users").Child(userId)
           .SetRawJsonValueAsync(json)
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
        const string msg = "User is not authenticated. Please log in first.";
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
