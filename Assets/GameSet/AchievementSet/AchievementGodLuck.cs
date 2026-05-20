using System;
namespace AchievementLibrary
{
public class AchievementGodLuck : AchievementBase
    {
        public AchievementGodLuck()
        {
            AchievementID = "GodLuck";
        }

        public override void Initialize()
        {
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as AchievementGodLuck).IsCompleted;
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
            GameEventCenter.Subscribe<ScratchCardCompletedEvent>(CheckCondition);

        protected override void UnsubscribeEvents() =>
            GameEventCenter.Unsubscribe<ScratchCardCompletedEvent>(CheckCondition);

        private void CheckCondition(ScratchCardCompletedEvent eventData)
        {
            if (eventData.PrizeLevel == 0)
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}
