using System;
namespace AchievementLibrary
{
public class AchievementBackHome : AchievementBase, IAchievementHiddenCondition
    {
        public AchievementBackHome()
        {
            AchievementID = "BackHome";
        }

        public override void Initialize()
        {
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
            if (customerId == "Kaguya" && itemId == "WaterRocket")
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}