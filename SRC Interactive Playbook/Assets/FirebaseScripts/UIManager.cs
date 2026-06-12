/*
 * Description: Central canvas/screen router.  Manages transitions between
 *              Login, Sign-Up, Homepage, and Leaderboard panels.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────────
    public static UIManager Instance { get; private set; }

    // ── Canvas name constants ──────────────────────────────────────────────────
    // Update these to exactly match your HomeScene Hierarchy names.
    [Header("Canvas GameObject names in HomeScene (must match Hierarchy exactly)")]
    [SerializeField] private string _landingName     = "LandingCanvas";
    [SerializeField] private string _loginName       = "LoginCanvas";
    [SerializeField] private string _signupName      = "SignUpCanvas";
    [SerializeField] private string _homepageName    = "Homepage";
    [SerializeField] private string _leaderboardName = "LeaderboardCanvas";

    // ── Live references (re-resolved every HomeScene load) ────────────────────
    private GameObject _landingCanvas;
    private GameObject _loginCanvas;
    private GameObject _signupCanvas;
    private GameObject _homepageCanvas;
    private GameObject _leaderboardCanvas;

    // ── Which scene holds the canvases ────────────────────────────────────────
    [Header("Scene name that contains the canvases")]
    [SerializeField] private string _homeSceneName = "HomeScene";

    // ══════════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Resolve references for the initial scene load
        ResolveCanvasReferences();
        ShowLanding();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Called automatically by Unity every time any scene finishes loading
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == _homeSceneName)
        {
            // HomeScene just (re)loaded — find the fresh canvas instances
            ResolveCanvasReferences();

            // If a user is already logged in (returning from ScenarioScene),
            // go straight to homepage instead of landing
            if (UserManager.Instance?.CurrentUser != null)
                ShowHomepage();
            else
                ShowLanding();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CANVAS REFERENCE RESOLUTION
    // ══════════════════════════════════════════════════════════════════════════

    private void ResolveCanvasReferences()
    {
        _landingCanvas     = FindCanvas(_landingName);
        _loginCanvas       = FindCanvas(_loginName);
        _signupCanvas      = FindCanvas(_signupName);
        _homepageCanvas    = FindCanvas(_homepageName);
        _leaderboardCanvas = FindCanvas(_leaderboardName);
    }

    private GameObject FindCanvas(string goName)
    {
        // GameObject.Find only searches active objects — use this version
        // that also finds inactive ones by searching all root objects
        Scene home = SceneManager.GetSceneByName(_homeSceneName);
        if (!home.IsValid() || !home.isLoaded) 
        {
            // Fall back to global search (works on initial load)
            GameObject found = GameObject.Find(goName);
            if (found == null)
                Debug.LogWarning($"[UIManager] Could not find canvas '{goName}'. " +
                                 $"Check the GameObject name in the Hierarchy.");
            return found;
        }

        // Search all root GameObjects in HomeScene (catches inactive panels)
        foreach (GameObject root in home.GetRootGameObjects())
        {
            if (root.name == goName) return root;

            // Also search one level deep for canvases nested under a parent
            Transform child = root.transform.Find(goName);
            if (child != null) return child.gameObject;
        }

        Debug.LogWarning($"[UIManager] Could not find canvas '{goName}' in HomeScene. " +
                         $"Check the name in the UIManager Inspector matches your Hierarchy.");
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PUBLIC NAVIGATION
    // ══════════════════════════════════════════════════════════════════════════

    public void ShowLanding()
    {
        Set(_landingCanvas,     true);
        Set(_loginCanvas,       false);
        Set(_signupCanvas,      false);
        Set(_homepageCanvas,    false);
        Set(_leaderboardCanvas, false);
    }

    public void ShowLogin()
    {
        Set(_landingCanvas,     false);
        Set(_loginCanvas,       true);
        Set(_signupCanvas,      false);
        Set(_homepageCanvas,    false);
        Set(_leaderboardCanvas, false);
    }

    public void ShowSignup()
    {
        Set(_landingCanvas,     false);
        Set(_loginCanvas,       false);
        Set(_signupCanvas,      true);
        Set(_homepageCanvas,    false);
        Set(_leaderboardCanvas, false);
    }

    public void ShowHomepage()
    {
        Set(_landingCanvas,     false);
        Set(_loginCanvas,       false);
        Set(_signupCanvas,      false);
        Set(_homepageCanvas,    true);
        Set(_leaderboardCanvas, false);
    }

    public void ShowLeaderboard()
    {
        Set(_landingCanvas,     false);
        Set(_loginCanvas,       false);
        Set(_signupCanvas,      false);
        Set(_homepageCanvas,    false);
        Set(_leaderboardCanvas, true);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private void Set(GameObject canvas, bool active)
    {
        if (canvas != null)
            canvas.SetActive(active);
        else
            Debug.LogWarning($"[UIManager] Tried to set a canvas but the reference is null. " +
                             $"Re-run the scene or check canvas names in the UIManager Inspector.");
    }
}