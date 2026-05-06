using System.Collections.Generic;

public static class GuaranteeDepositGuide
{
    private const string GuideMessage = "\u4ECA\u5929\u662F\u7E73\u4EA4\u5951\u7D04\u4FDD\u8B49\u91D1\u7684\u6700\u5F8C\u4E00\u5929\uFF0C\u8ACB\u958B\u555F\u5951\u7D04\u7E73\u4EA4\u5951\u7D04\u4FDD\u8B49\u91D1\u3002";

    private static GuideTask activeTask;

    public static bool ShouldBlockClose => ShouldKeepOpen(DataManager.Instance?.CurrentPlayerData);

    public static string CurrentMessage => GuideMessage;

    public static bool HasActiveMessage => ShouldKeepOpen(DataManager.Instance?.CurrentPlayerData);

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
            Restart();
            return;
        }

        if (activeTask == null)
            return;

        activeTask.Dispose();
        activeTask = null;
    }

    public static void ReapplyMessage()
    {
        if (!HasActiveMessage)
            return;

        if (activeTask == null)
        {
            Show();
            return;
        }

        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(GuideMessage, true);
    }

    private static void Show()
    {
        if (activeTask != null)
            return;

        activeTask = new GuaranteeDepositGuideTask(GuideMessage);
        activeTask.Start(() => activeTask = null);
    }

    private static void Restart()
    {
        if (activeTask != null)
        {
            activeTask.Dispose();
            activeTask = null;
        }

        Show();
    }

    private static bool ShouldShow(IReadOnlyPlayerData playerData)
    {
        return playerData != null
            && !playerData.HasReachedEnding
            && !playerData.HasPaidGuaranteeDeposit
            && IsGuidePhase(playerData);
    }

    private static bool ShouldKeepOpen(IReadOnlyPlayerData playerData)
    {
        return playerData != null
            && !playerData.HasReachedEnding
            && !playerData.HasPaidGuaranteeDeposit
            && IsGuidePhase(playerData);
    }

    private static bool IsGuidePhase(IReadOnlyPlayerData playerData)
    {
        int warningDay = EndingConditionDetector.GuaranteeDepositDeadlineDay - 1;
        int deadlineDay = EndingConditionDetector.GuaranteeDepositDeadlineDay;

        return (playerData.DaysPlayed == warningDay
                && (playerData.PlayingStatus == DayPhase.HumanDay
                    || playerData.PlayingStatus == DayPhase.AfterNoon))
            || (playerData.DaysPlayed == deadlineDay
                && playerData.PlayingStatus == DayPhase.Night);
    }

    private class GuaranteeDepositGuideTask : GuideTask
    {
        private readonly string message;

        public GuaranteeDepositGuideTask(string message)
        {
            this.message = message;
        }

        public override string TaskName => "Guarantee Deposit Guide";

        protected override List<GuideStep> BuildSteps()
        {
            return new List<GuideStep>
            {
                new PersistentHintStep(message)
            };
        }
    }
}
