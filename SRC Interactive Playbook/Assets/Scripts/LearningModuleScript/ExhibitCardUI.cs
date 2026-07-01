// ══════════════════════════════════════════════════════════════
// ExhibitCardUI — one card in the gallery (Image 1 style)
// ══════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using RedCross.Playbook.Data;
using RedCross.Playbook.Firebase;

namespace RedCross.Playbook.UI
{
    public class ExhibitCardUI : MonoBehaviour
    {
        // ── Inspector slots ────────────────────────────────────────
        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI exhibitNumberText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI pointsText;         // e.g. "+ 50 pts"

        [Header("Image")]
        [SerializeField] private RawImage thumbnail;

        [Header("Buttons & Badges")]
        [SerializeField] private Button enterButton;             // "Enter Exhibit" / "View"
        [SerializeField] private GameObject completedBadge;         // Green tick — optional

        [Header("Scene")]
        [Tooltip("Must match the exact scene name in File → Build Settings.")]
        [SerializeField] private string scenarioSceneName = "ScenarioScene";

        // ── Runtime ────────────────────────────────────────────────
        private string _scenarioId;

        // ══════════════════════════════════════════════════════════
        // Setup
        // ══════════════════════════════════════════════════════════

        private void Awake()
        {
            if (enterButton != null)
                enterButton.onClick.AddListener(OnEnterClicked);
            else
                Debug.LogWarning($"[ExhibitCardUI] enterButton is not assigned on {gameObject.name}. " +
                                 "Drag the button into the Enter Button slot in the Inspector.");
        }

        // Called by ScenarioListUI after instantiating the card prefab
        public void Initialise(ScenarioIndexEntry entry, System.Action<string> onTapped = null)
        {
            _scenarioId = entry.id;

            if (exhibitNumberText != null) exhibitNumberText.text = entry.exhibitNumber;
            if (titleText != null) titleText.text = entry.title;
            if (descriptionText != null) descriptionText.text = entry.outlineDescription;
            if (pointsText != null) pointsText.text = $"+ {entry.pointsOnCompletion} pts";

            // Load thumbnail from Resources (no extension)
            if (thumbnail != null && !string.IsNullOrEmpty(entry.thumbnailUrl))
            {
                var tex = Resources.Load<Texture2D>(entry.thumbnailUrl);
                if (tex != null) thumbnail.texture = tex;
            }

            // Show completed badge if this user has already finished the scenario
            string userId = PlayerPrefs.GetString("userId", "guest");
            FirebaseScenarioService.Instance.FetchUserProgress(
                userId, entry.id,
                progress =>
                {
                    if (completedBadge != null)
                        completedBadge.SetActive(progress != null && progress.completed);
                });
        }

        // ══════════════════════════════════════════════════════════
        // Button handler
        // ══════════════════════════════════════════════════════════
        private void OnEnterClicked()
        {
            if (string.IsNullOrEmpty(_scenarioId))
            {
                Debug.LogError("[ExhibitCardUI] _scenarioId is empty. " +
                               "Was Initialise() called on this card?");
                return;
            }

            Debug.Log($"[ExhibitCardUI] Loading scenario: {_scenarioId}");

            // ── Two-channel handoff to ScenarioSceneBootstrapper ──
            // Static field: survives scene load in the same process
            ScenarioSceneBootstrapper.PendingScenarioId = _scenarioId;
            // PlayerPrefs: fallback for edge cases / deep links
            PlayerPrefs.SetString("pendingScenarioId", _scenarioId);
            PlayerPrefs.Save();

            SceneManager.LoadScene(scenarioSceneName);
        }
    }
}