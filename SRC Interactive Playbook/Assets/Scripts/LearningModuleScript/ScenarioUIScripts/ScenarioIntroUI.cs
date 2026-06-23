using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;

public class ScenarioIntroUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI exhibitNumberText;
    [SerializeField] private RawImage thumbnailImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI outlineDescriptionText;
    [SerializeField] private Button enterButton;

    private Action _onEnterClicked;

    private void Awake()
    {
        enterButton.onClick.AddListener(() => _onEnterClicked?.Invoke());
    }

    public void Show(PlaybookScenario scenario, Action onEnterClicked)
    {
        _onEnterClicked = onEnterClicked;
        exhibitNumberText.text = scenario.exhibitNumber;
        titleText.text = scenario.title;
        outlineDescriptionText.text = scenario.outlineDescription;

        var tex = Resources.Load<Texture2D>(scenario.thumbnailUrl);
        if (tex != null) thumbnailImage.texture = tex;

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);
}
