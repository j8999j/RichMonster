using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#region Player Data & Inventory
/// <summary>
/// 玩家背包/倉庫資料
/// </summary>
[System.Serializable]
public class Inventory
{
    /// <summary> 背包內存放的物品清單 </summary>
    public List<Item> Items = new List<Item>();
}

/// <summary>
/// 玩家在一局遊戲中的核心資料包含財富、庫存、持有紀念品等。
/// </summary>
[System.Serializable]
public class PlayerData : IReadOnlyPlayerData
{
    /// <summary> 玩家資料 ID </summary>
    public int ID;
    /// <summary> 全域隨機數的種子 </summary>
    public int MasterSeed;
    /// <summary> 此存檔（局）總共遊玩的天數 </summary>
    public int DaysPlayed;
    /// <summary> 當日接待到的顧客索引 </summary>
    public int CustomerIndex;
    /// <summary> 當前的遊戲執行階段（如人界白日、妖界夜晚） </summary>
    public DayPhase PlayingStatus;
    /// <summary> 今日是否與妖怪交易過</summary>
    public bool IsTrade;
    /// <summary> 玩家持有的人界金錢 </summary>
    public int Gold;
    /// <summary> 玩家持有的妖界金錢 </summary>
    public int MonsterGold;
    public bool HasReachedEnding;
    public EndingType ReachedEndingType;
    public bool HasPaidGuaranteeDeposit;
    public bool HasPaidAuctionEntryFee;
    
    /// <summary> 玩家的倉庫/背包資料 </summary>
    public Inventory Inventory = new();
    
    /// <summary> 玩家在「當局」遊戲中持有的紀念品 ID 清單 </summary>
    public List<string> HoldAchievementSouvenirID = new List<string>();

    /// <summary> 該局遊戲的存檔紀錄 </summary>
    public GameSaveFile GameSaveFile = new GameSaveFile();

    // 實作唯讀介面 IReadOnlyPlayerData
    int IReadOnlyPlayerData.ID => ID;
    int IReadOnlyPlayerData.MasterSeed => MasterSeed;
    int IReadOnlyPlayerData.Gold => Gold;
    int IReadOnlyPlayerData.MonsterGold => MonsterGold;
    int IReadOnlyPlayerData.DaysPlayed => DaysPlayed;
    int IReadOnlyPlayerData.CustomerIndex => CustomerIndex;
    bool IReadOnlyPlayerData.IsTrade => IsTrade;
    bool IReadOnlyPlayerData.HasReachedEnding => HasReachedEnding;
    bool IReadOnlyPlayerData.HasPaidGuaranteeDeposit => HasPaidGuaranteeDeposit;
    bool IReadOnlyPlayerData.HasPaidAuctionEntryFee => HasPaidAuctionEntryFee;
    DayPhase IReadOnlyPlayerData.PlayingStatus => PlayingStatus;
    EndingType IReadOnlyPlayerData.ReachedEndingType => ReachedEndingType;
    IReadOnlyList<Item> IReadOnlyPlayerData.InventoryItems => Inventory?.Items ?? new List<Item>();
    IReadOnlyList<string> IReadOnlyPlayerData.HoldAchievementSouvenirID => HoldAchievementSouvenirID ?? new List<string>();
}

/// <summary>
/// 提供外界安全讀取玩家核心資料的唯讀介面
/// </summary>
public interface IReadOnlyPlayerData
{
    int ID { get; }
    int MasterSeed { get; }
    int Gold { get; }
    int MonsterGold { get; }
    int DaysPlayed { get; }
    int CustomerIndex { get; }
    DayPhase PlayingStatus { get; }
    bool IsTrade { get; }
    bool HasReachedEnding { get; }
    bool HasPaidGuaranteeDeposit { get; }
    bool HasPaidAuctionEntryFee { get; }
    EndingType ReachedEndingType { get; }
    IReadOnlyList<Item> InventoryItems { get; }
    IReadOnlyList<string> HoldAchievementSouvenirID { get; }
}
#endregion

