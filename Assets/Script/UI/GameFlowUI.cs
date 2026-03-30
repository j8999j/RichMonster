using UnityEngine;
using TMPro;
using System;
public class GameFlowUI : MonoBehaviour
{
    public GameObject GameFlowPanel;
    public TextMeshProUGUI GameFlowText;
    public static Action<string, bool> SetGameFlowTextEvent;
    private void OnEnable()
    {
        SetGameFlowTextEvent += SetGameFlowText;
    }

    private void OnDisable()
    {
        SetGameFlowTextEvent -= SetGameFlowText;
    }
    
    public void SetGameFlowText(string text, bool isActive)
    {
        GameFlowPanel.SetActive(isActive);
        GameFlowText.text = text;
    }
}
