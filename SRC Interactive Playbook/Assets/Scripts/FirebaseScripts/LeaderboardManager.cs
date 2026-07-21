/*
 * Description: Fetches all User records from Firebase and renders a ranked
 *              scrollable leaderboard sorted by total score.
 */

using RedCross.Playbook.Data;
using RedCross.Playbook.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.AudioSettings;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Prefab & Container")]
    public GameObject mobileLBEntryPrefab;
    public GameObject desktopLBEntryPrefab;
    public Transform mobileLBContainer;
    public Transform desktopLBContainer;

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
        if (mobileLBContainer || desktopLBContainer == null) FindContainerAutomatically();
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
        // 1. Check layout state
        bool isMobile = ResponsiveLayoutManager.Instance.IsMobileActive;

        // 2. Pick the correct container and prefab
        Transform activeContainer = isMobile ? mobileLBContainer : desktopLBContainer;
        GameObject activePrefab = isMobile ? mobileLBEntryPrefab : desktopLBEntryPrefab;

        // Clear out old entries in BOTH containers just to be safe
        foreach (Transform child in mobileLBContainer) Destroy(child.gameObject);
        foreach (Transform child in desktopLBContainer) Destroy(child.gameObject);


        if (ranked == null || ranked.Count == 0)
        {
            SetStatus("No scores yet. Complete a module to appear here!");
            return;
        }

        string currentUsername = UserManager.Instance?.CurrentUser?.username ?? string.Empty;

        for (int i = 0; i < ranked.Count; i++)
        {
            User user = ranked[i];
            bool isSelf = !string.IsNullOrEmpty(currentUsername) &&
                           user.username == currentUsername;

            var row = Instantiate(activePrefab, activeContainer, false);
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
                bool isMobile = ResponsiveLayoutManager.Instance.IsMobileActive;
                Transform activeContainer = isMobile ? mobileLBContainer : desktopLBContainer;
                if (t.name != name) continue;
                activeContainer = t;
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
