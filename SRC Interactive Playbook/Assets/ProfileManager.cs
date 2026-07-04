/*
    Author: Yeong Yu Seong
    Date: 1 July 2026
    Last Updated: 4 July 2026
    Description: This script manages the user's profile information, including displaying, editing, and saving changes to the profile data.
                 It interacts with Firebase Realtime Database and Firebase Authentication to retrieve and update user information.
*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;
using RedCross.Playbook.Data;

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
    private bool isProfileShown = false;
    [SerializeField] private TextMeshProUGUI errorText; // TextMeshProUGUI component to display error messages

    private DatabaseReference db;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        db = FirebaseDatabase.DefaultInstance.RootReference;
        errorText.text = ""; // Clear any existing error messages at the start
    }

    // Update is called once per frame
    void Update()
    {
        if (FirebaseManager.Instance.CurrentUserId != null && !isProfileShown)
        {
            ShowProfile();
            isProfileShown = true;
        } else if (FirebaseManager.Instance.CurrentUserId == null)
        {
            // Clear profile information when the user logs out
            for (int i = 0; i < profileTexts.Length; i++)
            {
                profileTexts[i].text = "";
            }
            profileImage.sprite = null;
            isProfileShown = false;
        }
    }

    /// <summary>
    /// Displays the user's profile information by retrieving it from the Firebase Realtime Database and populating the UI elements.
    /// </summary>
    public void ShowProfile()
    {
        // Retrieve user data from Firebase Realtime Database and populate the profile UI elements
        FirebaseDatabase.DefaultInstance
        .GetReference("users").Child(FirebaseManager.Instance.CurrentUserId)
        .GetValueAsync().ContinueWithOnMainThread(task => {
        if (task.IsFaulted) {
            Debug.LogError("Error retrieving user data: " + task.Exception);
        }
        else if (task.IsCompleted) {
            DataSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                profileTexts[0].text = snapshot.Child("username").Value.ToString();
                profileTexts[1].text = "********"; // Hide the actual password for security reasons
                profileTexts[2].text = snapshot.Child("email").Value.ToString();
                //profileImage.sprite = // Load the profile image
            }
            else
            {
                Debug.LogWarning("No user data found.");
            }
        }
        });
    }

    /// <summary>
    /// Switches the UI to the edit profile panel and populates the input fields with the current profile data.
    /// </summary>
    public void EditProfile()
    {
        profilePanel.SetActive(false);
        editPanel.SetActive(true);
        profileInputFields[0].text = profileTexts[0].text; // Populate the username input field with the current username
        profileInputFields[1].text = profileTexts[2].text; // Populate the email input field with the current email
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
        // Validate the input fields before saving changes
        errorText.text = ""; // Clear any existing error messages
        for (int i = 0; i < profileInputFields.Length; i++)
        {
            if (profileInputFields[i].text != profileTexts[i].text)
            {
                if (i == 1) // If the email field has changed, validate the new email format
                {
                    string email = profileInputFields[i].text;
                    if (!email.Contains("@") || !email.Contains("."))
                    {
                        errorText.text = "Invalid email format.";
                        return; // Exit the method if the email format is invalid
                    }
                }
                if (i == 0) // If the username field has changed, validate the new username format
                {
                    string username = profileInputFields[i].text;
                    if (username.Length < 3 || username.Length > 20)
                    {
                        errorText.text = "Username must be between 3 and 20 characters.";
                        return; // Exit the method if the username format is invalid
                    }
                }
            }
        }
        hasChanges = false;
        string newUsername = profileInputFields[0].text;
        string newEmail = profileInputFields[1].text;
        // Update the profile data in Firebase Realtime Database
        FirebaseManager.Instance.UpdateUserField("username", newUsername,
            () => Debug.Log("Username updated"),
            error => Debug.LogError(error));
        FirebaseManager.Instance.UpdateUserField("email", newEmail,
            () => Debug.Log("Email updated"),
            error => Debug.LogError(error));

        // Close the prompt and switch back to the profile panel
        prompt.SetActive(false);
        profilePanel.SetActive(true);
        editPanel.SetActive(false);
        errorText.text = ""; // Clear any error messages after saving changes

        // Update the profileTexts to reflect the changes
        profileTexts[0].text = newUsername;
        profileTexts[2].text = newEmail;
    }

    /// <summary>
    /// Sends a password reset email to the user's registered email address using Firebase Authentication.
    /// </summary>
    public void ChangePassword()
    {
        Debug.Log("Change Password button clicked.");
        FirebaseAuth.DefaultInstance.SendPasswordResetEmailAsync(FirebaseAuth.DefaultInstance.CurrentUser.Email).ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                Debug.Log("Password reset email sent.");
            }
            else
            {
                Debug.LogError("Error sending password reset email: " + task.Exception);
            }
        });
    }

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
}
