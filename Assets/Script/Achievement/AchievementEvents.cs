using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

#region 事件層

public class AchievementEvents
{
    // 當獲得物品時觸發 (參數: 物品ID)
    public static event Action<string> OnItemObtained;
    public static void GetItem(string itemId) => OnItemObtained?.Invoke(itemId);

    // 當交易完成時觸發 (參數: 顧客ID, 賣出的物品ID)
    public static event Action<string, string> OnTransactionCompleted;
    public static void TradeItem(string customerId, string itemId) => OnTransactionCompleted?.Invoke(customerId, itemId);

    // 當訂單完成時觸發 (參數: 訂單ID, 物品ID列表, 金幣)
    public static event Action<string, List<string>, int> OnOrderCompleted;
    public static void CompleteOrder(string orderId, List<string> itemIds, int gold) => OnOrderCompleted?.Invoke(orderId, itemIds, gold);
    // 金幣數量改變時觸發 (參數: 目前金幣, 變動量)
    public static event Action<int, int> OnGoldChanged;
    public static void GoldChanged(int gold, int goldChange) => OnGoldChanged?.Invoke(gold, goldChange);
    // 刮刮樂結算時觸發 (參數: 獎項等級)
    public static event Action<int> OnScratchCardCompleted;
    public static void ScratchCardCompleted(int prizeLevel) => OnScratchCardCompleted?.Invoke(prizeLevel);
    // 結束一天時觸發 (參數: 當前金幣)
    public static event Action<int> OnDayEndGold;
    public static void DayEndGold(int gold) => OnDayEndGold?.Invoke(gold);
}

#endregion

#region 設定 / 資料層

public class AchievementConfig
{
    public string AchievementID;
    public string AchievementName;
    public string ConditionDescription;
    public string Description;
    public AchievementCategory Category;
    public AchievementLevel Level;
}

public class AchievementDatabase
{
    public List<AchievementConfig> Achievements;
}

public enum AchievementCategory {Item, Transaction, Record, Others}
public enum AchievementLevel {Bronze, Silver, Gold}

#endregion

#region 介面層

// --- 儲存合約 ---
public interface IAchievementSave
{
    string AchievementID { get; set; }
    bool IsCompleted { get; set; }
    string FinishDay { get; set; }
    void Initialize();
}

// --- 顯示合約 ---

/// <summary>所有 View 的基底顯示合約</summary>
public interface IAchievementDisplayView
{
    void Bind(AchievementBase achievement);
    void Refresh();
    void Unbind();
    void SetNameText(string text);
    void SetConditionText(string text);
    void SetDescriptionText(string text);
}

/// <summary>有進度條的 View 合約</summary>
public interface IAchievementProgressView : IAchievementDisplayView
{
    void SetProgressText(string text);
    void SetProgressFloat(float progress); // 0~1
}

// --- 成就類型標記介面（由成就類別實作，宣告自己需要哪種顯示方式）---

/// <summary>標記：此成就的達成條件在未完成前隱藏</summary>
public interface IAchievementHiddenCondition { }

/// <summary>標記：此成就有累積進度，需顯示進度條</summary>
public interface IAchievementWithProgress
{
    [JsonIgnore] string ProgressText { get; }
    [JsonIgnore] float ProgressRatio { get; } // 0~1
}

// --- 綁定器合約 ---

/// <summary>決定如何將成就資料刷新至 View</summary>
public interface IAchievementViewBinder
{
    bool CanBind(AchievementBase achievement);
    void Refresh(AchievementBase achievement, IAchievementDisplayView view);
}

#endregion

#region 成就基底類別

public abstract class AchievementBase : IAchievementSave, IDisposable
{
    public string AchievementID { get; set; }
    public bool IsCompleted { get; set; }
    public string FinishDay { get; set; }
    [JsonIgnore]public string AchievementName { get; set; }
    [JsonIgnore]public string ConditionDescription { get; set; }
    [JsonIgnore]public string Description { get; set; }
    [JsonIgnore]public AchievementCategory Category { get; set; }
    [JsonIgnore]public AchievementLevel Level { get; set; }

    public event Action<AchievementBase> OnUnlocked;
    public event Action OnViewUpdated;

    protected abstract void SubscribeEvents();
    protected abstract void UnsubscribeEvents();
    protected abstract void SaveData();
    public virtual void Initialize()
    {
        SubscribeEvents();
    }

    public void LoadConfig(AchievementConfig config)
    {
        AchievementID        = config.AchievementID;
        AchievementName      = config.AchievementName;
        ConditionDescription = config.ConditionDescription;
        Description          = config.Description;
        Category             = config.Category;
        Level                = config.Level;
    }

    protected virtual void CompletedAchievement()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        OnUnlocked?.Invoke(this);
    }
    protected void NotifyViewUpdated() => OnViewUpdated?.Invoke();

    public void Dispose()
    {
        UnsubscribeEvents();
        OnUnlocked = null;
    }
}

#endregion

#region 綁定器實作

/// <summary>處理隱藏達成條件的顯示（未完成前顯示 ???）</summary>
public class HiddenConditionBinder : IAchievementViewBinder
{
    public bool CanBind(AchievementBase achievement) =>
        achievement is IAchievementHiddenCondition;

    public void Refresh(AchievementBase achievement, IAchievementDisplayView view)
    {
        view.SetNameText(achievement.AchievementName);
        view.SetConditionText(achievement.IsCompleted ? achievement.ConditionDescription : "??????");
        view.SetDescriptionText(achievement.Description);
    }
}

/// <summary>處理有累積進度的顯示</summary>
public class ProgressBinder : IAchievementViewBinder
{
    public bool CanBind(AchievementBase achievement) =>
        achievement is IAchievementWithProgress;

    public void Refresh(AchievementBase achievement, IAchievementDisplayView view)
    {
        var progress = (IAchievementWithProgress)achievement;
        view.SetNameText(achievement.AchievementName);
        view.SetConditionText(achievement.ConditionDescription);
        view.SetDescriptionText(achievement.Description);

        if (view is IAchievementProgressView progressView)
        {
            progressView.SetProgressText(progress.ProgressText);
            progressView.SetProgressFloat(progress.ProgressRatio);
        }
    }
}

/// <summary>預設顯示</summary>
public class DefaultBinder : IAchievementViewBinder
{
    public bool CanBind(AchievementBase achievement) => true;

    public void Refresh(AchievementBase achievement, IAchievementDisplayView view)
    {
        view.SetNameText(achievement.AchievementName);
        view.SetConditionText(achievement.ConditionDescription);
        view.SetDescriptionText(achievement.Description);
    }
}

#endregion