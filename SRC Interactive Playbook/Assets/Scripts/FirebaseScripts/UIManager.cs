/*
 * Description: Central canvas/screen router.  Manages transitions between
 *              Login, Sign-Up, Homepage, and Leaderboard panels.
 */

using System.Collections.Generic;
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
    [SerializeField] private string _trackSelectionName = "TrackSelectionPanel";
    [SerializeField] private string _homepageName = "HomepagePanel";
    [SerializeField] private string _leaderboardName = "LeaderboardPanel";
    [SerializeField] private string _cheatsheetName = "CheatsheetPanel";
    [SerializeField] private string _profileName = "ProfilePanel";
    [SerializeField] private string _presurveyName = "PresurveyPanel";
    [SerializeField] private string _postsurveyName = "PostsurveyPanel";
    [SerializeField] private string _homeSceneName = "HomeScene";

    // ── Live references (re-resolved on every HomeScene load) ──
    private List<GameObject> _landingPanels = new List<GameObject>();
    private List<GameObject> _signupPanels = new List<GameObject>(); 
    private List<GameObject> _loginPanels = new List<GameObject>(); 
    private List<GameObject> _trackSelectionPanels = new List<GameObject>();
    private List<GameObject> _homepagePanels = new List<GameObject>(); 
    private List<GameObject> _leaderboardPanels = new List<GameObject>();
    private List<GameObject> _cheatsheetPanels = new List<GameObject>();
    private List<GameObject> _profilePanels = new List<GameObject>();
    private List<GameObject> _presurveyPanels = new List<GameObject>();
    private List<GameObject> _postsurveyPanels = new List<GameObject>();

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
        SetList(_landingPanels, true);
        foreach (var nav in GetAllNavBars()) nav.HideNavBar();
    }

    public void ShowLogin()
    {
        SetAll(false);
        SetList(_loginPanels, true);
        foreach (var nav in GetAllNavBars()) nav.HideNavBar();
    }

    public void ShowSignup()
    {
        SetAll(false);
        SetList(_signupPanels, true);
        foreach (var nav in GetAllNavBars()) nav.HideNavBar();
    }

    public void ShowHomepage()
    {
        SetAll(false);
        var user = UserManager.Instance?.CurrentUser;
        if (user != null)
        {
            // 1. Check if track is selected
            if (string.IsNullOrEmpty(user.selectedTrack))
            {
                ShowTrackSelection();
                return;
            }

            // 2. Check if pre-survey is done
            if (!user.hasCompletedPreSurvey)
            {
                ShowPreSurvey();
                return;
            }
        }

        SetList(_homepagePanels, true);
        foreach (var nav in GetAllNavBars()) nav.ShowNavBar();
    }

    public void ShowTrackSelection()
    {
        SetAll(false);
        SetList(_trackSelectionPanels, true);
        foreach (var nav in GetAllNavBars()) nav.ShowNavBar();
    }
    public void ShowPreSurvey()
    {
        SetAll(false);
        SetList(_presurveyPanels, true);
        foreach (var nav in GetAllNavBars()) nav.ShowNavBar();
    }

    public void ShowLeaderboard()
    {
        // Leaderboard is now owned by NavBarController tab switching.
        // Calling this directly just ensures the homepage canvas is
        // visible and lets NavBar handle the panel.
        ShowHomepage();
        foreach (var nav in GetAllNavBars()) nav.ResetToModulesTab();
    }

    // ══════════════════════════════════════════════════════════
    // Reference resolution
    // ══════════════════════════════════════════════════════════

    private void ResolveReferences()
    {
        _landingPanels.Clear();
        _loginPanels.Clear();
        _signupPanels.Clear();
        _trackSelectionPanels.Clear();
        _homepagePanels.Clear();
        _leaderboardPanels.Clear();
        _cheatsheetPanels.Clear();
        _profilePanels.Clear();
        _presurveyPanels.Clear();
        _postsurveyPanels.Clear();

        FindAllInScene(_landingName, _landingPanels);
        FindAllInScene(_loginName, _loginPanels);
        FindAllInScene(_signupName, _signupPanels);
        FindAllInScene(_trackSelectionName, _trackSelectionPanels);
        FindAllInScene(_homepageName, _homepagePanels);
        FindAllInScene(_leaderboardName, _leaderboardPanels);
        FindAllInScene(_cheatsheetName, _cheatsheetPanels);
        FindAllInScene(_profileName, _profilePanels);
        FindAllInScene(_presurveyName, _presurveyPanels);
        FindAllInScene(_postsurveyName, _postsurveyPanels);
    }

    private void FindAllInScene(string goName, List<GameObject> results)
    {
        Scene home = SceneManager.GetSceneByName(_homeSceneName);
        if (home.IsValid() && home.isLoaded)
        {
            foreach (GameObject root in home.GetRootGameObjects())
            {
                if (root.name == goName) results.Add(root);
                FindAllDeep(root.transform, goName, results);
            }
        }
    }

    private void FindAllDeep(Transform parent, string name, List<GameObject> results)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) results.Add(child.gameObject);
            FindAllDeep(child, name, results);
        }
    }

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════

    private void SetAll(bool active)
    {
        SetList(_landingPanels, active);
        SetList(_loginPanels, active);
        SetList(_signupPanels, active);
        SetList(_trackSelectionPanels, active);
        SetList(_homepagePanels, active);
        SetList(_leaderboardPanels, active);
        SetList(_cheatsheetPanels, active);
        SetList(_profilePanels, active);
        SetList(_presurveyPanels, active);
        SetList(_postsurveyPanels, active);
    }

    private void SetList(List<GameObject> panels, bool active)
    {
        foreach (var panel in panels)
        {
            if (panel != null) panel.SetActive(active);
        }
    }

    public NavBarController[] GetAllNavBars()
    {
        // Finds both the Mobile and Desktop NavBars
        return FindObjectsByType<NavBarController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }
}