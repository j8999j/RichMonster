// ============================================================
// Task2_SecondTutorial.cs - 任務二：妖怪雜貨店與圖鑑
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class Task2_SecondTutorial : GuideTask
{
    public override string TaskName => "任務二：妖怪雜貨店與圖鑑";

    private GameObject _bookButtonObj;

    private void HideTutorialDisabledButtons()
    {
        if (GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideShopBook, out var bookBtn))
        {
            _bookButtonObj = bookBtn.ButtonObject;
            if (_bookButtonObj != null) _bookButtonObj.SetActive(false);
        }
    }

    protected override List<GuideStep> BuildSteps()
    {
        return new List<GuideStep>
        {
            // 等待妖怪場景載入完成
            SaveAfter(new WaitForSceneStep(GameSystem.SceneTransitionManager.SCENE_MONSTER)),

            // 步驟零：一開始先確保圖鑑按鈕隱藏（直到步驟二才給予玩家解鎖）
            new GiveRewardStep(() =>
            {
                Debug.Log("開始教學二");
                HideTutorialDisabledButtons();
            }),
            
            // 步驟一：引導前往妖怪雜貨店
            new WithMapGuideStep(
                inner: new ShowHintAndWaitStep(
                    "前往妖怪雜貨店查看",
                    new InteractWithObjectListener(GuideIDs.Interactable.GuideGroceryStore)),
                targetId: GuideIDs.Interactable.GuideGroceryStore),
            
            // 步驟二：獲得隨機三項庫存、隨機解鎖兩條妖怪情報，並且顯示妖怪圖鑑按鈕
            SaveAfter(new GiveRewardStep(() =>
            {
                DataManager.Instance.ModifyMonsterGold(5000);
                GiveRandomMonsterItems(3, 5000, true, 2);
                
                // 隨機解鎖兩條尚未解鎖的妖怪情報
                DataManager.Instance.UnlockRandomMonsterInformation();
                DataManager.Instance.UnlockRandomMonsterInformation();

                if (_bookButtonObj != null) _bookButtonObj.SetActive(true);
            })),
            
            // 步驟三：強制提示點擊妖怪圖鑑按鈕
            new WithPlayerLockedStep(
                new ForceUIButtonStep(
                    new Vector2(637, -257), // TODO: 填入 Book 按鈕的目標相對座標
                    GuideIDs.Button.GuideShopBook,
                    "點擊查看妖怪圖鑑")),

            new ShowHintAndWaitStep(
                "點擊開始接待接待妖怪客人",
                new MonsterTradeStartedListener()),

            SaveAfter(new WaitForListenerStep(new MonsterTradeCompletedListener())),

            new ShowHintAndWaitStep(
                "可使用紀念品鑰匙回家休息，或到處逛逛",
                new ButtonClickListener(GuideIDs.Button.GuideUseKey))
        };
    }

    private static GuideStep SaveAfter(GuideStep step, int? saveStepIndex = null)
    {
        return new WithTutorialSaveStep(step, saveStepIndex);
    }

    protected override void OnResume(int fromStep)
    {
        // 如果恢復進度時還不到步驟三（未解鎖圖鑑前），確保隱藏圖鑑按鈕
        if (fromStep <= 2)
        {
            HideTutorialDisabledButtons();
        }
        else
        {
            // 如果已經到步驟二之後，確保圖鑑按鈕顯示
            if (GuideLookupRegistry.Instance.TryGetButton(GuideIDs.Button.GuideShopBook, out var bookBtn))
            {
                _bookButtonObj = bookBtn.ButtonObject;
                if (_bookButtonObj != null) _bookButtonObj.SetActive(true);
            }
        }
    }

    private void GiveRandomMonsterItems(int count, int monsterGoldAmount = 0, bool includeMonsterBook = false, int monsterInformationAmount = 0)
    {
        var itemIDs = DataManager.Instance.GetRandomDistinctItemIds(ItemWorld.Monster, Rarity.Common, count);
        List<NoticeItemEntry> noticeItems = new List<NoticeItemEntry>();

        if (monsterGoldAmount > 0)
        {
            noticeItems.Add(NoticeItemEntry.MonsterGold(monsterGoldAmount));
        }

        if (includeMonsterBook)
        {
            noticeItems.Add(NoticeItemEntry.MonsterBook());
        }

        if (monsterInformationAmount > 0)
        {
            noticeItems.Add(NoticeItemEntry.MonsterInformation(monsterInformationAmount));
        }

        foreach (var itemID in itemIDs)
        {
            DataManager.Instance.AddItem(itemID, 0);
        }
        foreach (var itemID in itemIDs)
        {
            noticeItems.Add(NoticeItemEntry.ItemEntry(itemID, 1));
        }
        NoticeGetItemEvents.InvokeShowNotice("爺爺的妖怪雜貨", noticeItems);
    }
}
