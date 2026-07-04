/*
 * Description: Runtime singleton that holds the active session's User object.
 *              Other managers (ScoreManager, LeaderboardManager, etc.) read
 *              from and write to this central state, which is then persisted
 *              to Firebase via FirebaseManager.
 */

using System;
using System.Collections.Generic;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;

public class UserManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static UserManager Instance { get; private set; }

    private User _currentUser;
    public User CurrentUser => _currentUser;

    public event Action<User> OnUserUpdated;

    [Header("HUD (optional)")]
    [SerializeField] private TMP_Text hudUsernameText;
    [SerializeField] private TMP_Text hudScoreText;

    // ══════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SESSION
    // ══════════════════════════════════════════════════════════════════════════

    public void SetUserData(User user)
    {
        _currentUser = user;
        RefreshHUD();
        OnUserUpdated?.Invoke(_currentUser);
        Debug.Log($"[UserManager] Session started: {user.username}");
    }

    public void ClearUserData()
    {
        _currentUser = null;
        RefreshHUD();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCORE RECORDING
    // ══════════════════════════════════════════════════════════════════════════

    public void AwardSimulationPoints(string simulationId, int points,
                                      Action onSuccess = null,
                                      Action<string> onError = null)
    {
        if (!EnsureUser(onError)) return;

        FirebaseManager.Instance.RecordSimulationCompletion(
            simulationId, points, _currentUser,
            onSuccess: updatedUser =>
            {
                _currentUser = updatedUser;
                RefreshHUD();
                OnUserUpdated?.Invoke(_currentUser);
                Debug.Log($"[UserManager] '{simulationId}' +{points} pts → {_currentUser.score}");
                onSuccess?.Invoke();
            },
            onError: err =>
            {
                Debug.LogError($"[UserManager] Simulation save failed: {err}");
                onError?.Invoke(err);
            });
    }

    /// <summary>
    /// Awards points to the current user after completing a quiz and syncs the updated user data to Firebase.
    /// </summary>
    public void AwardQuizPoints(string quizId, int points,
                                int correctAnswers, int totalQuestions,
                                Dictionary<string, string> answers,
                                Action onSuccess = null,
                                Action<string> onError = null)
    {
        if (!EnsureUser(onError)) return;

        FirebaseManager.Instance.RecordQuizCompletion(
            quizId, points, correctAnswers, totalQuestions, answers, _currentUser,
            onSuccess: updatedUser =>
            {
                _currentUser = updatedUser;
                RefreshHUD();
                OnUserUpdated?.Invoke(_currentUser);
                Debug.Log($"[UserManager] '{quizId}' +{points} pts → {_currentUser.score}");
                onSuccess?.Invoke();
            },
            onError: err =>
            {
                Debug.LogError($"[UserManager] Quiz save failed: {err}");
                onError?.Invoke(err);
            });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════════════════════════════

    private void RefreshHUD()
    {
        if (hudUsernameText != null)
            hudUsernameText.text = _currentUser?.username ?? string.Empty;

        if (hudScoreText != null)
            hudScoreText.text = _currentUser != null
                ? _currentUser.score.ToString("N0") + " pts"
                : string.Empty;
    }

    private bool EnsureUser(Action<string> onError)
    {
        if (_currentUser != null) return true;
        const string msg = "No user session active.";
        Debug.LogError($"[UserManager] {msg}");
        onError?.Invoke(msg);
        return false;
    }
}