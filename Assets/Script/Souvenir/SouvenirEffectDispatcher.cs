using System;
using System.Collections.Generic;
using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 負責紀念品效果的事件訂閱、生命週期與分派。
    /// SouvenirManager 管理資料與所有權；此類別只處理「哪些效果該被觸發」。
    /// </summary>
    public class SouvenirEffectDispatcher
    {
        private readonly Func<IEnumerable<SouvenirBase>> _getOwnedSouvenirs;
        private readonly Func<IEnumerable<SpecialSouvenir>> _getAllSpecialSouvenirs;
        private bool _eventsSubscribed;

        public SouvenirEffectDispatcher(
            Func<IEnumerable<SouvenirBase>> getOwnedSouvenirs,
            Func<IEnumerable<SpecialSouvenir>> getAllSpecialSouvenirs)
        {
            _getOwnedSouvenirs = getOwnedSouvenirs;
            _getAllSpecialSouvenirs = getAllSpecialSouvenirs;
        }

        public void SubscribeGameEvents()
        {
            if (_eventsSubscribed) return;

            GameEventCenter.Subscribe<ItemPurchasedEvent>(OnItemPurchased);
            GameEventCenter.Subscribe<MonsterTradeCompletedEvent>(OnMonsterTradeCompleted);
            GameEventCenter.Subscribe<MonsterTradeFailedEvent>(OnMonsterTradeFailed);
            _eventsSubscribed = true;
        }

        public void UnsubscribeGameEvents()
        {
            if (!_eventsSubscribed) return;

            GameEventCenter.Unsubscribe<ItemPurchasedEvent>(OnItemPurchased);
            GameEventCenter.Unsubscribe<MonsterTradeCompletedEvent>(OnMonsterTradeCompleted);
            GameEventCenter.Unsubscribe<MonsterTradeFailedEvent>(OnMonsterTradeFailed);
            _eventsSubscribed = false;
        }

        public void RegisterAllSpecialSouvenirs()
        {
            foreach (var souvenir in _getAllSpecialSouvenirs())
            {
                souvenir.Register();
            }

            Debug.Log("[SouvenirEffectDispatcher] 已註冊所有特殊紀念品事件");
        }

        public void UnregisterAllSpecialSouvenirs()
        {
            foreach (var souvenir in _getAllSpecialSouvenirs())
            {
                souvenir.Unregister();
            }

            Debug.Log("[SouvenirEffectDispatcher] 已取消註冊所有紀念品事件");
        }

        public void ApplyAllStartEffects()
        {
            ForEachOwned<IApplyStartEffect>(startEffect => startEffect.ApplyStartEffect());
            Debug.Log("[SouvenirEffectDispatcher] 已觸發所有持有的 IApplyStartEffect 開局效果");
        }

        public void ApplyAllShopDiscounts(string shopId, List<Shop.ShelfSlot> items)
        {
            if (items == null || items.Count == 0) return;
            ForEachOwned<IShopDiscountProvider>(discountProvider => discountProvider.ApplyShopDiscount(shopId, items));
        }

        public List<ShelfSlotVisualInfo> BuildShopVisualInfos(string shopId, List<Shop.ShelfSlot> items)
        {
            var visualInfos = new List<ShelfSlotVisualInfo>();
            if (items == null) return visualInfos;

            foreach (var slot in items)
            {
                var info = new ShelfSlotVisualInfo { SlotIndex = slot.SlotIndex };
                visualInfos.Add(info);
                slot.VisualInfo = info;
            }

            ForEachOwned<IShopVisualModifier>(visualModifier => visualModifier.ModifyVisual(shopId, visualInfos));
            return visualInfos;
        }

        public void ApplyAllDailyEffects()
        {
            ForEachOwned<IDailyEffect>(daily => daily.ApplyDailyEffect());
            Debug.Log("[SouvenirEffectDispatcher] 已觸發所有每日效果");
        }

        public bool IsScratchCardFree()
        {
            bool isFree = false;
            ForEachOwned<IFreeScratchCardProvider>(provider =>
            {
                if (provider.IsScratchCardFree())
                {
                    isFree = true;
                }
            });
            return isFree;
        }

        public int GetExtraBagCapacity()
        {
            int extraCapacity = 0;
            ForEachOwned<IBagCapacityProvider>(provider =>
            {
                extraCapacity += provider.GetExtraCapacity();
            });
            return extraCapacity;
        }

        private void OnItemPurchased(ItemPurchasedEvent eventData)
        {
            ForEachOwned<IShopPurchaseListener>(
                purchaseListener => purchaseListener.OnItemPurchased(eventData.ShopId, eventData.ItemId, eventData.Amount));
        }

        private void OnMonsterTradeCompleted(MonsterTradeCompletedEvent eventData)
        {
            ForEachOwned<IMonsterTradeListener>(
                listener => listener.OnTradeCompleted(eventData.Satisfaction));
            ForEachAllSpecial<IMonsterTradeWithRaceListener>(
                listener => listener.OnTradeCompletedWithRace(eventData.Satisfaction, eventData.Race));
        }

        private void OnMonsterTradeFailed(MonsterTradeFailedEvent eventData)
        {
            ForEachAllSpecial<IMonsterTradeFailedListener>(
                listener => listener.OnTradeFailed(eventData.Race));
        }

        private void ForEachOwned<T>(Action<T> action) where T : class
        {
            foreach (var souvenir in _getOwnedSouvenirs())
            {
                if (souvenir is T target)
                {
                    action(target);
                }
            }
        }

        private void ForEachAllSpecial<T>(Action<T> action) where T : class
        {
            foreach (var souvenir in _getAllSpecialSouvenirs())
            {
                if (souvenir is T target)
                {
                    action(target);
                }
            }
        }
    }
}
