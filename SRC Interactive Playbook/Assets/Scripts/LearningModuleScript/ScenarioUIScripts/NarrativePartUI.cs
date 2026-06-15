using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;

public class NarrativePartUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI narrativeText;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject tapToContinueHint;
    [SerializeField] private Button fullScreenTapZone;

    private Action _onContinue;
    private Coroutine _autoAdvanceCoroutine;

    private void Awake()
    {
        continueButton.onClick.AddListener(HandleContinue);
        if (fullScreenTapZone != null)
            fullScreenTapZone.onClick.AddListener(HandleContinue);
    }

    public void Show(ScenePart part, Action onContinue)
    {
        _onContinue = onContinue;
        narrativeText.text = part.narrativeText;

        bool autoAdvance = part.displayDurationSecs > 0;
        continueButton.gameObject.SetActive(!autoAdvance);
        if (tapToContinueHint != null) tapToContinueHint.SetActive(!autoAdvance);

        if (autoAdvance)
        {
            if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = StartCoroutine(AutoAdvance(part.displayDurationSecs));
        }

        panel.SetActive(true);
    }

    public void Hide()
    {
        if (_autoAdvanceCoroutine != null)
        {
            StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = null;
        }
        panel.SetActive(false);
    }

    private void HandleContinue()
    {
        if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
        _onContinue?.Invoke();
    }

    private IEnumerator AutoAdvance(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _onContinue?.Invoke();
    }
}
