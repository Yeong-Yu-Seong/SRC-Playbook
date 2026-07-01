/*
 * Description: Stateless helper that calculates points for each activity type
 *              and delegates persistence to UserManager → FirebaseManager.
 *
 *  Activity                          | Base pts  | Bonus
 *  ─────────────────────────────────────────────────────
 *  Branching Simulation (any ending) | 50        | +25 per "ideal" choice
 *  Quiz – Facts vs Opinions          | 10/correct| –
 *  Quiz – MCQ                        | 10/correct| –
 *  Quiz – Drag-and-Drop              | 15/correct| –
 *  Perfect-score bonus (any quiz)    | 20        | flat bonus
 */

using System;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static ScoreManager Instance { get; private set; }

    [Header("Simulation Points")]
    public int simulationBasePoints = 50;
    public int simulationIdealChoiceBonus = 25;

    [Header("Quiz Points")]
    public int pointsPerCorrectFactsOpinions = 10;
    public int pointsPerCorrectMCQ = 10;
    public int pointsPerCorrectDragDrop = 15;
    public int perfectScoreBonus = 20;

    // ══════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SIMULATION
    // ══════════════════════════════════════════════════════════════════════════

    public void SubmitSimulationScore(string simulationId, int idealChoicesMade,
                                      Action onSuccess = null,
                                      Action<string> onError = null)
    {
        int points = simulationBasePoints + (idealChoicesMade * simulationIdealChoiceBonus);
        Debug.Log($"[ScoreManager] Simulation '{simulationId}': {points} pts");
        UserManager.Instance.AwardSimulationPoints(simulationId, points, onSuccess, onError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  QUIZ — three formats
    //  All three require answers dict so quiz_progress is fully populated.
    // ══════════════════════════════════════════════════════════════════════════

    /// <param name="answers">Map of questionId → chosen answerId for the progress record.</param>
    public void SubmitFactsOpinionsScore(string quizId,
                                         int correctCount, int totalCount,
                                         Dictionary<string, string> answers,
                                         Action onSuccess = null,
                                         Action<string> onError = null)
    {
        int pts = CalculateQuizPoints(correctCount, totalCount, pointsPerCorrectFactsOpinions);
        Debug.Log($"[ScoreManager] Facts/Opinions '{quizId}': {correctCount}/{totalCount} = {pts} pts");
        UserManager.Instance.AwardQuizPoints(quizId, pts, correctCount, totalCount,
                                             answers, onSuccess, onError);
    }

    public void SubmitMCQScore(string quizId,
                               int correctCount, int totalCount,
                               Dictionary<string, string> answers,
                               Action onSuccess = null,
                               Action<string> onError = null)
    {
        int pts = CalculateQuizPoints(correctCount, totalCount, pointsPerCorrectMCQ);
        Debug.Log($"[ScoreManager] MCQ '{quizId}': {correctCount}/{totalCount} = {pts} pts");
        UserManager.Instance.AwardQuizPoints(quizId, pts, correctCount, totalCount,
                                             answers, onSuccess, onError);
    }

    public void SubmitDragDropScore(string quizId,
                                    int correctCount, int totalCount,
                                    Dictionary<string, string> answers,
                                    Action onSuccess = null,
                                    Action<string> onError = null)
    {
        int pts = CalculateQuizPoints(correctCount, totalCount, pointsPerCorrectDragDrop);
        Debug.Log($"[ScoreManager] Drag-and-Drop '{quizId}': {correctCount}/{totalCount} = {pts} pts");
        UserManager.Instance.AwardQuizPoints(quizId, pts, correctCount, totalCount,
                                             answers, onSuccess, onError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE
    // ══════════════════════════════════════════════════════════════════════════

    private int CalculateQuizPoints(int correctCount, int totalCount, int pointsEach)
    {
        int pts = correctCount * pointsEach;
        if (totalCount > 0 && correctCount == totalCount) pts += perfectScoreBonus;
        return pts;
    }
}