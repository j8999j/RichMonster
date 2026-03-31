using UnityEngine;
using Player;
using System.Collections.Generic;
using System.Linq;
using GameSystem;

public class AbyssShop : MonoBehaviour, IInteractable, IMapGuideTarget
{
    public GameObject Prompt_E;

    [Header("深淵商店設定")]
    public AbyssShopRewardConfig RewardConfig;
    public AbyssView ShopView;
    public string ID => "AbyssShop";
    [Header("深淵遊戲狀態")]
    private bool _isPlayed;
    private bool _ArrivedBottom;
    private int _currentLayer = 0; // 0: 未開始, 1~5: 當前層數
    private int _accumulatedMonsterGold = 0;
    private List<string> _accumulatedItems = new List<string>();

    // 每層成功率：100%, 75%, 50%, 40%, 20%
    private readonly float[] _successRates = { 0.90f, 0.75f, 0.50f, 0.40f, 0.20f };
    public void SetMapGuide()
    {
        NoticeGetItemEvents.InvokeSetMapGuide(ID,transform);
    }
    public void Interact()
    {
        LoadGame();
        if (ShopView != null)
        {
            ShopView.Open(_isPlayed);
            if (_isPlayed)
            {
                ShopView.IsPlayView();
            }
        }
    }

    private void Awake()
    {
        if (ShopView != null)
        {
            ShopView.OnContinueClicked += HandleContinue;
            ShopView.OnLeaveClicked    += ExitGame;
            ShopView.OnFail            += HandleFail;
            ShopView.OnItemDroppedToStart += HandleItemDropped;
        }
    }
    private void OnDestroy()
    {
        if (ShopView != null)
        {
            ShopView.OnContinueClicked -= HandleContinue;
            ShopView.OnLeaveClicked    -= ExitGame;
            ShopView.OnFail            -= HandleFail;
            ShopView.OnItemDroppedToStart -= HandleItemDropped;
        }
    }
    private void LoadGame()
    {
        if (DataManager.Instance.GetPlayerSaveData<AbyssSave>("AbyssShop") != null)
        {
            var abyssSave = DataManager.Instance.GetPlayerSaveData<AbyssSave>("AbyssShop");
            if (abyssSave.LastUpdatedDay == GameManager.Instance.gameFlow.CurrentDay)
            {
                _isPlayed = abyssSave.IsPlayed;
                _ArrivedBottom = abyssSave.ArrivedBottom;
            }
            else
            {
                _isPlayed = false;
                _ArrivedBottom = false;
                SaveGame();
            }
        }
    }
    private void SaveGame()
    {
        var abyssSave = new AbyssSave
        {
            IsPlayed = _isPlayed,
            ArrivedBottom = _ArrivedBottom,
            LastUpdatedDay = GameManager.Instance.gameFlow.CurrentDay
        };
        DataManager.Instance.SetPlayerData("AbyssShop", abyssSave);
    }
    private void HandleContinue()
    {
        if (!_isPlayed)
        {
            Debug.Log("[AbyssShop] 請從左側背包拖拉物品至深淵入口做為入場費");
        }
        else
        {
            ExploreNextLayer();
        }
    }

    private void HandleFail()
    {
        if (!_isPlayed) return;
        Debug.Log("[AbyssShop] 玩家掉入坑洞，探險失敗！所有獎勵歸零。");
        _accumulatedMonsterGold = 0;
        _accumulatedItems.Clear();

        if (ShopView != null) ShopView.ClearRewards();
        SaveGame();
    }

    /// <summary>
    /// 當玩家拖入特定物品作入場費時觸發
    /// </summary>
    private void HandleItemDropped(Item droppedItem)
    {
        if (_isPlayed || droppedItem == null) return;
        if (DataManager.Instance == null) return;

        bool success = DataManager.Instance.RemoveItem(droppedItem);
        if (success)
        {
            Debug.Log($"[AbyssShop] 消耗拖入場內的物品: {droppedItem.ItemId}，進入深淵！");
            _isPlayed = true;
            _currentLayer = 1;
            _accumulatedMonsterGold = 0;
            _accumulatedItems.Clear();

            if (ShopView != null) ShopView.ClearRewards();

            // 進入第 1 層，獲得第 1 層獎勵 (100% 成功)
            GiveLayerReward(1);

            if (ShopView != null)
            {
                ShopView.SetLayer(1);
                ShopView.StartGameplay();
            }
        }
    }

    /// <summary>
    /// 備用/舊版強制開始遊戲
    /// </summary>
    public void StartGame()
    {
        Debug.LogWarning("[AbyssShop] StartGame 被直接呼叫。建議使用拖拉物品進場。");
    }

