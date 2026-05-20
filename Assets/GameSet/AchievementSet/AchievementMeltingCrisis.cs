using System;
namespace AchievementLibrary
{
    [AchievementDefinition("MeltingCrisis")]
    public class AchievementMeltingCrisis : AchievementBase, IAchievementHiddenCondition
    {
        public AchievementMeltingCrisis()
        {
            AchievementID = "MeltingCrisis";
        }
        public override void Initialize()
        {
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as AchievementMeltingCrisis).IsCompleted;
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
            GameEventCenter.Subscribe<MonsterTradeCompletedEvent>(CheckCondition);

        protected override void UnsubscribeEvents() =>
            GameEventCenter.Unsubscribe<MonsterTradeCompletedEvent>(CheckCondition);

        private void CheckCondition(MonsterTradeCompletedEvent eventData)
        {
            if (eventData.CustomerId == "RookieAdventurer" && eventData.ItemId == "Hairdryer")
            {
                CompletedAchievement();
                SaveData();
                UnsubscribeEvents();
            }
        }
    }
}
