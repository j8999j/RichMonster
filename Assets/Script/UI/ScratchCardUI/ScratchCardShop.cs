using UnityEngine;
using GameSystem;
using Player;
using Unity.VisualScripting;

public class ScratchCardShop : MonoBehaviour, IInteractable
{
    [SerializeField] private ScratchCard scratchCard;

    private bool isScratched;
    private int CurrentDay;
    public GameObject Prompt;
    private ScratchCardPrizeType currentPrize;
    private void OnEnable()
    {
        scratchCard.OnScratchComplete += OnScratchComplete;
    }
    private void OnDisable()
    {
        scratchCard.OnScratchComplete -= OnScratchComplete;
    }

    /// <summary>
    /// 各獎項權重 (對應 ScratchCardPrizeType 順序)
    /// </summary>
    private static readonly int[] PrizeWeights = { 1, 4, 5, 10, 20, 30, 30 };

    public void Start()
    {
        isScratched = false;
        CurrentDay = GameManager.Instance.gameFlow.CurrentDay;

        if (LoadData())
        {
            isScratched = true;
        }

        // 使用 GameRng 抽獎並設定獎品圖片
        currentPrize = DrawPrize();
        scratchCard.SetPrize(currentPrize);
    }

    /// <summary>
    /// 使用 GameRng 進行加權抽獎
    /// </summary>
    private ScratchCardPrizeType DrawPrize()
    {
        int totalWeight = 0;
        foreach (var w in PrizeWeights)
            totalWeight += w;

        int roll = GameRng.RangeKeyed(0, totalWeight, "ScratchCard");

        int cumulative = 0;
        for (int i = 0; i < PrizeWeights.Length; i++)
        {
            cumulative += PrizeWeights[i];
            if (roll < cumulative)
            {
                Debug.Log($"[ScratchCardShop] 抽獎結果: {(ScratchCardPrizeType)i}");
                return (ScratchCardPrizeType)i;
            }
        }

        return ScratchCardPrizeType.NoWin;
    }

    private bool LoadData()
    {
        var data = DataManager.Instance.GetPlayerSaveData<ScratchCardShopData>("ScratchCardShopData");
        return data.IsScratched;
    }

    private void SaveData()
    {
        var data = new ScratchCardShopData();
        data.IsScratched = isScratched;
        data.LastUpdatedDay = CurrentDay;
        DataManager.Instance.SetPlayerData("ScratchCardShopData", data);
    }
    /// <summary>
    /// 買刮刮卡
    /// </summary>
    private void BuyScratchCard()
    {
        if(isScratched == false && DataManager.Instance.TrySpendGold(300))
        {
            DataManager.Instance.ModifyGold(-300);
            scratchCard.ShowScratchCard(false);
        }
        else if(DataManager.Instance.TrySpendGold(300) == false)
        {
            scratchCard.NotEnoughGold();
        }
    }
    /// <summary>
    /// ScratchCard 刮除完成後的回調 (由 ScratchCard.onScratchComplete 事件觸發)
    /// </summary>
    private void OnScratchComplete()
    {
        isScratched = true;
        SaveData();
        SettlePrize(currentPrize);
    }
    /// <summary>
    /// 根據抽獎結果結算獎勵
    /// </summary>
    private void SettlePrize(ScratchCardPrizeType prize)
    {
        int goldReward = 0;

        switch (prize)
        {
            case ScratchCardPrizeType.GrandPrize:
                goldReward = 10000;
                AchievementEvents.ScratchCardCompleted(0);
                break;
            case ScratchCardPrizeType.FirstPrize:
                goldReward = 5000;
                AchievementEvents.ScratchCardCompleted(1);
                break;
            case ScratchCardPrizeType.SecondPrize:
                goldReward = 2000;
                AchievementEvents.ScratchCardCompleted(2);
                break;
            case ScratchCardPrizeType.ThirdPrize:
                goldReward = 500;
                AchievementEvents.ScratchCardCompleted(3);
                break;
            case ScratchCardPrizeType.FourthPrize:
                goldReward = 300;
                AchievementEvents.ScratchCardCompleted(4);
                break;
            case ScratchCardPrizeType.FifthPrize:
                goldReward = 100;
                AchievementEvents.ScratchCardCompleted(5);
                break;
            case ScratchCardPrizeType.NoWin:
                goldReward = 0;
                AchievementEvents.ScratchCardCompleted(6);
                break;
        }
        DataManager.Instance.ModifyGold(goldReward);
        Debug.Log($"[ScratchCardShop] 恭喜獲得 {prize}！獎勵金幣: {goldReward}");
        ShowPrizePanel(prize);
    }
    private async void ShowPrizePanel(ScratchCardPrizeType prize)
    {
        await GameManager.Instance.gameFlow.SaveGameAsync();
        scratchCard.ShowCompletePrize(prize);
    }
    /// <summary>
    /// 顯示提示
    /// </summary>
    public void ShowPrompt()
    {
        Prompt.SetActive(true);
    }
    /// <summary>
    /// 隱藏提示
    /// </summary>
    public void HidePrompt()
    {
        Prompt.SetActive(false);
    }
    public void Interact()
    {
        scratchCard.ShowCardPanel(isScratched);
    }
}

public class ScratchCardShopData : ISaveData
{
    public string UniqueID => "ScratchCardShopData";
    public int LastUpdatedDay { get; set; }
    public bool IsScratched;
}

public enum ScratchCardPrizeType
{
    GrandPrize,
    FirstPrize,
    SecondPrize,
    ThirdPrize,
    FourthPrize,
    FifthPrize,
    NoWin
}