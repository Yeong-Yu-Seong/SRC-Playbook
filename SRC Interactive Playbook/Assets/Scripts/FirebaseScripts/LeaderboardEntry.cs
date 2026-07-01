using TMPro;
using UnityEngine;

// ══════════════════════════════════════════════════════════════════════════════
//  LEADERBOARD ENTRY — attach to the row prefab
// ══════════════════════════════════════════════════════════════════════════════

public class LeaderboardEntry : MonoBehaviour
{
    [Header("Row UI Elements")]
    public TMP_Text rankText;
    public TMP_Text usernameText;
    public TMP_Text scoreText;

    // Optional: highlight the row if it belongs to the logged-in user
    [SerializeField] private GameObject selfHighlight;

    /// <summary>
    /// Populates a single leaderboard row.
    /// </summary>
    /// <param name="rank">1-based rank position.</param>
    /// <param name="username">Learner's display name.</param>
    /// <param name="score">Total accumulated score.</param>
    /// <param name="isSelf">True to apply a self-highlight style.</param>
    public void SetEntry(int rank, string username, int score, bool isSelf = false)
    {
        if (rankText != null) rankText.text = rank.ToString();
        if (usernameText != null) usernameText.text = username;
        if (scoreText != null) scoreText.text = score.ToString("N0") + " pts";

        if (selfHighlight != null) selfHighlight.SetActive(isSelf);
    }
}