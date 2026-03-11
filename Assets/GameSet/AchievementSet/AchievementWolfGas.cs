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
        AchievementEvents.OnTransactionCompleted += CheckCondition;

    protected override void UnsubscribeEvents() =>
        AchievementEvents.OnTransactionCompleted -= CheckCondition;

    private void CheckCondition(string customerId, string itemId)
    {
        if (customerId == "Wolf" && itemId == "WolfGas")
        {
            CompletedAchievement();
            SaveData();
            UnsubscribeEvents();
        }
    }
}
}