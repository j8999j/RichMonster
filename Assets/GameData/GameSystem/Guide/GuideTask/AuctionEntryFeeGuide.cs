using System.Collections.Generic;
using UnityEngine;

public static class AuctionEntryFeeGuide
{
    private const string GuideMessage = "\u4ECA\u5929\u662F\u7E73\u4EA4\u62CD\u8CE3\u6703\u5165\u5834\u8CBB\u7684\u6700\u5F8C\u4E00\u5929\uFF0C\u8ACB\u524D\u5F80\u62CD\u8CE3\u6703\u7E73\u4EA4\u5165\u5834\u8CBB\u3002";

    private static GuideTask activeTask;

    public static bool HasActiveMessage => ShouldShow(DataManager.Instance?.CurrentPlayerData);

    public static void Refresh()
    {
        if (ShouldShow(DataManager.Instance?.CurrentPlayerData))
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public static void Hide()
    {
        if (activeTask == null)
            return;

        activeTask.Dispose();
        activeTask = null;
    }

    private static void Show()
    {
        if (activeTask != null)
        {
            ReapplyMessage();
            return;
        }

        activeTask = new AuctionEntryFeeGuideTask(GuideMessage);
        activeTask.Start(() => activeTask = null);
    }

    public static void ReapplyMessage()
    {
        if (!HasActiveMessage)
            return;

        if (activeTask == null)
        {
            activeTask = new AuctionEntryFeeGuideTask(GuideMessage);
            activeTask.Start(() => activeTask = null);
            return;
        }

        RegisterAuctionNpcGuide();
        NoticeGetItemEvents.InvokeStartMapGuide(GuideIDs.Interactable.AuctionNpc);
        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(GuideMessage, true);
    }

    private static bool ShouldShow(IReadOnlyPlayerData playerData)
    {
        return playerData != null
            && !playerData.HasReachedEnding
            && !playerData.HasPaidAuctionEntryFee
            && playerData.DaysPlayed == EndingConditionDetector.AuctionEntryFeeDeadlineDay - 1;
    }

    private class AuctionEntryFeeGuideTask : GuideTask
    {
        private readonly string message;

        public AuctionEntryFeeGuideTask(string message)
        {
            this.message = message;
        }

        public override string TaskName => "Auction Entry Fee Guide";

        protected override List<GuideStep> BuildSteps()
        {
            return new List<GuideStep>
            {
                new PersistentHintStep(
                    message,
                    onExecuteCallback: () =>
                    {
                        RegisterAuctionNpcGuide();
                        NoticeGetItemEvents.InvokeStartMapGuide(GuideIDs.Interactable.AuctionNpc);
                    },
                    onDisposeCallback: NoticeGetItemEvents.InvokeClearMapGuide)
            };
        }
    }

    private static void RegisterAuctionNpcGuide()
    {
        AuctionNpc[] npcs = Object.FindObjectsOfType<AuctionNpc>(true);
        for (int i = 0; i < npcs.Length; i++)
        {
            npcs[i]?.SetMapGuide();
        }
    }
}
