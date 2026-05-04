public static class AuctionEntryFeeGuide
{
    private const string GuideMessage = "\u4ECA\u5929\u662F\u7E73\u4EA4\u62CD\u8CE3\u6703\u5165\u5834\u8CBB\u7684\u6700\u5F8C\u4E00\u5929\uFF0C\u8ACB\u524D\u5F80\u62CD\u8CE3\u6703NPC\u7E73\u4EA4\u5165\u5834\u8CBB\u3002";

    private static bool isActive;

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
        if (!isActive)
            return;

        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(string.Empty, false);
        NoticeGetItemEvents.InvokeClearMapGuide();
        isActive = false;
    }

    private static void Show()
    {
        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(GuideMessage, true);
        NoticeGetItemEvents.InvokeStartMapGuide(GuideIDs.Interactable.AuctionNpc);
        isActive = true;
    }

    private static bool ShouldShow(IReadOnlyPlayerData playerData)
    {
        return playerData != null
            && !playerData.HasReachedEnding
            && !playerData.HasPaidAuctionEntryFee
            && playerData.DaysPlayed == EndingConditionDetector.AuctionEntryFeeDeadlineDay - 1;
    }
}
