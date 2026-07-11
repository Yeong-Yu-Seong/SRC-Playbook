using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace RedCross.Playbook.UI
{
    public class PreSurveyUI : MonoBehaviour
    {
        [Header("Q1: Familiarity with feedback (1-5)")]
        [Tooltip("Assign the 5 toggles in order from 1 to 5")]
        [SerializeField] private Toggle[] q1FamiliarityScale = new Toggle[5];

        [Header("Q2: Confidence applying skills (1-5)")]
        [SerializeField] private Toggle[] q2ConfidenceScale = new Toggle[5];

        [Header("Q3: Hope to learn")]
        [SerializeField] private TMP_InputField q3HopeToLearnInput;

        [Header("Controls")]
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI errorText;

        private void OnEnable()
        {
            if (errorText != null) errorText.text = "";
            submitButton.onClick.AddListener(SubmitSurvey);
        }

        private void OnDisable()
        {
            submitButton.onClick.RemoveListener(SubmitSurvey);
        }

        private void SubmitSurvey()
        {
            int q1Score = GetScaleValue(q1FamiliarityScale);
            int q2Score = GetScaleValue(q2ConfidenceScale);
            string q3Answer = q3HopeToLearnInput.text.Trim();

            // Basic validation to ensure they answered everything
            if (q1Score == 0 || q2Score == 0 || string.IsNullOrEmpty(q3Answer))
            {
                if (errorText != null) errorText.text = "Please answer all questions before submitting.";
                return;
            }

            submitButton.interactable = false;
            if (errorText != null) errorText.text = "Saving...";

            var answers = new Dictionary<string, object>
            {
                { "q1_familiarity", q1Score },
                { "q2_confidence", q2Score },
                { "q3_hopeToLearn", q3Answer }
            };

            FirebaseManager.Instance.RecordPulseSurvey("pre_survey", answers, UserManager.Instance.CurrentUser,
                onSuccess: (user) =>
                {
                    // Route user to the homepage now that pre-survey is done
                    UIManager.Instance.ShowHomepage();
                },
                onError: (err) =>
                {
                    submitButton.interactable = true;
                    if (errorText != null) errorText.text = $"Error: {err}";
                });
        }

        // Helper to get the 1-5 value from the toggle arrays
        private int GetScaleValue(Toggle[] scale)
        {
            for (int i = 0; i < scale.Length; i++)
            {
                if (scale[i] != null && scale[i].isOn)
                    return i + 1; // Returns 1 to 5
            }
            return 0; // Indicates nothing was selected
        }
    }
}