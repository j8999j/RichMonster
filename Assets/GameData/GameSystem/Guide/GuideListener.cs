// ============================================================
// GuideListener.cs - 抽象監聽器 + 所有具體監聽器
// ============================================================
using UnityEngine;
using GameSystem;
/// <summary>所有監聽器的抽象基底</summary>
public abstract class GuideListener
{
    protected System.Action onTriggered;

    public void StartListen(System.Action callback)
    {
        onTriggered = callback;
        OnStartListen();
    }

    public void StopListen()
    {
        OnStopListen();
        onTriggered = null;
    }

    protected abstract void OnStartListen();
    protected abstract void OnStopListen();
}

// ─────────────────────────────────────────────────────
/// <summary>監聽對話結束</summary>
public class DialogueEndListener : GuideListener
{
    protected override void OnStartListen()
        => GameManager.Instance.talkSystem.OnDialogueEnd += onTriggered;

    protected override void OnStopListen()
        => GameManager.Instance.talkSystem.OnDialogueEnd -= onTriggered;
}

// ─────────────────────────────────────────────────────
/// <summary>監聽購買任一物品</summary>
public class PurchaseItemListener : GuideListener
{
    protected override void OnStartListen()
        => DataManager.Instance.OnItemPurchased += OnPurchased;

    protected override void OnStopListen()
        => DataManager.Instance.OnItemPurchased -= OnPurchased;

    private void OnPurchased() => onTriggered?.Invoke();
}

// ─────────────────────────────────────────────────────
/// <summary>監聽指定物件互動 - 透過 Registry 查找</summary>
public class InteractWithObjectListener : GuideListener
{
    private readonly string targetId;
    private IGuideInteractable target;

    public InteractWithObjectListener(string targetId) => this.targetId = targetId;

    protected override void OnStartListen()
    {
        if (GuideLookupRegistry.Instance.TryGetInteractable(targetId, out target))
            target.OnInteracted += OnInteracted;
        else
        {
            Debug.LogWarning($"[Listener] 找不到互動物件: {targetId}，等待登記");
            WaitForRegister();
        }
    }

    protected override void OnStopListen()
    {
        if (target != null) target.OnInteracted -= OnInteracted;
    }

    private void OnInteracted(string id) { if (id == targetId) onTriggered?.Invoke(); }

    private async void WaitForRegister()
    {
        while (!GuideLookupRegistry.Instance.TryGetInteractable(targetId, out target))
            await System.Threading.Tasks.Task.Yield();
        target.OnInteracted += OnInteracted;
    }
}

// ─────────────────────────────────────────────────────
/// <summary>監聽指定按鈕點擊 - 透過 Registry 查找</summary>
public class ButtonClickListener : GuideListener
{
    private readonly string buttonId;
    private IGuideButton button;

    public ButtonClickListener(string buttonId) => this.buttonId = buttonId;

    protected override void OnStartListen()
    {
        if (GuideLookupRegistry.Instance.TryGetButton(buttonId, out button))
            button.OnClicked += OnClicked;
        else
        {
            Debug.LogWarning($"[Listener] 找不到按鈕: {buttonId}，等待登記");
            WaitForRegister();
        }
    }

    protected override void OnStopListen()
    {
        if (button != null) button.OnClicked -= OnClicked;
    }

    private void OnClicked(string id) { if (id == buttonId) onTriggered?.Invoke(); }

    private async void WaitForRegister()
    {
        while (!GuideLookupRegistry.Instance.TryGetButton(buttonId, out button))
            await System.Threading.Tasks.Task.Yield();
        button.OnClicked += OnClicked;
    }
}

// ─────────────────────────────────────────────────────
/// <summary>監聽前往妖界</summary>
public class EnterSpiritWorldListener : GuideListener
{
    protected override void OnStartListen()
        => Debug.Log("EnterMonsterWorldListener");

    protected override void OnStopListen()
        => Debug.Log("EnterMonsterWorldListener");
}
// ============================================================
// BackgroundListener.cs - 提早監聽容器
// ============================================================
public class BackgroundListener
{
    private GuideListener listener;
    private bool isTriggered = false;
    private System.Action pendingCallback;

    public bool IsTriggered => isTriggered;

    public void StartEarly(GuideListener guideListener)
    {
        listener = guideListener;
        listener.StartListen(() =>
        {
            isTriggered = true;
            listener.StopListen();
            pendingCallback?.Invoke();
            pendingCallback = null;
        });
    }

    /// <summary>步驟到達時消費結果：已觸發立即完成，否則等待</summary>
    public void Consume(System.Action onComplete)
    {
        if (isTriggered) onComplete?.Invoke();
        else             pendingCallback = onComplete;
    }
    public void Dispose()
    {
        listener?.StopListen();
        pendingCallback = null;
    }
}