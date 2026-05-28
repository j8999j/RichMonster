using UnityEngine;

namespace Souvenir
{
    public sealed class SouvenirEffectDispatcher
    {
        private readonly SouvenirEffectRegistry _registry;
        private bool _eventsSubscribed;

        public SouvenirEffectDispatcher(SouvenirEffectRegistry registry)
        {
            _registry = registry;
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

        public void ApplyAllStartEffects()
        {
            foreach (var effect in _registry.GetOwned<IApplyStartEffect>())
            {
                effect.ApplyStartEffect();
            }

            Debug.Log("[SouvenirEffectDispatcher] Applied owned start effects.");
        }

        public void ApplyAllDailyEffects()
        {
            foreach (var effect in _registry.GetOwned<IDailyEffect>())
            {
                effect.ApplyDailyEffect();
            }

            Debug.Log("[SouvenirEffectDispatcher] Applied owned daily effects.");
        }

        private void OnItemPurchased(ItemPurchasedEvent eventData)
        {
            foreach (var listener in _registry.GetOwned<IShopPurchaseListener>())
            {
                listener.OnItemPurchased(eventData.ShopId, eventData.ItemId, eventData.Amount);
            }
        }

        private void OnMonsterTradeCompleted(MonsterTradeCompletedEvent eventData)
        {
            foreach (var listener in _registry.GetOwned<IMonsterTradeListener>())
            {
                listener.OnTradeCompleted(eventData.Satisfaction);
            }

            foreach (var listener in _registry.GetAllSpecial<IMonsterTradeWithRaceListener>())
            {
                listener.OnTradeCompletedWithRace(eventData.Satisfaction, eventData.Race);
            }
        }

        private void OnMonsterTradeFailed(MonsterTradeFailedEvent eventData)
        {
            foreach (var listener in _registry.GetAllSpecial<IMonsterTradeFailedListener>())
            {
                listener.OnTradeFailed(eventData.Race);
            }
        }
    }
}
