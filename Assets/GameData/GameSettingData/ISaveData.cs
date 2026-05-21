using System.Collections.Generic;
public class GameSaveFile
{
    public Dictionary<string, ISaveData> GameData = new Dictionary<string, ISaveData>();
}
public interface ISaveData
{
    string UniqueID { get; }
    int LastUpdatedDay { get; }
}

/// <summary>
/// 每日重置資料：讀取時若 LastUpdatedDay 不是目前天數，就回傳新的空資料。
/// </summary>
public interface IDailySaveData : ISaveData { }

/// <summary>
/// 單局長期資料：同一輪遊戲期間持續保存，不因換日自動重置。
/// </summary>
public interface IRunSaveData : ISaveData { }

/// <summary>
/// 流程暫存資料：服務於當前交易、訂單、切場等流程，是否清除由流程控制。
/// </summary>
public interface IFlowSaveData : ISaveData { }

public class MissionSaveData : IDailySaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    public bool IsFinish;
}
public class SpicalItemList : IDailySaveData
{
    public string UniqueID { get; set; } = SaveDataKeys.SpicalItemList;
    public int LastUpdatedDay { get; set; }
    public List<SpicalItem> PurchasedItemsList;
}
public class SpicalItem
{
    public string ShopID;
    public string ItemID;
    public bool Purchased;
}
public class TutorialSaveData : IRunSaveData
{
    public string UniqueID => SaveDataKeys.Tutorial;
    public int LastUpdatedDay { get; set; }
    public int CurrentTaskIndex;
    public int CurrentStepIndex;
    public string CurrentStepId;
    public bool IsComplete;
    public bool IsPurchased;
    public bool Task2SecondRewardClaimed;
}
