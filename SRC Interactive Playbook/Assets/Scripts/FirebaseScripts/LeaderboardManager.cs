/*
 * Description: Fetches all User records from Firebase and renders a ranked
 *              scrollable leaderboard sorted by total score.
 */

using System.Collections.Generic;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Prefab & Container")]
    public GameObject leaderboardEntryPrefab;
    public Transform leaderboardContainer;

    [Header("Settings")]
    public int displayCount = 10;

    [Header("Status Labels (optional)")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text myRankText;

    // ══════════════════════════════════════════════════════════════════════════
    //  PUBLIC
    // ══════════════════════════════════════════════════════════════════════════

    public void OpenLeaderboard()
    {
        if (leaderboardContainer == null) FindContainerAutomatically();
        UIManager.Instance.ShowLeaderboard();
        RefreshLeaderboard();
    }

    public void RefreshLeaderboard()
    {
        if (FirebaseManager.Instance == null) { SetStatus("Firebase not available."); return; }

        SetStatus("Loading scores…");
        FirebaseManager.Instance.FetchLeaderboard(displayCount, OnLeaderboardFetched, OnFetchError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════════════════════════════

    private void OnLeaderboardFetched(List<User> ranked)
    {
        if (ranked == null || ranked.Count == 0)
        {
            SetStatus("No scores yet. Complete a module to appear here!");
            return;
        }

        if (leaderboardContainer == null || leaderboardEntryPrefab == null)
        {
            Debug.LogError("[LeaderboardManager] Container or prefab not assigned.");
            return;
        }

        foreach (Transform child in leaderboardContainer)
            Destroy(child.gameObject);

        string currentUsername = UserManager.Instance?.CurrentUser?.username ?? string.Empty;

        for (int i = 0; i < ranked.Count; i++)
        {
            User user = ranked[i];
            bool isSelf = !string.IsNullOrEmpty(currentUsername) &&
                           user.username == currentUsername;

            var row = Instantiate(leaderboardEntryPrefab, leaderboardContainer, false);
            var entry = row.GetComponent<LeaderboardEntry>();

            if (entry != null)
                entry.SetEntry(i + 1, user.username, user.score, isSelf);
            else
                Debug.LogError("[LeaderboardManager] LeaderboardEntry component missing on prefab.");

            if (isSelf && myRankText != null)
                myRankText.text = $"Your rank: #{i + 1}";
        }

        SetStatus(string.Empty);
    }

    private void OnFetchError(string error)
    {
        SetStatus($"Could not load leaderboard: {error}");
        Debug.LogError($"[LeaderboardManager] {error}");
    }

    private void FindContainerAutomatically()
    {
        foreach (string name in new[] { "LeaderboardContent", "Content" })
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name) continue;
                leaderboardContainer = t;
                Debug.Log($"[LeaderboardManager] Auto-found: {t.name}");
                return;
            }
        }
        Debug.LogError("[LeaderboardManager] Could not auto-find container.");
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}