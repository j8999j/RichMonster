using UnityEngine;
using TMPro;

public class SystemInfoItem : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float moveSpeed = 50f;
    public float stayTime = 1.5f;
    public float fadeTime = 0.5f;

    private float timer;
    private CanvasGroup canvasGroup;
    private System.Action<SystemInfoItem> onRecycle;

    public void Init(string msg, System.Action<SystemInfoItem> recycleCallback)
    {
        text.text = msg;
        onRecycle = recycleCallback;

        timer = 0;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 1;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer > stayTime)
        {
            // 往上飄
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            // 淡出
            float fadeProgress = (timer - stayTime) / fadeTime;
            canvasGroup.alpha = 1 - fadeProgress;
        }

        if (timer >= stayTime + fadeTime)
        {
            onRecycle?.Invoke(this);
        }
    }
}