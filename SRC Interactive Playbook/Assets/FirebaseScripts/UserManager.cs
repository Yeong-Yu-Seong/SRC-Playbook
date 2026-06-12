/*
 * Description: Runtime singleton that holds the active session's User object.
 *              Other managers (ScoreManager, LeaderboardManager, etc.) read
 *              from and write to this central state, which is then persisted
 *              to Firebase via FirebaseManager.
 */

using System;
using UnityEngine;
using TMPro;

public class UserManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static UserManager Instance { get; private set; }

    // ── Current session user ───────────────────────────────────────────────────
    private User _currentUser;

    /// <summary>The User currently logged in. Null if not authenticated.</summary>
    public User CurrentUser => _currentUser;

    /// <summary>Fired whenever user data is updated (score changed, etc.).</summary>
    public event Action<User> OnUserUpdated;

    // ── Optional HUD references ────────────────────────────────────────────────
    [Header("HUD (optional — assign if you display score live)")]
    [SerializeField] private TMP_Text hudUsernameText;
    [SerializeField] private TMP_Text hudScoreText;

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

    // ══════════════════════════════════════════════════════════════════════════
    //  SESSION MANAGEMENT
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Called by LoginUIManager after a successful login.</summary>
    public void SetUserData(User user)
    {
        _currentUser = user;
        RefreshHUD();
        OnUserUpdated?.Invoke(_currentUser);
        Debug.Log($"[UserManager] Session started for: {user.username}");
    }

    /// <summary>Clears the session (used on logout).</summary>
    public void ClearUserData()
    {
        _currentUser = null;
        RefreshHUD();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCORE RECORDING (called by ScoreManager or directly by game systems)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Awards points for completing a branching simulation and persists to Firebase.
    /// </summary>
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
                Debug.Log($"[UserManager] Simulation '{simulationId}' awarded {points} pts. " +
                          $"New total: {_currentUser.score}");
                onSuccess?.Invoke();
            },
            onError: err =>
            {
                Debug.LogError($"[UserManager] Failed to save simulation score: {err}");
                onError?.Invoke(err);
            }
        );
    }

    /// <summary>
    /// Awards points for completing an assessment quiz and persists to Firebase.
    /// </summary>
    public void AwardQuizPoints(string quizId, int points,
                                Action onSuccess = null,
                                Action<string> onError = null)
    {
        if (!EnsureUser(onError)) return;

        FirebaseManager.Instance.RecordQuizCompletion(
            quizId, points, _currentUser,
            onSuccess: updatedUser =>
            {
                _currentUser = updatedUser;
                RefreshHUD();
                OnUserUpdated?.Invoke(_currentUser);
                Debug.Log($"[UserManager] Quiz '{quizId}' awarded {points} pts. " +
                          $"New total: {_currentUser.score}");
                onSuccess?.Invoke();
            },
            onError: err =>
            {
                Debug.LogError($"[UserManager] Failed to save quiz score: {err}");
                onError?.Invoke(err);
            }
        );
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
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
        const string msg = "No user session active. Please log in.";
        Debug.LogError($"[UserManager] {msg}");
        onError?.Invoke(msg);
        return false;
    }
}
