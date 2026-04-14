using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
public class GuideFlowUI : MonoBehaviour
{
    public GameObject GameFlowPanel;
    public TextMeshProUGUI GameFlowText;
    public RectTransform GuideImage;
    public static Action<string, bool> SetGuideFlowTextEvent;
    public static Action<Vector2, bool> SetGuideImageEvent;
    private void OnEnable()
    {
        SetGuideFlowTextEvent += SetGuideFlowText;
        SetGuideImageEvent += SetGuideImage;
        GuideImage.gameObject.GetComponent<Image>().alphaHitTestMinimumThreshold = 0.3f;
    }

    private void OnDisable()
    {
        SetGuideFlowTextEvent -= SetGuideFlowText;
        SetGuideImageEvent -= SetGuideImage;
    }

    public void SetGuideFlowText(string text, bool isActive)
    {
        GameFlowPanel.SetActive(isActive);
        GameFlowText.text = text;
    }

    private void SetGuideImage(Vector2 position, bool isActive)
    {
        GuideImage.gameObject.SetActive(isActive);
        if (isActive)
            GuideImage.anchoredPosition = position;
    }
}
