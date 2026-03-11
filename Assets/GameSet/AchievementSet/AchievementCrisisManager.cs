using System;
namespace AchievementLibrary
{
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
            AchievementEvents.OnDayEndGold += CheckCondition;

        protected override void UnsubscribeEvents() =>
            AchievementEvents.OnDayEndGold -= CheckCondition;

        private void CheckCondition(int gold)
        {
            if (gold < 50)
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}