// ══════════════════════════════════════════════════════════════
// ScenarioSceneBootstrapper — reads pending ID and starts play
// Attach to any GameObject in ScenarioScene
// ══════════════════════════════════════════════════════════════

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using RedCross.Playbook.Scenario;

namespace RedCross.Playbook.UI
{
    public class ScenarioSceneBootstrapper : MonoBehaviour
    {
        // ── Static handoff from ExhibitCardUI ─────────────────────
        public static string PendingScenarioId = "";

        [Header("Scene references")]
        [Tooltip("Drag the MasterCanvas GameObject here so the bootstrapper can ensure it starts active.")]
        [SerializeField] private GameObject masterCanvas;

        private void Start()
        {
            if (masterCanvas != null)
                masterCanvas.SetActive(true);
            else
                Debug.LogWarning("[ScenarioSceneBootstrapper] MobileLayout not assigned. " +
                                 "Drag MasterCanvas from the Hierarchy into this component's slot.");

            StartCoroutine(StartAfterFrame());
        }

        private IEnumerator StartAfterFrame()
        {
            yield return null; 

            // Read the scenario ID — prefer static field (set same frame
            // as LoadScene), fall back to PlayerPrefs
            string id = PendingScenarioId;
            string source = "static field";

            if (string.IsNullOrEmpty(id))
            {
                id = PlayerPrefs.GetString("pendingScenarioId", "");
                source = "PlayerPrefs";
            }

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[ScenarioSceneBootstrapper] No pending scenario ID found in " +
                               "either ScenarioSceneBootstrapper.PendingScenarioId or " +
                               "PlayerPrefs[\"pendingScenarioId\"]. " +
                               "Make sure ExhibitCardUI.OnEnterClicked() is running before LoadScene.");
                SceneManager.LoadScene("HomeScene");
                yield break;
            }

            Debug.Log($"[ScenarioSceneBootstrapper] Starting scenario '{id}' (from {source}).");

            // Clear so back-navigation doesn't re-trigger
            PendingScenarioId = "";
            PlayerPrefs.DeleteKey("pendingScenarioId");

            if (ScenarioManager.Instance == null)
            {
                Debug.LogError("[ScenarioSceneBootstrapper] ScenarioManager.Instance is null. " +
                               "Make sure a GameObject with ScenarioManager.cs exists in ScenarioScene " +
                               "and is NOT marked DontDestroyOnLoad.");
                yield break;
            }
            if (PointsManager.Instance != null)
                PointsManager.Instance.TrySubscribeToScenarioManager();
            else
                Debug.LogWarning("[ScenarioSceneBootstrapper] PointsManager.Instance not found. " +
                                 "Points will not be tracked. Make sure PointsManager is on a " +
                                 "DontDestroyOnLoad object initialised in HomeScene.");
            ScenarioManager.Instance.StartScenario(id);
        }
    }
}