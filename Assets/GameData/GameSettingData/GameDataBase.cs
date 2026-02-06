using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;


// --- 玩家與存檔 ---
[System.Serializable]
public class Inventory
{
    public List<Item> Items = new List<Item>();
}

[System.Serializable]
public class PlayerData : IReadOnlyPlayerData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    public int ID;
    public int MasterSeed;
    public int DaysPlayed;//歷史遊玩天數
    public int CustomerIndex;//當日顧客索引
    public DayPhase PlayingStatus;
    public int Gold;
    public int MonsterGold;
    //玩家倉庫
    public Inventory Inventory = new();
    // 商店庫存變更紀錄
    public List<ShopShelfData> ShopShelves = new List<ShopShelfData>();
    public List<OrderProgress> OrderHistory;
    public TradeProgress TradeHistory;
    public MonsterTradeProgress MonsterTradeHistory;
    public GameSaveFile gameSaveFile;

    //interface
    int IReadOnlyPlayerData.ID => ID;
    int IReadOnlyPlayerData.MasterSeed => MasterSeed;
    int IReadOnlyPlayerData.Gold => Gold;
    int IReadOnlyPlayerData.MonsterGold => MonsterGold;
    int IReadOnlyPlayerData.DaysPlayed => DaysPlayed;
    int IReadOnlyPlayerData.CustomerIndex => CustomerIndex;
    DayPhase IReadOnlyPlayerData.PlayingStatus => PlayingStatus;
    IReadOnlyList<Item> IReadOnlyPlayerData.InventoryItems => Inventory?.Items ?? new List<Item>();
    IReadOnlyList<ShopShelfData> IReadOnlyPlayerData.ShopShelves => ShopShelves ?? new List<ShopShelfData>();
    IReadOnlyList<OrderProgress> IReadOnlyPlayerData.OrderHistory => OrderHistory;
    TradeProgress IReadOnlyPlayerData.TradeHistory => TradeHistory;
    MonsterTradeProgress IReadOnlyPlayerData.MonsterTradeHistory => MonsterTradeHistory;

    GameSaveFile IReadOnlyPlayerData.gameSaveFile => gameSaveFile;

}
[System.Serializable]
public class ItemDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    public ItemType Type { get; set; }
    public Rarity Rarity { get; set; }
    public ItemWorld World { get; set; }
    public int BasePrice { get; set; }
    public string Description { get; set; }
    //可以出現的店ID列表
    public List<string> ShopType { get; set; } = new List<string>();
}
[System.Serializable]
public class Item
{
    public string ItemId;
    public int CostPrice;
}


public class ItemTags
{
    public string TagID;
    public string TagName;
}
public class ItemTagsDatabase
{
    public List<ItemTags> ItemTags;
}
public class ItemDatabase
{
    public List<ItemDefinition> Items;
}

// --- 客人設定 (Customer Definitions) ---
[System.Serializable]
public class TraitDefinition
{
    public string Id;
    public string TraitName;
    public string Description;
    // 互斥標籤
    public string MutexTag;
    public float BudgetModifier = 1.0f;    // 預算乘數
    public float BargainModifier = 1.0f;       // 議價乘數
    public float LoseUp = 0.01f; // 議價失敗後增加的風險(機率)
    public List<EffectDefinition> OtherEffect;          // 其他效果描述
}

public class TraitDatabase
{
    public List<TraitDefinition> Traits;
}
public class TraitEffect
{
    public string TraitEffectType;
    public string Value;
}
public class EffectDefinition
{
    public string EffectType; // e.g., "Patience", "Quality"
    public string Target;     // e.g., "Perfect,Normal" or ""
    public float Value;       // e.g., 2.0 or 0.1
}

[System.Serializable]
public class ProfessionDefinition
{
    public string ProfessionId;
    public string ProfessionName;
    public ProfessionType Type;
    //預算
    public float BaseBudgetMultiplier;
    //議價
    public float BaseAppraisalChance;
    //喜好
    public float BaseBargainingPower;
    //鑑定
    public float IdentificationAbility;
    public string Description;
    // 修改：加入偏好標籤
    public List<string> PreferredTags = new List<string>();
}

public class ProfessionDatabase
{
    public List<ProfessionDefinition> Professions;
}

