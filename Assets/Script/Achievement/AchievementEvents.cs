using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Souvenir;

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

public enum AchievementCategory {Item, Transaction, Record, Others, SpecialSouvenir}
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

// --- 共用顯示資料合約 ---
public interface IAchievementDisplayData
{
    string AchievementName { get; }
    string ConditionDescription { get; }
    string Description { get; }
    bool IsCompleted { get; }
    AchievementLevel Level { get; }
    /// <summary>圖示 ID（傳給 SpriteLoader）；成就回傳 null 由 View 依 Level 選圖，特殊紀念品回傳 SouvenirID</summary>
    string IconId { get; }
    /// <summary>是否以灰階顯示圖示；特殊紀念品未解鎖時為 true</summary>
    bool IsIconGrayscale { get; }
}

// --- 顯示元件合約 ---

/// <summary>所有 View 的基底顯示合約</summary>
public interface IAchievementDisplayView
{
    void Bind(IAchievementDisplayData data);
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

// --- 成就類型 / 顯示類型標記介面 ---

/// <summary>標記：此項目的達成條件在未完成前隱藏</summary>
public interface IAchievementHiddenCondition : IAchievementDisplayData { }

/// <summary>標記：此項目有累積進度，需顯示進度條</summary>
public interface IAchievementWithProgress : IAchievementDisplayData
{
    [JsonIgnore] string ProgressText { get; }
    [JsonIgnore] float ProgressRatio { get; } // 0~1
}

// --- 綁定器合約 ---

/// <summary>決定如何將顯示資料刷新至 View</summary>
public interface IAchievementViewBinder
{
    bool CanBind(IAchievementDisplayData data);
    void Refresh(IAchievementDisplayData data, IAchievementDisplayView view);
}

// --- 資料來源合約 ---

/// <summary>特殊紀念品資料來源合約（供 AchievementViewFactory 注入，取代 SouvenirManager.Instance 直接依賴）</summary>
public interface ISpecialSouvenirProvider
{
    IReadOnlyList<ISpecialSouvenirSave> GetAllSpecialSouvenirSaves();
}

#endregion

#region 成就基底類別

public abstract class AchievementBase : IAchievementSave, IDisposable, IAchievementDisplayData
{
    public string AchievementID { get; set; }
    public bool IsCompleted { get; set; }
    public string FinishDay { get; set; }
    [JsonIgnore]public string AchievementName { get; set; }
    [JsonIgnore]public string ConditionDescription { get; set; }
    [JsonIgnore]public string Description { get; set; }
    [JsonIgnore]public AchievementCategory Category { get; set; }
    [JsonIgnore]public AchievementLevel Level { get; set; }
    [JsonIgnore]public virtual string IconId => null; // View 依 Level 從 LevelImageList 選圖
    [JsonIgnore]public bool IsIconGrayscale => false;

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
    public bool CanBind(IAchievementDisplayData data) =>
        data is IAchievementHiddenCondition;

    public void Refresh(IAchievementDisplayData data, IAchievementDisplayView view)
    {
        view.SetNameText(data.AchievementName);
        view.SetConditionText(data.IsCompleted ? data.ConditionDescription : "??????");
        view.SetDescriptionText(data.Description);
    }
}

/// <summary>處理有累積進度的顯示</summary>
public class ProgressBinder : IAchievementViewBinder
{
    public bool CanBind(IAchievementDisplayData data) =>
        data is IAchievementWithProgress;

    public void Refresh(IAchievementDisplayData data, IAchievementDisplayView view)
    {
        var progress = (IAchievementWithProgress)data;
        view.SetNameText(data.AchievementName);
        view.SetConditionText(data.ConditionDescription);
        view.SetDescriptionText(data.Description);

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
    public bool CanBind(IAchievementDisplayData data) => true;

    public void Refresh(IAchievementDisplayData data, IAchievementDisplayView view)
    {
        view.SetNameText(data.AchievementName);
        view.SetConditionText(data.ConditionDescription);
        view.SetDescriptionText(data.Description);
    }
}

#endregion