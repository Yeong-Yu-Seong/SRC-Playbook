using System;
using System.Collections.Generic;
using System.Linq;
using RedCross.Playbook.Data;
using UnityEngine;
using FirebaseWebGL.Scripts.FirebaseBridge; // Uses the WebGL Bridge
using Newtonsoft.Json;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private bool _isInitialized;
    private string _currentUserId;

    public bool IsInitialized => _isInitialized;
    public string CurrentUserId => _currentUserId;
    private bool IsAuthenticated => !string.IsNullOrEmpty(_currentUserId);

    // Temporary Callback Storage for WebGL string messaging
    private Action _onSignUpSuccess;
    private Action<string> _onSignUpError;
    private Action<User> _onLoginSuccess;
    private Action<string> _onLoginError;
    private string _pendingUsername;
    private string _pendingEmail;

    [Serializable]
    private class WebGLAuthResponse
    {
        public string uid;
        public string email;
        public string displayName;
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start()
    {
        _isInitialized = true; // WebGL doesn't require native dependency checks
        Debug.Log("[FirebaseManager] Initialized for WebGL.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  AUTHENTICATION
    // ══════════════════════════════════════════════════════════════════════════

    public void SignUp(string username, string email, string password, Action onSuccess, Action<string> onError)
    {
        if (!_isInitialized) { onError("Firebase not ready."); return; }
        _onSignUpSuccess = onSuccess;
        _onSignUpError = onError;
        _pendingUsername = username;
        _pendingEmail = email;

        FirebaseAuth.CreateUserWithEmailAndPassword(email, password, gameObject.name, "OnSignUpSuccess", "OnAuthFailed");
    }

    public void OnSignUpSuccess(string userDataJSON)
    {
        var authData = JsonUtility.FromJson<WebGLAuthResponse>(userDataJSON);
        _currentUserId = authData.uid;
        CreateUserDocument(_pendingUsername, _pendingEmail, _onSignUpSuccess, _onSignUpError);
    }

    public void Login(string email, string password, Action<User> onSuccess, Action<string> onError)
    {
        if (!_isInitialized) { onError("Firebase not ready."); return; }
        _onLoginSuccess = onSuccess;
        _onLoginError = onError;
        FirebaseAuth.SignInWithEmailAndPassword(email, password, gameObject.name, "OnLoginSuccess", "OnAuthFailed");
    }

    public void OnLoginSuccess(string userDataJSON)
    {
        var authData = JsonUtility.FromJson<WebGLAuthResponse>(userDataJSON);
        _currentUserId = authData.uid;

        UpdateUserField("lastLoginAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), () => { }, _ => { });

        LoadUserDocument(user => {
            if (ProfileManager.profileManagerInstance != null)
            {
                ProfileManager.profileManagerInstance.gameObject.SetActive(true);
                ProfileManager.profileManagerInstance.ShowProfile();
            }
            _onLoginSuccess?.Invoke(user);
        }, _onLoginError);
    }

    public void LoginWithGoogle(Action<User> onSuccess, Action<string> onError)
    {
        if (!_isInitialized) { onError("Firebase not ready."); return; }

#if UNITY_EDITOR
        Debug.Log("<color=yellow>[FirebaseManager] Editor detected: Mocking Google Login.</color>");
        _currentUserId = "mock_editor_uid";
        User mockUser = new User("EditorTester", "tester@google.com") { score = 999, hasCompletedPreSurvey = true };
        onSuccess(mockUser);
        return;
#endif

        _onLoginSuccess = onSuccess;
        _onLoginError = onError;
        FirebaseAuth.SignInWithGoogle(gameObject.name, "OnGoogleLoginSuccess", "OnAuthFailed");
    }

    public void OnGoogleLoginSuccess(string userDataJSON)
    {
        var authData = JsonUtility.FromJson<WebGLAuthResponse>(userDataJSON);
        _currentUserId = authData.uid;
        string newUsername = string.IsNullOrEmpty(authData.displayName) ? "Google User" : authData.displayName;

        LoadUserDocument(user => {
            UpdateUserField("lastLoginAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds(), () => { }, _ => { });
            _onLoginSuccess?.Invoke(user);
        }, err => {
            CreateUserDocument(newUsername, authData.email, () => LoadUserDocument(_onLoginSuccess, _onLoginError), _onLoginError);
        });
    }

    public void OnAuthFailed(string errorJSON) { _onSignUpError?.Invoke(errorJSON); _onLoginError?.Invoke(errorJSON); }

    public void Logout(Action onSuccess = null, Action<string> onError = null)
    {
        _currentUserId = null;
        FirebaseAuth.SignOut(gameObject.name, "OnLogoutSuccess", "OnLogoutFailed");
        onSuccess?.Invoke();
    }
    public void OnLogoutSuccess(string info) { Debug.Log("Signed out."); }
    public void OnLogoutFailed(string err) { Debug.LogError("Sign out failed: " + err); }

    // ══════════════════════════════════════════════════════════════════════════
    //  DATABASE — USER DOCUMENT
    // ══════════════════════════════════════════════════════════════════════════

    public void CreateUserDocument(string username, string email, Action onSuccess, Action<string> onError)
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

    private Action<User> _onLoadUserSuccess;
    private Action<string> _onLoadUserError;

    public void LoadUserDocument(Action<User> onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;
        _onLoadUserSuccess = onSuccess;
        _onLoadUserError = onError;
        FirebaseDatabase.GetJSON($"users/{CurrentUserId}", gameObject.name, "OnLoadUserSuccess", "OnDatabaseError");
    }
    public void OnLoadUserSuccess(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "null") { _onLoadUserError?.Invoke("User not found."); return; }
        _onLoadUserSuccess?.Invoke(JsonUtility.FromJson<User>(json));
    }

    public void UpdateUserField(string fieldName, object value, Action onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;
        string jsonValue = JsonConvert.SerializeObject(value);
        FirebaseDatabase.UpdateJSON($"users/{CurrentUserId}", $"{{\"{fieldName}\": {jsonValue}}}", gameObject.name, "OnUpdateSuccess", "OnDatabaseError");
        onSuccess?.Invoke(); // Simplified callback for WebGL
    }

    public void OnUpdateSuccess(string info) { }
    public void OnDatabaseError(string err) { Debug.LogError($"Database Error: {err}"); }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCORING — SIMULATION / QUIZ / SURVEY
    // ══════════════════════════════════════════════════════════════════════════

    public void RecordSimulationCompletion(string simulationId, int pointsEarned, User currentUser, Action<User> onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        currentUser.score += pointsEarned; // Assuming this is a new score for brevity in WebGL port
        var entry = new CompletedModule(simulationId, "scenario", pointsEarned);

        string entryJson = JsonConvert.SerializeObject(entry);
        FirebaseDatabase.UpdateJSON($"users/{CurrentUserId}/completedSimulations", $"{{\"{simulationId}\": {entryJson}}}", gameObject.name, "OnUpdateSuccess", "OnDatabaseError");
        UpdateUserField("score", currentUser.score, () => onSuccess(currentUser), onError);
    }

    public void RecordQuizCompletion(string quizId, int pointsEarned, int correctAnswers, int totalQuestions, Dictionary<string, string> answers, User currentUser, Action<User> onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        currentUser.score += pointsEarned;
        var entry = new CompletedModule(quizId, "quiz", pointsEarned);
        string entryJson = JsonConvert.SerializeObject(entry);

        FirebaseDatabase.UpdateJSON($"users/{CurrentUserId}/completedQuizzes", $"{{\"{quizId}\": {entryJson}}}", gameObject.name, "OnUpdateSuccess", "OnDatabaseError");
        UpdateUserField("score", currentUser.score, () => onSuccess(currentUser), onError);
    }

    public void RecordPulseSurvey(string surveyType, Dictionary<string, object> answers, User currentUser, Action<User> onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;
        string flagField = surveyType == "pre_survey" ? "hasCompletedPreSurvey" : "hasCompletedPostSurvey";
        if (surveyType == "pre_survey") currentUser.hasCompletedPreSurvey = true;
        else currentUser.hasCompletedPostSurvey = true;

        UpdateUserField(flagField, true, () => onSuccess(currentUser), onError);
    }

    public void RecordCheatsheetAccess(string cheatsheetId, Action onSuccess = null, Action<string> onError = null)
    {
        if (!AssertAuthenticated(onError ?? (_ => { }))) return;
        // In WebGL, simple metrics are often sent as a fire-and-forget push.
        FirebaseDatabase.PushJSON($"playbook/cheatsheet_access/{CurrentUserId}/{cheatsheetId}", "{\"accessedAt\": " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "}", gameObject.name, "OnUpdateSuccess", "OnDatabaseError");
        onSuccess?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  LEADERBOARD
    // ══════════════════════════════════════════════════════════════════════════

    private Action<List<User>> _onLeaderboardSuccess;
    public void FetchLeaderboard(int limit, Action<List<User>> onSuccess, Action<string> onError)
    {
        _onLeaderboardSuccess = onSuccess;
        FirebaseDatabase.GetJSON("users", gameObject.name, "OnLeaderboardGetSuccess", "OnDatabaseError");
    }

    public void OnLeaderboardGetSuccess(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "null") { _onLeaderboardSuccess?.Invoke(new List<User>()); return; }
        var usersDict = JsonConvert.DeserializeObject<Dictionary<string, User>>(json);
        var results = usersDict.Values.Where(u => u != null && !string.IsNullOrEmpty(u.username)).OrderByDescending(u => u.score).Take(10).ToList();
        _onLeaderboardSuccess?.Invoke(results);
    }

    private void WriteUserJson(User user, Action onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(user);
        FirebaseDatabase.PostJSON($"users/{CurrentUserId}", json, gameObject.name, "OnUpdateSuccess", "OnDatabaseError");
        onSuccess?.Invoke();
    }

    private bool AssertAuthenticated(Action<string> onError)
    {
        if (IsAuthenticated) return true;
        onError("User is not authenticated.");
        return false;
    }
}