using System;
namespace AchievementLibrary
{
public class NewWorldGodAchievement : AchievementBase
    {
        public NewWorldGodAchievement()
        {
            AchievementID = "LifeOverDeath";
        }

        public override void Initialize()
        {
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as NewWorldGodAchievement).IsCompleted;
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
            AchievementEvents.OnItemObtained += CheckCondition;

        protected override void UnsubscribeEvents() =>
            AchievementEvents.OnItemObtained -= CheckCondition;

        private void CheckCondition(string itemId)
        {
            if (itemId == "DeathNote")
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}