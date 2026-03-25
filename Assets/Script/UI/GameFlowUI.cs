using UnityEngine;
using TMPro;

public class GameFlowUI : MonoBehaviour
{
    public GameObject GameFlowPanel;
    public TextMeshProUGUI GameFlowText;
    private void OnEnable()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.GameFlowNoticeUpdate += SetGameFlowText;
        }
    }

    private void OnDisable()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.GameFlowNoticeUpdate -= SetGameFlowText;
        }
    }
    
    public void SetGameFlowText(string text, bool isActive)
    {
        GameFlowPanel.SetActive(isActive);
        GameFlowText.text = text;
    }
}
