public static class GuaranteeDepositGuide
{
    private const string GuideMessage = "\u4ECA\u5929\u662F\u7E73\u4EA4\u5951\u7D04\u4FDD\u8B49\u91D1\u7684\u6700\u5F8C\u4E00\u5929\uFF0C\u8ACB\u958B\u555F\u5951\u7D04\u7E73\u4EA4\u5951\u7D04\u4FDD\u8B49\u91D1\u3002";

    private static bool isActive;

    public static bool ShouldBlockClose => isActive && ShouldShow(DataManager.Instance?.CurrentPlayerData);

    public static string CurrentMessage => GuideMessage;

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
        if (ShouldBlockClose)
        {
            Show();
            return;
        }

        if (!isActive)
            return;

        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(string.Empty, false);
        isActive = false;
    }

    private static void Show()
    {
        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(GuideMessage, true);
        isActive = true;
    }

    private static bool ShouldShow(IReadOnlyPlayerData playerData)
    {
        return playerData != null
            && !playerData.HasReachedEnding
            && !playerData.HasPaidGuaranteeDeposit
            && playerData.PlayingStatus == DayPhase.HumanDay
            && playerData.DaysPlayed == EndingConditionDetector.GuaranteeDepositDeadlineDay - 1;
    }
}
