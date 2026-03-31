// ============================================================
// Task1_FirstTutorial.cs - 任務一：起始教學
// ============================================================
using System.Collections.Generic;

public class Task1_FirstTutorial : GuideTask
{
    //任務一：
    //步驟一：進入對話模式
    //步驟二：對話結束後提示前往與雜貨店互動(開始監聽步驟四)
    //步驟三：雜貨店互動結束後給予獎勵(剩餘的庫存)
    //步驟四：在商店中購買任一項物品(如果已完成跳過進行步驟五)
    //步驟五：提示與雜貨店互動(準備切換到午後)
    //步驟六：與雜貨店互動後，強制提示"點擊休息一下"(開啟面板)
    //步驟七：強制提示"點擊確定"(切換到午後)
    public override string TaskName => "任務一：起始教學";
    private bool IsPurchased;
    // 步驟二執行時啟動，提早監聽「購買物品」
    private BackgroundListener earlyPurchaseListener = new BackgroundListener();
    protected override List<GuideStep> BuildSteps()
    {
        return new List<GuideStep>
        {
            // 步驟一：進入對話模式
            new ForceDialogueStep(
                GuideIDs.Dialogue.Task1_FirstTutorial_Dialogue),
            // 步驟二：對話結束後提示前往雜貨店
            // 同時啟動背景監聽「購買物品」(提早監聽步驟四)
            new ShowHintAndWaitStep(
                "前往爺爺的雜貨店查看",
                new InteractWithObjectListener(GuideIDs.Interactable.GuideOrderShop),
                onExecuteCallback: () =>
                    earlyPurchaseListener.StartEarly(new PurchaseItemListener())),

            // 步驟三：雜貨店互動結束後給予獎勵（剩餘庫存）
            new GiveRewardStep(
                () =>
                {
                    Step_3_Reward();
                }),
            // 步驟四：購買任一物品（提早買過 or 已有購買紀錄則跳過）
            new SkippableListenStep(
                "請在其他商店中購買任一物品",
                listener:           null,
                skipCondition:      () => IsPurchased,
                backgroundListener: earlyPurchaseListener),

            // 步驟五：提示再次與雜貨店互動（準備切換到午後）
            new ShowHintAndWaitStep(
                "前往爺爺的雜貨店，休息一下",
                new InteractWithObjectListener(GuideIDs.Interactable.GuideOrderShop)),

            // 步驟六：強制提示點擊「休息一下」（開啟面板）
            new ForceUIButtonStep(
                GuideIDs.Panel.GuideRestPanel,
                GuideIDs.Button.GuideRest,
                "點擊「休息一下」"),

            // 步驟七：強制提示點擊「確定」（切換到午後）
            new ForceUIButtonStep(
                GuideIDs.Panel.GuideConfirmRestPanel,
                GuideIDs.Button.GuideConfirmAfternoon,
                "點擊「確定」切換到午後"),
        };
    }
    private void Step_3_Reward()
    {
        var itemIDs = DataManager.Instance.GetRandomDistinctItemIds(ItemWorld.Human, Rarity.Common, 3);
        foreach (var itemID in itemIDs)
        {
            DataManager.Instance.AddItem(itemID, 0);
        }
        List<NoticeItemEntry> noticeItems = new List<NoticeItemEntry>();
        foreach (var itemID in itemIDs)
        {
            noticeItems.Add(NoticeItemEntry.ItemEntry(itemID,1));
        }
        NoticeGetItemEvents.InvokeShowNotice("爺爺的庫存", noticeItems);
    }
}