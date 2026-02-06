using System;
using System.Collections.Generic;
public class GameSaveFile
{
    public Dictionary<string, ISaveData> WorldState = new Dictionary<string, ISaveData>();
}
public interface ISaveData
{
    string UniqueID { get; }
    int LastUpdatedDay { get; }
}
public class NpcMission : ISaveData
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