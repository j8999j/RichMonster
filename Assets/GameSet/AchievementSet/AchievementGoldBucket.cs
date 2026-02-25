using System;
using GameSystem;
namespace AchievementLibrary
{
public class AchievementGoldBucket : AchievementBase, IAchievementWithProgress
{
    public int goldRecorder{ get; set;}
    public AchievementGoldBucket()
    {
        AchievementID = "GoldBucket";
    }
    protected override void SaveData()
    {
        DataManager.Instance.UpdateAchievementSaveData(this);
    }
    protected override void SubscribeEvents()
    {
        AchievementEvents.OnGoldChanged += CheckCondition;
    }

    protected override void UnsubscribeEvents()
    {
        AchievementEvents.OnGoldChanged -= CheckCondition;
    }
    public override void Initialize()
    {
        var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
        if (data != null)
        {
            goldRecorder = (data as AchievementGoldBucket).goldRecorder;
        }
        else
        {
            goldRecorder = 0;
        }
        base.Initialize();
    }
    private void CheckCondition(int gold, int goldChange)
    {
        goldRecorder += goldChange;
        if (goldRecorder >= 100000) // 企劃書中的關鍵道具 ID
        {
            CompletedAchievement();
        }
        SaveData();
    }
    public string ProgressText => goldRecorder >= 100000
        ? $"目前累積獲得:{goldRecorder}"
        : $"{goldRecorder}/100000";
    public float ProgressRatio => (float)goldRecorder / 100000;
}
}