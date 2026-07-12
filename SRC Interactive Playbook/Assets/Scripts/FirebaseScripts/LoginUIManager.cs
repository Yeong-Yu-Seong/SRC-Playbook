/*
 * Description: Handles the Login screen — input validation, Firebase Auth
 *              sign-in, and routing to the Museum Homepage on success.
 */

using UnityEngine;
using TMPro;

public class LoginUIManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField passwordField;

    [Header("Feedback")]
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text loadingText;   // optional spinner label

    // ══════════════════════════════════════════════════════════════════════════
    //  PUBLIC ACTIONS (wired to UI buttons)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Called by the Login button's OnClick event.</summary>
    public void OnLoginPressed()
    {
        ClearError();
        string email    = emailField.text.Trim();
        string password = passwordField.text;

        // ── Validation ────────────────────────────────────────────────────────
        if (string.IsNullOrEmpty(email))            { ShowError("Email cannot be empty.");              return; }
        if (!email.Contains("@") || !email.Contains(".")) { ShowError("Please enter a valid email address."); return; }
        if (string.IsNullOrEmpty(password))         { ShowError("Password cannot be empty.");           return; }
        if (password.Length < 6)                    { ShowError("Password must be at least 6 characters."); return; }

        SetLoading(true);

        // ── Firebase login ────────────────────────────────────────────────────
        FirebaseManager.Instance.Login(
            email, password,
            onSuccess: user =>
            {
                SetLoading(false);
                Debug.Log($"[LoginUIManager] Welcome back, {user.username}! Score: {user.score}");

                // Hand the loaded user to the central UserManager
                UserManager.Instance.SetUserData(user);
                UIManager.Instance.ShowHomepage();
            },
            onError: err =>
            {
                SetLoading(false);
                ShowError($"Login failed: {err}");
            }
        );
    }

    public void OnGoogleLoginPressed()
    {
        SetLoading(true);

        FirebaseManager.Instance.LoginWithGoogle(
            onSuccess: user =>
            {
                SetLoading(false);
                UserManager.Instance.SetUserData(user);
                UIManager.Instance.ShowHomepage();
            },
            onError: err =>
            {
                SetLoading(false);
                ShowError(err);
            }
        );
    }

    /// <summary>Navigates to the Sign-Up screen.</summary>
    public void OnGoToSignupPressed() => UIManager.Instance.ShowSignup();

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
        Debug.LogWarning($"[LoginUIManager] {msg}");
    }

    private void ClearError()
    {
        if (errorText != null) errorText.text = string.Empty;
    }

    private void SetLoading(bool active)
    {
        if (loadingText != null) loadingText.gameObject.SetActive(active);
    }
}
