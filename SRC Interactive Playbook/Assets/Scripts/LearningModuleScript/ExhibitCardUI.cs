using RedCross.Playbook.Data;
using RedCross.Playbook.Firebase;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace RedCross.Playbook.UI
{
    public class ExhibitCardUI : MonoBehaviour
    {
        [Header("Text Fields")]
        [SerializeField] private TextMeshProUGUI exhibitNumberText;

        [Header("Images")]
        [SerializeField] private RawImage thumbnail;
        [Tooltip("Assign the FrameOuter Image component here")]
        [SerializeField] private Image frameImage;

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

            // 1. Load Thumbnail (Dynamic URL from Firebase Storage)
            if (thumbnail != null && !string.IsNullOrEmpty(entry.thumbnailUrl))
            {
                StartCoroutine(LoadTextureFromUrl(entry.thumbnailUrl, thumbnail));
            }

            // 2. Load Local Frame Sprite based on Admin Dashboard selection
            if (frameImage != null && !string.IsNullOrEmpty(entry.frameUrl))
            {
                // Loads the exact sprite name selected in the dashboard from the Resources/Frames folder
                Sprite loadedFrame = Resources.Load<Sprite>($"Frames/{entry.frameUrl}");
                if (loadedFrame != null)
                {
                    frameImage.sprite = loadedFrame;
                }
                else
                {
                    Debug.LogWarning($"[ExhibitCardUI] Could not find frame sprite: Resources/Frames/{entry.frameUrl}");
                }
            }

            // 3. Grab the base sizes from Firebase (or use safe fallbacks)
            float finalWidth = entry.cardWidth > 0 ? entry.cardWidth : 300f;
            float finalHeight = entry.cardHeight > 0 ? entry.cardHeight : 450f;
            float finalImgWidth = entry.imgWidth > 0 ? entry.imgWidth : 280f;
            float finalImgHeight = entry.imgHeight > 0 ? entry.imgHeight : 200f;

            // 4. If we are on Desktop, scale the sizes up using the responsive layout multiplier
            if (!ResponsiveLayoutManager.Instance.IsMobileActive)
            {
                float scaleX = 1206f / 1920f;
                finalWidth *= scaleX;
                finalHeight *= scaleX;
                finalImgWidth *= scaleX;
                finalImgHeight *= scaleX;
            }

            // 5. Apply the final scaled sizes to the main Card RectTransform
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(finalWidth, finalHeight);
            }

            // 6. Apply the final scaled sizes to the inner Artwork Frame RectTransform
            if (frameButton != null)
            {
                RectTransform frameRt = frameButton.GetComponent<RectTransform>();
                if (frameRt != null)
                {
                    frameRt.sizeDelta = new Vector2(finalImgWidth, finalImgHeight);
                }
            }

            // 7. Fetch User Progress for Completion Badge
            string userId = PlayerPrefs.GetString("userId", "guest");
            FirebaseScenarioService.Instance.FetchUserProgress(userId, entry.id,
                progress =>
                {
                    if (completedBadge != null)
                        completedBadge.SetActive(progress != null && progress.completed);
                });
        }

        // ── Coroutine: Dynamic Thumbnail Downloading ──
        private IEnumerator LoadTextureFromUrl(string url, RawImage target)
        {
            if (!url.StartsWith("http"))
            {
                var tex = Resources.Load<Texture2D>(url);
                if (tex != null && target != null) target.texture = tex;
                yield break;
            }

            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && target != null)
                {
                    target.texture = DownloadHandlerTexture.GetContent(req);
                }
            }
        }

        // ── Navigation & Animation ──
        private void OnEnterClicked()
        {
            if (string.IsNullOrEmpty(_scenarioId)) return;

            ScenarioSceneBootstrapper.PendingScenarioId = _scenarioId;
            PlayerPrefs.SetString("pendingScenarioId", _scenarioId);
            PlayerPrefs.Save();
            SceneManager.LoadScene(scenarioSceneName);
        }

        private void OnFrameTapped()
        {
            StartCoroutine(PunchScale());
            InfoOverlayUI.Instance.Show(_entry, OnEnterClicked);
        }

        private IEnumerator PunchScale()
        {
            Vector3 original = transform.localScale;
            Vector3 punched = original * 1.04f;
            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                transform.localScale = Vector3.Lerp(original, punched, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;

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