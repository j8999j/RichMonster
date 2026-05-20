using System;
namespace AchievementLibrary
{
[AchievementDefinition("LLL")]
public class AchievementLLL : AchievementBase
    {
        public AchievementLLL()
        {
            AchievementID = "LLL";
        }

        public override void Initialize()
        {
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as AchievementLLL).IsCompleted;
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
            if (DataManager.Instance.GetItemById(eventData.ItemId).Rarity == Rarity.Legendary)
            {
                if(DataManager.Instance.GetItemCountByRarity(Rarity.Legendary) >= 3)
                {
                    CompletedAchievement();
                    SaveData();
                    UnsubscribeEvents();
                }
            }
        }
    }
}
