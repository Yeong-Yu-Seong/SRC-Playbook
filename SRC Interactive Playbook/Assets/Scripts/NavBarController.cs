// ============================================================
// NavBarController.cs  —  Red Cross Playbook
//
// Controls the bottom navigation bar that appears on:
//   HomeScene:     Homepage, Leaderboard, Cheatsheet, Profile
//   ScenarioScene: ScenarioIntroPanel only
//
// HOW TO SET UP:
// 1. In HomeScene: create a new child panel under your main
//    Canvas called "NavBar". Add this script to it.
//    Wire the 4 tab buttons and the 4 page panels below.
//
// 2. In ScenarioScene: create a SEPARATE NavBar panel under
//    the MobileLayout Canvas. Add NavBarController to it.
//    Leave all homeScene slots empty — only assign
//    scenarioIntroPanel. The script detects which scene
//    it is in and shows/hides accordingly.
//
// 3. The NavBar panel itself should sit at the BOTTOM of the
//    Canvas with a fixed height (e.g. 80px), anchored to
//    bottom-stretch. Set its Rect Transform:
//      Anchor: bottom-stretch (min X=0, max X=1, min Y=0, max Y=0)
//      Height: 80
//      Pos Y: 0
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class NavBarController : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────
    // Not DontDestroyOnLoad — each scene has its own NavBar.
    // Static so ScenarioScene navbar can tell HomeScene which
    // tab to restore on return.
    public static Tab RequestedTab = Tab.Modules;

    public enum Tab { Modules, Leaderboard, Cheatsheet, Profile }

    // ── Inspector: Tab buttons ─────────────────────────────────
    [Header("Nav Tab Buttons")]
    [SerializeField] private Button modulesTab;
    [SerializeField] private Button leaderboardTab;
    [SerializeField] private Button cheatsheetTab;
    [SerializeField] private Button profileTab;

    // ── Inspector: Tab icons ───────────────────────────────────
    [Header("Tab icons (Image component on each button)")]
    [SerializeField] private Image modulesIcon;
    [SerializeField] private Image leaderboardIcon;
    [SerializeField] private Image cheatsheetIcon;
    [SerializeField] private Image profileIcon;

    [SerializeField] private Color activeColor = new Color(0.75f, 0.16f, 0.11f);
    [SerializeField] private Color inactiveColor = new Color(0.55f, 0.51f, 0.47f);

    // ── Inspector: HomeScene panels ────────────────────────────
    [Header("HomeScene panels (leave ALL null in ScenarioScene)")]
    [SerializeField] private GameObject homepagePanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject cheatsheetPanel;
    [SerializeField] private GameObject profilePanel;

    // ── Inspector: ScenarioScene only ─────────────────────────
    [Header("ScenarioScene only — assign ScenarioIntroPanel here")]
    [SerializeField] private GameObject scenarioIntroPanel;

    // ── Inspector: LeaderboardManager ─────────────────────────
    [Header("Optional — auto-refresh leaderboard on tab tap")]
    [SerializeField] private LeaderboardManager leaderboardManager;

    // ── Scene name ─────────────────────────────────────────────
    [Header("Scene names — must match Build Settings exactly")]
    [SerializeField] private string homeSceneName = "HomeScene";
    [SerializeField] private string scenarioSceneName = "ScenarioScene";

    // ── Internal ───────────────────────────────────────────────
    private Tab _activeTab;
    private bool _isScenarioScene;

    // ══════════════════════════════════════════════════════════
    // Lifecycle
    // ══════════════════════════════════════════════════════════

    private void Awake()
    {
        _isScenarioScene = SceneManager.GetActiveScene().name == scenarioSceneName;

        WireButtons();

        if (!_isScenarioScene)
        {
            SwitchTab(RequestedTab);

            bool isLoggedIn = UserManager.Instance != null && 
                              UserManager.Instance.CurrentUser != null;
            
            gameObject.SetActive(isLoggedIn);
        }
        else
        {
            // Scenario scene always starts hidden
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // ScenarioScene only: show NavBar exclusively while
        // ScenarioIntroPanel is active
        if (_isScenarioScene)
        {
            bool introVisible = scenarioIntroPanel != null &&
                                scenarioIntroPanel.activeInHierarchy;
            if (gameObject.activeSelf != introVisible)
                gameObject.SetActive(introVisible);
        }
    }

    // ══════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Call this after login or signup succeeds.
    /// UIManager.ShowHomepage() should call this.
    /// </summary>
    public void ShowNavBar()
    {
        gameObject.SetActive(true);
        if (_isScenarioScene) return;
        SwitchTab(RequestedTab);
    }

    /// <summary>Call on logout to hide the navbar.</summary>
    public void HideNavBar() => gameObject.SetActive(false);

    public void ResetToModulesTab()
    {
        RequestedTab = Tab.Modules;
        if (gameObject.activeSelf)
            SwitchTab(Tab.Modules);
    }

    // ══════════════════════════════════════════════════════════
    // Tab switching
    // ══════════════════════════════════════════════════════════

    private void SwitchTab(Tab tab)
    {
        _activeTab = tab;

        if (_isScenarioScene)
        {
            // In ScenarioScene, tabs navigate back to HomeScene.
            // Store which tab was requested so HomeScene restores it.
            RequestedTab = tab;
            SceneManager.LoadScene(homeSceneName);
            return;
        }

        // HomeScene — switch panels normally
        SetPanel(homepagePanel, tab == Tab.Modules);
        SetPanel(leaderboardPanel, tab == Tab.Leaderboard);
        SetPanel(cheatsheetPanel, tab == Tab.Cheatsheet);
        SetPanel(profilePanel, tab == Tab.Profile);

        SetIconColor(modulesIcon, tab == Tab.Modules);
        SetIconColor(leaderboardIcon, tab == Tab.Leaderboard);
        SetIconColor(cheatsheetIcon, tab == Tab.Cheatsheet);
        SetIconColor(profileIcon, tab == Tab.Profile);

        if (tab == Tab.Leaderboard && leaderboardManager != null)
            leaderboardManager.RefreshLeaderboard();

        if (tab == Tab.Profile && ProfileManager.profileManagerInstance != null)
        {
            ProfileManager.profileManagerInstance.ShowProfile();
        }

        Debug.Log($"[NavBarController] Switched to tab: {tab}");
    }

    // ══════════════════════════════════════════════════════════
    // Button wiring
    // ══════════════════════════════════════════════════════════

    private void WireButtons()
    {
        if (modulesTab != null) modulesTab.onClick.AddListener(() => SwitchTab(Tab.Modules));
        if (leaderboardTab != null) leaderboardTab.onClick.AddListener(() => SwitchTab(Tab.Leaderboard));
        if (cheatsheetTab != null) cheatsheetTab.onClick.AddListener(() => SwitchTab(Tab.Cheatsheet));
        if (profileTab != null) profileTab.onClick.AddListener(() => SwitchTab(Tab.Profile));
    }

    // ══════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private void SetIconColor(Image icon, bool active)
    {
        if (icon != null) icon.color = active ? activeColor : inactiveColor;
    }
}