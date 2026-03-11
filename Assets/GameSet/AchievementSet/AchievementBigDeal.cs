using System;
using System.Collections.Generic;
namespace AchievementLibrary
{
public class AchievementBigDeal : AchievementBase
    {
        public AchievementBigDeal()
        {
            AchievementID = "BigDeal";
        }

        public override void Initialize()
        {
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as AchievementBigDeal).IsCompleted;
            }
            base.Initialize();
        }
        protected override void SaveData()
        {
            FinishDay = DateTime.Now.ToString("yyyy-MM-dd");
            DataManager.Instance.UpdateAchievementSaveData(this);
        }
        protected override void SubscribeEvents() =>
            AchievementEvents.OnOrderCompleted += CheckCondition;

        protected override void UnsubscribeEvents() =>
            AchievementEvents.OnOrderCompleted -= CheckCondition;

        private void CheckCondition(string orderId, List<string> itemIds, int gold)
        {
            if (gold > 10000)
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}