#region Item Settings
/// <summary>
/// 定義遊戲中基礎物品的靜態屬性資料（對應 JSON）
/// </summary>
[System.Serializable]
public class ItemDefinition
{
    /// <summary> 物品唯一識別碼 </summary>
    public string Id { get; set; }
    /// <summary> 物品的顯示名稱 </summary>
    public string Name { get; set; }
    /// <summary> 物品擁有的特性標籤 </summary>
    public List<string> Tags { get; set; } = new List<string>();
    /// <summary> 物品類型（裝備、食物、道具等） </summary>
    public ItemType Type { get; set; }
    /// <summary> 物品稀有度 </summary>
    public Rarity Rarity { get; set; }
    /// <summary> 物品所屬世界（人界、妖界） </summary>
    public ItemWorld World { get; set; }
    /// <summary> 物品的基礎售價 </summary>
    public int BasePrice { get; set; }
    /// <summary> 物品的描述敘述 </summary>
    public string Description { get; set; }
    /// <summary> 可以在哪些商店出現的 ID 列表 </summary>
    public List<string> ShopType { get; set; } = new List<string>();
}

/// <summary>
/// 玩家實際持有的物品實例資料
/// </summary>
[System.Serializable]
public class Item
{
    /// <summary> 對應 ItemDefinition 中的 Id </summary>
    public string ItemId;
    /// <summary> 玩家買入該物品時的成本價格 </summary>
    public int CostPrice;
}

/// <summary>
/// 物品標籤的基礎定義資料
/// </summary>
public class ItemTags
{
    /// <summary> 標籤唯一識別碼 </summary>
    public string TagID;
    /// <summary> 標籤顯示名稱 </summary>
    public string TagName;
}

/// <summary> 物品標籤資料庫 (對應 JSON 結構) </summary>
public class ItemTagsDatabase
{
    public List<ItemTags> ItemTags;
}

/// <summary> 物品資料庫 (對應 JSON 結構) </summary>
public class ItemDatabase
{
    public List<ItemDefinition> Items;
}
#endregion
#region Shop & Trade History
/// <summary>
/// 商店基本資料定義
/// </summary>
public class ShopDefinition
{
    /// <summary> 對應 JSON 的 Id 欄位，也就是商店的唯一識別碼 </summary>
    [JsonProperty("Id")]
    public string ShopID;
    /// <summary> 商店名稱 </summary>
    public string ShopName;
    /// <summary> 商店擁有的預設貨架數量 </summary>
    public int ShelfCount;
}

/// <summary> 商店資料庫 (對應 JSON 結構) </summary>
public class ShopDatabase
{
    public List<ShopDefinition> Shops;
}

/// <summary>
/// 商店分類佈局與換貨定義
/// </summary>
[System.Serializable]
public class ShopCategoryDefinition
{
    /// <summary> 關聯的商店 ID </summary>
    public string ShopID;
    /// <summary> 所在的網格位置或索引 </summary>
    public int Gridindex;
    /// <summary> 可更換或刷新商品的次數上限 </summary>
    public int ChangeCount;
}

/// <summary> 商店分類資料庫 (對應 JSON 結構) </summary>
public class ShopCategoryDatabase
{
    public List<ShopCategoryDefinition> Categories;
}

// --- 存檔相關資料 (Shop Shelf) ---
/// <summary>
/// 商店貨架的存檔資料，紀錄各個商店當局的商品購買狀態
/// </summary>
[System.Serializable]
public class ShopShelfData : ISaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    
    /// <summary> 保存庫存變更紀錄（增減量與所在格位） </summary>
    public List<ShopInventoryChange> Changes = new List<ShopInventoryChange>();
}

