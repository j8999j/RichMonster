using System;
namespace AchievementLibrary
{
public class AchievementBackHome : AchievementBase, IAchievementHiddenCondition
    {
        public AchievementBackHome()
        {
            AchievementID = "BackHome";
        }
        protected override void SaveData()
        {
            FinishDay = DateTime.Now.ToString("yyyy-MM-dd");
            DataManager.Instance.UpdateAchievementSaveData(this);
        }
        public override void Initialize()
        {
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as AchievementBackHome).IsCompleted;
            }
            if (IsCompleted) return;
            base.Initialize();
        }
        protected override void SubscribeEvents() =>
            GameEventCenter.Subscribe<MonsterTradeCompletedEvent>(CheckCondition);

        protected override void UnsubscribeEvents() =>
            GameEventCenter.Unsubscribe<MonsterTradeCompletedEvent>(CheckCondition);

        private void CheckCondition(MonsterTradeCompletedEvent eventData)
        {
            if (eventData.CustomerId == "Kaguya-hime" && eventData.ItemId == "WaterRocket")
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}
