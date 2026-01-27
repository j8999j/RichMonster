using System.Collections.Generic;
using UnityEngine;
using GameSystem;
namespace Shop
{
    public class YokaiStore : ShopBase, Itrade
    {
        void OnEnable()
        {
            _shopUIView.OnCloseShopUI += EndInteract;
        }
        void OnDisable()
        {
            _shopUIView.OnCloseShopUI -= EndInteract;
        }
        // 稀有度對應的抽選權重
        private readonly Dictionary<Rarity, int> _rarityWeight = new Dictionary<Rarity, int>
        {
            { Rarity.Common, 80 },
            { Rarity.Uncommon, 60 },
            { Rarity.Rare, 40 },
            { Rarity.Epic, 20 },
            { Rarity.Legendary, 10 }
        };
        protected override void OnInteract()
        {
            var CurrentDay = GameManager.Instance.gameFlow.CurrentDay;
            var items = SyncPurchaseState(GenerateTodayShopItems(CurrentDay));
            items = ApplyPriceFactor(items);
            // 2. 顯示 UI
            if (_shopUIView != null)
            {
                // 3. 把資料丟給 View，並註冊「當玩家想買東西時」的回呼函式
                _shopUIView.ShowItems(items, OnPlayerTryToBuyItem);
                _shopUIView.SetVisible(true);
            }
            GameManager.Instance.SetPlayerInteract(false);
            GameManager.Instance.SetPlayerMove(false);
        }
        private async void EndInteract()
        {
            await GameManager.Instance.gameFlow.SaveGameAsync();
            GameManager.Instance.SetPlayerInteract(true);
            GameManager.Instance.SetPlayerMove(true);
        }
        private void OnPlayerTryToBuyItem(ShelfSlot slotData)
        {
            // 執行繼承自 ShopBase 或自己實作的交易邏輯
            tradeitem(slotData);
        }

        /// <summary>
        /// 依指定天數生成當日貨架清單（以稀有度權重抽選）。
        /// </summary>
        public List<ShelfSlot> GenerateTodayShopItems(int currentDay)
        {
            int slotCount = Mathf.Max(1, ShopInventorySize);
            TodayShopItemList = new List<ShelfSlot>(slotCount);

            if (ShopItemList == null || ShopItemList.Count == 0)
            {
                return TodayShopItemList;
            }

            for (int i = 0; i < slotCount; i++)
            {
                var item = PickWeighted(ShopItemList, i);
                if (item == null) continue;
                TodayShopItemList.Add(new ShelfSlot
                {
                    SlotIndex = i,
                    Item = item,
                    Purchased = false
                });
            }
            return TodayShopItemList;
        }

        #region PickWeight
        private ItemDefinition PickWeighted(List<ItemDefinition> pool, int index)
        {
            if (pool == null || pool.Count == 0) return null;

            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += GetItemWeight(pool[i]);
            }

            if (totalWeight <= 0) return null;

            int roll = GameRng.RangeKeyed(0, totalWeight, ShopID + index.ToString());

            for (int i = 0; i < pool.Count; i++)
            {
                int weight = GetItemWeight(pool[i]);
                if (roll < weight) return pool[i];
                roll -= weight;
            }

            return pool[pool.Count - 1];
        }

        private int GetItemWeight(ItemDefinition item)
        {
            if (item == null) return 0;
            return _rarityWeight.TryGetValue(item.Rarity, out int w) ? w : 1;
        }

        #endregion
        #region Trade
        public void tradeitem(ShelfSlot shelfSlot)
        {
            if (shelfSlot.Purchased || shelfSlot.Item == null) return;
            // 嘗試扣款
            if (DataManager.Instance.TrySpendMonsterGold(shelfSlot.Price))
            {
                DataManager.Instance.AddItem(shelfSlot.Item.Id, shelfSlot.Price);
                Debug.Log($"[商店] 購買成功: {shelfSlot.Item.Name} (價格: {shelfSlot.Price})");
                // 標記為已購買
                shelfSlot.Purchased = true;
                NewShopShelfData(shelfSlot);
                // 存檔同步
                SyncPurchaseState(TodayShopItemList);
                // **關鍵：通知 View 刷新 (包含列表變灰 + 按鈕變灰)**
                _shopUIView.RefreshAll();
            }
            else
            {
                Debug.Log("金幣不足");
            }
        }
        /// <summary>
        /// 新增商店存貨狀態
        /// </summary>
        /// <param name="shelfSlot">新增的商店購買紀錄</param>
        public void NewShopShelfData(ShelfSlot shelfSlot)
        {
            ThisShopShelfData.Changes[shelfSlot.SlotIndex] = new ShopInventoryChange
            {
                SlotIndex = shelfSlot.SlotIndex,
                ItemId = shelfSlot.Item.Id,
                Purchased = true
            };
            DataManager.Instance.AddShopShelfData(ThisShopShelfData);
        }
        #endregion
    }
}