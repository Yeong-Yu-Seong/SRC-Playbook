using RedCross.Playbook.Data;
using RedCross.Playbook.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionPartUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI contextHintText;
    [SerializeField] private GameObject contextHintBubble;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Transform choicesContainer;

    [Header("Prefab")]
    [SerializeField] private GameObject mobileCBPrefab;
    [SerializeField] private GameObject desktopCBPrefab;

    private Action<Choice> _onChoiceSelected;
    private readonly List<GameObject> _spawnedButtons = new();

    public void Show(ScenePart part, Action<Choice> onChoiceSelected)
    {
        _onChoiceSelected = onChoiceSelected;

        bool hasHint = !string.IsNullOrEmpty(part.contextHintText);
        contextHintBubble.SetActive(hasHint);
        if (hasHint) contextHintText.text = part.contextHintText;

        questionText.text = part.questionText;

        foreach (var b in _spawnedButtons) Destroy(b);
        _spawnedButtons.Clear();

        bool isMobile = ResponsiveLayoutManager.Instance.IsMobileActive;
        GameObject prefabToUse = isMobile ? mobileCBPrefab : desktopCBPrefab;
        foreach (var choice in part.choices)
        {
            // 3. Instantiate the correct prefab
            var go = Instantiate(prefabToUse, choicesContainer);
            var btn = go.GetComponent<ChoiceButtonUI>();
            btn.Initialise(choice, OnChoiceButtonClicked);
            _spawnedButtons.Add(go);
        }

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    private void OnChoiceButtonClicked(Choice choice)
    {
        foreach (var go in _spawnedButtons)
            go.GetComponent<ChoiceButtonUI>()?.SetInteractable(false);

        _onChoiceSelected?.Invoke(choice);
    }
}
