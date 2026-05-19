using System.Collections.Generic;
using UnityEngine;

public class Task2_SecondTutorial : GuideTask, ITutorialTaskState
{
    public override string TaskName => "第二段教學";

    private const int GroceryGuideStepIndex = 2;
    private const int AfterSecondRewardStepIndex = 4;

    private const string WaitMonsterSceneStepId = "Task2.WaitMonsterScene";
    private const string InitStepId = "Task2.Init";
    private const string GroceryGuideStepId = "Task2.GroceryGuide";
    private const string SecondRewardStepId = "Task2.SecondReward";
    private const string ShopBookButtonStepId = "Task2.ShopBookButton";
    private const string TradeStartedStepId = "Task2.TradeStarted";
    private const string TradeCompletedStepId = "Task2.TradeCompleted";
    private const string UseKeyStepId = "Task2.UseKey";

    protected override IReadOnlyList<string> StepIds => new[]
    {
        WaitMonsterSceneStepId,
        InitStepId,
        GroceryGuideStepId,
        SecondRewardStepId,
        ShopBookButtonStepId,
        TradeStartedStepId,
        TradeCompletedStepId,
        UseKeyStepId
    };

    public bool SecondRewardClaimed;

    private GameObject _bookButtonObj;

    protected override List<GuideStep> BuildSteps()
    {
        return new List<GuideStep>
        {
            SaveAfter(new WaitForSceneStep(GameSystem.SceneTransitionManager.SCENE_MONSTER)),

            new GiveRewardStep(
                () =>
                {
                    Debug.Log("Start second tutorial.");
                    HideTutorialDisabledButtons();
                },
                restoreAction: HideTutorialDisabledButtons),

            new WithMapGuideStep(
                inner: new ShowHintAndWaitStep(
                    "請前往爺爺的雜貨店",
                    new InteractWithObjectListener(GuideIDs.Interactable.GuideGroceryStore)),
                targetId: GuideIDs.Interactable.GuideGroceryStore),

            SaveAfter(new GiveRewardStep(
                () =>
                {
                    GiveSecondRewardOnce();
                    ShowBookButton();
                },
                restoreAction: ShowBookButton), GroceryGuideStepId),

            new WithPlayerLockedStep(
                new ForceUIButtonStep(
                    new Vector2(637, -257),
                    GuideIDs.Button.GuideShopBook,
                    "查看妖怪圖鑑")),

            new ShowHintAndWaitStep(
                "請開始今天的接待",
                new MonsterTradeStartedListener()),

            SaveAfter(new WaitForListenerStep(new MonsterTradeCompletedListener())),

            new ShowHintAndWaitStep(
                "到處逛逛或使用鑰匙回家休息",
                new ButtonClickListener(GuideIDs.Button.GuideUseKey))
        };
    }

    public void LoadState(TutorialSaveData data)
    {
        SecondRewardClaimed = data.Task2SecondRewardClaimed
            || data.CurrentStepIndex >= AfterSecondRewardStepIndex
            || data.CurrentStepId == ShopBookButtonStepId
            || data.CurrentStepId == TradeStartedStepId
            || data.CurrentStepId == TradeCompletedStepId
            || data.CurrentStepId == UseKeyStepId;
    }

    public void WriteState(TutorialSaveData data)
    {
        data.Task2SecondRewardClaimed = SecondRewardClaimed;
    }

    public string ResolveResumeStepId(TutorialSaveData data)
    {
        if (string.IsNullOrEmpty(data.CurrentStepId))
        {
            return ResolveLegacyStepId(GetResumeStep(data.CurrentStepIndex));
        }

        return data.CurrentStepId == ShopBookButtonStepId
            ? GroceryGuideStepId
            : data.CurrentStepId;
    }

    public int GetResumeStep(int savedStep)
    {
        if (savedStep == AfterSecondRewardStepIndex)
            return GroceryGuideStepIndex;

        return savedStep;
    }

    private string ResolveLegacyStepId(int stepIndex)
    {
        return stepIndex >= 0 && stepIndex < StepIds.Count
            ? StepIds[stepIndex]
            : null;
    }

    private void HideTutorialDisabledButtons()
    {
        if (GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideShopBook, out var bookBtn))
        {
            _bookButtonObj = bookBtn.ButtonObject;
            if (_bookButtonObj != null)
                _bookButtonObj.SetActive(false);
        }
    }

    private void ShowBookButton()
    {
        if (_bookButtonObj == null
            && GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideShopBook, out var bookBtn))
        {
            _bookButtonObj = bookBtn.ButtonObject;
        }

        if (_bookButtonObj != null)
            _bookButtonObj.SetActive(true);
    }

    private void GiveRandomMonsterItems(int count, int monsterGoldAmount = 0, bool includeMonsterBook = false, int monsterInformationAmount = 0)
    {
        var itemIDs = DataManager.Instance.GetRandomDistinctItemIds(ItemWorld.Monster, Rarity.Common, count);
        List<NoticeItemEntry> noticeItems = new List<NoticeItemEntry>();

        if (monsterGoldAmount > 0)
            noticeItems.Add(NoticeItemEntry.MonsterGold(monsterGoldAmount));

        if (includeMonsterBook)
            noticeItems.Add(NoticeItemEntry.MonsterBook());

        if (monsterInformationAmount > 0)
            noticeItems.Add(NoticeItemEntry.MonsterInformation(monsterInformationAmount));

        foreach (var itemID in itemIDs)
            DataManager.Instance.AddItem(itemID, 0);

        foreach (var itemID in itemIDs)
            noticeItems.Add(NoticeItemEntry.ItemEntry(itemID, 1));

        NoticeGetItemEvents.InvokeShowNotice("獲得妖怪世界道具", noticeItems);
    }

    private void GiveSecondRewardOnce()
    {
        if (SecondRewardClaimed)
            return;

        SecondRewardClaimed = true;
        DataManager.Instance.ModifyMonsterGold(5000);
        GiveRandomMonsterItems(3, 5000, true, 2);
        DataManager.Instance.UnlockRandomMonsterInformation();
        DataManager.Instance.UnlockRandomMonsterInformation();
    }
}
