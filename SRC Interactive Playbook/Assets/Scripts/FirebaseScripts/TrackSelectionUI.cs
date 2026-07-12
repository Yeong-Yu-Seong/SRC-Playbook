using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;
using RedCross.Playbook.Firebase;
using RedCross.Playbook.UI;


public class TrackSelectionUI : MonoBehaviour
{
    [SerializeField] private Button employeeButton;
    [SerializeField] private Button managerButton;
    [SerializeField] private TextMeshProUGUI bubbleText;

    private void OnEnable()
    {
        employeeButton.onClick.AddListener(() => SelectTrack("Employee"));
        managerButton.onClick.AddListener(() => SelectTrack("Manager"));
        if (UserManager.Instance?.CurrentUser != null)
            DisplaySpeechBubble(UserManager.Instance.CurrentUser);
    }

    private void OnDisable()
    {
        employeeButton.onClick.RemoveAllListeners();
        managerButton.onClick.RemoveAllListeners();
    }

    private void DisplaySpeechBubble(User user)
    {
        if (bubbleText != null) 
            bubbleText.text = $"Hello {user.username}, don't forget to take your ticket to enter!";

    }

    private void SelectTrack(string track)
    {
        SetButtonsInteractable(false);
        FirebaseManager.Instance.UpdateUserField("selectedTrack", track,
            onSuccess: () =>
            {
                // Update local user object
                UserManager.Instance.CurrentUser.selectedTrack = track;
                Debug.Log($"[TrackSelection] Saved track: {track}");

                // Move to the next step (Pre-Survey or Homepage)
                UIManager.Instance.ShowHomepage();
            },
            onError: (err) =>
            {
                SetButtonsInteractable(true);
            });
    }

    private void SetButtonsInteractable(bool state)
    {
        employeeButton.interactable = state;
        managerButton.interactable = state;
    }
}