/// <summary>
/// 紀錄單個商品在貨架上的異動狀態
/// </summary>
[System.Serializable]
public class ShopInventoryChange
{
    /// <summary> 異動的物品 ID </summary>
    public string ItemId;
    /// <summary> 是否已被購買 (true: 已被購買, false: 尚未購買) </summary>
    public bool Purchased; 
    /// <summary> 該商品所在的貨架格位索引 </summary>
    public int SlotIndex = -1;
}

/// <summary>
/// 妖怪交易的進度存檔資料
/// </summary>
[System.Serializable]
public class MonsterTradeProgress : ISaveData
{
    public string UniqueID { get; set; } = SaveDataKeys.MonsterTradeProgress;
    public int LastUpdatedDay { get; set; }
    
    /// <summary> 當日顧客排隊或接待的索引進度 </summary>
    public int CustomerIndex;
}

/// <summary>
/// 玩家的訂單歷史與進度紀錄存檔
/// </summary>
public class OrderHistoryData : ISaveData
{
    public string UniqueID { get; set; } = SaveDataKeys.OrderHistory;
    public int LastUpdatedDay { get; set; }
    
    /// <summary> 歷史訂單的完成狀態清單 </summary>
    public List<OrderProgress> OrderHistory = new List<OrderProgress>();
}

/// <summary>
/// 單筆訂單的完成進度
/// </summary>
public class OrderProgress
{
    /// <summary> 訂單紀錄的唯一識別碼 </summary>
    public string OrderID;
    /// <summary> 是否已完成 </summary>
    public bool IsCompleted;
}

/// <summary>
/// 單次交易的詳細進度與狀態存檔
/// </summary>
[System.Serializable]
public class TradeProgress : ISaveData
{
    public string UniqueID { get; set; }
    public int LastUpdatedDay { get; set; }
    
    /// <summary> 當前正在交易的物品實例 </summary>
    public Item NowItem;
    /// <summary> 當前的顧客索引 </summary>
    public int CustomerIndex;
    /// <summary> 玩家的剩餘交易（報價）次數 </summary>
    public int TradeTimes;
    /// <summary> 本次交易限制的最高金額 </summary>
    public int MaxPrice;
    /// <summary> 顧客的剩餘耐心值 </summary>
    public int Patience;
    /// <summary> 是否處於選物階段 (true = 選擇商品中, false = 議價中) </summary>
    public bool OnSelect;
}
#endregion

#region BookData
/// <summary>
/// 跨單局的整體圖鑑與成就進度存檔資料
/// </summary>
public class GameSaveBook
{
    /// <summary> 物品圖鑑的解鎖進度 </summary>
    public ItemBookData ItemBookData;
    /// <summary> 妖怪圖鑑與趣聞故事的解鎖進度 </summary>
    public MonsterBookData MonsterBookData;
    /// <summary> 成就列表的進度與狀態資料 </summary>
    public List<IAchievementSave> AchievementData;
    /// <summary> 永久解鎖的成就紀念品 ID 列表 </summary>
    public List<string> UnLockAchievementSouvenirID;
    /// <summary> 永久解鎖的特殊紀念品 ID 列表 </summary>
    public List<string> UnLockSpecialSouvenirID;
    /// <summary> 特殊紀念品的獨立進度追蹤資料 </summary>
    public List<Souvenir.ISpecialSouvenirSave> SpecialSouvenirProgressData;
}

/// <summary>
/// 妖怪圖鑑存檔資料
/// </summary>
public class MonsterBookData
{
    /// <summary> 已解鎖的妖怪情報資訊 ID 列表 </summary>
    public List<string> UnlockMonsterInformationID;
    /// <summary> 新解鎖但尚未在圖鑑畫面中被玩家確認的情報 ID </summary>
    public List<string> NewMonsterInformationID;
    /// <summary> 新解鎖但尚未在圖鑑畫面中被玩家確認的故事 ID </summary>
    public List<string> NewMonsterStoryID;
}

