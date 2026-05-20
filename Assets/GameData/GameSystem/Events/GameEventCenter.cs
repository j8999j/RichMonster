using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 統一的遊戲事件中心。
/// 發布端只描述「發生了什麼」，訂閱端自行決定是否處理，避免直接耦合成就、圖鑑、紀念品或 UI 系統。
/// </summary>
public static class GameEventCenter
{
    private static readonly Dictionary<Type, Delegate> EventHandlers = new Dictionary<Type, Delegate>();

    /// <summary>訂閱指定型別的遊戲事件。</summary>
    public static void Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) return;

        var eventType = typeof(TEvent);
        if (EventHandlers.TryGetValue(eventType, out var existing))
        {
            EventHandlers[eventType] = Delegate.Combine(existing, handler);
        }
        else
        {
            EventHandlers[eventType] = handler;
        }
    }

    /// <summary>取消訂閱指定型別的遊戲事件。</summary>
    public static void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) return;

        var eventType = typeof(TEvent);
        if (!EventHandlers.TryGetValue(eventType, out var existing)) return;

        var updated = Delegate.Remove(existing, handler);
        if (updated == null)
        {
            EventHandlers.Remove(eventType);
        }
        else
        {
            EventHandlers[eventType] = updated;
        }
    }

    /// <summary>發布指定型別的遊戲事件。</summary>
    public static void Publish<TEvent>(TEvent eventData)
    {
        if (eventData == null) return;

        if (EventHandlers.TryGetValue(typeof(TEvent), out var existing)
            && existing is Action<TEvent> handlers)
        {
            foreach (var handlerDelegate in handlers.GetInvocationList())
            {
                var handler = handlerDelegate as Action<TEvent>;
                if (handler == null) continue;

                try
                {
                    handler.Invoke(eventData);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[GameEventCenter] 處理事件 {typeof(TEvent).Name} 時發生錯誤: {exception}");
                }
            }
        }
    }

    /// <summary>清除所有事件訂閱；通常只在測試或完整重置流程使用。</summary>
    public static void ClearAll()
    {
        EventHandlers.Clear();
    }
}

/// <summary>遊戲內貨幣種類。</summary>
public enum GameCurrencyType
{
    None,
    Gold,
    MonsterGold,
    AchievementPoint
}

/// <summary>玩家獲得物品事件。</summary>
public class ItemObtainedEvent
{
    public string ItemId { get; }
    public int CostPrice { get; }
    public string Source { get; }

    public ItemObtainedEvent(string itemId, int costPrice, string source = "")
    {
        ItemId = itemId;
        CostPrice = costPrice;
        Source = source;
    }
}

/// <summary>玩家在商店購買物品成功事件。</summary>
public class ItemPurchasedEvent
{
    public string ShopId { get; }
    public string ItemId { get; }
    public int Price { get; }
    public GameCurrencyType CurrencyType { get; }
    public int Amount { get; }

    public ItemPurchasedEvent(string shopId, string itemId, int price, GameCurrencyType currencyType, int amount = 1)
    {
        ShopId = shopId;
        ItemId = itemId;
        Price = price;
        CurrencyType = currencyType;
        Amount = amount;
    }
}

/// <summary>玩家貨幣數量變更事件。</summary>
public class CurrencyChangedEvent
{
    public GameCurrencyType CurrencyType { get; }
    public int Before { get; }
    public int After { get; }
    public int Delta { get; }
    public string Reason { get; }

    public CurrencyChangedEvent(GameCurrencyType currencyType, int before, int after, int delta, string reason = "")
    {
        CurrencyType = currencyType;
        Before = before;
        After = after;
        Delta = delta;
        Reason = reason;
    }
}

/// <summary>妖怪交易成功事件。</summary>
public class MonsterTradeCompletedEvent
{
    public string CustomerId { get; }
    public string ItemId { get; }
    public TradeSatisfaction Satisfaction { get; }
    public int Price { get; }
    public string Race { get; }

    public MonsterTradeCompletedEvent(string customerId, string itemId, TradeSatisfaction satisfaction, int price, string race)
    {
        CustomerId = customerId;
        ItemId = itemId;
        Satisfaction = satisfaction;
        Price = price;
        Race = race;
    }
}

