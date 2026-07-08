// ============================================================
// PointsManager.cs
// Central hub for earning, storing, and broadcasting points.
// Connects ScenarioManager → Firebase leaderboard node.
// Subscribe to OnTotalPointsChanged to update any score UI.
// ============================================================

using System;
using RedCross.Playbook.Data;
using RedCross.Playbook.Scenario;
using UnityEngine;

namespace RedCross.Playbook.Scenario
{
    public class PointsManager : MonoBehaviour
    {
        public static PointsManager Instance { get; private set; }

        public event Action<int> OnPointsAdded;
        public event Action<int> OnTotalPointsChanged;

        private int _sessionPoints;
        public int SessionPoints => _sessionPoints;

        public int TotalPoints =>
            UserManager.Instance?.CurrentUser != null
                ? UserManager.Instance.CurrentUser.score
                : _sessionPoints;

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
            TrySubscribeToScenarioManager();
            if (UserManager.Instance != null)
            {
                UserManager.Instance.OnUserUpdated += OnUserUpdatedHandler;
            }
        }
        private void OnDestroy()
        {
            UnsubscribeFromScenarioManager();
            if (UserManager.Instance != null)
            {
                UserManager.Instance.OnUserUpdated -= OnUserUpdatedHandler;
            }
        }

        private void OnUserUpdatedHandler(User updatedUser)
        {
            OnTotalPointsChanged?.Invoke(TotalPoints);
        }

        // ══════════════════════════════════════════════════════════
        //  SUBSCRIPTION
        // ══════════════════════════════════════════════════════════

        public void TrySubscribeToScenarioManager()
        {
            if (ScenarioManager.Instance == null) return;
            UnsubscribeFromScenarioManager();
            ScenarioManager.Instance.OnPointsAwarded += OnPointsAwardedHandler;
            ScenarioManager.Instance.OnScenarioCompleted += OnScenarioCompleted;
            Debug.Log("[PointsManager] Subscribed to ScenarioManager.");
        }

        private void UnsubscribeFromScenarioManager()
        {
            if (ScenarioManager.Instance == null) return;
            ScenarioManager.Instance.OnPointsAwarded -= OnPointsAwardedHandler;
            ScenarioManager.Instance.OnScenarioCompleted -= OnScenarioCompleted;
        }

        // ══════════════════════════════════════════════════════════
        //  HANDLERS
        // ══════════════════════════════════════════════════════════

        private void OnPointsAwardedHandler(int delta)
        {
            if (delta <= 0) return;
            _sessionPoints += delta;
            OnPointsAdded?.Invoke(delta);
            OnTotalPointsChanged?.Invoke(TotalPoints);
        }

        private void OnScenarioCompleted(UserScenarioProgress progress)
        {
            if (UserManager.Instance?.CurrentUser == null)
            {
                Debug.LogError("[PointsManager] No user session — score not saved.");
                return;
            }

            UserManager.Instance.AwardSimulationPoints(
                simulationId: progress.scenarioId,
                points: progress.score,
                onSuccess: () =>
                {
                    Debug.Log($"[PointsManager] Score saved. Total: {UserManager.Instance.CurrentUser.score}");
                    OnTotalPointsChanged?.Invoke(TotalPoints);
                },
                onError: err => Debug.LogError($"[PointsManager] Save failed: {err}"));
        }

        // ══════════════════════════════════════════════════════════
        //  LOGOUT
        // ══════════════════════════════════════════════════════════

        public void ResetSession()
        {
            _sessionPoints = 0;
            OnTotalPointsChanged?.Invoke(0);
        }
    }
}