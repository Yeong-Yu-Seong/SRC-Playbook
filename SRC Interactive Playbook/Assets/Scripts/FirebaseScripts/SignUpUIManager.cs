/*
 * Description: Handles the Sign-Up screen — input validation, Firebase Auth
 *              account creation, and User document initialisation in the database.
 */

using UnityEngine;
using TMPro;

public class SignUpUIManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private TMP_InputField confirmPasswordField;

    [Header("Feedback")]
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text loadingText;

    // ══════════════════════════════════════════════════════════════════════════
    //  PUBLIC ACTIONS
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Called by the Sign-Up button's OnClick event.</summary>
    public void OnSignupPressed()
    {
        ClearError();
        string username        = usernameField.text.Trim();
        string email           = emailField.text.Trim();
        string password        = passwordField.text;
        string confirmPassword = confirmPasswordField != null ? confirmPasswordField.text : password;

        // ── Validation ────────────────────────────────────────────────────────
        if (string.IsNullOrEmpty(username))               { ShowError("Username cannot be empty.");               return; }
        if (username.Length < 2)                          { ShowError("Username must be at least 2 characters."); return; }
        if (string.IsNullOrEmpty(email))                  { ShowError("Email cannot be empty.");                  return; }
        if (!email.Contains("@") || !email.Contains(".")) { ShowError("Please enter a valid email address.");     return; }
        if (string.IsNullOrEmpty(password))               { ShowError("Password cannot be empty.");               return; }
        if (password.Length < 6)                          { ShowError("Password must be at least 6 characters."); return; }
        if (password != confirmPassword)                  { ShowError("Passwords do not match.");                 return; }

        SetLoading(true);

        // ── Firebase sign-up ──────────────────────────────────────────────────
        FirebaseManager.Instance.SignUp(
            username, email, password,
            onSuccess: () =>
            {
                SetLoading(false);
                Debug.Log("[SignUpUIManager] Account created. Redirecting to login...");
                UIManager.Instance.ShowLogin();
            },
            onError: err =>
            {
                SetLoading(false);
                // Make common Firebase error codes human-friendly
                ShowError(LocaliseFirebaseError(err));
            }
        );
    }

    /// <summary>Navigates back to the Login screen.</summary>
    public void OnGoToLoginPressed() => UIManager.Instance.ShowLogin();

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
        Debug.LogWarning($"[SignUpUIManager] {msg}");
    }

    private void ClearError()
    {
        if (errorText != null) errorText.text = string.Empty;
    }

    private void SetLoading(bool active)
    {
        if (loadingText != null) loadingText.gameObject.SetActive(active);
    }

    /// <summary>Converts raw Firebase error messages to friendlier strings.</summary>
    private string LocaliseFirebaseError(string firebaseMsg)
    {
        if (firebaseMsg.Contains("email-already-in-use"))
            return "An account with this email already exists.";
        if (firebaseMsg.Contains("invalid-email"))
            return "The email address is not valid.";
        if (firebaseMsg.Contains("weak-password"))
            return "Password is too weak. Use at least 6 characters.";
        return $"Sign-up failed: {firebaseMsg}";
    }
}
