using UnityEngine;
using DG.Tweening;
using Player;
using UnityEngine.UI;
using GameSystem;

public class TelePoint : MonoBehaviour, IInteractable
{
    public Vector3 TelePosition;
    public GameObject interactPrompt;
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 整體淡入淡出
    [SerializeField] private Image BlackImage;
    [SerializeField] private float fadeDuration = 0.5f;
    public void Interact()
    {
        //傳送前往妖界
        NextMap();
    }
    public void ShowPrompt()
    {
        interactPrompt.SetActive(true);
    }
    public void HidePrompt()
    {
        interactPrompt.SetActive(false);
    }
    void NextMap()
    {
        LoadMapPos(fadeDuration);
    }
    public void LoadMapPos(float fadeDuration)
    {
        Sequence enterSeq = DOTween.Sequence();

        // A. 阻擋點擊
        enterSeq.AppendCallback(() => fadeCanvasGroup.blocksRaycasts = true);
        // B. 畫面變黑
        enterSeq.Append(BlackImage.DOFade(1f, fadeDuration));
        enterSeq.OnComplete(() =>
        {
            GameManager.Instance.SwitchPlayerPos(TelePosition);
            Sequence EndSeq = DOTween.Sequence();
            EndSeq.AppendInterval(0.3f);
            // A. 阻擋點擊
            EndSeq.AppendCallback(() => fadeCanvasGroup.blocksRaycasts = false);
            // B. 畫面變黑
            EndSeq.Append(BlackImage.DOFade(0f, fadeDuration));
        });
    }
}
