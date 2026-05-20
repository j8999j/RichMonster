using System;
namespace AchievementLibrary
{
[AchievementDefinition("CrisisManager")]
public class AchievementCrisisManager : AchievementBase, IAchievementHiddenCondition
    {
        public AchievementCrisisManager()
        {
            AchievementID = "CrisisManager";
        }

        public override void Initialize()
        {
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as AchievementCrisisManager).IsCompleted;
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
            GameEventCenter.Subscribe<DayEndedEvent>(CheckCondition);

        protected override void UnsubscribeEvents() =>
            GameEventCenter.Unsubscribe<DayEndedEvent>(CheckCondition);

        private void CheckCondition(DayEndedEvent eventData)
        {
            if (eventData.Gold < 50)
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}
