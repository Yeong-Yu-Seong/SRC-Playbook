/*
    Author: Yeong Yu Seong
    Date: 1 July 2026
    Last Updated: 28 July 2026 (By Kwek Sin En)
    Description: This script manages the user's profile information, including displaying, editing, and saving changes to the profile data.
                 It interacts with Firebase Realtime Database and Firebase Authentication to retrieve and update user information.
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RedCross.Playbook.Data;
using FirebaseWebGL.Scripts.FirebaseBridge;

public class ProfileManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject profilePanel;
    [SerializeField] private GameObject editPanel;

    [Header("Profile UI Elements")]
    [SerializeField] private TextMeshProUGUI[] profileTexts; // Array to hold profile name, password, and email TextMeshProUGUI components
    [SerializeField] private Image profileImage;
    [SerializeField] private GameObject prompt;

    [Header("Profile Data")]
    [SerializeField] private TMP_InputField[] profileInputFields;
    [SerializeField] private Image profileImageInput;

    [Header("Change Tracking")]
    private bool hasChanges = false;
    //private string currentUser;
    [SerializeField] private TextMeshProUGUI errorText; // TextMeshProUGUI component to display error messages

    public static ProfileManager profileManagerInstance; // Singleton instance of ProfileManager

    private void Awake()
    {
        profileManagerInstance = this;
     
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        errorText.text = ""; // Clear any existing error messages at the start
    }

    /// <summary>
    /// Displays the user's profile information by retrieving it from the Firebase Realtime Database and populating the UI elements.
    /// </summary>
    public void ShowProfile()
    {
        if (profilePanel != null) profilePanel.SetActive(true);
   
        // Simply fetch the data from our active session in UserManager
        User currentUser = UserManager.Instance.CurrentUser;

        if (currentUser != null)
        {
            profileTexts[0].text = currentUser.username;
            profileTexts[1].text = currentUser.email;
        }
        else
        {
            Debug.LogWarning("Cannot display profile: CurrentUser in UserManager is null.");
        }
    }

    /// <summary>
    /// Switches the UI to the edit profile panel and populates the input fields with the current profile data.
    /// </summary>
    public void EditProfile()
    {
        profilePanel.SetActive(false);
        editPanel.SetActive(true);
        profileInputFields[0].text = profileTexts[0].text; // Populate the username input field with the current username
        profileInputFields[1].text = profileTexts[1].text; // Populate the email input field with the current email
    }

    /// <summary>
    /// Checks for changes in the profile data and prompts the user to save if there are unsaved changes.
    /// </summary>
    public void CheckForChanges()
    {
        hasChanges = false; // Reset the hasChanges flag before checking for changes
        // Check for changes in the input fields compared to the displayed profile data
        for (int i = 0; i < profileInputFields.Length; i++)
        {
            if (profileInputFields[i].text != profileTexts[i].text)
            {
                hasChanges = true;
            }
        }
        if (hasChanges)
        {
            // Prompt the user if they want to save changes
            prompt.SetActive(true);
        }
        else
        {
            profilePanel.SetActive(true);
            editPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Saves the changes made to the profile data and resets the hasChanges flag.
    /// </summary>
    public void SaveChanges()
    {
        errorText.text = "";

        // Check if the username is valid
        string newUsername = profileInputFields[0].text;
        if (newUsername.Length < 3 || newUsername.Length > 20)
        {
            errorText.text = "Username must be between 3 and 20 characters.";
            return;
        }

        // Only update the username in Firebase
        FirebaseManager.Instance.UpdateUserField("username", newUsername,
            () => Debug.Log("Username updated"),
            error => Debug.LogError(error));
        hasChanges = false;
        // Update the profile data in Firebase Realtime Database
        FirebaseManager.Instance.UpdateUserField("username", newUsername,
            () => Debug.Log("Username updated"),
            error => Debug.LogError(error));

        // Switch back to the profile panel
        profilePanel.SetActive(true);
        editPanel.SetActive(false);
        errorText.text = ""; // Clear any error messages after saving changes

        // Update the profileTexts to reflect the changes
        profileTexts[0].text = newUsername;

        if (UserManager.Instance.CurrentUser != null)
        {
            UserManager.Instance.CurrentUser.username = newUsername;
            // This method triggers RefreshHUD() automatically!
            UserManager.Instance.SetUserData(UserManager.Instance.CurrentUser);
        }
    }

    /// <summary>
    /// Sends a password reset email to the user's registered email address using Firebase Authentication.
    /// </summary>
    public void ChangePassword()
    {
        Debug.Log("Change Password button clicked.");
        string email = profileTexts[1].text;

        // WebGL Bridge Call
        FirebaseAuth.SendPasswordResetEmail(email, gameObject.name, "OnPasswordResetSuccess", "OnPasswordResetFailed");
    }

    // --- WebGL Callbacks ---
    public void OnPasswordResetSuccess(string info)
    {
        Debug.Log("Password reset email sent.");
    }

    public void OnPasswordResetFailed(string error)
    {
        Debug.LogError("Error sending password reset email: " + error);
    }
    // -----------------------

    /// <summary>
    /// Cancels any unsaved changes and switches back to the profile panel without saving.
    /// </summary>
    public void CancelChanges()
    {
        prompt.SetActive(false);
        profilePanel.SetActive(true);
        editPanel.SetActive(false);
        errorText.text = ""; // Clear any error messages when canceling changes
    }

    /// <summary>
    /// Clears the profile information displayed in the UI elements, resetting them to their default state.
    /// This method is typically called when the user logs out or when the profile information needs to be cleared for any reason.
    /// </summary>
    public void ClearProfile()
    {
        FirebaseManager.Instance.Logout(); // Log out the user from Firebase Authentication
        UserManager.Instance.ClearUserData(); // Clear the current user data in UserManager
        UIManager.Instance.ShowLanding(); // Show the landing page after logout
        profileTexts[0].text = ""; // Clear the username text
        profileTexts[1].text = ""; // Clear the email text
    }
}
