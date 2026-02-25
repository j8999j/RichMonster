using System;
namespace AchievementLibrary
{
public class AchievementNoDead : AchievementBase
    {
        public AchievementNoDead()
        {
            AchievementID = "NoDead";
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
            AchievementEvents.OnItemObtained += CheckCondition;

        protected override void UnsubscribeEvents() =>
            AchievementEvents.OnItemObtained -= CheckCondition;

        private void CheckCondition(string itemId)
        {
            if (itemId == "FullMoonPalaceElixir")
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}