    private void GiveLayerReward(int layer)
    {
        // 第五層特殊結算
        if (layer == 5)
        {
            _accumulatedMonsterGold += 5000;
            _accumulatedItems.Add("SpecialReward_AbyssCore"); // Placeholder
            Debug.Log("[AbyssShop] 達到第 5 層！獲得特殊獎勵：5000 妖怪幣 與 SpecialReward_AbyssCore");

            if (ShopView != null)
            {
                ShopView.AddRewardDisplay(AbyssRewardType.MonsterGold, "", 5000);
                ShopView.AddRewardDisplay(AbyssRewardType.Item, "SpecialReward_AbyssCore", 0);
            }
            return;
        }

        if (RewardConfig == null)
        {
            Debug.LogWarning("[AbyssShop] 未綁定 RewardConfig，給予預設獎勵。");
            _accumulatedMonsterGold += layer * 100;
            return;
        }

        var pool = RewardConfig.GetPoolForLayer(layer);
        if (pool == null || pool.Rewards == null || pool.Rewards.Count == 0)
        {
            Debug.LogWarning($"[AbyssShop] 找不到第 {layer} 層的獎勵池或池為空。");
            return;
        }

        int totalWeight = pool.Rewards.Sum(r => r.Weight);
        if (totalWeight <= 0) return;

        int currentDay = DataManager.Instance.CurrentPlayerData?.DaysPlayed ?? 0;

        // 依照層數決定抽取次數
        for (int drawIndex = 0; drawIndex < layer; drawIndex++)
        {
            // 使用 GameRng 抽選，加上 drawIndex 避免同層每次抽中一樣的東西
            string rndKey = $"AbyssReward_Day{currentDay}_Layer{layer}_Draw{drawIndex}";
            int roll = GameRng.RangeKeyed(0, totalWeight, rndKey);

            int currentWeight = 0;
            AbyssRewardItem selectedReward = null;

            foreach (var reward in pool.Rewards)
            {
                currentWeight += reward.Weight;
                if (roll < currentWeight)
                {
                    selectedReward = reward;
                    break;
                }
            }

            if (selectedReward != null)
            {
                if (selectedReward.RewardType == AbyssRewardType.MonsterGold)
                {
                    _accumulatedMonsterGold += selectedReward.GoldAmount;
                    Debug.Log($"[AbyssShop] 第 {layer} 層第 {drawIndex + 1} 抽獲得妖怪幣: {selectedReward.GoldAmount}");

                    if (ShopView != null) ShopView.AddRewardDisplay(AbyssRewardType.MonsterGold, "", selectedReward.GoldAmount);
                }
                else if (selectedReward.RewardType == AbyssRewardType.Item)
                {
                    for (int i = 0; i < selectedReward.ItemAmount; i++)
                    {
                        _accumulatedItems.Add(selectedReward.ItemID);
                        
                        if (ShopView != null) ShopView.AddRewardDisplay(AbyssRewardType.Item, selectedReward.ItemID, 0);
                    }
                    Debug.Log($"[AbyssShop] 第 {layer} 層第 {drawIndex + 1} 抽獲得物品: {selectedReward.ItemID} x{selectedReward.ItemAmount}");
                }
            }
        }
    }

    /// <summary>
    /// 繼續探索下一層
    /// </summary>
    public void ExploreNextLayer()
    {
        if (!_isPlayed) return;
        if (_currentLayer >= 5) return;

        // 確保不會超過陣列長度
        // _currentLayer 是當前已完成的層數，下一層的索引是 _currentLayer
        // 例如，從第1層進入第2層，_currentLayer=1，nextLayerIndex=1，對應 _successRates[1]
        int nextLayerIndex = _currentLayer; 
        float rate = (nextLayerIndex < _successRates.Length) ? _successRates[nextLayerIndex] : 0.2f;

        int currentDay = DataManager.Instance.CurrentPlayerData?.DaysPlayed ?? 0;
        string rndKey = $"AbyssRate_Day{currentDay}_Layer{_currentLayer}";
        float roll = GameRng.ValueKeyed(rndKey); // 取 0.0f ~ 1.0f

        _currentLayer++;

        if (roll <= rate)
        {
            GiveLayerReward(_currentLayer);
            Debug.Log($"[AbyssShop] 通過！累計金幣: {_accumulatedMonsterGold}");

            if (ShopView != null)
            {
                ShopView.ProceedToNextLayer(_currentLayer, true);
            }

            // 抵達第5層強制結算（或者您可以讓玩家選擇是否要自己按離開，此處暫不強制立即退出關閉UI）
            if (_currentLayer == 5)
            {
                Debug.Log("[AbyssShop] 達到最底層！");
                _ArrivedBottom = true;
            }
        }
        else
        {
            Debug.Log("[AbyssShop] 探險失敗命中！本層將是必定吃洞的危險層。");
            if (ShopView != null)
            {
                ShopView.ProceedToNextLayer(_currentLayer, false);
            }
        }
    }

    /// <summary>
    /// 結算退出並給予獎勵
    /// </summary>
    public void ExitGame()
    {
        if (!_isPlayed) return;

        // 建立通知顯示清單
        var noticeItems = new List<NoticeItemEntry>();

        if (_accumulatedMonsterGold > 0)
        {
            DataManager.Instance.ModifyMonsterGold(_accumulatedMonsterGold);
            noticeItems.Add(NoticeItemEntry.MonsterGold(_accumulatedMonsterGold));
        }

        foreach (var itemId in _accumulatedItems)
        {
            DataManager.Instance.AddItem(itemId, 0); // 本金設為 0
            noticeItems.Add(NoticeItemEntry.ItemEntry(itemId));
        }

        // 觸發取得物品通知
        if (noticeItems.Count > 0)
        {
            NoticeGetItemEvents.InvokeShowNotice("貪婪之淵探索獎勵", noticeItems);
        }
        _accumulatedMonsterGold = 0;
        _accumulatedItems.Clear();
        
        if (ShopView != null)
        {
            ShopView.ClearRewards();
            ShopView.Close();
        }
        SaveGame();
    }
    public void ShowPrompt()
    {
        if (Prompt_E != null) Prompt_E.SetActive(true);
    }

    public void HidePrompt()
    {
        if (Prompt_E != null) Prompt_E.SetActive(false);
    }
}
public class AbyssSave : ISaveData
{
    public string UniqueID => "AbyssShop";
    public int LastUpdatedDay { get; set; } = 0;
    public bool IsPlayed = false;
    public bool ArrivedBottom = false;
}