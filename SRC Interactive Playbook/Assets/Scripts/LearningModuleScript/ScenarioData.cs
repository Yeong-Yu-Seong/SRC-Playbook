// ============================================================
// ScenarioData.cs
// Core data models for the Choose Your Next Step feature.
// All scenario content is driven by these serialisable classes,
// which map 1-to-1 with the Firebase / local-JSON structure.
// ============================================================

using System;
using System.Collections.Generic;

namespace RedCross.Playbook.Data
{
    // ══════════════════════════════════════════════════════════════════════════
    //  SCENARIO
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Full scenario document stored at /playbook/scenarios/{id}.</summary>
    [Serializable]
    public class PlaybookScenario
    {
        public string id;
        public string title;
        public string exhibitNumber;
        public string outlineDescription;
        public string thumbnailUrl;
        public bool isPublished;
        public bool isMainScenario;
        public int pointsOnCompletion;
        public int pointsPerCorrect;
        //To categorize the scenario
        public string category; // e.g., "Main", "BiteSized"

        /// <summary>
        /// Documents the scoring formula in the DB so it is explicit for admins.
        /// Value: "base + (correctAnswers * pointsPerCorrect)"
        /// </summary>
        public string scoringFormula;
        public string createdBy;
        public long createdAt;
        public string lastUpdatedBy;

        /// <summary>Set via ServerValue.TIMESTAMP on every admin write. Never 0.</summary>
        public long lastUpdatedTimestamp;

        public List<ScenePart> sceneParts = new();

        public PlaybookQuiz quiz;
    }

    /// <summary>A single beat — either a narrative segment or a question.</summary>
    [Serializable]
    public class ScenePart
    {
        public string id;
        public ScenePartType type;

        // Narrative
        public string narrativeText;
        public string backgroundImageUrl;
        public string audioUrl;
        public float displayDurationSecs;

        // Question
        public string questionText;
        public string contextHintText;
        public List<Choice> choices = new();
    }

    [Serializable]
    public enum ScenePartType { Narrative, Question }

    /// <summary>One answer option inside a Question part.</summary>
    [Serializable]
    public class Choice
    {
        public string id;
        public string label;
        public string text;
        public bool isCorrect;
        public string feedbackText;
        public List<string> feedbackTags = new();
        public FeedbackType feedbackType;
    }

    [Serializable]
    public enum FeedbackType { Correct, Incorrect, Urgent }

    // ══════════════════════════════════════════════════════════════════════════
    //  SCENARIO INDEX
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lightweight index entry stored at /playbook/scenarios_index/{id}.
    /// Contains only what the homepage gallery needs to render a card.
    /// </summary>
    [Serializable]
    public class ScenarioIndexEntry
    {
        public string id;
        public string exhibitNumber;
        public string thumbnailUrl;
        public bool isPublished;
        public int pointsOnCompletion;
        public int totalQuestions;
        public string title;
        public string characterText;
        public string outlineDescription;
        /// <summary>
        /// Controls gallery display order.
        /// Firebase RTDB returns object keys lexicographically without this,
        /// making card order non-deterministic as more scenarios are added.
        /// Query with .orderByChild("sortOrder").
        /// </summary>
        public int sortOrder;
        public string category;

        // ── Museum gallery layout (curator-controlled from Firebase) ──────────
        /// <summary>Horizontal anchored position on the gallery wall canvas.</summary>
        public float wallX = 0f;
        /// <summary>Vertical anchored position on the gallery wall canvas.</summary>
        public float wallY = 0f;
        /// <summary>Width of the exhibit frame RectTransform in Unity UI units.</summary>
        public float cardWidth = 300f;
        /// <summary>Height of the exhibit frame RectTransform.</summary>
        public float cardHeight = 240f;
        public float imgWidth = 700f;
        public float imgHeight = 526f;

        public bool isMainScenario;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  USER PROGRESS — SCENARIO
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Per-user scenario progress stored at
    /// /playbook/user_progress/{uid}/{scenarioId}.
    /// </summary>
    [Serializable]
    public class UserScenarioProgress
    {
        /// <summary>
        /// DEPRECATED — kept as a read-only field so existing DB records deserialise
        /// without throwing. New writes in ScenarioManager pass scenarioId as the
        /// node key parameter instead of embedding it in the document.
        /// Do not write to this field in new code.
        /// </summary>
        [Obsolete("Use the Firebase node key, not this field. Kept for backwards-compat reads only.")]
        public string scenarioId;

        public bool completed;
        public int score;
        public int correctAnswers;
        public int totalQuestions;
        public long completedTimestamp;
        public List<string> answeredChoiceIds = new();
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  QUIZ — question bank and progress
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Quiz definition stored at /playbook/quizzes/{quizId}.</summary>
    [Serializable]
    public class PlaybookQuiz
    {
        public string id;
        public string title;

        /// <summary>"FactsVsOpinions", "MCQ", or "DragAndDrop".</summary>
        public string type;

        /// <summary>Links this quiz to its parent scenario exhibit.</summary>
        public string linkedScenarioId;

        public int pointsOnCompletion;
        public bool isPublished;
        public int sortOrder;

        public List<QuizQuestion> questions = new();
    }

    [Serializable]
    public class QuizQuestion
    {
        public string id;
        public string prompt;
        public string correctAnswerId;
        public string feedbackText;
        public List<QuizChoice> choices = new();
    }

    [Serializable]
    public class QuizChoice
    {
        public string id;
        public string text;
    }

    /// <summary>
    /// Lightweight quiz index entry stored at /playbook/quizzes_index/{quizId}.
    /// Mirrors the ScenarioIndexEntry pattern.
    /// </summary>
    [Serializable]
    public class QuizIndexEntry
    {
        public string id;
        public string title;
        public string type;
        public string linkedScenarioId;
        public bool isPublished;
        public int pointsOnCompletion;
        public int totalQuestions;
        public int sortOrder;
    }

    /// <summary>
    /// Per-user quiz progress stored at /playbook/quiz_progress/{uid}/{quizId}.
    /// </summary>
    [Serializable]
    public class UserQuizProgress
    {
        public bool completed;
        public int score;
        public int correctAnswers;
        public int totalQuestions;
        public long completedTimestamp;

        /// <summary>
        /// Map of questionId → chosen answerId for this run.
        /// </summary>
        public List<QuizAnswer> answers = new();
    }

    /// <summary>
    /// Serialisable key-value pair replacing Dictionary&lt;string,string&gt; answers.
    /// JsonUtility cannot serialise Dictionary — this is the Unity-safe equivalent.
    /// </summary>
    [Serializable]
    public class QuizAnswer
    {
        public string questionId;
        public string choiceId;

        public QuizAnswer() { }
        public QuizAnswer(string questionId, string choiceId)
        {
            this.questionId = questionId;
            this.choiceId = choiceId;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CHEATSHEET
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cheatsheet stored at /playbook/cheatsheets/{cheatsheetId}.
    /// </summary>
    [Serializable]
    public class Cheatsheet
    {
        public string id;
        public string title;
        public string mascot;
        public List<string> points = new();
        public int sortOrder;
    }
}