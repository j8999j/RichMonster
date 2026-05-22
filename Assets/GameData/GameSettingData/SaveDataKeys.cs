/// <summary>
/// 集中管理所有 ISaveData 類型使用的存檔 ID。
/// 固定資料請使用 const；依資料來源產生的動態資料請使用 Build* 方法。
/// </summary>
public static class SaveDataKeys
{
    /// <summary>特殊商品購買狀態清單。</summary>
    public const string SpicalItemList = "SpicalItemList";

    /// <summary>教學與引導流程進度。</summary>
    public const string Tutorial = "TutorialSaveData";

    /// <summary>怪物交易流程進度。</summary>
    public const string MonsterTradeProgress = "MonsterTradeHistory";

    /// <summary>當日人類訂單完成紀錄。</summary>
    public const string OrderHistory = "OrderHistory";

    /// <summary>收藏任務單局長期進度。</summary>
    public const string CollectionMission = "CollectionMissionProgress";

    /// <summary>每日妖怪包裹生成與拾取狀態。</summary>
    public const string YokaiPackage = "YokaiPackage";

    /// <summary>每日流浪妖怪商人生成狀態。</summary>
    public const string WanderingYokaiMerchant = "WanderingYokaiMerchant";

    /// <summary>每日深淵商店探索狀態。</summary>
    public const string Abyss = "AbyssShop";

    /// <summary>每日刮刮樂商店狀態。</summary>
    public const string ScratchCardShop = "ScratchCardShopData";

    /// <summary>紀念品商店購買狀態。</summary>
    public const string SouvenirShop = "SouvenirShopSaveData";

    /// <summary>雜貨店買十送一紀念品累積進度。</summary>
    public const string GroceryPurchase = "GroceryPurchaseSaveData";

    /// <summary>商店貨架每日狀態 ID 後綴。</summary>
    public const string ShopShelfSuffix = "ShopShelfData";

    /// <summary>建立指定商店的每日貨架狀態 ID。</summary>
    public static string BuildShopShelf(string shopId) => shopId + ShopShelfSuffix;

    /// <summary>建立指定任務的存檔 ID。</summary>
    public static string BuildMission(string missionId) => missionId;

    /// <summary>建立指定 NPC 任務的每日完成狀態 ID。</summary>
    public static string BuildNPCMission(string missionId) => missionId;

    /// <summary>建立指定交易流程的存檔 ID。</summary>
    public static string BuildTrade(string tradeId) => tradeId;
}
