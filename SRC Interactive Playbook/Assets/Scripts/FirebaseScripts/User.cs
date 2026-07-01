/*
 * User.cs
 * Learner profile stored at /users/{uid} in Firebase Realtime Database.
 */

using System;
using System.Collections.Generic;

[Serializable]
public class User
{
    // ── Core identity ──────────────────────────────────────────────────────────
    public string username;
    public string email;

    // ── Scoring — single source of truth ──────────────────────────────────────
    /// <summary>
    /// Total points accumulated across all activities (scenarios + quizzes).
    /// FIXED: was duplicated across score, simulationScore, quizScore.
    /// Those extra fields are removed — this is the only score field.
    /// </summary>
    public int score;

    // ── Metadata ───────────────────────────────────────────────────────────────
    /// <summary>Unix timestamp (seconds) of account creation.</summary>
    public long createdAt;

    /// <summary>Unix timestamp (seconds) of most recent login.</summary>
    public long lastLoginAt;

    // ── In-memory completion history (NOT written to Firebase as part of this doc) ──
    /// <summary>
    /// Populated from Firebase after login if local query is needed.
    /// [NonSerialized] — Firebase stores these as push()-keyed child nodes, not
    /// as part of the flat user document. WriteUserJson() will never overwrite them.
    /// To add a record, call FirebaseManager.RecordSimulationCompletion().
    /// </summary>
    [NonSerialized]
    public Dictionary<string, CompletedModule> completedSimulations = new();

    /// <summary>
    /// Same pattern as completedSimulations, for quiz completions.
    /// [NonSerialized] for the same reason.
    /// </summary>
    [NonSerialized]
    public Dictionary<string, CompletedModule> completedQuizzes = new();

    // ── Constructor ────────────────────────────────────────────────────────────
    public User() { }

    public User(string username, string email)
    {
        this.username = username;
        this.email = email;
        this.score = 0;
        this.createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        this.lastLoginAt = 0;
    }
}

/// <summary>
/// Lightweight record of one completed learning module.
/// Stored as a child under users/{uid}/completedSimulations/{pushId} in Firebase.
/// Never serialised as part of the parent User document.
/// </summary>
[Serializable]
public class CompletedModule
{
    /// <summary>Unique identifier of the module (e.g. "scenario_malcolm_otter").</summary>
    public string moduleId;

    /// <summary>"scenario" or "quiz" — lets the UI filter completion history by type.</summary>
    public string moduleType;

    /// <summary>Points awarded upon completion of this run.</summary>
    public int pointsEarned;

    /// <summary>Unix timestamp (seconds) when the module was completed.</summary>
    public long completedAt;

    public CompletedModule() { }

    /// <summary>
    /// FIXED: previous constructor passed moduleType before assigning it,
    /// so moduleType was always null in the DB.
    /// </summary>
    public CompletedModule(string moduleId, string moduleType, int pointsEarned)
    {
        this.moduleId = moduleId;
        this.moduleType = moduleType;   // assigned before use
        this.pointsEarned = pointsEarned;
        this.completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}