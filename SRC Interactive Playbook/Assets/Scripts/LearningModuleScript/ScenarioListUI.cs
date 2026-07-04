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

        private void Start() => LoadScenarioList();

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

            if (entries == null || entries.Count == 0)
            {
                if (emptyState != null) emptyState.SetActive(true);
                return;
            }

            // FIXED: Firebase returns entries already ordered by sortOrder (via
            // .orderByChild("sortOrder") in FetchIndexFromFirebase). Local fallback
            // also sorts before returning. Belt-and-suspenders sort here as well.
            entries.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            foreach (var entry in entries)
            {
                // Instantiate without parenting first so we can set RectTransform freely
                var go = Instantiate(exhibitCardPrefab, cardContainer, false);

                // Now each card is placed at its curator-specified wall position and size
                // using the wallX/wallY/cardWidth/cardHeight fields from Firebase.
                // The cardContainer must NOT have a LayoutGroup component attached —
                // remove any GridLayoutGroup or VerticalLayoutGroup from it in the Inspector.
                var rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);   // top-left anchor
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(entry.wallX, -entry.wallY);
                    rt.sizeDelta = new Vector2(entry.cardWidth, entry.cardHeight);
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