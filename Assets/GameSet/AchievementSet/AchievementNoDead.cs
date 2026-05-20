using System;
namespace AchievementLibrary
{
[AchievementDefinition("NoDead")]
public class AchievementNoDead : AchievementBase
    {
        public AchievementNoDead()
        {
            AchievementID = "NoDead";
        }

        public override void Initialize()
        {
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as AchievementNoDead).IsCompleted;
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
            GameEventCenter.Subscribe<ItemObtainedEvent>(CheckCondition);

        protected override void UnsubscribeEvents() =>
            GameEventCenter.Unsubscribe<ItemObtainedEvent>(CheckCondition);

        private void CheckCondition(ItemObtainedEvent eventData)
        {
            if (eventData.ItemId == "FullMoonPalaceElixir")
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}
