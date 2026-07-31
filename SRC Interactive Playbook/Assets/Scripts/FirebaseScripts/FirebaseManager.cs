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

    public void OnAutoLoginSuccess(string userDataJSON)
    {
        Debug.Log("[FirebaseManager] Auto-login triggered by browser refresh!");
        var authData = JsonUtility.FromJson<WebGLAuthResponse>(userDataJSON);
        _currentUserId = authData.uid;

        LoadUserDocument(user =>
        {
            UserManager.Instance.SetUserData(user);
            UIManager.Instance.ShowHomepage();
        }, err => Debug.LogError("Auto-login DB fetch failed: " + err));
    }

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
        onSuccess?.Invoke();
    }

    public void OnUpdateSuccess(string info) { }
    public void OnDatabaseError(string err) { Debug.LogError($"Database Error: {err}"); }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCORING — SIMULATION / QUIZ (Highest Score Only)
    // ══════════════════════════════════════════════════════════════════════════

    private string _pendingModuleId;
    private string _pendingModuleType;
    private int _pendingModulePoints;
    private User _pendingUser;
    private Action<User> _pendingScoreSuccess;
    private Action<string> _pendingScoreError;

    public void RecordSimulationCompletion(string simulationId, int pointsEarned, User currentUser, Action<User> onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        _pendingModuleId = simulationId;
        _pendingModuleType = "scenario";
        _pendingModulePoints = pointsEarned;
        _pendingUser = currentUser;
        _pendingScoreSuccess = onSuccess;
        _pendingScoreError = onError;

        // Fetch the user's existing score for this scenario before adding points
        FirebaseDatabase.GetJSON($"users/{CurrentUserId}/completedSimulations/{simulationId}", gameObject.name, "OnCheckScoreSuccess", "OnDatabaseError");
    }

    public void RecordQuizCompletion(string quizId, int pointsEarned, int correctAnswers, int totalQuestions, Dictionary<string, string> answers, User currentUser, Action<User> onSuccess, Action<string> onError)
    {
        if (!AssertAuthenticated(onError)) return;

        _pendingModuleId = quizId;
        _pendingModuleType = "quiz";
        _pendingModulePoints = pointsEarned;
        _pendingUser = currentUser;
        _pendingScoreSuccess = onSuccess;
        _pendingScoreError = onError;

        // Fetch the user's existing score for this quiz before adding points
        FirebaseDatabase.GetJSON($"users/{CurrentUserId}/completedQuizzes/{quizId}", gameObject.name, "OnCheckScoreSuccess", "OnDatabaseError");
    }

    public void OnCheckScoreSuccess(string json)
    {
        int oldScore = 0;

        // Parse the existing score if they have played this before
        if (!string.IsNullOrEmpty(json) && json != "null")
        {
            var entry = JsonConvert.DeserializeObject<CompletedModule>(json);
            if (entry != null) oldScore = entry.pointsEarned;
        }

        // Only add points and save if they beat their previous high score
        if (_pendingModulePoints > oldScore)
        {
            int scoreDifference = _pendingModulePoints - oldScore;
            _pendingUser.score += scoreDifference;

            var entry = new CompletedModule(_pendingModuleId, _pendingModuleType, _pendingModulePoints);
            string entryJson = JsonConvert.SerializeObject(entry);
            string dbPath = _pendingModuleType == "quiz" ? "completedQuizzes" : "completedSimulations";

            FirebaseDatabase.UpdateJSON($"users/{CurrentUserId}/{dbPath}", $"{{\"{_pendingModuleId}\": {entryJson}}}", gameObject.name, "OnUpdateSuccess", "OnDatabaseError");
            UpdateUserField("score", _pendingUser.score, () => _pendingScoreSuccess?.Invoke(_pendingUser), _pendingScoreError);
        }
        else
        {
            // The score was lower than or equal to their best. Do nothing but return success.
            Debug.Log($"[FirebaseManager] Score {_pendingModulePoints} did not beat previous high score {oldScore}. Points not added.");
            _pendingScoreSuccess?.Invoke(_pendingUser);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SURVEY & CHEATSHEET
    // ══════════════════════════════════════════════════════════════════════════

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