using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RedCross.Playbook.UI
{
    public class PostSurveyUI : MonoBehaviour
    {
        [Header("Q1: Objectives clear (1-5)")]
        [SerializeField] private Toggle[] q1ObjectivesScale = new Toggle[5];

        [Header("Q2: Confidence applying now (1-5)")]
        [SerializeField] private Toggle[] q2ConfidenceScale = new Toggle[5];

        [Header("Q3: Most useful part")]
        [SerializeField] private TMP_InputField q3MostUsefulInput;

        [Header("Q4: Suggestions / Feedback")]
        [SerializeField] private TMP_InputField q4SuggestionsInput;

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
            int q1Score = GetScaleValue(q1ObjectivesScale);
            int q2Score = GetScaleValue(q2ConfidenceScale);
            string q3Answer = q3MostUsefulInput.text.Trim();
            string q4Answer = q4SuggestionsInput.text.Trim();

            if (q1Score == 0 || q2Score == 0 || string.IsNullOrEmpty(q3Answer) || string.IsNullOrEmpty(q4Answer))
            {
                if (errorText != null) errorText.text = "Please answer all questions before submitting.";
                return;
            }

            submitButton.interactable = false;
            if (errorText != null) errorText.text = "Saving...";

            var answers = new Dictionary<string, object>
            {
                { "q1_objectivesClear", q1Score },
                { "q2_confidenceNow", q2Score },
                { "q3_mostUseful", q3Answer },
                { "q4_suggestions", q4Answer }
            };

            FirebaseManager.Instance.RecordPulseSurvey("post_survey", answers, UserManager.Instance.CurrentUser,
                onSuccess: (user) =>
                {
                    // Route user back to homepage or a final "Thank You" screen
                    UIManager.Instance.ShowHomepage();
                },
                onError: (err) =>
                {
                    submitButton.interactable = true;
                    if (errorText != null) errorText.text = $"Error: {err}";
                });
        }

        private int GetScaleValue(Toggle[] scale)
        {
            for (int i = 0; i < scale.Length; i++)
            {
                if (scale[i] != null && scale[i].isOn)
                    return i + 1;
            }
            return 0;
        }
    }
}