using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class MainUIView : Singleton<MainUIView>
{
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 整體淡入淡出
    [SerializeField] private Image BlackImage;
    public void LoadMapPos(float fadeDuration)
    {
        Sequence enterSeq = DOTween.Sequence();

        // A. 阻擋點擊
        enterSeq.AppendCallback(() => fadeCanvasGroup.blocksRaycasts = true);
        // B. 畫面變黑
        enterSeq.Append(BlackImage.DOFade(1f, fadeDuration));
        enterSeq.OnComplete(() =>
        {
            Sequence EndSeq = DOTween.Sequence();
            EndSeq.AppendInterval(0.3f);
            // A. 阻擋點擊
            EndSeq.AppendCallback(() => fadeCanvasGroup.blocksRaycasts = false);
            // B. 畫面變黑
            EndSeq.Append(BlackImage.DOFade(0f, fadeDuration));
        });
    }
}
