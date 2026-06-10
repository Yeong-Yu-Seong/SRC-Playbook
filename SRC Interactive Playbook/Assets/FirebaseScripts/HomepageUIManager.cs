using UnityEngine;
using TMPro;

namespace RedCross.Playbook.UI
{
    public class HomepageUIManager : MonoBehaviour
    {
        // ── Inspector: Panels ──────────────────────────────────────
        [Header("Panels — drag from HomeScene Hierarchy")]
        [SerializeField] private GameObject homepagePanel;
        [SerializeField] private GameObject signInPanel;
        [SerializeField] private GameObject signUpPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject leaderboardPanel;

        // ── Inspector: Text fields ─────────────────────────────────
        [Header("Homepage text fields")]
        [Tooltip("Shows 'Welcome, {username}'")]
        [SerializeField] private TextMeshProUGUI displayNameText;

        [Tooltip("Shows total score in pts")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Tooltip("Optional — simulation score only")]
        [SerializeField] private TextMeshProUGUI simulationScoreText;

        // ── Inspector: Scenario list ───────────────────────────────
        [Header("Scenario list (refreshes completed badges on return)")]
        [SerializeField] private ScenarioListUI scenarioListUI;

        // ══════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════

        private void Start()
        {
            // Subscribe to UserManager so any score change updates the UI
            if (UserManager.Instance != null)
                UserManager.Instance.OnUserUpdated += OnUserUpdated;
            else
                Debug.LogWarning("[HomepageUIManager] UserManager.Instance not found. " +
                                 "Make sure UserManager is initialised before HomepageUIManager.");

            // Subscribe to PointsManager for live in-scenario updates
            if (RedCross.Playbook.Scenario.PointsManager.Instance != null)
                RedCross.Playbook.Scenario.PointsManager.Instance.OnTotalPointsChanged += OnTotalPointsChanged;
        }

        private void OnEnable()
        {
            // Called every time this panel activates —
            // including when HomeScene reloads from ScenarioScene.
            RefreshHomepage();
        }

        private void OnDestroy()
        {
            if (UserManager.Instance != null)
                UserManager.Instance.OnUserUpdated -= OnUserUpdated;

            if (RedCross.Playbook.Scenario.PointsManager.Instance != null)
                RedCross.Playbook.Scenario.PointsManager.Instance.OnTotalPointsChanged -= OnTotalPointsChanged;
        }

        // ══════════════════════════════════════════════════════════
        // Refresh — called on OnEnable and whenever user data changes
        // ══════════════════════════════════════════════════════════

        public void RefreshHomepage()
        {
            // 1. Force correct panel visibility
            ShowHomepagePanel();

            // 2. Populate from UserManager (already loaded by FirebaseManager at login)
            if (UserManager.Instance?.CurrentUser != null)
                ApplyUserToUI(UserManager.Instance.CurrentUser);
            else
                Debug.LogWarning("[HomepageUIManager] CurrentUser is null — " +
                                 "display name and score will be blank until login completes.");

            // 3. Refresh exhibit cards so completed badges update
            if (scenarioListUI != null)
                scenarioListUI.RefreshList();
        }

        // ══════════════════════════════════════════════════════════
        // Event handlers
        // ══════════════════════════════════════════════════════════

        // Fires after login and after every Firebase score write
        private void OnUserUpdated(User user) => ApplyUserToUI(user);

        // Fires live during a scenario (per correct answer)
        private void OnTotalPointsChanged(int total)
        {
            if (scoreText != null)
                scoreText.text = $"{total} pts";
        }

        // ══════════════════════════════════════════════════════════
        // UI helpers
        // ══════════════════════════════════════════════════════════

        public void ShowHomepagePanel()
        {
            if (homepagePanel != null) homepagePanel.SetActive(true);
            if (signInPanel != null) signInPanel.SetActive(false);
            if (signUpPanel != null) signUpPanel.SetActive(false);
            if (loginPanel != null) loginPanel.SetActive(false);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        }

        private void ApplyUserToUI(User user)
        {
            if (displayNameText != null)
                displayNameText.text = $"Welcome, {user.username}";

            if (scoreText != null)
                scoreText.text = $"{user.score} pts";

            if (simulationScoreText != null)
                simulationScoreText.text = $"{user.simulationScore} pts";

            Debug.Log($"[HomepageUIManager] UI updated — " +
                      $"user: {user.username}, score: {user.score}");
        }
    }
}