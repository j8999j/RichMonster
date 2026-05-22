using System.Collections.Generic;
using UnityEngine;

namespace Souvenir
{
    [System.Serializable]
    public class GroceryPurchaseSaveData : IRunSaveData
    {
        public string UniqueID { get; set; } = SaveDataKeys.GroceryPurchase;
        public int LastUpdatedDay { get; set; }
        public int PurchaseCount;
    }

    [SouvenirDefinition("SouAch_GroceryCards")]
    public class GroceryBuyTenGetOneSouvenir : AchievementSouvenir, IShopPurchaseListener, ISouvenirInteractive
    {
        private int _pendingRewardCount;
        private bool _isShowingReward;

        public override string SouvenirID => "SouAch_GroceryCards";

        public void OnItemPurchased(string shopId, string itemId, int amount)
        {
            if (shopId != ShopIDs.GroceryStore) return;

            GroceryPurchaseSaveData saveData = DataManager.Instance
                .GetRunSaveData<GroceryPurchaseSaveData>(SaveDataKeys.GroceryPurchase);

            if (string.IsNullOrEmpty(saveData.UniqueID))
            {
                saveData.UniqueID = SaveDataKeys.GroceryPurchase;
            }

            int completedRewardCount = 0;
            saveData.PurchaseCount += amount;
            while (saveData.PurchaseCount >= 10)
            {
                saveData.PurchaseCount -= 10;
                completedRewardCount++;
            }

            saveData.LastUpdatedDay = DataManager.Instance.CurrentPlayerData.DaysPlayed;
            DataManager.Instance.SetRunSaveData(saveData);

            QueueRewards(completedRewardCount);
        }

        private void QueueRewards(int count)
        {
            if (count <= 0) return;

            _pendingRewardCount += count;
            TryShowNextReward();
        }

        private void TryShowNextReward()
        {
            if (_isShowingReward || _pendingRewardCount <= 0) return;

            _pendingRewardCount--;
            _isShowingReward = true;

            GroceryCardsPresenter.ShowAndCloseAfterReveal(10, () =>
            {
                GiveRandomGift();
                _isShowingReward = false;
                TryShowNextReward();
            });
        }

        private void GiveRandomGift()
        {
            string itemId = PickRandomGroceryStoreItemId();
            if (string.IsNullOrEmpty(itemId)) return;

            DataManager.Instance.AddItem(itemId, 0);
            NoticeGetItemEvents.InvokeShowNotice(
                "\u96dc\u8ca8\u5e97\u7d00\u5ff5\u54c1",
                new List<NoticeItemEntry> { NoticeItemEntry.ItemEntry(itemId, 1) });

            string itemName = DataManager.Instance.GetItemById(itemId)?.Name;
            Debug.Log($"[Souvenir] Grocery buy-ten reward granted: {itemName}");
        }

        private string PickRandomGroceryStoreItemId()
        {
            List<ItemDefinition> groceryItems = DataManager.Instance.GetItemsByShopType(ShopIDs.GroceryStore);
            if (groceryItems != null && groceryItems.Count > 0)
            {
                int index = Random.Range(0, groceryItems.Count);
                return groceryItems[index]?.Id;
            }

            List<string> fallbackItems = DataManager.Instance.GetRandomDistinctItemIds(ItemWorld.Human, Rarity.Common, 1);
            return fallbackItems != null && fallbackItems.Count > 0 ? fallbackItems[0] : null;
        }

        public bool HasInteraction => true;
        public string InteractionButtonText => "\u67e5\u770b";

        public bool OnInteraction()
        {
            GroceryCardsPresenter.Show(GetCurrentPoints());
            return true;
        }

        public bool CanShowInteractionButton() => true;

        private int GetCurrentPoints()
        {
            GroceryPurchaseSaveData saveData = DataManager.Instance
                .GetRunSaveData<GroceryPurchaseSaveData>(SaveDataKeys.GroceryPurchase);

            return saveData != null && !string.IsNullOrEmpty(saveData.UniqueID)
                ? saveData.PurchaseCount
                : 0;
        }
    }
}
