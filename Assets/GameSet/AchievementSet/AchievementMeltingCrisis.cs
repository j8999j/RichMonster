using System;
namespace AchievementLibrary
{
public class AchievementMeltingCrisis : AchievementBase, IAchievementHiddenCondition
{
    public AchievementMeltingCrisis()
    {
        AchievementID = "MeltingCrisis";
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
        if (customerId == "YukiOnna" && itemId == "HairDryer")
        {
            CompletedAchievement();
            SaveData();
            UnsubscribeEvents();
        }
    }
}
}