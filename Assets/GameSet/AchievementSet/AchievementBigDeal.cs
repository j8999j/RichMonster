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
            GameEventCenter.Subscribe<HumanOrderCompletedEvent>(CheckCondition);

        protected override void UnsubscribeEvents() =>
            GameEventCenter.Unsubscribe<HumanOrderCompletedEvent>(CheckCondition);

        private void CheckCondition(HumanOrderCompletedEvent eventData)
        {
            if (eventData.Gold > 10000)
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}
