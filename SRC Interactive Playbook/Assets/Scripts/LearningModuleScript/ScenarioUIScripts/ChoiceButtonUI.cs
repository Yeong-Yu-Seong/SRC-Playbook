using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;
public class ChoiceButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI choiceText;

    private Choice _choice;
    private Action<Choice> _onClicked;

    private void Awake()
    {
        button.onClick.AddListener(() => _onClicked?.Invoke(_choice));
    }

    public void Initialise(Choice choice, Action<Choice> onClicked)
    {
        _choice = choice;
        _onClicked = onClicked;
        labelText.text = choice.label;
        choiceText.text = choice.text;
    }

    public void SetInteractable(bool interactable) => button.interactable = interactable;
}