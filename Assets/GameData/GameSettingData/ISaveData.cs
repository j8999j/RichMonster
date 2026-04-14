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
    public string UniqueID { get; set; }
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
    public string UniqueID => "TutorialSaveData";
    public int LastUpdatedDay { get; set; }
    public int CurrentTaskIndex;
    public int CurrentStepIndex;
    public bool IsComplete;
    public bool IsPurchased;
}