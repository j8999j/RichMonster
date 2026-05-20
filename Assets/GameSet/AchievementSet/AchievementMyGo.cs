using System;
namespace AchievementLibrary
{
[AchievementDefinition("MyGo")]
public class AchievementMyGo : AchievementBase, IAchievementHiddenCondition
{
    public AchievementMyGo()
    {
        AchievementID = "MyGo";
    }
    public override void Initialize()
    {
        var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
        if (data != null)
        {
            IsCompleted = (data as AchievementMyGo).IsCompleted;
        }
        if (IsCompleted) return;
        base.Initialize();
    }
    protected override void SaveData()
    {
        FinishDay = DateTime.Now.ToString("yyyy-MM-dd");
        DataManager.Instance.UpdateAchievementSaveData(this);
    }
    protected override void SubscribeEvents() =>
        GameEventCenter.Subscribe<MonsterTradeCompletedEvent>(CheckCondition);

    protected override void UnsubscribeEvents() =>
        GameEventCenter.Unsubscribe<MonsterTradeCompletedEvent>(CheckCondition);

    private void CheckCondition(MonsterTradeCompletedEvent eventData)
    {
        if (eventData.CustomerId == "Kaguya-hime" && eventData.ItemId == "Compass")
        {
            CompletedAchievement();
            SaveData();
            UnsubscribeEvents();
        }
    }
}
}
