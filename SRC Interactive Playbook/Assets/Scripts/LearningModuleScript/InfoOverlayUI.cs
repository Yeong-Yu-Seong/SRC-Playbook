using RedCross.Playbook.Data;
using System.Collections;           // ? needed for IEnumerator (Option B)
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// using DG.Tweening;               // ? uncomment instead if DOTween is installed

public class InfoOverlayUI : MonoBehaviour
{
    public static InfoOverlayUI Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI exhibitLabel, titleText, characterText, descriptionText, pointsText;
    [SerializeField] private Button enterButton, backdropButton;

    private System.Action _onEnter;

    private void Awake()
    {
        Instance = this;
        backdropButton.onClick.AddListener(Hide);
        enterButton.onClick.AddListener(() => { Hide(); _onEnter?.Invoke(); });
    }

    public void Show(ScenarioIndexEntry entry, System.Action onEnter)
    {
        _onEnter = onEnter;

        exhibitLabel.text = entry.exhibitNumber;
        pointsText.text = $"+{entry.pointsOnCompletion} pts";
        if (titleText != null) titleText.text = entry.title;
        if (characterText != null) characterText.text = entry.characterText;
        if (descriptionText != null) descriptionText.text = entry.outlineDescription;

        // Reset position and visibility before animating in
        panel.anchoredPosition = new Vector2(0, -300);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Option B — coroutines (no DOTween needed)
        StopAllCoroutines();
        StartCoroutine(AnimateIn());

        // Option A — uncomment below and delete the two lines above if DOTween is installed
        // DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, 0.25f);
        // panel.DOAnchorPosY(0, 0.3f).SetEase(Ease.OutCubic);
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateOut());

        // Option A — uncomment below and delete the two lines above if DOTween is installed
        // DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0f, 0.2f)
        //     .OnComplete(() => { canvasGroup.interactable = false; canvasGroup.blocksRaycasts = false; });
    }

    private IEnumerator AnimateIn()
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector2 startPos = new Vector2(0, -300);
        Vector2 endPos = Vector2.zero;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);          // smoothstep — matches Ease.OutCubic feel

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            panel.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        panel.anchoredPosition = endPos;
    }

    private IEnumerator AnimateOut()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}