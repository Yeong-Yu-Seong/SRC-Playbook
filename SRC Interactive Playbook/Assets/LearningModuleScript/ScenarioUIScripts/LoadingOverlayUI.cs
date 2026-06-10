using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RedCross.Playbook.Data;
public class LoadingOverlayUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI messageText;

    public void Show(string message = "Loading…")
    {
        messageText.text = message;
        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);
}
