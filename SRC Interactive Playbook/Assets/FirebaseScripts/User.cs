/*
 * Description: User data model representing a learner's profile, scores,
 *              and module completion state stored in Firebase Realtime Database.
 */

using System;
using System.Collections.Generic;

[Serializable]
public class User
{
    // ── Core identity ──────────────────────────────────────────────────────────
    public string username;
    public string email;
    public bool   isLoggedIn;

    // ── Scoring ────────────────────────────────────────────────────────────────
    /// <summary>Total points accumulated across all activities.</summary>
    public int score;

    /// <summary>Points earned from branching-story simulations only.</summary>
    public int simulationScore;

    /// <summary>Points earned from assessment quizzes only.</summary>
    public int quizScore;

    // ── Progress tracking ──────────────────────────────────────────────────────
    /// <summary>
    /// IDs of branching simulations the user has completed.
    /// Key = simulationId, Value = points earned for that run.
    /// </summary>
    public List<CompletedModule> completedSimulations = new List<CompletedModule>();

    /// <summary>
    /// IDs of assessment quizzes the user has completed.
    /// Key = quizId, Value = points earned.
    /// </summary>
    public List<CompletedModule> completedQuizzes = new List<CompletedModule>();

    // ── Metadata ───────────────────────────────────────────────────────────────
    /// <summary>Unix timestamp (seconds) of account creation.</summary>
    public long createdAt;

    /// <summary>Unix timestamp of the most recent login.</summary>
    public long lastLoginAt;

    // ── Constructor ────────────────────────────────────────────────────────────
    public User(string username, string email)
    {
        this.username        = username;
        this.email           = email;
        this.isLoggedIn      = false;
        this.score           = 0;
        this.simulationScore = 0;
        this.quizScore       = 0;
        this.createdAt       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        this.lastLoginAt     = 0;

        completedSimulations = new List<CompletedModule>();
        completedQuizzes     = new List<CompletedModule>();
    }
}

/// <summary>
/// Lightweight record of one completed learning module and its points awarded.
/// </summary>
[Serializable]
public class CompletedModule
{
    /// <summary>Unique identifier for this module (e.g. "sim_feedback_01").</summary>
    public string moduleId;

    /// <summary>Points awarded upon completion.</summary>
    public int pointsEarned;

    /// <summary>Unix timestamp when the module was completed.</summary>
    public long completedAt;

    public CompletedModule() { }

    public CompletedModule(string moduleId, int pointsEarned)
    {
        this.moduleId     = moduleId;
        this.pointsEarned = pointsEarned;
        this.completedAt  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
