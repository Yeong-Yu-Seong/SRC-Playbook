using RedCross.Playbook.Data;
using RedCross.Playbook.Firebase;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;           // ← needed for IEnumerator (Option B)
// using DG.Tweening;               // ← uncomment this instead if DOTween is installed

namespace RedCross.Playbook.UI
{
    public class ExhibitCardUI : MonoBehaviour
    {
        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI exhibitNumberText;
        [SerializeField] private TextMeshProUGUI pointsText;

        [Header("Image")]
        [SerializeField] private RawImage thumbnail;

        [Header("Buttons & Badges")]
        [SerializeField] private GameObject completedBadge;
        [SerializeField] private Button frameButton;

        [Header("Scene")]
        [Tooltip("Must match the exact scene name in File → Build Settings.")]
        [SerializeField] private string scenarioSceneName = "ScenarioScene";

        private string _scenarioId;
        private ScenarioIndexEntry _entry;

        private void Awake()
        {
            if (frameButton != null)
                frameButton.onClick.AddListener(OnFrameTapped);
        }

        public void Initialise(ScenarioIndexEntry entry, System.Action<string> onTapped = null)
        {
            _entry = entry;
            _scenarioId = entry.id;

            if (exhibitNumberText != null) exhibitNumberText.text = entry.exhibitNumber;
            if (pointsText != null) pointsText.text = $"+{entry.pointsOnCompletion}";

            if (thumbnail != null && !string.IsNullOrEmpty(entry.thumbnailUrl))
            {
                var tex = Resources.Load<Texture2D>(entry.thumbnailUrl);
                if (tex != null) thumbnail.texture = tex;
            }
            // 1. Grab the base sizes from Firebase (or use defaults)
            float finalWidth = entry.cardWidth > 0 ? entry.cardWidth : 300f;
            float finalHeight = entry.cardHeight > 0 ? entry.cardHeight : 240f;

            float finalImgWidth = entry.imgWidth > 0 ? entry.imgWidth : 700f;
            float finalImgHeight = entry.imgHeight > 0 ? entry.imgHeight : 526f;

            // 2. If we are on Desktop, scale the sizes up using the same math!
            if (!ResponsiveLayoutManager.Instance.IsMobileActive)
            {
                float scaleX = 1206f / 1920f;
                //float scaleY = 2622f / 1080f; 

                finalWidth *= scaleX;
                finalHeight *= scaleX;

                finalImgWidth *= scaleX;
                finalImgHeight *= scaleX;
            }

            // 3. Apply the final scaled sizes to the RectTransforms
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(finalWidth, finalHeight);
            }

            if (frameButton != null)
            {
                RectTransform frameRt = frameButton.GetComponent<RectTransform>();
                //if (frameRt != null)
                //{
                //    frameRt.sizeDelta = new Vector2(finalImgWidth, finalImgHeight);
                //}
            }
            string userId = PlayerPrefs.GetString("userId", "guest");
            FirebaseScenarioService.Instance.FetchUserProgress(userId, entry.id,
                progress =>
                {
                    if (completedBadge != null)
                        completedBadge.SetActive(progress != null && progress.completed);
                });

        }

        private void OnEnterClicked()
        {
            if (string.IsNullOrEmpty(_scenarioId))
            {
                Debug.LogError("[ExhibitCardUI] _scenarioId is empty. Was Initialise() called?");
                return;
            }

            Debug.Log($"[ExhibitCardUI] Loading scenario: {_scenarioId}");
            ScenarioSceneBootstrapper.PendingScenarioId = _scenarioId;
            PlayerPrefs.SetString("pendingScenarioId", _scenarioId);
            PlayerPrefs.Save();
            SceneManager.LoadScene(scenarioSceneName);
        }

        private void OnFrameTapped()
        {
            // Option B — coroutine punch scale (no DOTween needed)
            StartCoroutine(PunchScale());

            // Option A — uncomment below and delete the line above if DOTween is installed
            // transform.DOPunchScale(Vector3.one * 0.04f, 0.25f, 1, 0.5f);

            InfoOverlayUI.Instance.Show(_entry, OnEnterClicked);
        }

        // ── Coroutine: quick scale punch (replaces DOPunchScale) ──
        private IEnumerator PunchScale()
        {
            Vector3 original = transform.localScale;
            Vector3 punched = original * 1.04f;
            float duration = 0.12f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(original, punched, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;

            // Scale back down
            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(punched, original, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = original;
        }
    }
}