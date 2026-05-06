using UnityEngine;
using TMPro;
using System;
using System.Collections;
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
        GuaranteeDepositGuide.ReapplyMessage();
        AuctionEntryFeeGuide.ReapplyMessage();
        StartCoroutine(ReapplyActiveGuideAfterSceneSetup());
    }

    private void OnDisable()
    {
        SetGuideFlowTextEvent -= SetGuideFlowText;
        SetGuideImageEvent -= SetGuideImage;
    }

    public void SetGuideFlowText(string text, bool isActive)
    {
        if (!isActive && GuaranteeDepositGuide.ShouldBlockClose)
        {
            GameFlowPanel.SetActive(true);
            GameFlowText.text = GuaranteeDepositGuide.CurrentMessage;
            return;
        }

        GameFlowPanel.SetActive(isActive);
        GameFlowText.text = text;
    }

    private void SetGuideImage(Vector2 position, bool isActive)
    {
        GuideImage.gameObject.SetActive(isActive);
        if (isActive)
            GuideImage.anchoredPosition = position;
    }

    private IEnumerator ReapplyActiveGuideAfterSceneSetup()
    {
        yield return null;
        GuaranteeDepositGuide.ReapplyMessage();
        AuctionEntryFeeGuide.ReapplyMessage();
    }
}
