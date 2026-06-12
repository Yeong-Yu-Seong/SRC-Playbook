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
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static ScoreManager Instance { get; private set; }

    // ── Configurable point values (editable in Inspector) ─────────────────────
    [Header("Simulation Points")]
    [Tooltip("Base points for completing any simulation branch.")]
    public int simulationBasePoints      = 50;
    [Tooltip("Bonus per individually marked 'ideal' dialogue choice.")]
    public int simulationIdealChoiceBonus = 25;

    [Header("Quiz Points")]
    public int pointsPerCorrectFactsOpinions  = 10;
    public int pointsPerCorrectMCQ            = 10;
    public int pointsPerCorrectDragDrop       = 15;
    [Tooltip("Flat bonus for answering all questions correctly.")]
    public int perfectScoreBonus              = 20;

    // ══════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SIMULATION SCORING
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Call this when a "Choose Your Next Step" simulation ends.
    /// </summary>
    /// <param name="simulationId">Unique ID of the completed simulation.</param>
    /// <param name="idealChoicesMade">Number of choices the system flagged as ideal.</param>
    /// <param name="onSuccess">Callback when Firebase save is confirmed.</param>
    /// <param name="onError">Callback on failure.</param>
    public void SubmitSimulationScore(string simulationId,
                                      int idealChoicesMade,
                                      Action onSuccess = null,
                                      Action<string> onError = null)
    {
        int points = simulationBasePoints +
                     (idealChoicesMade * simulationIdealChoiceBonus);

        Debug.Log($"[ScoreManager] Simulation '{simulationId}': base {simulationBasePoints} " +
                  $"+ {idealChoicesMade} ideal choices × {simulationIdealChoiceBonus} = {points} pts");

        UserManager.Instance.AwardSimulationPoints(simulationId, points, onSuccess, onError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  QUIZ SCORING — three formats
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Calculates and submits score for a Facts vs Opinions quiz.</summary>
    /// <param name="quizId">e.g. "quiz_fvo_module1"</param>
    /// <param name="correctCount">Number of questions answered correctly.</param>
    /// <param name="totalCount">Total questions in the quiz.</param>
    public void SubmitFactsOpinionsScore(string quizId, int correctCount, int totalCount,
                                         Action onSuccess = null, Action<string> onError = null)
    {
        int pts = CalculateQuizPoints(correctCount, totalCount,
                                       pointsPerCorrectFactsOpinions);
        Debug.Log($"[ScoreManager] Facts/Opinions quiz '{quizId}': {correctCount}/{totalCount} = {pts} pts");
        UserManager.Instance.AwardQuizPoints(quizId, pts, onSuccess, onError);
    }

    /// <summary>Calculates and submits score for a Multiple-Choice Quiz.</summary>
    /// <param name="quizId">e.g. "quiz_mcq_module2"</param>
    public void SubmitMCQScore(string quizId, int correctCount, int totalCount,
                                Action onSuccess = null, Action<string> onError = null)
    {
        int pts = CalculateQuizPoints(correctCount, totalCount,
                                       pointsPerCorrectMCQ);
        Debug.Log($"[ScoreManager] MCQ quiz '{quizId}': {correctCount}/{totalCount} = {pts} pts");
        UserManager.Instance.AwardQuizPoints(quizId, pts, onSuccess, onError);
    }

    /// <summary>Calculates and submits score for a Drag-and-Drop quiz.</summary>
    /// <param name="quizId">e.g. "quiz_dnd_module3"</param>
    public void SubmitDragDropScore(string quizId, int correctCount, int totalCount,
                                     Action onSuccess = null, Action<string> onError = null)
    {
        int pts = CalculateQuizPoints(correctCount, totalCount,
                                       pointsPerCorrectDragDrop);
        Debug.Log($"[ScoreManager] Drag-and-Drop quiz '{quizId}': {correctCount}/{totalCount} = {pts} pts");
        UserManager.Instance.AwardQuizPoints(quizId, pts, onSuccess, onError);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private int CalculateQuizPoints(int correctCount, int totalCount, int pointsEach)
    {
        int pts = correctCount * pointsEach;
        // Perfect-score bonus
        if (totalCount > 0 && correctCount == totalCount)
            pts += perfectScoreBonus;
        return pts;
    }
}
