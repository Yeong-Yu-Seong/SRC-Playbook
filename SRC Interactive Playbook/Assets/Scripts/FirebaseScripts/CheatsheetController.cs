using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CheatsheetController : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button givingFeedbackButton;
    [SerializeField] private TextMeshProUGUI givingFeedbackButtonText;
    [SerializeField] private Button receivingFeedbackButton;
    [SerializeField] private TextMeshProUGUI receivingFeedbackButtonText;

    [SerializeField] private TextMeshProUGUI title;

    [Header("CARE Text Elements")]
    [SerializeField] private TextMeshProUGUI clarifyText;
    [SerializeField] private TextMeshProUGUI addressText;
    [SerializeField] private TextMeshProUGUI respondText;
    [SerializeField] private TextMeshProUGUI enhanceText;

    [Header("Visual Feedback")]
    [SerializeField] private Color activeTabColor = new Color(0.75f, 0.16f, 0.11f); // Red Cross Red
    [SerializeField] private Color inactiveTabColor = Color.gray;
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = Color.black;

    private Image givingTabImage;
    private Image receivingTabImage;
    private string currentTrack = "Employee"; // Default fallback

    private void Awake()
    {
        givingTabImage = givingFeedbackButton.GetComponent<Image>();
        receivingTabImage = receivingFeedbackButton.GetComponent<Image>();

        givingFeedbackButton.onClick.AddListener(() => ShowContent(true));
        receivingFeedbackButton.onClick.AddListener(() => ShowContent(false));
    }

    private void OnEnable()
    {
        // Fetch the selected track from the current user when the panel opens
        if (UserManager.Instance?.CurrentUser != null)
        {
            currentTrack = UserManager.Instance.CurrentUser.selectedTrack;
        }

        // Default to showing "Giving Feedback" when opened
        ShowContent(true);
    }

    private void ShowContent(bool isGivingFeedback)
    {
        // Update Tab Visuals
        if (givingTabImage != null) givingTabImage.color = isGivingFeedback ? activeTabColor : inactiveTabColor;
        if (givingFeedbackButtonText != null) givingFeedbackButtonText.color = isGivingFeedback ? activeTextColor : inactiveTextColor;
        if (receivingTabImage != null) receivingTabImage.color = !isGivingFeedback ? activeTabColor : inactiveTabColor;
        if (receivingFeedbackButtonText != null) receivingFeedbackButtonText.color = !isGivingFeedback ? activeTextColor : inactiveTextColor;

        // Load Content Based on Track and Tab
        if (currentTrack == "Manager")
        {
            if (isGivingFeedback)
            {
                title.text = "Giving Feedback: 4 Simple Steps";
                clarifyText.text = "• Gather objective facts.\n• Review attendance records, work outputs, and specific examples. \n• Avoid assumptions.";
                addressText.text = "• Deliver feedback respectfully.\n• Focus on observable behaviours and their operational impact, never personality or character.";
                respondText.text = "• Listen actively to their perspective.\n• Collaborate on an actionable improvement plan with measurable goals and support.";
                enhanceText.text = "• Follow up consistently.\n• Recognise progress, reinforce positive change, and maintain psychological safety throughout.";
            }
            else
            {
                title.text = "Receiving Feedback: 4 Simple Steps";
                clarifyText.text = "• Seek to understand the feedback without defensiveness.\n• Ask for specific examples of how your leadership impacts the team.";
                addressText.text = "• Acknowledge the feedback professionally.\n• Maintain open body language and a calm tone.";
                respondText.text = "• Ask clarifying questions to build a shared understanding.\n• Collaborate on actionable next steps for your leadership growth.";
                enhanceText.text = "• Implement agreed actions.\n• Proactively seek follow-up feedback to ensure your adjustments are effective.";
            }
        }
        else // Employee Track
        {
            if (isGivingFeedback)
            {
                title.text = "Giving Feedback: 4 Simple Steps";
                clarifyText.text = "• Identify specific behaviours you observed (not personality).\n• Understand the context before speaking up.";
                addressText.text = "• Be constructive; your goal is improvement, not blame.\n• Focus on the operational or team impact of those behaviours.";
                respondText.text = "• Allow your peer or manager to share their perspective.\n• Listen fully without immediately reacting.";
                enhanceText.text = "• Keep the conversation ongoing, not a one-off.\n• Recognise progress and continue collaborating on solutions.";
            }
            else
            {
                title.text = "Receiving Feedback: 4 Simple Steps";
                clarifyText.text = "• Prepare before a feedback conversation.\n• Gather specific examples of behaviours, understand, and keep an open mind.";
                addressText.text = "• Listen actively and stay calm.\n• Focus on behaviours and their impact, not on personal judgement.";
                respondText.text = "• Ask clarifying questions.\n• Acknowledge the feedback and collaborate on solutions and next steps.";
                enhanceText.text = "• Follow through on agreed actions.\n• Seek ongoing feedback and celebrate progress.";
            }
        }
    }
}