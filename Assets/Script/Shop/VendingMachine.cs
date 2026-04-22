using System.Collections.Generic;
using UnityEngine;
using GameSystem;

namespace Shop
{
    /// <summary>
    /// 飲料自動販賣機：結構類似 GroceryStore，使用人間金幣；
    /// UI 改用 VendingMachineShopView（每格下方有購買按鈕，點擊直接購買）。
    /// </summary>
    public class VendingMachine : ShopBase, Itrade
    {
        void OnEnable()
        {
            _shopUIView.OnCloseShopUI += EndInteract;
        }

        void OnDisable()
        {
            _shopUIView.OnCloseShopUI -= EndInteract;
        }

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
            if (GameManager.Instance.IsPlayerMoveLocked("VendingMachine"))
            {
                _shopUIView.SetVisible();
                GameManager.Instance.UnlockPlayerMove("VendingMachine");
                return;
            }
            var CurrentDay = GameManager.Instance.gameFlow.CurrentDay;
            var items = SyncPurchaseState(GenerateTodayShopItems(CurrentDay));
            items = ApplyPriceFactor(items);
            Souvenir.SouvenirManager.Instance.BuildShopVisualInfos(ShopID, items);

            if (_shopUIView != null)
            {
                _shopUIView.ShowItems(items, OnPlayerTryToBuyItem);
            }
            _shopUIView.SetVisible();
            GameManager.Instance.LockPlayerMove("VendingMachine");
        }

        private async void EndInteract()
        {
            await GameManager.Instance.gameFlow.SaveGameAsync();
            GameManager.Instance.UnlockPlayerMove("VendingMachine");
        }

        private void OnPlayerTryToBuyItem(ShelfSlot slotData)
        {
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
            if (DataManager.Instance.TrySpendGold(shelfSlot.Price))
            {
                DataManager.Instance.AddItem(shelfSlot.Item.Id, shelfSlot.Price);
                shelfSlot.Purchased = true;
                NewShopShelfData(shelfSlot);
                SyncPurchaseState(TodayShopItemList);
                _shopUIView.RefreshAll();
                Souvenir.SouvenirManager.Instance.NotifyItemPurchased(ShopID, shelfSlot.Item.Id, 1);
            }
        }

        /// <summary>
        /// 新增商店存貨狀態
        /// </summary>
        public void NewShopShelfData(ShelfSlot shelfSlot)
        {
            ThisShopShelfData.UniqueID = ShopID + "ShopShelfData";
            ThisShopShelfData.LastUpdatedDay = GameManager.Instance.gameFlow.CurrentDay;
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
