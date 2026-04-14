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
    }
    // 按鈕
    public static class Button
    {
        public const string GuideRest = "GuideRest";
        public const string GuideConfirmAfternoon = "GuideConfirmAfternoon";
        public const string GuideSouvenirBox = "GuideSouvenirBox";
        public const string GuideUseKey = "GuideUseKey";
    }

    // 按鈕 enum（Inspector 下拉用）
    public enum ButtonType
    {
        GuideRest,
        GuideConfirmAfternoon,
        GuideSouvenirBox,
        GuideUseKey
    }

    public static string ToId(ButtonType type) => type switch
    {
        ButtonType.GuideRest => Button.GuideRest,
        ButtonType.GuideConfirmAfternoon => Button.GuideConfirmAfternoon,
        ButtonType.GuideSouvenirBox => Button.GuideSouvenirBox,
        ButtonType.GuideUseKey => Button.GuideUseKey,
        _ => type.ToString()
    };

    // 對話
    public static class Dialogue
    {
        public const string Task1_FirstTutorial_Dialogue = "Task1_FirstTutorial_Dialogue";
    }
}