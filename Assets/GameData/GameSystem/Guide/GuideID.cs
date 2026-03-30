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
    // 面板
    public static class Panel
    {
        public const string GuideRestPanel = "GuideRestPanel";
        public const string GuideConfirmRestPanel = "GuideConfirmRestPanel";
    }

    // 按鈕
    public static class Button
    {
        public const string GuideRest = "GuideRest";
        public const string GuideConfirmAfternoon = "GuideConfirmAfternoon";
    }

    // 對話
    public static class Dialogue
    {
        public const string Task1_FirstTutorial_Dialogue = "Task1_FirstTutorial_Dialogue";
    }
}