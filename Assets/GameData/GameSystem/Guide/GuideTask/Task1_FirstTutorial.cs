using System.Collections.Generic;
using UnityEngine;

public class Task1_FirstTutorial : GuideTask, ITutorialTaskState
{
    public override string TaskName => "任務一：起始教學";

    private const string InitStepId = "Task1.Init";
    private const string IntroDialogueStepId = "Task1.IntroDialogue";
    private const string OrderShopGuideStepId = "Task1.OrderShopGuide";
    private const string FirstRewardStepId = "Task1.FirstReward";
    private const string WaitPurchaseStepId = "Task1.WaitPurchase";
    private const string ReturnOrderShopStepId = "Task1.ReturnOrderShop";
    private const string EnableRestButtonStepId = "Task1.EnableRestButton";
    private const string RestButtonStepId = "Task1.RestButton";
    private const string ConfirmAfternoonStepId = "Task1.ConfirmAfternoon";
    private const string SouvenirBoxStepId = "Task1.SouvenirBox";
    private const string UseKeyStepId = "Task1.UseKey";

    protected override IReadOnlyList<string> StepIds => new[]
    {
        InitStepId,
        IntroDialogueStepId,
        OrderShopGuideStepId,
        FirstRewardStepId,
        WaitPurchaseStepId,
        ReturnOrderShopStepId,
        EnableRestButtonStepId,
        RestButtonStepId,
        ConfirmAfternoonStepId,
        SouvenirBoxStepId,
        UseKeyStepId
    };

    public bool IsPurchased;

    private readonly BackgroundListener earlyPurchaseListener = new BackgroundListener();
    private GameObject _restButtonObj;
    private GameObject _bookButtonObj;

    protected override List<GuideStep> BuildSteps()
    {
        earlyPurchaseListener.OnTriggered = MarkPurchased;

        return new List<GuideStep>
        {
            Step(InitStepId, new GiveRewardStep(
                HideTutorialDisabledButtons,
                restoreAction: HideTutorialDisabledButtons)),

            Step(IntroDialogueStepId, SaveAfter(new ForceDialogueStep(
                GuideIDs.Dialogue.Task1_FirstTutorial_Dialogue))),

            Step(OrderShopGuideStepId, SaveAfter(new WithMapGuideStep(
                inner: new ShowHintAndWaitStep(
                    "前往爺爺的雜貨店查看",
                    new InteractWithObjectListener(GuideIDs.Interactable.GuideOrderShop),
                    onExecuteCallback: StartEarlyPurchaseListener,
                    onRestoreCallback: StartEarlyPurchaseListener),
                targetId: GuideIDs.Interactable.GuideOrderShop))),

            Step(FirstRewardStepId, SaveAfter(new GiveRewardStep(GiveFirstReward))),

            Step(WaitPurchaseStepId, SaveAfter(new SkippableListenStep(
                "請在其他商店中購買任一物品",
                listener: null,
                skipCondition: () => IsPurchased,
                backgroundListener: earlyPurchaseListener), WaitPurchaseStepId)),

            Step(ReturnOrderShopStepId, new WithMapGuideStep(
                inner: new ShowHintAndWaitStep(
                    "回到爺爺的雜貨店，休息一下",
                    new InteractWithObjectListener(GuideIDs.Interactable.GuideOrderShop)),
                targetId: GuideIDs.Interactable.GuideOrderShop)),

            Step(EnableRestButtonStepId, SaveAfter(new GiveRewardStep(
                ShowRestButton,
                restoreAction: ShowRestButton), ReturnOrderShopStepId)),

            new WithPlayerLockedStep(
                new ForceUIButtonStep(
                    new Vector2(-322, -115),
                    GuideIDs.Button.GuideRest,
                    "點擊躺椅休息一下")),

            SaveAfter(new WithPlayerLockedStep(
                new ForceUIButtonStep(
                    new Vector2(-171.8f, -122.8f),
                    GuideIDs.Button.GuideConfirmAfternoon,
                    "點擊確定，切換到午後"))),

            new WithPlayerLockedStep(
                new ForceUIButtonStep(
                    new Vector2(-880, -265),
                    GuideIDs.Button.GuideSouvenirBox,
                    "打開紀念品盒")),

            new WithPlayerLockedStep(
                new ForceUIButtonStep(
                    new Vector2(-210, 86),
                    GuideIDs.Button.GuideUseKey,
                    "使用黃昏鑰匙"))
        };
    }

    public void LoadState(TutorialSaveData data)
    {
        IsPurchased = data.IsPurchased;
    }

    public void WriteState(TutorialSaveData data)
    {
        data.IsPurchased = IsPurchased;
    }

    public string ResolveResumeStepId(TutorialSaveData data)
    {
        return string.IsNullOrEmpty(data.CurrentStepId)
            ? ResolveLegacyStepId(data.CurrentStepIndex)
            : data.CurrentStepId;
    }

    private string ResolveLegacyStepId(int stepIndex)
    {
        return stepIndex >= 0 && stepIndex < StepIds.Count
            ? StepIds[stepIndex]
            : null;
    }

    private void HideTutorialDisabledButtons()
    {
        if (GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideRest, out var restBtn))
        {
            _restButtonObj = restBtn.ButtonObject;
            if (_restButtonObj != null)
                _restButtonObj.SetActive(false);
        }

        if (GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideBook, out var bookBtn))
        {
            _bookButtonObj = bookBtn.ButtonObject;
            if (_bookButtonObj != null)
                _bookButtonObj.SetActive(false);
        }
    }

    private void StartEarlyPurchaseListener()
    {
        if (!IsPurchased)
            earlyPurchaseListener.StartEarly(new PurchaseItemListener());
    }

    private void ShowRestButton()
    {
        if (_restButtonObj == null
            && GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideRest, out var restBtn))
        {
            _restButtonObj = restBtn.ButtonObject;
        }

        if (_restButtonObj != null)
            _restButtonObj.SetActive(true);
    }

    private void MarkPurchased()
    {
        if (IsPurchased)
            return;

        IsPurchased = true;
        RequestProgressSave();
    }

    private void GiveFirstReward()
    {
        var itemIDs = DataManager.Instance.GetRandomDistinctItemIds(ItemWorld.Human, Rarity.Common, 3);
        foreach (var itemID in itemIDs)
            DataManager.Instance.AddItem(itemID, 0);

        List<NoticeItemEntry> noticeItems = new List<NoticeItemEntry>();
        foreach (var itemID in itemIDs)
            noticeItems.Add(NoticeItemEntry.ItemEntry(itemID, 1));

        NoticeGetItemEvents.InvokeShowNotice("獲得爺爺的庫存", noticeItems);
    }
}
