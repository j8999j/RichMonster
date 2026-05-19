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
public class MissionSaveData : ISaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    public bool IsFinish;
}
public class SpicalItemList : ISaveData
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
public class TutorialSaveData : ISaveData
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
