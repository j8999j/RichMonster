using System;
namespace AchievementLibrary
{
public class AchievementMyGo : AchievementBase, IAchievementHiddenCondition
{
    public AchievementMyGo()
    {
        AchievementID = "MyGo";
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
        if (customerId == "Kaguya" && itemId == "Compass")
        {
            CompletedAchievement();
            SaveData();
            UnsubscribeEvents();
        }
    }
}
}