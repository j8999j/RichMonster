// ============================================================
// 6. 防止 magic string 散落各處
// ============================================================
public static class GuideIDs
{
    // 互動物件
    public static class Interactable
    {
        public const string GuideOrderShop = "GuideOrderShop";
        public const string GuideGroceryStore = "GuideGroceryStore";
        public const string AuctionNpc = "AuctionNpc";
        public const string TrashCan = "TrashCan";
        public const string TelePoint = "TelePoint";
        public const string TelePointAuctionGuide = "TelePointAuctionGuide";
        public const string ScratchCardShop = "ScratchCardShop";
        public const string HumanDoor = "HumanDoor";
        public const string ChangeSceneDoor = "ChangeSceneDoor";
        public const string AbyssShop = "AbyssShop";
        public const string CollectionMissionPrefix = "CollectionMission_";

        public static string CollectionMission(CollectionMissionRace race)
        {
            return $"{CollectionMissionPrefix}{race}";
        }
    }
    // 按鈕
    public static class Button
    {
        public const string GuideRest = "GuideRest";
        public const string GuideConfirmAfternoon = "GuideConfirmAfternoon";
        public const string GuideSouvenirBox = "GuideSouvenirBox";
        public const string GuideUseKey = "GuideUseKey";
        public const string GuideBook = "GuideBook";           // 主圖鑑
        public const string GuideShopBook = "GuideShopBook"; // 商店內圖鑑
        public const string GuideStartReception = "GuideStartReception"; // 開始接待
    }

    // 按鈕 enum（Inspector 下拉用）
    public enum ButtonType
    {
        GuideRest,
        GuideConfirmAfternoon,
        GuideSouvenirBox,
        GuideUseKey,
        GuideBook,
        GuideShopBook,
        GuideStartReception
    }

    public static string ToId(ButtonType type) => type switch
    {
        ButtonType.GuideRest => Button.GuideRest,
        ButtonType.GuideConfirmAfternoon => Button.GuideConfirmAfternoon,
        ButtonType.GuideSouvenirBox => Button.GuideSouvenirBox,
        ButtonType.GuideUseKey => Button.GuideUseKey,
        ButtonType.GuideBook => Button.GuideBook,
        ButtonType.GuideShopBook => Button.GuideShopBook,
        ButtonType.GuideStartReception => Button.GuideStartReception,
        _ => type.ToString()
    };

    // 對話
    public static class Dialogue
    {
        public const string Task1_FirstTutorial_Dialogue = "Task1_FirstTutorial_Dialogue";
    }
}
