using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;

public class ScenarioIntroUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI exhibitNumberText;
    [SerializeField] private RawImage thumbnailImage;

    [Tooltip("Optional: Assign a Frame Image here if your Intro UI also uses frames.")]
    [SerializeField] private Image frameImage;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI outlineDescriptionText;
    [SerializeField] private Button enterButton;

    private Action _onEnterClicked;

    private void Awake()
    {
        if (enterButton != null)
            enterButton.onClick.AddListener(() => _onEnterClicked?.Invoke());
    }

    public void Show(PlaybookScenario scenario, Action onEnterClicked)
    {
        _onEnterClicked = onEnterClicked;

        if (exhibitNumberText != null) exhibitNumberText.text = scenario.exhibitNumber;
        if (titleText != null) titleText.text = scenario.title;
        if (outlineDescriptionText != null) outlineDescriptionText.text = scenario.outlineDescription;

        // 1. Load Thumbnail (Dynamic URL from Firebase Storage)
        if (thumbnailImage != null && !string.IsNullOrEmpty(scenario.thumbnailUrl))
        {
            StartCoroutine(LoadTextureFromUrl(scenario.thumbnailUrl, thumbnailImage));
        }

        // 2. Load Local Frame Sprite based on Admin Dashboard selection
        if (frameImage != null && !string.IsNullOrEmpty(scenario.frameUrl))
        {
            Sprite loadedFrame = Resources.Load<Sprite>($"Frames/{scenario.frameUrl}");
            if (loadedFrame != null)
            {
                frameImage.sprite = loadedFrame;
            }
            else
            {
                Debug.LogWarning($"[ScenarioIntroUI] Could not find frame sprite: Resources/Frames/{scenario.frameUrl}");
            }
        }

        if (panel != null) panel.SetActive(true);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ── Coroutine: Dynamic Thumbnail Downloading ──
    private IEnumerator LoadTextureFromUrl(string url, RawImage target)
    {
        // Fallback for local files if the url doesn't start with http
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
            else
            {
                Debug.LogWarning($"[ScenarioIntroUI] Failed to load thumbnail from URL: {req.error}");
            }
        }
    }
}