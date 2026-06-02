/*
 * Description: Central canvas/screen router.  Manages transitions between
 *              Login, Sign-Up, Homepage, and Leaderboard panels.
 */

using UnityEngine;

public class UIManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static UIManager Instance { get; private set; }

    // ── Canvas references (assign in Inspector) ────────────────────────────────
    [Header("Canvas Panels")]
    [SerializeField] private GameObject landingCanvas;
    [SerializeField] private GameObject loginCanvas;
    [SerializeField] private GameObject signupCanvas;
    [SerializeField] private GameObject homepageCanvas;
    [SerializeField] private GameObject leaderboardCanvas;

    // ══════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start() => ShowLanding();

    // ══════════════════════════════════════════════════════════════════════════
    //  PUBLIC NAVIGATION
    // ══════════════════════════════════════════════════════════════════════════

    public void ShowLanding()
    {
        Set(landingCanvas,   true);
        Set(loginCanvas,     false);
        Set(signupCanvas,    false);
        Set(homepageCanvas,  false);
        Set(leaderboardCanvas, false);
    }
    public void ShowLogin()
    {
        Set(landingCanvas,    false);
        Set(loginCanvas,      true);
        Set(signupCanvas,     false);
        Set(homepageCanvas,   false);
        Set(leaderboardCanvas, false);
    }

    public void ShowSignup()
    {
        Set(landingCanvas,    false);
        Set(loginCanvas,      false);
        Set(signupCanvas,     true);
        Set(homepageCanvas,   false);
        Set(leaderboardCanvas, false);
    }

    public void ShowHomepage()
    {
        Set(landingCanvas,    false);
        Set(loginCanvas,      false);
        Set(signupCanvas,     false);
        Set(homepageCanvas,   true);
        Set(leaderboardCanvas, false);
    }

    public void ShowLeaderboard()
    {
        Set(landingCanvas,    false);
        Set(loginCanvas,      false);
        Set(signupCanvas,     false);
        Set(homepageCanvas,   false);
        Set(leaderboardCanvas, true);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void Set(GameObject canvas, bool active)
    {
        if (canvas != null) canvas.SetActive(active);
    }
}
