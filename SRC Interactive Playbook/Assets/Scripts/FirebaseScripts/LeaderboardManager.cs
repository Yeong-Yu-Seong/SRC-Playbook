/*
 * Description: Fetches all User records from Firebase and renders a ranked
 *              scrollable leaderboard sorted by total score.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

// ══════════════════════════════════════════════════════════════════════════════
//  LEADERBOARD MANAGER
// ══════════════════════════════════════════════════════════════════════════════

public class LeaderboardManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Prefab & Container")]
    [Tooltip("The row prefab containing a LeaderboardEntry component.")]
    public GameObject leaderboardEntryPrefab;

    [Tooltip("The ScrollRect's Content transform. Leave null to auto-find.")]
    public Transform leaderboardContainer;

    [Header("Settings")]
    [Tooltip("Maximum number of rows to display.")]
    public int displayCount = 10;

    [Header("Status Labels (optional)")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text myRankText;   // shows the current user's rank

    // ══════════════════════════════════════════════════════════════════════════
    //  PUBLIC ENTRY POINTS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Opens the leaderboard panel, resolves UI references, and fetches fresh data.
    /// Wire this to UIManager.ShowLeaderboard() or call directly from a button.
    /// </summary>
    public void OpenLeaderboard()
    {
        Debug.Log("[LeaderboardManager] Opening leaderboard...");
        if (leaderboardContainer == null) FindContainerAutomatically();
        UIManager.Instance.ShowLeaderboard();
        RefreshLeaderboard();
    }

    /// <summary>Forces a fresh fetch from Firebase and rebuilds the list.</summary>
    public void RefreshLeaderboard()
    {
        if (FirebaseManager.Instance == null)
        {
            SetStatus("Firebase not available.");
            return;
        }

        SetStatus("Loading scores…");
        FirebaseManager.Instance.FetchAllUsers(OnUsersFetched, OnFetchError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE CALLBACKS
    // ══════════════════════════════════════════════════════════════════════════

    private void OnUsersFetched(Dictionary<string, User> userDict)
    {
        if (userDict == null || userDict.Count == 0)
        {
            SetStatus("No scores yet. Complete a module to appear here!");
            return;
        }

        if (leaderboardContainer == null)
        {
            Debug.LogError("[LeaderboardManager] leaderboardContainer is null — cannot populate rows.");
            return;
        }

        if (leaderboardEntryPrefab == null)
        {
            Debug.LogError("[LeaderboardManager] leaderboardEntryPrefab is not assigned.");
            return;
        }

        // Clear existing rows
        foreach (Transform child in leaderboardContainer)
            Destroy(child.gameObject);

        // Sort descending by total score
        List<User> ranked = userDict.Values
            .OrderByDescending(u => u.score)
            .Take(displayCount)
            .ToList();

        string currentUsername = UserManager.Instance?.CurrentUser?.username ?? string.Empty;

        for (int i = 0; i < ranked.Count; i++)
        {
            User  u       = ranked[i];
            bool  isSelf  = !string.IsNullOrEmpty(currentUsername) &&
                             u.username == currentUsername;

            GameObject row   = Instantiate(leaderboardEntryPrefab, leaderboardContainer, false);
            LeaderboardEntry entry = row.GetComponent<LeaderboardEntry>();

            if (entry != null)
            {
                entry.SetEntry(i + 1, u.username, u.score, isSelf);
                Debug.Log($"[LeaderboardManager] #{i + 1}: {u.username} – {u.score} pts");
            }
            else
            {
                Debug.LogError("[LeaderboardManager] LeaderboardEntry component missing on prefab!");
            }

            // Show the logged-in user's rank below the list
            if (isSelf && myRankText != null)
                myRankText.text = $"Your rank: #{i + 1}";
        }

        SetStatus(string.Empty);
        Debug.Log($"[LeaderboardManager] Displayed {ranked.Count} entries.");
    }

    private void OnFetchError(string error)
    {
        SetStatus($"Could not load leaderboard: {error}");
        Debug.LogError($"[LeaderboardManager] Fetch error: {error}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void FindContainerAutomatically()
    {
        // Walk down common hierarchy: Canvas > ScrollView > Viewport > Content
        string[] searchNames = { "LeaderboardContent", "Content" };

        foreach (string targetName in searchNames)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name == targetName)
                {
                    leaderboardContainer = t;
                    Debug.Log($"[LeaderboardManager] Auto-found container: {t.name}");
                    return;
                }
            }
        }
        Debug.LogError("[LeaderboardManager] Could not auto-find a container. Assign it manually.");
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}