// --- 商店設定 (Shop Definitions) ---
public class ShopDefinition
{
    // 對應 JSON 的 Id 欄位
    [JsonProperty("Id")]
    public string ShopID;
    public string ShopName;
    public int ShelfCount;
}
public class ShopDatabase
{
    public List<ShopDefinition> Shops;
}
#region Shop & Trade History
[System.Serializable]
public class ShopCategoryDefinition
{
    // 修改：職業 ID 改為 string
    public string ShopID;
    public int Gridindex;
    public int ChangeCount;
}

public class ShopCategoryDatabase
{
    public List<ShopCategoryDefinition> Categories;
}

// --- 存檔相關資料 (Shop Shelf) ---
[System.Serializable]
public class ShopShelfData : ISaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    public string ShopID;
    // 保存庫存變更（增減量與格位）
    public List<ShopInventoryChange> Changes = new List<ShopInventoryChange>();
}

[System.Serializable]
public class ShopInventoryChange
{
    public string ItemId;
    public bool Purchased; // true: 已被購買, false: 尚未購買
    public int SlotIndex = -1;
}
[System.Serializable]
public class MonsterTradeProgress : ISaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    public int CustomerIndex;//當日顧客索引
}
public class OrderProgress : ISaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    public string OrderID;//訂單紀錄索引
    public bool IsCompleted;//是否完成
}
[System.Serializable]
public class TradeProgress : ISaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    public Item NowItem;
    public int CustomerIndex;//當日顧客索引
    public int TradeTimes;//交易次數
    public int MaxPrice;//交易限制最高金額
    public int Patience;//剩餘耐心值
    public bool OnSelect;//是否在選擇中否則在議價中
}
#endregion
#region DataInterFace
public interface IReadOnlyPlayerData
{
    int ID { get; }
    int MasterSeed { get; }
    int Gold { get; }
    int MonsterGold { get; }
    int DaysPlayed { get; }
    int CustomerIndex { get; }
    DayPhase PlayingStatus { get; }
    IReadOnlyList<Item> InventoryItems { get; }
    IReadOnlyList<ShopShelfData> ShopShelves { get; }
    IReadOnlyList<OrderProgress> OrderHistory { get; }
    TradeProgress TradeHistory { get; }
    MonsterTradeProgress MonsterTradeHistory { get; }
    GameSaveFile gameSaveFile { get; }
}
#endregion
#region BookData
public class GameSaveBook //跨單局物品圖鑑與妖怪圖鑑存檔
{
    public ItemBookData ItemBookData;
    public MonsterBookData MonsterBookData;
}
public class MonsterBookData //妖怪圖鑑
{
    public List<MonsterBookDatabase> MonsterBooks;
    public List<MonsterStoryDatabase> monsterStoryDatabases;
}
public class MonsterBookDatabase //妖怪圖鑑妖怪趣聞
{
    public string MonsterID;
    public string InformationID;
    public string MonsterInformation;
}
public class MonsterStoryDatabase //妖怪圖鑑妖怪小故事
{
    public string MonsterID;
    public string MonsterStoryID;
    public int MonsterStory;
}
public class ItemBookDatabase //物品圖鑑已取得過資料
{
    public string ItemID;
    public bool IsBooked;
}
public class ItemBookData //物品圖鑑
{
    public List<ItemBookDatabase> ItemBooks;
}
#endregion
#region MonsterEvents
public class MonsterEvent
{
    public string MonsterID;
    public string EventName;
    public string EventDescription;
    public List<EventTime> EventTimes = new List<EventTime>();
}
#endregion
#region GameEnum
[JsonConverter(typeof(StringEnumConverter))]
public enum DayPhase
{
    HumanDay,   // 人間日
    Night,      // 夜間：妖怪採購
    NightTrade  // 夜間：妖怪交易
}
[JsonConverter(typeof(StringEnumConverter))]
public enum ProfessionType
{
    Regular,    // 一般客人
    Rare,       // 稀有客人
    Rich        // 富豪客人
}
public enum ItemQuality
{
    Good,
    Normal,
    Bad,
    None

}
public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}
public enum ItemWorld
{
    Human,
    Monster
}
public enum ItemType
{
    Equipment,
    Food,
    Prop
}

#endregion