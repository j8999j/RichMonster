public static class EndingConditionDetector
{
    public const int GuaranteeDepositDeadlineDay = 7;
    public const int GuaranteeDepositAmount = 30000;
    public const int AuctionEntryFeeDeadlineDay = 15;
    public const int AuctionEntryFeeAmount = 100000;
    public const int AuctionGoldCheckDay = 22;
    public const int RequiredAuctionGold = 1000000;

    public static EndingType Evaluate(IReadOnlyPlayerData playerData)
    {
        return EvaluateForHumanDay(playerData);
    }

    public static EndingType EvaluateForHumanDay(IReadOnlyPlayerData playerData)
    {
        if (playerData == null || playerData.HasReachedEnding)
            return EndingType.None;

        if (playerData.DaysPlayed >= GuaranteeDepositDeadlineDay
            && !playerData.HasPaidGuaranteeDeposit)
            return EndingType.Type1;

        if (playerData.DaysPlayed >= AuctionGoldCheckDay
            && playerData.Gold < RequiredAuctionGold)
            return EndingType.Type3;

        return EndingType.None;
    }

    public static EndingType EvaluateForNewMonsterDay(IReadOnlyPlayerData playerData)
    {
        if (playerData == null || playerData.HasReachedEnding)
            return EndingType.None;

        if (playerData.DaysPlayed >= AuctionEntryFeeDeadlineDay
            && !playerData.HasPaidAuctionEntryFee)
            return EndingType.Type2;

        return EndingType.None;
    }
}