/// <summary>妖怪交易失敗事件。</summary>
public class MonsterTradeFailedEvent
{
    public string CustomerId { get; }
    public string ItemId { get; }
    public TradeSatisfaction Satisfaction { get; }
    public string Race { get; }

    public MonsterTradeFailedEvent(string customerId, string itemId, TradeSatisfaction satisfaction, string race)
    {
        CustomerId = customerId;
        ItemId = itemId;
        Satisfaction = satisfaction;
        Race = race;
    }
}

/// <summary>人類訂單完成事件。</summary>
public class HumanOrderCompletedEvent
{
    public string OrderId { get; }
    public IReadOnlyList<string> ItemIds { get; }
    public int Gold { get; }
    public bool IsLargeOrder { get; }

    public HumanOrderCompletedEvent(string orderId, IReadOnlyList<string> itemIds, int gold, bool isLargeOrder)
    {
        OrderId = orderId;
        ItemIds = itemIds;
        Gold = gold;
        IsLargeOrder = isLargeOrder;
    }
}

/// <summary>刮刮樂結算事件。</summary>
public class ScratchCardCompletedEvent
{
    public int PrizeLevel { get; }
    public int GoldReward { get; }

    public ScratchCardCompletedEvent(int prizeLevel, int goldReward)
    {
        PrizeLevel = prizeLevel;
        GoldReward = goldReward;
    }
}

/// <summary>遊戲日變更事件。</summary>
public class DayChangedEvent
{
    public int Day { get; }

    public DayChangedEvent(int day)
    {
        Day = day;
    }
}

/// <summary>遊戲時段變更事件。</summary>
public class DayPhaseChangedEvent
{
    public DayPhase Phase { get; }

    public DayPhaseChangedEvent(DayPhase phase)
    {
        Phase = phase;
    }
}

/// <summary>每日結算事件。</summary>
public class DayEndedEvent
{
    public int Day { get; }
    public int Gold { get; }

    public DayEndedEvent(int day, int gold)
    {
        Day = day;
        Gold = gold;
    }
}

/// <summary>圖鑑資料變更事件。</summary>
public class BookDataChangedEvent
{
    public bool HasUnsavedChanges { get; }

    public BookDataChangedEvent(bool hasUnsavedChanges)
    {
        HasUnsavedChanges = hasUnsavedChanges;
    }
}

/// <summary>物品圖鑑解鎖事件。</summary>
public class ItemBookUnlockedEvent
{
    public string ItemId { get; }

    public ItemBookUnlockedEvent(string itemId)
    {
        ItemId = itemId;
    }
}

/// <summary>妖怪情報解鎖事件。</summary>
public class MonsterInformationUnlockedEvent
{
    public string InformationId { get; }
    public string MonsterId { get; }

    public MonsterInformationUnlockedEvent(string informationId, string monsterId)
    {
        InformationId = informationId;
        MonsterId = monsterId;
    }
}

/// <summary>妖怪故事解鎖事件。</summary>
public class MonsterStoryUnlockedEvent
{
    public string StoryId { get; }
    public string MonsterId { get; }

    public MonsterStoryUnlockedEvent(string storyId, string monsterId)
    {
        StoryId = storyId;
        MonsterId = monsterId;
    }
}

/// <summary>成就解鎖事件。</summary>
public class AchievementUnlockedEvent
{
    public string AchievementId { get; }
    public string AchievementName { get; }
    public string Description { get; }
    public AchievementLevel Level { get; }

    public AchievementUnlockedEvent(string achievementId, string achievementName, string description, AchievementLevel level)
    {
        AchievementId = achievementId;
        AchievementName = achievementName;
        Description = description;
        Level = level;
    }
}

/// <summary>成就紀念品購買成功事件。</summary>
public class SouvenirPurchasedEvent
{
    public string SouvenirId { get; }
    public int Cost { get; }
    public int RemainingPoints { get; }

    public SouvenirPurchasedEvent(string souvenirId, int cost, int remainingPoints)
    {
        SouvenirId = souvenirId;
        Cost = cost;
        RemainingPoints = remainingPoints;
    }
}
