namespace GameSystem
{
    /// <summary>
    /// 全專案 LockPlayerMove / UnlockPlayerMove / IsPlayerMoveLocked 的 source 字串集中地。
    /// 新增互動鎖時在此加入常數，呼叫端統一引用，避免字串打錯。
    /// </summary>
    public static class PlayerLockSources
    {
        public const string Guide           = "Guide";
        public const string TrashCan        = "TrashCan";
        public const string ScratchCardShop = "ScratchCardShop";
        public const string PlayerInfoUI    = "PlayerInfoUI";
        public const string NoticeGetItem   = "NoticeGetItem";
        public const string MonsterTrade    = "MonsterTrade";
        public const string HumanOrderView  = "HumanOrderView";
        public const string NpcOnMap        = "NpcOnMap";
        public const string AbyssShop       = "AbyssShop";
        public const string TalkSystem      = "TalkSystem";
        public const string TelePoint       = "TelePoint";
        public const string AuctionNpc      = "AuctionNpc";

        // 商店類
        public const string GroceryStore    = "GroceryStore";
        public const string YokaiStore      = "YokaiStore";
        public const string FurnituresShop  = "FurnituresShop";
        public const string FoodShop        = "FoodShop";
        public const string VendingMachine  = "VendingMachine";
        public const string YokaiEat        = "YokaiEat";
        public const string HumanShopEat    = "HumanShopEat";
        public const string WanderingYokaiMerchant = "WanderingYokaiMerchant";
    }
}
