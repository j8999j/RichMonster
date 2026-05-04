using UnityEngine;

public static class AuctionDayGuide
{
    public const int AuctionDay = 21;

    private const string HumanDayMessage = "\u4ECA\u5929\u5C07\u8209\u884C\u62CD\u8CE3\u6703\uFF0C\u8ACB\u5728\u9EC3\u660F\u524D\u505A\u597D\u6E96\u5099\u3002";
    private const string AfterNoonMessage = "\u505A\u597D\u6240\u6709\u6E96\u5099\uFF0C\u958B\u59CB\u62CD\u8CE3\u6703\u3002";

    private static bool isActive;
    private static bool isAfterNoonGuideCompleted;

    public static void Refresh()
    {
        IReadOnlyPlayerData playerData = DataManager.Instance?.CurrentPlayerData;
        bool isAuctionDay = IsAuctionDay(playerData);
        bool showHumanDay = ShouldShowHumanDayMessage(playerData);
        bool isAfterNoon = IsAuctionAfterNoon(playerData);

        SetAuctionTelePointActive(isAfterNoon);

        if (showHumanDay)
        {
            isAfterNoonGuideCompleted = false;
            ShowTextOnly(HumanDayMessage);
            return;
        }

        if (isAfterNoon && !isAfterNoonGuideCompleted)
        {
            ShowAfterNoonGuide();
            return;
        }

        if (!isAuctionDay)
        {
            isAfterNoonGuideCompleted = false;
        }

        Hide();
    }

    public static void CompleteAuctionStartGuide()
    {
        isAfterNoonGuideCompleted = true;
        Hide();
    }

    public static bool ShouldHideSouvenirButton(IReadOnlyPlayerData playerData)
    {
        return IsAuctionAfterNoon(playerData) && !isAfterNoonGuideCompleted;
    }

    public static bool ShouldShowAfterNoonGuide(IReadOnlyPlayerData playerData)
    {
        return IsAuctionAfterNoon(playerData) && !isAfterNoonGuideCompleted;
    }

    private static bool ShouldShowHumanDayMessage(IReadOnlyPlayerData playerData)
    {
        return IsAuctionDay(playerData)
            && playerData.PlayingStatus == DayPhase.HumanDay;
    }

    private static bool IsAuctionDay(IReadOnlyPlayerData playerData)
    {
        return playerData != null
            && !playerData.HasReachedEnding
            && playerData.DaysPlayed == AuctionDay;
    }

    private static bool IsAuctionAfterNoon(IReadOnlyPlayerData playerData)
    {
        return IsAuctionDay(playerData)
            && playerData.PlayingStatus == DayPhase.AfterNoon;
    }

    private static void ShowTextOnly(string message)
    {
        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(message, true);
        NoticeGetItemEvents.InvokeClearMapGuide();
        isActive = true;
    }

    private static void ShowAfterNoonGuide()
    {
        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(AfterNoonMessage, true);
        RegisterAuctionTelePointGuide();
        NoticeGetItemEvents.InvokeStartMapGuide(GuideIDs.Interactable.TelePointAuctionGuide);
        isActive = true;
    }

    private static void Hide()
    {
        if (!isActive)
            return;

        GuideFlowUI.SetGuideFlowTextEvent?.Invoke(string.Empty, false);
        NoticeGetItemEvents.InvokeClearMapGuide();
        isActive = false;
    }

    private static void SetAuctionTelePointActive(bool active)
    {
        TelePointAuctionGuide[] guides = Object.FindObjectsOfType<TelePointAuctionGuide>(true);
        for (int i = 0; i < guides.Length; i++)
        {
            if (guides[i] == null)
                continue;

            if (guides[i].gameObject.activeSelf != active)
            {
                guides[i].gameObject.SetActive(active);
            }
        }
    }

    private static void RegisterAuctionTelePointGuide()
    {
        TelePointAuctionGuide[] guides = Object.FindObjectsOfType<TelePointAuctionGuide>(true);
        for (int i = 0; i < guides.Length; i++)
        {
            if (guides[i] != null)
            {
                guides[i].SetMapGuide();
            }
        }
    }
}
