using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;
//控制人界場景物件
public class HumanSceneController : MonoBehaviour
{
    public GameObject DayLight;
    public GameObject NoonLight;
    public GameObject DayBackGround;
    public GameObject NoonBackGround;
    public GameObject DayCloud;
    public GameObject NoonCloud;
    public GameObject DayLightVertical;
    public GameObject NoonLightVertical;
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 整體淡入淡出
    [SerializeField] private Image BlackImage;
    [SerializeField] private float fadeDuration = 0.3f;
    private void OnEnable()
    {
        GameEventCenter.Subscribe<DayPhaseChangedEvent>(SwitchDayPhase);
    }
    private void OnDisable()
    {
        GameEventCenter.Unsubscribe<DayPhaseChangedEvent>(SwitchDayPhase);
    }
    private void SwitchDayPhase(DayPhaseChangedEvent eventData)
    {
        switch (eventData.Phase)
        {
            case DayPhase.AfterNoon:
                LoadingScene(SwitchToNoon);
                break;
        }
    }
    public void SwitchToNoon()
    {
        DayLight.SetActive(false);
        NoonLight.SetActive(true);
        DayBackGround.SetActive(false);
        NoonBackGround.SetActive(true);
        DayCloud.SetActive(false);
        NoonCloud.SetActive(true);
        DayLightVertical.SetActive(false);
        NoonLightVertical.SetActive(true);
    }
    public void SwitchToDay()
    {
        DayLight.SetActive(true);
        NoonLight.SetActive(false);
        DayBackGround.SetActive(true);
        NoonBackGround.SetActive(false);
        DayCloud.SetActive(true);
        NoonCloud.SetActive(false);
        DayLightVertical.SetActive(true);
        NoonLightVertical.SetActive(false);
    }
    public void LoadingScene(Action SceneChange)
    {
        Sequence enterSeq = DOTween.Sequence();

        // A. 阻擋點擊
        enterSeq.AppendCallback(() => fadeCanvasGroup.blocksRaycasts = true);
        // B. 畫面變黑
        enterSeq.Append(BlackImage.DOFade(1f, fadeDuration));
        enterSeq.OnComplete(() =>
        {
            SceneChange?.Invoke();
            Sequence EndSeq = DOTween.Sequence();
            EndSeq.AppendInterval(0.3f);
            // A. 阻擋點擊
            EndSeq.AppendCallback(() => fadeCanvasGroup.blocksRaycasts = false);
            // B. 畫面變黑
            EndSeq.Append(BlackImage.DOFade(0f, fadeDuration));
        });
    }
}
