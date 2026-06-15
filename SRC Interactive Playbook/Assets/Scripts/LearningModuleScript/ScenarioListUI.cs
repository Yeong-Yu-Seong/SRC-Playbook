// ============================================================
// ScenarioListUI.cs
// Populates the homepage exhibit gallery with scenario cards.
// Tapping a card loads the ScenarioScene and starts that scenario.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using RedCross.Playbook.Data;
using RedCross.Playbook.Firebase;
using RedCross.Playbook.Scenario;

namespace RedCross.Playbook.UI
{
    public class ScenarioListUI : MonoBehaviour
    {
        [Header("Gallery — drag from Hierarchy")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject exhibitCardPrefab;

        [Header("States")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private GameObject emptyState;

        [Header("Scene")]
        [Tooltip("Must match the exact scene name in Build Settings.")]
        [SerializeField] private string scenarioSceneName = "ScenarioScene";

        private readonly List<GameObject> _spawnedCards = new();

        // ── Called on first load ───────────────────────────────────
        private void Start() => LoadScenarioList();

        // ── Called by HomepageUIManager on return from ScenarioScene ──
        public void RefreshList() => LoadScenarioList();

        // ══════════════════════════════════════════════════════════
        // Load & render
        // ══════════════════════════════════════════════════════════

        private void LoadScenarioList()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(true);
            if (emptyState != null) emptyState.SetActive(false);

            FirebaseScenarioService.Instance.FetchScenarioIndex(OnIndexReceived);
        }

        private void OnIndexReceived(List<ScenarioIndexEntry> entries)
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(false);

            foreach (var c in _spawnedCards) Destroy(c);
            _spawnedCards.Clear();

            if (entries == null || entries.Count == 0)
            {
                if (emptyState != null) emptyState.SetActive(true);
                return;
            }

            foreach (var entry in entries)
            {
                var go = Instantiate(exhibitCardPrefab, cardContainer);
                var card = go.GetComponent<ExhibitCardUI>();
                if (card != null)
                    card.Initialise(entry);
                else
                    Debug.LogError("[ScenarioListUI] ExhibitCard prefab is missing ExhibitCardUI component.");

                _spawnedCards.Add(go);
            }
        }
    }
}