public class NewWorldGodAchievement : AchievementBase
{
    public NewWorldGodAchievement()
    {
        AchievementID = "NewWorldGod"; 
    }
    protected override void SubscribeEvents()
    {
        AchievementEvents.OnItemObtained += CheckCondition;
    }

    protected override void UnsubscribeEvents()
    {
        AchievementEvents.OnItemObtained -= CheckCondition;
    }
    public override void Initialize()
    {
        if (IsCompleted) return;
        base.Initialize();
    }
    private void CheckCondition(string itemId)
    {
        if (itemId == "DeathNotePage") // 企劃書中的關鍵道具 ID
        {
            CompletedAchievement();
        }
    }
}