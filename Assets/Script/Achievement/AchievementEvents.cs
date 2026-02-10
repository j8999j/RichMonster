using System;
using System.Collections.Generic;
using UnityEngine;

public class AchievementEvents
{
    // 當獲得物品時觸發(參數: 物品ID)
    public static event Action<string> OnItemObtained;
    public static void GetItem(string itemId) => OnItemObtained?.Invoke(itemId);
    // 當交易完成時觸發(參數: 顧客ID, 賣出的物品ID)
    public static event Action<string, string> OnTransactionCompleted;
    public static void TradeItem(string customerId, string itemId) => OnTransactionCompleted?.Invoke(customerId, itemId);
}
public abstract class AchievementBase : IAchievementSave, IAchievementViewBase, IDisposable
{
    // 這是邏輯層的 Key，用來跟 JSON 對應
    public string AchievementID{ get; set;}
    public bool IsCompleted { get; set;}
    public string FinishDay { get; set;}
    public string AchievementName{ get; set;}
    public string ConditionDescription{ get; set;}
    public string Description{ get; set;}
    public AchievementCategory Category;
    public AchievementLevel Level;
    protected abstract void SubscribeEvents();
    protected abstract void UnsubscribeEvents();
    public event Action<AchievementBase> OnUnlocked;

    // 初始化：讓成就自己決定要監聽什麼事件
    public virtual void Initialize()
    {
        //LoadConfig(AchievementManager.Instance.GetAchievementConfig(Key));
        SubscribeEvents();
    }
    public void LoadConfig(AchievementConfig config)
    {
        AchievementID = config.AchievementID;
        AchievementName = config.AchievementName;
        ConditionDescription = config.ConditionDescription;
        Description = config.Description;
        Category = config.Category;
        Level = config.Level;
    }
    // 當達成條件時呼叫此方法
    protected virtual void CompletedAchievement()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        Debug.Log($"成就解鎖:{AchievementName}");
        // 通知外部
        OnUnlocked?.Invoke(this);
        // 解鎖後，通常就不需要再監聽了
        UnsubscribeEvents();
    }
    public void Dispose()
    {
        UnsubscribeEvents();
        OnUnlocked = null; // 清空訂閱者
    }
}
public class AchievementConfig
{
    public string AchievementID;
    public string AchievementName;
    public string ConditionDescription;
    public string Description;
    public AchievementCategory Category;
    public AchievementLevel Level;
}
public enum AchievementCategory { Item, Transaction, Exploration }
public enum AchievementLevel { Bronze, Silver, Gold }
public class AchievementDatabase
{
    public List<AchievementConfig> Achievements;
}
public interface IAchievementSave
{
    public string AchievementID { get; set;}
    public bool IsCompleted { get; set;}
    public string FinishDay { get; set;}
    public void Initialize();
}
public interface IAchievementViewBase
{
    public string AchievementName{ get; set;}
    public string ConditionDescription{ get; set;}
    public string Description{ get; set;}
}
public interface IAchievementViewProgress : IAchievementViewBase
{
    string ProgressText { get; }
}

