using UnityEngine;

namespace Souvenir
{
    // 自訂的 ISaveData，用於紀錄跨日購買次數
    [System.Serializable]
    public class GroceryPurchaseSaveData : ISaveData
    {
        public string UniqueID { get; set; } = "GroceryPurchaseSaveData";
        public int LastUpdatedDay { get; set; }
        public int PurchaseCount;
    }

    /// <summary>
    /// 雜貨店每購買 10 項商品隨機獲得一項贈品
    /// </summary>
    public class GroceryBuyTenGetOneSouvenir : AchievementSouvenir, IShopPurchaseListener, ISouvenirInteractive
    {
        public override string SouvenirID => "SouAch_GroceryCards";

        public void OnItemPurchased(string shopId, string itemId, int amount)
        {
            // 僅限雜貨店
            if (shopId != ShopIDs.GroceryStore) return;

            // 取出不被跨日自動重置的持久資料
            var saveData = DataManager.Instance.GetPersistentSaveData<GroceryPurchaseSaveData>("GroceryPurchaseSaveData");
            
            // 處理新的無資料情況
            if (string.IsNullOrEmpty(saveData.UniqueID))
            {
                saveData.UniqueID = "GroceryPurchaseSaveData";
            }

            saveData.PurchaseCount += amount;

            // 累積滿 10 次
            if (saveData.PurchaseCount >= 10)
            {
                saveData.PurchaseCount -= 10;
                GiveRandomGift();
            }

            // 更新時間並存檔
            saveData.LastUpdatedDay = DataManager.Instance.CurrentPlayerData.DaysPlayed;
            DataManager.Instance.SetPlayerData("GroceryPurchaseSaveData", saveData);
        }

        private void GiveRandomGift()
        {
            // 示範抽選隨機獎品（抽選人間普通物品 1 件）
            var items = DataManager.Instance.GetRandomDistinctItemIds(ItemWorld.Human, Rarity.Common, 1);
            if (items != null && items.Count > 0)
            {
                DataManager.Instance.AddItem(items[0], 0);
                string itemName = DataManager.Instance.GetItemById(items[0])?.Name;
                Debug.Log($"[Souvenir] 雜貨店買十送一紀念品觸發！累積滿 10 次購買，獲得免費贈品：{itemName}");
            }
        }

        #region ISouvenirInteractive 實作

        public bool HasInteraction => true;
        public string InteractionButtonText => "查看";

        public void OnInteraction()
        {
            int currentPoints = 0;
            // 取出不被跨日自動重置的持久資料以確認目前的購買次數
            var saveData = DataManager.Instance.GetPersistentSaveData<GroceryPurchaseSaveData>("GroceryPurchaseSaveData");
            if (saveData != null && !string.IsNullOrEmpty(saveData.UniqueID))
            {
                currentPoints = saveData.PurchaseCount;
            }
            
            Debug.Log($"[Souvenir] 雜貨店贈品進度：目前已累積 {currentPoints} / 10 次購買");
            // TODO: 若未來有統一的系統提示 UI，可以在此呼叫通知玩家的 API
        }

        public bool CanShowInteractionButton() => true;

        #endregion
    }
}
