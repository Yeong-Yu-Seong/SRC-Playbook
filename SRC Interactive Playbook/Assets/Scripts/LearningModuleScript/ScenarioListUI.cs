// ============================================================
// ScenarioListUI.cs
// Populates the homepage exhibit gallery with scenario cards.
// Tapping a card loads the ScenarioScene and starts that scenario.
// ============================================================
using System.Collections.Generic;
using RedCross.Playbook.Data;
using RedCross.Playbook.Firebase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private readonly List<GameObject> _spawnedCards = new();

        // ══════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ══════════════════════════════════════════════════════════

        private void Start()
        {
            if (FirebaseScenarioService.Instance.IsInitialized)
            {
                LoadScenarioList(); // Your method that calls FetchScenarioIndex
            }
            else
            {
                // Wait for Firebase to connect before fetching
                FirebaseScenarioService.Instance.OnFirebaseReady += LoadScenarioList;
            }
        }

        /// <summary>Called by HomepageUIManager on return from ScenarioScene.</summary>
        public void RefreshList() => LoadScenarioList();

        // ══════════════════════════════════════════════════════════
        //  LOAD & RENDER
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

            // ── NEW: Filter by Track ──────────────────────────────────────
            string userTrack = UserManager.Instance?.CurrentUser?.selectedTrack;

            if (entries != null)
            {
                entries = entries.FindAll(entry =>
                    string.IsNullOrEmpty(entry.track) || // Failsafe for older database entries
                    entry.track == "Both" ||             // Shared content
                    entry.track == userTrack             // Matches the user's chosen track
                );
            }
            // ──────────────────────────────────────────────────────────────

            if (entries == null || entries.Count == 0)
            {
                if (emptyState != null) emptyState.SetActive(true);
                return;
            }

            // Belt-and-suspenders sort
            entries.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            foreach (var entry in entries)
            {
                var go = Instantiate(exhibitCardPrefab, cardContainer, false);

                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);

                    // 1. Grab the Firebase coordinates
                    float finalX = entry.wallX;
                    float finalY = entry.wallY;

                    // 2. If we are on Desktop, scale the coordinates up to match the 1920x1080 canvas!
                    if (!ResponsiveLayoutManager.Instance.IsMobileActive)
                    {
                        finalX *= (1206f / 1920f);

                        finalY *= (1206f / 1920f);
                    }

                    // 3. Apply the scaled coordinates
                    rt.anchoredPosition = new Vector2(finalX, -finalY);

                    // (Don't set sizeDelta here anymore, since we moved the sizing logic inside ExhibitCardUI in the previous fix!)
                }

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