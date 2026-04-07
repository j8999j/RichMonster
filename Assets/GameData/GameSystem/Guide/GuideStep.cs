// ============================================================
// GuideStep.cs - 所有具體步驟
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Talksystem;
using GameSystem;
/// <summary>所有引導步驟的抽象基底</summary>
public abstract class GuideStep
{
    public int StepIndex { get; set; }
    public abstract void Execute(System.Action onComplete);
    public virtual void Dispose() { }
}
// ─────────────────────────────────────────────────────
/// <summary>強制進入對話模式，對話結束後完成</summary>
public class ForceDialogueStep : GuideStep
{
    private readonly string dialogueId;
    private DialogueEndListener listener;

    public ForceDialogueStep(string dialogueId) => this.dialogueId = dialogueId;

    public override void Execute(System.Action onComplete)
    {
        listener = new DialogueEndListener();
        listener.StartListen(() => { listener.StopListen(); onComplete?.Invoke(); });
        Addressables.LoadAssetAsync<TextAsset>(dialogueId).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
            {
                GameManager.Instance.talkSystem.StartDialogue(handle.Result);
            }
            else
            {
                Debug.LogError($"[ForceDialogueStep] Addressables 找不到對應名稱的 TextAsset: {dialogueId}");
                listener.StopListen();
                onComplete?.Invoke(); // 如果找不到，直接結束步驟避免卡死
            }
        };
    }

    public override void Dispose() => listener?.StopListen();
}

// ─────────────────────────────────────────────────────
/// <summary>顯示提示並等待監聽器觸發，可同時啟動背景監聽</summary>
public class ShowHintAndWaitStep : GuideStep
{
    private readonly string hintMessage;
    private readonly GuideListener listener;
    private readonly System.Action onExecuteCallback; // 可選：用來啟動背景監聽

    public ShowHintAndWaitStep(
        string hintMessage,
        GuideListener listener,
        System.Action onExecuteCallback = null)
    {
        this.hintMessage        = hintMessage;
        this.listener           = listener;
        this.onExecuteCallback  = onExecuteCallback;
    }

    public override void Execute(System.Action onComplete)
    {
        onExecuteCallback?.Invoke(); // 啟動背景監聽（若有）
        GameFlowUI.SetGameFlowTextEvent?.Invoke(hintMessage, true);
        listener.StartListen(() =>
        {
            GameFlowUI.SetGameFlowTextEvent?.Invoke("", false);
            listener.StopListen();
            onComplete?.Invoke();
        });
    }

    public override void Dispose()
    {
        GameFlowUI.SetGameFlowTextEvent?.Invoke("", false);
        listener?.StopListen();
    }
}

// ─────────────────────────────────────────────────────
/// <summary>發放獎勵後立即完成</summary>
public class GiveRewardStep : GuideStep
{
    private readonly System.Action rewardAction;
    public GiveRewardStep(System.Action rewardAction)
        => this.rewardAction = rewardAction;
    public override void Execute(System.Action onComplete)
    {
        rewardAction?.Invoke();
        onComplete?.Invoke();
    }
}

// ─────────────────────────────────────────────────────
/// <summary>可略過步驟：條件達成或背景已監聽到則直接完成</summary>
public class SkippableListenStep : GuideStep
{
    private readonly string hintMessage;
    private readonly GuideListener listener;
    private readonly System.Func<bool> skipCondition;
    private readonly BackgroundListener backgroundListener;

    public SkippableListenStep(
        string hintMessage,
        GuideListener listener,
        System.Func<bool> skipCondition,
        BackgroundListener backgroundListener = null)
    {
        this.hintMessage        = hintMessage;
        this.listener           = listener;
        this.skipCondition      = skipCondition;
        this.backgroundListener = backgroundListener;
    }

    private bool CanSkip()
        => (skipCondition?.Invoke() ?? false)
        || (backgroundListener?.IsTriggered ?? false);

    public override void Execute(System.Action onComplete)
    {
        // 優先消費背景監聽結果
        GameFlowUI.SetGameFlowTextEvent?.Invoke(hintMessage, true);
        if (backgroundListener != null)
        {
            if (CanSkip()) { onComplete?.Invoke(); return; }
            backgroundListener.Consume(onComplete);
            return;
        }

        if (CanSkip()) { onComplete?.Invoke(); return; }
        listener.StartListen(() =>
        {
            GameFlowUI.SetGameFlowTextEvent?.Invoke("", false);
            listener.StopListen();
            onComplete?.Invoke();
        });
    }

    public override void Dispose()
    {
        GameFlowUI.SetGameFlowTextEvent?.Invoke("", false);
        listener?.StopListen();
        backgroundListener?.Dispose();
    }
}

// ─────────────────────────────────────────────────────
/// <summary>強制開啟 UI 面板並等待指定按鈕點擊</summary>
public class ForceUIButtonStep : GuideStep
{
    private readonly string panelId;
    private readonly string buttonId;
    private readonly string hintMessage;
    private ButtonClickListener listener;

    public ForceUIButtonStep(string panelId, string buttonId, string hintMessage)
    {
        this.panelId      = panelId;
        this.buttonId     = buttonId;
        this.hintMessage  = hintMessage;
    }

    public override void Execute(System.Action onComplete)
    {
        GameFlowUI.SetGameFlowTextEvent?.Invoke(hintMessage, true);

        listener = new ButtonClickListener(buttonId);
        listener.StartListen(() =>
        {
            GameFlowUI.SetGameFlowTextEvent?.Invoke("", false);
            listener.StopListen();
            onComplete?.Invoke();
        });
    }

    public override void Dispose()
    {
        listener?.StopListen();
        GameFlowUI.SetGameFlowTextEvent?.Invoke("", false);
    }
}
// ============================================================
// GuideStepDecorator.cs - 步驟裝飾器基底
// ============================================================

/// <summary>
/// 包裝任意步驟，在執行前後插入附加行為
/// 不修改任何現有步驟類別，符合 OCP
/// </summary>
public abstract class GuideStepDecorator : GuideStep
{
    protected readonly GuideStep inner;  // 被包裝的步驟

    protected GuideStepDecorator(GuideStep inner)
        => this.inner = inner;

    public override void Execute(System.Action onComplete)
    {
        OnBeforeExecute();
        inner.Execute(() =>
        {
            OnAfterComplete();
            onComplete?.Invoke();
        });
    }

    public override void Dispose()
    {
        OnDispose();
        inner.Dispose();
    }

    protected virtual void OnBeforeExecute() { }  // 步驟開始前
    protected virtual void OnAfterComplete() { }  // 步驟完成後
    protected virtual void OnDispose()       { }  // 清理時
}
// ============================================================
// WithMapGuideStep.cs - 地圖點位裝飾器
// ============================================================

/// <summary>
/// 包裝任意步驟，執行時顯示地圖點位，完成後自動清除
/// </summary>
public class WithMapGuideStep : GuideStepDecorator
{
    private readonly string targetId;

    public WithMapGuideStep(GuideStep inner, string targetId)
        : base(inner)
        => this.targetId = targetId;

    protected override void OnBeforeExecute()
        => NoticeGetItemEvents.InvokeStartMapGuide(targetId);  // 步驟開始 → 顯示點位

    protected override void OnAfterComplete()
        => NoticeGetItemEvents.InvokeClearMapGuide();          // 步驟完成 → 清除點位

    protected override void OnDispose()
        => NoticeGetItemEvents.InvokeClearMapGuide();          // 異常中斷也確保清除
}