/// <summary>
/// 妖怪圖鑑 - 妖怪情報/趣聞定義
/// </summary>
public class MonsterInformationDatabase
{
    public string MonsterID;
    public string InformationID;
    public string MonsterInformationName;
    public string MonsterInformation;
    public string TagID;
}

/// <summary>
/// 妖怪圖鑑 - 妖怪小故事定義
/// </summary>
public class MonsterStoryDatabase
{
    public string MonsterID;
    public int StoryIndex;
    public string MonsterStoryID;
    public string MonsterStoryName;
    public string MonsterStory;
}

/// <summary> 妖怪情報資料庫 (對應 JSON 結構) </summary>
public class MonsterInformationDatabaseRoot
{
    public List<MonsterInformationDatabase> MonsterInformations;
}

/// <summary> 妖怪故事資料庫 (對應 JSON 結構) </summary>
public class MonsterStoryDatabaseRoot
{
    public List<MonsterStoryDatabase> MonsterStories;
}

/// <summary>
/// 單件物品圖鑑的解鎖紀錄
/// </summary>
public class ItemBookDatabase
{
    /// <summary> 物品唯一識別碼 </summary>
    public string ItemID;
    /// <summary> 該物品是否曾被玩家取得過並登錄至圖鑑 </summary>
    public bool IsBooked;
}

/// <summary>
/// 物品圖鑑存檔集合
/// </summary>
public class ItemBookData
{
    /// <summary> 紀錄各物品登錄狀態的列表 </summary>
    public List<ItemBookDatabase> ItemBooks;
}
#endregion
#region Souvenir
/// <summary>
/// 靜態的成就紀念品設定資料 (對應 JSON)
/// </summary>
public class AchievementSouvenirData
{
    /// <summary> 紀念品唯一識別碼 </summary>
    public string SouvenirID;
    /// <summary> 紀念品名稱 </summary>
    public string SouvenirName;
    /// <summary> 紀念品敘述 </summary>
    public string SouvenirDescription;
    /// <summary> 紀念品具體功能的文字說明 </summary>
    public string SouvenirFunctionDescription;
    /// <summary> 購買所需消耗的成就點數 </summary>
    public int PointsFee;  
}

/// <summary>
/// 靜態的特殊紀念品設定資料 (對應 JSON)
/// </summary>
public class SpecialSouvenirData
{
    /// <summary> 特殊紀念品唯一識別碼 </summary>
    public string SouvenirID;
    /// <summary> 紀念品名稱 </summary>
    public string SouvenirName;
    /// <summary> 解鎖獲得的條件說明 </summary>
    public string SouvenirCondition;
    /// <summary> 紀念品敘述 </summary>
    public string SouvenirDescription;
}

/// <summary> 成就紀念品資料庫 (對應 JSON 結構) </summary>
public class AchievementSouvenirDatabaseRoot
{
    public List<AchievementSouvenirData> AchievementSouvenirs;
}

/// <summary> 特殊紀念品資料庫 (對應 JSON 結構) </summary>
public class SpecialSouvenirDatabaseRoot
{
    public List<SpecialSouvenirData> SpecialSouvenirs;
}
#endregion

#region MonsterEvents
/// <summary>
/// 妖怪特殊的事件資料定義
/// </summary>
public class MonsterEvent
{
    /// <summary> 觸發事件的妖怪 ID </summary>
    public string MonsterID;
    /// <summary> 事件名稱 </summary>
    public string EventName;
    /// <summary> 事件描述 </summary>
    public string EventDescription;
    /// <summary> 發生事件的時機/時間列表 </summary>
    public List<EventTime> EventTimes = new List<EventTime>();
}
#endregion

#region GameEnum
[JsonConverter(typeof(StringEnumConverter))]
public enum DayPhase
{
    HumanDay,    // 人間日
    AfterNoon,   // 人間午後
    Night        // 夜間：妖怪採購
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
public enum TradeSatisfaction
{
    Hated,
    Okay,
    Satisfied,
    VerySatisfied
}
#endregion
