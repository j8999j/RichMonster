using System;
namespace AchievementLibrary
{
public class AchievementFakeFish : AchievementBase, IAchievementHiddenCondition
{
    public AchievementFakeFish()
    {
        AchievementID = "FakeFish";
    }
    public override void Initialize()
    {
        var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
        if (data != null)
        {
            IsCompleted = (data as AchievementFakeFish).IsCompleted;
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
        AchievementEvents.OnTransactionCompleted += CheckCondition;

    protected override void UnsubscribeEvents() =>
        AchievementEvents.OnTransactionCompleted -= CheckCondition;

    private void CheckCondition(string customerId, string itemId)
    {
        if (customerId == "Nekomata" && itemId == "Taiyaki")
        {
            CompletedAchievement();
            SaveData();
            UnsubscribeEvents();
        }
    }
}
}