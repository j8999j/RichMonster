using System.Collections.Generic;
using System.Linq;
using GameSystem;
using UnityEngine;

namespace Shop
{
    public abstract class ShelfShopBase : ShopBase, Itrade
    {
        private static readonly Dictionary<Rarity, int> RarityWeights = new Dictionary<Rarity, int>
        {
            { Rarity.Common, 80 },
            { Rarity.Uncommon, 60 },
            { Rarity.Rare, 40 },
            { Rarity.Epic, 20 },
            { Rarity.Legendary, 10 }
        };

        protected abstract string LockSource { get; }
        protected abstract GameCurrencyType CurrencyType { get; }
        protected virtual bool AllowDuplicateItems => true;
        protected virtual bool ApplyShopVisualInfo => false;

        protected override void Start()
        {
            base.Start();
            RegisterViewEvents();
        }

        protected virtual void OnEnable()
        {
            RegisterViewEvents();
        }

        protected virtual void OnDisable()
        {
            if (_shopUIView != null)
            {
                _shopUIView.OnCloseShopUI -= EndInteract;
            }
        }

        protected void RegisterViewEvents()
        {
            if (_shopUIView == null) return;
            _shopUIView.OnCloseShopUI -= EndInteract;
            _shopUIView.OnCloseShopUI += EndInteract;
        }

        protected override void OnInteract()
        {
            if (GameManager.Instance.IsPlayerMoveLocked(LockSource))
            {
                _shopUIView.SetVisible();
                GameManager.Instance.UnlockPlayerMove(LockSource);
                return;
            }

            OpenShop();
        }

        protected virtual void OpenShop()
        {
            int currentDay = GameManager.Instance.gameFlow.CurrentDay;
            var items = SyncPurchaseState(GenerateTodayShopItems(currentDay));
            items = ApplyPriceFactor(items);
            TodayShopItemList = items;

            if (ApplyShopVisualInfo)
            {
                Souvenir.SouvenirManager.Instance.BuildShopVisualInfos(ShopID, items);
            }

            if (_shopUIView != null)
            {
                _shopUIView.ShowItems(items, OnPlayerTryToBuyItem);
                _shopUIView.SetVisible();
            }

            GameManager.Instance.LockPlayerMove(LockSource);
        }

        protected virtual async void EndInteract()
        {
            await GameManager.Instance.gameFlow.SaveGameAsync();
            GameManager.Instance.UnlockPlayerMove(LockSource);
        }

        private void OnPlayerTryToBuyItem(ShelfSlot slotData)
        {
            tradeitem(slotData);
        }

        public virtual List<ShelfSlot> GenerateTodayShopItems(int currentDay)
        {
            int slotCount = Mathf.Max(1, ShopInventorySize);
            TodayShopItemList = new List<ShelfSlot>(slotCount);

            if (ShopItemList == null || ShopItemList.Count == 0)
            {
                return TodayShopItemList;
            }

            if (AllowDuplicateItems)
            {
                for (int i = 0; i < slotCount; i++)
                {
                    AddWeightedShelfSlot(ShopItemList, i);
                }
            }
            else
            {
                int actualCount = Mathf.Min(slotCount, ShopItemList.Count);
                var pool = new List<ItemDefinition>(ShopItemList);
                for (int i = 0; i < actualCount; i++)
                {
                    var item = PickWeighted(pool, i);
                    if (item == null) break;
                    pool.Remove(item);
                    AddShelfSlot(item, i);
                }
            }

            return TodayShopItemList;
        }

        private void AddWeightedShelfSlot(List<ItemDefinition> pool, int index)
        {
            var item = PickWeighted(pool, index);
            if (item != null)
            {
                AddShelfSlot(item, index);
            }
        }

        private void AddShelfSlot(ItemDefinition item, int index)
        {
            TodayShopItemList.Add(new ShelfSlot
            {
                SlotIndex = index,
                Item = item,
                Purchased = false
            });
        }

        protected virtual ItemDefinition PickWeighted(List<ItemDefinition> pool, int index)
        {
            if (pool == null || pool.Count == 0) return null;

            int totalWeight = pool.Sum(GetItemWeight);
            if (totalWeight <= 0) return null;

            int roll = GameRng.RangeKeyed(0, totalWeight, ShopID + index.ToString());
            foreach (var item in pool)
            {
                int weight = GetItemWeight(item);
                if (roll < weight) return item;
                roll -= weight;
            }

            return pool[pool.Count - 1];
        }

        protected virtual int GetItemWeight(ItemDefinition item)
        {
            if (item == null) return 0;
            return RarityWeights.TryGetValue(item.Rarity, out int weight) ? weight : 1;
        }

        public virtual void tradeitem(ShelfSlot shelfSlot)
        {
            if (shelfSlot == null || shelfSlot.Purchased || shelfSlot.Item == null)
            {
                _shopUIView.PlayBuyFailedSfx();
                return;
            }

            if (!TrySpendCurrency(shelfSlot.Price))
            {
                _shopUIView.PlayBuyFailedSfx();
                return;
            }

            DataManager.Instance.AddItem(shelfSlot.Item.Id, shelfSlot.Price);
            shelfSlot.Purchased = true;
            NewShopShelfData(shelfSlot);
            SyncPurchaseState(TodayShopItemList);
            _shopUIView.RefreshAll();
            _shopUIView.PlayBuySuccessSfx();
            GameEventCenter.Publish(new ItemPurchasedEvent(ShopID, shelfSlot.Item.Id, shelfSlot.Price, CurrencyType));
        }

        protected virtual bool TrySpendCurrency(int price)
        {
            switch (CurrencyType)
            {
                case GameCurrencyType.Gold:
                    return DataManager.Instance.TrySpendGoldForItemPurchase(price);
                case GameCurrencyType.MonsterGold:
                    return DataManager.Instance.TrySpendMonsterGoldForItemPurchase(price);
                default:
                    return false;
            }
        }

        public virtual void NewShopShelfData(ShelfSlot shelfSlot)
        {
            string shelfDataId = SaveDataKeys.BuildShopShelf(ShopID);
            ThisShopShelfData ??= new ShopShelfData { UniqueID = shelfDataId, Changes = new List<ShopInventoryChange>() };
            ThisShopShelfData.UniqueID = shelfDataId;
            ThisShopShelfData.LastUpdatedDay = GameManager.Instance.gameFlow.CurrentDay;
            ThisShopShelfData.Changes ??= new List<ShopInventoryChange>();

            var change = ThisShopShelfData.Changes.FirstOrDefault(x => x.SlotIndex == shelfSlot.SlotIndex);
            if (change == null)
            {
                ThisShopShelfData.Changes.Add(new ShopInventoryChange
                {
                    SlotIndex = shelfSlot.SlotIndex,
                    ItemId = shelfSlot.Item.Id,
                    Purchased = true
                });
            }
            else
            {
                change.ItemId = shelfSlot.Item.Id;
                change.Purchased = true;
            }

            DataManager.Instance.AddShopShelfData(ThisShopShelfData);
        }
    }
}
