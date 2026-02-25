using System;
namespace AchievementLibrary
{
public class AchievementFoodie : AchievementBase, IAchievementHiddenCondition
    {
        public AchievementFoodie()
        {
            AchievementID = "Foodie";
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
            if (DataManager.Instance.GetItemById(itemId).Type == ItemType.Food)
            {
                if(DataManager.Instance.GetDistinctItemCountByTypeAndWorld(ItemType.Food, ItemWorld.Human) >= 3)
                {
                    if(DataManager.Instance.GetDistinctItemCountByTypeAndWorld(ItemType.Food, ItemWorld.Monster) >= 3)
                    {
                        CompletedAchievement();
                        SaveData();
                        UnsubscribeEvents();
                    }
                }
            }
        }
    }
}