// ============================================================
// Task1_FirstTutorial.cs - 任務一：起始教學
// ============================================================
using System.Collections.Generic;
using UnityEngine;

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
    public bool IsPurchased;
    // 步驟二執行時啟動，提早監聽「購買物品」
    private BackgroundListener earlyPurchaseListener = new BackgroundListener();
    // 緩存「休息一下」按鈕的 GameObject，因為隱藏後會從 Registry 註銷
    private GameObject _restButtonObj;
    private GameObject _bookButtonObj;
    
    private void HideTutorialDisabledButtons()
    {
        if (GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideRest, out var restBtn))
        {
            _restButtonObj = restBtn.ButtonObject;
            if (_restButtonObj != null) _restButtonObj.SetActive(false);
        }
        if (GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideBook, out var bookBtn))
        {
            _bookButtonObj = bookBtn.ButtonObject;
            if (_bookButtonObj != null) _bookButtonObj.SetActive(false);
        }
    }

    protected override List<GuideStep> BuildSteps()
    {
        earlyPurchaseListener.OnTriggered = () => IsPurchased = true;
        return new List<GuideStep>
        {
            // 步驟零：隱藏教學期間暫時不讓玩家點擊的按鈕
            new GiveRewardStep(() =>
            {
                HideTutorialDisabledButtons();
            }),
            // 步驟一：進入對話模式
            new ForceDialogueStep(
                GuideIDs.Dialogue.Task1_FirstTutorial_Dialogue),
            // 步驟二：對話結束後提示前往雜貨店
            // 同時啟動背景監聽「購買物品」(提早監聽步驟四)
            new WithMapGuideStep(
            inner: new ShowHintAndWaitStep(
                "前往爺爺的雜貨店查看",
                new InteractWithObjectListener(GuideIDs.Interactable.GuideOrderShop),
                onExecuteCallback: () =>
                    earlyPurchaseListener.StartEarly(new PurchaseItemListener())),
            targetId: GuideIDs.Interactable.GuideOrderShop),
            //  ↑ 到達雜貨店後自動清除地圖點位

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
            new WithMapGuideStep(
            inner: new ShowHintAndWaitStep(
                "回到爺爺的雜貨店，休息一下",
                new InteractWithObjectListener(GuideIDs.Interactable.GuideOrderShop)),
            targetId: GuideIDs.Interactable.GuideOrderShop),
            // 步驟六前置：顯示「休息一下」按鈕
            new GiveRewardStep(() =>
            {
                if (_restButtonObj != null) _restButtonObj.SetActive(true);
            }),
            // 步驟六：強制提示點擊「休息一下」（開啟面板）— 鎖定玩家
            new WithPlayerLockedStep(
            new ForceUIButtonStep(
                new Vector2(-322,-115), // TODO: 填入「休息一下」按鈕的螢幕座標
                GuideIDs.Button.GuideRest,
                "點擊躺椅，休息一下")),
            // 步驟七：強制提示點擊「確定」（切換到午後）— 鎖定玩家
            new WithPlayerLockedStep(
            new ForceUIButtonStep(
                new Vector2(-171.8f,-122.8f), // TODO: 填入「確定」按鈕的螢幕座標
                GuideIDs.Button.GuideConfirmAfternoon,
                "點擊「確定」切換到午後")),
            // 步驟八：強制提示點擊紀念品箱 — 鎖定玩家
            new WithPlayerLockedStep(
            new ForceUIButtonStep(
                new Vector2(-880,-265), // TODO: 填入「紀念品箱」按鈕的螢幕座標
                GuideIDs.Button.GuideSouvenirBox,
                "打開紀念品箱使用鑰匙前往妖界")),
            // 步驟九：強制提示使用鑰匙前往妖界 — 鎖定玩家
            new WithPlayerLockedStep(
            new ForceUIButtonStep(
                new Vector2(-210,86), // TODO: 填入「使用鑰匙」按鈕的螢幕座標
                GuideIDs.Button.GuideUseKey,
                "點擊使用鑰匙前往妖界"))
        };
    }
    protected override void OnResume(int fromStep)
    {
        // 步驟 1 (index 1) 正常情況下會啟動背景監聽
        // 若從步驟 2 之後恢復且尚未購買，需重新啟動背景監聽
        if (fromStep >= 2 && !IsPurchased)
        {
            earlyPurchaseListener.StartEarly(new PurchaseItemListener());
        }

        // 恢復任務時再次隱藏應該全程禁用的按鈕
        HideTutorialDisabledButtons();
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
            noticeItems.Add(NoticeItemEntry.ItemEntry(itemID, 1));
        }
        NoticeGetItemEvents.InvokeShowNotice("爺爺的庫存", noticeItems);
    }
}