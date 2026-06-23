// ============================================================
// ScenarioData.cs
// Core data models for the Choose Your Next Step feature.
// All scenario content is driven by these serialisable classes,
// which map 1-to-1 with the Firebase / local-JSON structure.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedCross.Playbook.Data
{
    // ── Top-level wrapper stored in Firebase at /scenarios/{id} ──

    [Serializable]
    public class PlaybookScenario
    {
        public string id;
        public string title;
        public string exhibitNumber;
        public string outlineDescription;
        public string thumbnailUrl;
        public bool isPublished;
        public int pointsOnCompletion;
        public int pointsPerCorrect;
        public string createdBy;
        public string lastUpdatedBy;
        public long lastUpdatedTimestamp;

        public List<ScenePart> sceneParts = new();
    }

    // ── A single "beat" — either a narrative segment or a question ──

    [Serializable]
    public class ScenePart
    {
        public string id;
        public ScenePartType type;

        // Narrative fields
        public string narrativeText;
        public string backgroundImageUrl;
        public string audioUrl;
        public float displayDurationSecs;

        // Question fields
        public string questionText;
        public string contextHintText;
        public List<Choice> choices = new();
    }

    [Serializable]
    public enum ScenePartType
    {
        Narrative,
        Question
    }

    // ── One answer option inside a Question part ──

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
    public enum FeedbackType
    {
        Correct,
        Incorrect,
        Urgent
    }

    // ── Lightweight index entry for the homepage gallery ──

    [Serializable]
    public class ScenarioIndexEntry
    {
        public string id;
        public string title;
        public string exhibitNumber;
        public string outlineDescription;
        public string thumbnailUrl;
        public bool isPublished;
        public int totalQuestions;
        public int pointsOnCompletion;
    }

    // ── Per-user progress ──

    [Serializable]
    public class UserScenarioProgress
    {
        public string scenarioId;
        public bool completed;
        public int score;
        public int correctAnswers;
        public int totalQuestions;
        public long completedTimestamp;
        public List<string> answeredChoiceIds = new();
    }
}