/// <summary>
/// 集中管理所有 ISaveData 類型使用的存檔 ID。
/// 固定 ID 請在此宣告常數；需要依物件 ID 組合的動態 ID，請在此提供產生方法。
/// </summary>
public static class SaveDataKeys
{
    /// <summary>特殊商品清單存檔 ID。保留既有拼字以維持舊存檔相容。</summary>
    public const string SpicalItemList = "SpicalItemList";

    /// <summary>新手教學流程存檔 ID。</summary>
    public const string Tutorial = "TutorialSaveData";

    /// <summary>妖怪交易進度存檔 ID。</summary>
    public const string MonsterTradeProgress = "MonsterTradeHistory";

    /// <summary>人類訂單完成紀錄存檔 ID。</summary>
    public const string OrderHistory = "OrderHistory";

    /// <summary>收集任務進度存檔 ID。</summary>
    public const string CollectionMission = "CollectionMissionProgress";

    /// <summary>妖怪包裹每日生成與拾取狀態存檔 ID。</summary>
    public const string YokaiPackage = "YokaiPackage";

    /// <summary>流浪妖怪商人每日生成狀態存檔 ID。</summary>
    public const string WanderingYokaiMerchant = "WanderingYokaiMerchant";

    /// <summary>深淵商店每日遊玩狀態存檔 ID。</summary>
    public const string Abyss = "AbyssShop";

    /// <summary>刮刮樂商店每日狀態存檔 ID。</summary>
    public const string ScratchCardShop = "ScratchCardShopData";

    /// <summary>紀念品商店購買紀錄存檔 ID。</summary>
    public const string SouvenirShop = "SouvenirShopSaveData";

    /// <summary>雜貨店買十送一紀念品累計購買數存檔 ID。</summary>
    public const string GroceryPurchase = "GroceryPurchaseSaveData";

    /// <summary>商店貨架存檔 ID 後綴；完整 ID 由商店 ID 加上此後綴組成。</summary>
    public const string ShopShelfSuffix = "ShopShelfData";

    /// <summary>建立商店貨架存檔 ID。</summary>
    public static string BuildShopShelf(string shopId) => shopId + ShopShelfSuffix;

    /// <summary>建立一般任務存檔 ID；目前直接使用任務 ID。</summary>
    public static string BuildMission(string missionId) => missionId;

    /// <summary>建立 NPC 任務存檔 ID；目前直接使用任務 ID。</summary>
    public static string BuildNPCMission(string missionId) => missionId;

    /// <summary>建立交易存檔 ID；目前直接使用交易 ID。</summary>
    public static string BuildTrade(string tradeId) => tradeId;
}
