using System;
namespace AchievementLibrary
{
public class AchievementWolfGas : AchievementBase, IAchievementHiddenCondition
{
    public AchievementWolfGas()
    {
        AchievementID = "WolfGas";
    }
        public override void Initialize()
    {
        var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
        if (data != null)
        {
            IsCompleted = (data as AchievementWolfGas).IsCompleted;
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
        if (eventData.CustomerId == "Wolf" && eventData.ItemId == "WolfGas")
        {
            CompletedAchievement();
            SaveData();
            UnsubscribeEvents();
        }
    }
}
}
