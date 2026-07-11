/*
 * Description: Central canvas/screen router.  Manages transitions between
 *              Login, Sign-Up, Homepage, and Leaderboard panels.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    public static UIManager Instance { get; private set; }

    // ── Canvas name constants ──────────────────────────────────
    [Header("Canvas GameObject names — must match Hierarchy exactly")]
    [SerializeField] private string _landingName = "LandingPagePanel";
    [SerializeField] private string _loginName = "LoginPanel";
    [SerializeField] private string _signupName = "SignUpPanel";
    [SerializeField] private string _homepageName = "HomepagePanel";
    [SerializeField] private string _leaderboardName = "LeaderboardPanel";
    [SerializeField] private string _cheatsheetName = "CheatsheetPanel";
    [SerializeField] private string _profileName = "ProfilePanel";
    [SerializeField] private string _presurveyName = "PresurveyPanel";
    [SerializeField] private string _postsurveyName = "PostsurveyPanel";
    [SerializeField] private string _homeSceneName = "HomeScene";

    // ── Live references (re-resolved on every HomeScene load) ──
    private GameObject _landingPanel;
    private GameObject _loginPanel;
    private GameObject _signupPanel;
    private GameObject _homepagePanel;
    private GameObject _leaderboardPanel;
    private GameObject _cheatsheetPanel;
    private GameObject _profilePanel;
    private GameObject _presurveyPanel;
    private GameObject _postsurveyPanel;

    // ══════════════════════════════════════════════════════════
    // Lifecycle
    // ══════════════════════════════════════════════════════════

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
        ResolveReferences();
        ShowLanding();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _homeSceneName) return;

        ResolveReferences();

        // Returning from ScenarioScene — user is already logged in
        if (UserManager.Instance?.CurrentUser != null)
            ShowHomepage();
        else
            ShowLanding();
    }

    // ══════════════════════════════════════════════════════════
    // Public navigation
    // ══════════════════════════════════════════════════════════

    public void ShowLanding()
    {
        SetAll(false);
        Set(_landingPanel, true);
        GetNavBar()?.HideNavBar();
    }

    public void ShowLogin()
    {
        SetAll(false);
        Set(_loginPanel, true);
        GetNavBar()?.HideNavBar();
    }

    public void ShowSignup()
    {
        SetAll(false);
        Set(_signupPanel, true);
        GetNavBar()?.HideNavBar();
    }

    public void ShowHomepage()
    {
        SetAll(false);
        Set(_homepagePanel, true);

        // Check if pre-survey is done
        if (UserManager.Instance?.CurrentUser != null && !UserManager.Instance.CurrentUser.hasCompletedPreSurvey)
        {
            ShowPreSurvey();
            return;
        }

        Set(_homepagePanel, true);
        GetNavBar()?.ShowNavBar();
    }

    public void ShowPreSurvey()
    {
        SetAll(false);
        Set(_presurveyPanel, true);
        GetNavBar()?.HideNavBar();
    }

    public void ShowLeaderboard()
    {
        // Leaderboard is now owned by NavBarController tab switching.
        // Calling this directly just ensures the homepage canvas is
        // visible and lets NavBar handle the panel.
        ShowHomepage();
        GetNavBar()?.ResetToModulesTab();
    }

    // ══════════════════════════════════════════════════════════
    // Reference resolution
    // ══════════════════════════════════════════════════════════

    private void ResolveReferences()
    {
        // All panels live inside SignInCanvas in your hierarchy,
        // so we search its children
        _landingPanel = FindInScene(_landingName);
        _loginPanel = FindInScene(_loginName);
        _signupPanel = FindInScene(_signupName);
        _homepagePanel = FindInScene(_homepageName);
        _leaderboardPanel = FindInScene(_leaderboardName);
        _cheatsheetPanel = FindInScene(_cheatsheetName);
        _profilePanel = FindInScene(_profileName);
        _presurveyPanel = FindInScene(_presurveyName);
        _postsurveyPanel = FindInScene(_postsurveyName);
    }

    private GameObject FindInScene(string goName)
    {
        Scene home = SceneManager.GetSceneByName(_homeSceneName);
        if (home.IsValid() && home.isLoaded)
        {
            foreach (GameObject root in home.GetRootGameObjects())
            {
                if (root.name == goName) return root;
                Transform found = FindDeep(root.transform, goName);
                if (found != null) return found.gameObject;
            }
        }
        // Fallback for first load before scene is fully registered
        GameObject direct = GameObject.Find(goName);
        if (direct == null)
            Debug.LogWarning($"[UIManager] Could not find '{goName}' in scene. " +
                             $"Check the name matches your Hierarchy exactly.");
        return direct;
    }

    private Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════

    private void SetAll(bool active)
    {
        Set(_landingPanel, active);
        Set(_loginPanel, active);
        Set(_signupPanel, active);
        Set(_homepagePanel, active);
        Set(_leaderboardPanel, active);
        Set(_cheatsheetPanel, active);
        Set(_profilePanel, active);
        Set(_presurveyPanel, active);
        Set(_postsurveyPanel, active);
    }

    private void Set(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    public NavBarController GetNavBar()
    {
        // NavBarController is scene-local so FindObjectOfType works here
        NavBarController nav = FindFirstObjectByType<NavBarController>(FindObjectsInactive.Include);
        if (nav == null)
            Debug.LogWarning("[UIManager] NavBarController not found in scene.");
        return nav;
    }
}