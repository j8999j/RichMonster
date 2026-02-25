using System;
namespace AchievementLibrary
{
public class AchievementLLL : AchievementBase
    {
        public AchievementLLL()
        {
            AchievementID = "LLL";
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
            if (DataManager.Instance.GetItemById(itemId).Rarity == Rarity.Legendary)
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