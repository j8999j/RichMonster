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
            var data = DataManager.Instance.GetAchievementSaveData(AchievementID);
            if (data != null)
            {
                IsCompleted = (data as AchievementFoodie).IsCompleted;
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
            if (DataManager.Instance.GetItemById(eventData.ItemId).Type == ItemType.Food)
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
