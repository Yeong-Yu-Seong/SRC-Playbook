// ============================================================
// PointsManager.cs
// Central hub for earning, storing, and broadcasting points.
// Connects ScenarioManager → Firebase leaderboard node.
// Subscribe to OnTotalPointsChanged to update any score UI.
// ============================================================

using System;
using UnityEngine;
using RedCross.Playbook.Data;
using RedCross.Playbook.Scenario;

namespace RedCross.Playbook.Scenario
{
    public class PointsManager : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────
        public static PointsManager Instance { get; private set; }

        // ── Events ─────────────────────────────────────────────────
        public event Action<int> OnPointsAdded;
        public event Action<int> OnTotalPointsChanged;

        // ── State ──────────────────────────────────────────────────
        // Session total — accumulates points earned this session.
        // Resets when the user logs out.
        private int _sessionPoints = 0;
        public int SessionPoints => _sessionPoints;

        // Full total = session points + points already saved in Firebase
        // Read from UserManager.CurrentUser.score for display.
        public int TotalPoints =>
            UserManager.Instance?.CurrentUser != null
                ? UserManager.Instance.CurrentUser.score
                : _sessionPoints;

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
            TrySubscribeToScenarioManager();
        }

        private void OnDestroy()
        {
            UnsubscribeFromScenarioManager();
        }

        // ══════════════════════════════════════════════════════════
        // ScenarioManager subscription
        // Called by ScenarioSceneBootstrapper after scene loads
        // ══════════════════════════════════════════════════════════

        public void TrySubscribeToScenarioManager()
        {
            if (ScenarioManager.Instance == null)
            {
                Debug.Log("[PointsManager] ScenarioManager not found yet — will subscribe when available.");
                return;
            }

            UnsubscribeFromScenarioManager(); // prevent double-subscribe
            ScenarioManager.Instance.OnPointsAwarded += OnPointsAwardedHandler;
            ScenarioManager.Instance.OnScenarioCompleted += OnScenarioCompleted;
            Debug.Log("[PointsManager] Subscribed to ScenarioManager events.");
        }

        private void UnsubscribeFromScenarioManager()
        {
            if (ScenarioManager.Instance == null) return;
            ScenarioManager.Instance.OnPointsAwarded -= OnPointsAwardedHandler;
            ScenarioManager.Instance.OnScenarioCompleted -= OnScenarioCompleted;
        }

        // ══════════════════════════════════════════════════════════
        // Points tracking (called during the scenario, per answer)
        // ══════════════════════════════════════════════════════════

        private void OnPointsAwardedHandler(int delta)
        {
            if (delta <= 0) return;
            _sessionPoints += delta;
            OnPointsAdded?.Invoke(delta);
            OnTotalPointsChanged?.Invoke(TotalPoints);
            Debug.Log($"[PointsManager] +{delta} pts this session → session total: {_sessionPoints}");
        }

        // ══════════════════════════════════════════════════════════
        // Scenario complete — write to Firebase via UserManager
        // ══════════════════════════════════════════════════════════

        private void OnScenarioCompleted(UserScenarioProgress progress)
        {
            if (UserManager.Instance == null || UserManager.Instance.CurrentUser == null)
            {
                Debug.LogError("[PointsManager] Cannot save score — no user session in UserManager. " +
                               "Make sure the user is logged in before playing a scenario.");
                return;
            }

            string username = UserManager.Instance.CurrentUser.username;
            string scenarioId = progress.scenarioId;
            int points = progress.score;

            Debug.Log($"[PointsManager] Scenario '{scenarioId}' complete for '{username}'. " +
                      $"Awarding {points} pts via UserManager.");

            // This calls FirebaseManager.RecordSimulationCompletion()
            // which writes to /users/{uid}.simulationScore and .score
            // — the same path your app already uses everywhere.
            UserManager.Instance.AwardSimulationPoints(
                simulationId: scenarioId,
                points: points,
                onSuccess: () =>
                {
                    Debug.Log($"[PointsManager] Score saved to Firebase. " +
                              $"New total: {UserManager.Instance.CurrentUser.score}");
                    // Fire the event so HomepageUIManager updates the score label
                    OnTotalPointsChanged?.Invoke(TotalPoints);
                },
                onError: err =>
                {
                    Debug.LogError($"[PointsManager] Failed to save score to Firebase: {err}");
                }
            );
        }

        // ══════════════════════════════════════════════════════════
        // Call this on logout to reset session state
        // ══════════════════════════════════════════════════════════

        public void ResetSession()
        {
            _sessionPoints = 0;
            OnTotalPointsChanged?.Invoke(0);
        }
    }
}