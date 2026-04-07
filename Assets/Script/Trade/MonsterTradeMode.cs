using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameSystem;



public class MonsterTradeMode : MonoBehaviour
{
    private MonsterTradeView tradeView;
    //交易邏輯
    private MonsterGuestGenerator _generator;
    private List<MonsterGuest> _TodayMonsterGuestList;
    //交易狀態紀錄
    private MonsterTradeProgress monsterTradeProgress;
    private MonsterGuest currentmonsterGuest;
    void Awake()
    {
        tradeView = GetComponent<MonsterTradeView>();
    }
    void Start()
    {
        // 建立生成器
        _generator = new MonsterGuestGenerator(
            new Dictionary<string, MonsterProfessionDefinition>(DataManager.Instance.MonsterProfessionDict),
            new Dictionary<string, MonsterTraitDefinition>(DataManager.Instance.MonsterTraitDict),
            new Dictionary<string, ItemTags>(DataManager.Instance.ItemTagsDict),
            new Dictionary<string, ItemDefinition>(DataManager.Instance.ItemDict)
        );

    }
    void OnEnable()
    {
        tradeView.OnOpenShop += StartTradeMode;
        tradeView.TradePrice += PriceTrade;
    }
    void OnDisable()
    {
        tradeView.OnOpenShop -= StartTradeMode;
        tradeView.TradePrice -= PriceTrade;
    }
    /// <summary>
    /// 根據當前天數與庫存生成完整Guest列表
    /// </summary>
    public void GenerateGuestList()
    {
        _TodayMonsterGuestList = _generator.GenerateGuestsForDay(1);
        LogAllGuestDetails();
    }

    /// <summary>
    /// 開始交易模式，開始抽選並回復資料
    /// </summary>
    public void StartTradeMode()
    {
        GenerateGuestList();
        LoadHistory();

        tradeView.UpdateTradeInfo(_TodayMonsterGuestList[monsterTradeProgress.CustomerIndex], DataManager.Instance.CurrentPlayerData.InventoryItems.ToList(), monsterTradeProgress.CustomerIndex, _TodayMonsterGuestList.Count, DataManager.Instance.CurrentPlayerData.MonsterGold);
        UpdateGuestDialog();
    }

    /// <summary>
    /// 記錄所有Guest的詳細資訊
    /// </summary>
    private void LogAllGuestDetails()
    {
        Debug.Log($"客人數: {_TodayMonsterGuestList.Count}");

        for (int i = 0; i < _TodayMonsterGuestList.Count; i++)
        {
            var guest = _TodayMonsterGuestList[i];
            var customer = guest.monsterCustomer;
            var request = guest.monsterRequest;

            string traits = customer.Traits.Count > 0
                ? string.Join(", ", customer.TraitNames)
                : "無";

            string tags = request.RequestTags.Count > 0
                ? string.Join(", ", request.RequestTags)
                : "無";

            string preferredTags = customer.PreferredTags.Count > 0
                ? string.Join(", ", customer.PreferredTags)
                : "無";

            Debug.Log($"[客人 {i + 1}] " +
                $"職業: {customer.ProfessionName} ({customer.Type}) | " +
                $"種族: {customer.Race} | " +
                $"預算乘數: {customer.BudgetMultiplier:F2}");
            
            string categoryStr = request.IsType2Category ? "種類2(實體配對)" : "種類1(隨機屬性)";
            string preferStr = request.TriggeredPreference ? "【有觸發】" : "無";

            Debug.Log($"    生成來源: {categoryStr} | 偏好加成: {preferStr}");
            Debug.Log($"    偏好標籤: {preferredTags}");
            Debug.Log($"    請求類型: {request.itemType} | 請求標籤: {tags}");
        }

        Debug.Log("==========================================================");
    }
    #region TradeLogic
    /// <summary>
    /// 載入玩家交易進度
    /// </summary>
    private void LoadHistory()
    {
        monsterTradeProgress = DataManager.Instance.LoadMonsterTradeHistory();
        if (monsterTradeProgress == null)
        {
            monsterTradeProgress = new MonsterTradeProgress
            {
                CustomerIndex = 0,
            };
            currentmonsterGuest = _TodayMonsterGuestList[0];
        }
        else
        {
            currentmonsterGuest = _TodayMonsterGuestList[monsterTradeProgress.CustomerIndex];
        }
    }
    /// <summary>
    /// 根據當前顧客需求生成隨機對話
    /// </summary>
    private string GenerateRequestDialog(MonsterGuest guest)
    {
        if (guest == null) return "...";

        var request = guest.monsterRequest;
        var customer = guest.monsterCustomer;
        // 對話模板列表
        var dialogTemplates = new List<string>
        {
            "我想要{type}...",
            "有沒有{type}啊？",
            "給我來點{type}吧！",
            "我在找{type}...",
            "你這有{type}嗎？",
            "聽說這裡有{type}？"
        };

        // 帶標籤的對話模板
        var tagDialogTemplates = new List<string>
        {
            "我想要{tag}的{type}...",
            "有沒有{tag}一點的{type}？",
            "給我{tag}的{type}！",
            "我在找{tag}的東西...",
            "有{tag}的商品嗎？"
        };

        // 物品類型對應的中文名稱
        string typeName = request.itemType switch
        {
            ItemType.Equipment => "裝備",
            ItemType.Food => "食物",
            ItemType.Prop => "道具",
            _ => "東西"
        };
        string dialog;
        // 如果有請求標籤，必定使用帶標籤的對話以給予玩家完整提示
        if (request.RequestTags != null && request.RequestTags.Count > 0)
        {
            // 將所有請求標籤都轉為顯示名稱，如果有多個則用「又」連接
            var tagNames = request.RequestTags.Select(t => GetTagDisplayName(t)).ToList();
            string combinedTags = string.Join("又", tagNames);

            // 隨機選一個帶標籤的模板
            int templateIndex = GameRng.Range(0, tagDialogTemplates.Count);
            dialog = tagDialogTemplates[templateIndex]
                .Replace("{tag}", combinedTags)
                .Replace("{type}", typeName);
        }
        else
        {
            // 隨機選一個基本模板
            int templateIndex = GameRng.Range(0, dialogTemplates.Count);
            dialog = dialogTemplates[templateIndex].Replace("{type}", typeName);
        }

        return dialog;
    }

    /// <summary>
    /// 取得標籤的顯示名稱
    /// </summary>
    private string GetTagDisplayName(string tagId)
    {
        if (DataManager.Instance.ItemTagsDict.TryGetValue(tagId, out var tagData))
        {
            return tagData.TagName ?? tagId;
        }
        return tagId;
    }

    /// <summary>
    /// 更新當前顧客的對話
    /// </summary>
    private void UpdateGuestDialog()
    {
        string dialog = GenerateRequestDialog(currentmonsterGuest);
        tradeView.UpdateDialog(dialog);
    }
    /// <summary>
    /// 下一位客人
    /// </summary>
    private void NextGuest()
    {
        // 只計算妖界物品
        var PlayerInventory = DataManager.Instance.CurrentPlayerData.InventoryItems
            .Where(item =>
            {
                var definition = DataManager.Instance.GetItemById(item.ItemId);
                return definition != null && definition.World == ItemWorld.Human;
            })
            .ToList();
        tradeView.SetSelectTradeUI();
        monsterTradeProgress.CustomerIndex += 1;
        if (PlayerInventory.Count <= 0)
        {
            ClearTradeProgress();
            tradeView.EndTradeMode();
            Debug.Log("商品不足本日結束");
            //商品不足本日結束
            //本日結束存檔
        }
        if (monsterTradeProgress.CustomerIndex >= _TodayMonsterGuestList.Count)
        {
            ClearTradeProgress();
            tradeView.EndTradeMode();
            //本日結束存檔
        }
        else//下一位
        {
            currentmonsterGuest = _TodayMonsterGuestList[monsterTradeProgress.CustomerIndex];
            tradeView.UpdateTradeInfo(_TodayMonsterGuestList[monsterTradeProgress.CustomerIndex], PlayerInventory, monsterTradeProgress.CustomerIndex, _TodayMonsterGuestList.Count, DataManager.Instance.CurrentPlayerData.MonsterGold);
            UpdateGuestDialog();
        }
    }
    void ClearTradeProgress()
    {
        monsterTradeProgress.CustomerIndex = 0;
    }
    #endregion
    #region TradePrice
    private void PriceTrade(Item item)
    {
        var price = CaculatePrice(item);
        AchievementEvents.TradeItem(currentmonsterGuest.monsterCustomer.Profession, item.ItemId);
        
        TradeSatisfaction satisfaction = CalculateSatisfaction(item);
        
        // 呼叫 View 表達視覺 (先留空)
        tradeView.ShowSatisfactionVisual(satisfaction);

        if (satisfaction != TradeSatisfaction.Hated)
        {
            // 交易成功
            DataManager.Instance.ModifyMonsterGold((int)price);
            tradeView.UpdateSoulDisplayAnimation((int)price);
            tradeView.UpdateSoulDisplay(DataManager.Instance.CurrentPlayerData.MonsterGold);
            DataManager.Instance.RemoveItem(item);

            // 交易成功，立刻更新畫面上的背包並清除已選取的物品顯示
            var updatedInventory = DataManager.Instance.CurrentPlayerData.InventoryItems.ToList();
            tradeView.ShowBagItems(updatedInventory);
            tradeView.ClearBagImage();

            // 廣播給紀念品系統（例如：交易滿意時給額外獎勵）
            Souvenir.SouvenirManager.Instance.NotifyMonsterTradeCompleted(satisfaction);

            tradeView.FadeOutCustomerThenCallback(() => NextGuest());
        }
        else
        {
            Debug.Log($"交易失敗: {satisfaction}");
            // 交易失敗
            GuestLeave();
        }
    }
    /// <summary>
    /// 顧客離開 - 耐心耗盡時觸發
    /// </summary>
    private void GuestLeave()
    {
        Debug.Log($"顧客討厭該商品，顧客離開");
        // 即刻清除被選取的物品顯示
        tradeView.ClearBagImage();
        // 切換到下一位顧客
        tradeView.FadeOutCustomerThenCallback(() => NextGuest());
    }
    /// <summary>
    /// 檢查交易滿意度
    /// </summary>
    private TradeSatisfaction CalculateSatisfaction(Item item)
    {
        if (item == null || currentmonsterGuest == null) return TradeSatisfaction.Hated;

        var itemDefinition = DataManager.Instance.GetItemById(item.ItemId);
        if (itemDefinition == null) return TradeSatisfaction.Hated;

        var request = currentmonsterGuest.monsterRequest;
        var customer = currentmonsterGuest.monsterCustomer;

        // 1. 厭惡 (Hated): 包含厭惡標籤
        if (customer.HateTags != null && customer.HateTags.Any(t => itemDefinition.Tags.Contains(t)))
        {
            return TradeSatisfaction.Hated;
        }

        bool isTypeMatch = itemDefinition.Type == request.itemType;
        bool hasAllRequestTags = request.RequestTags == null || !request.RequestTags.Except(itemDefinition.Tags).Any();
        bool hasAnyPreferTag = customer.PreferredTags != null && customer.PreferredTags.Any(t => itemDefinition.Tags.Contains(t));
        bool hasAllPreferTags = customer.PreferredTags != null && customer.PreferredTags.Count > 0 && !customer.PreferredTags.Except(itemDefinition.Tags).Any();

        // 4. 非常滿意 (VerySatisfied)
        // 條件 A: 符合需求類型且具有所有需求標籤並包含任意偏好標籤
        bool verySatisfiedA = isTypeMatch && hasAllRequestTags && hasAnyPreferTag;
        // 條件 B: 提交物品符合所有偏好標籤(忽略需求部分)
        bool verySatisfiedB = hasAllPreferTags;

        if (verySatisfiedA || verySatisfiedB)
        {
            return TradeSatisfaction.VerySatisfied;
        }

        // 3. 滿意 (Satisfied)
        // 條件 A: 符合需求類型且具有所有需求標籤
        bool satisfiedA = isTypeMatch && hasAllRequestTags;
        // 條件 B: 需求類型符合且物品包含任意偏好標籤
        bool satisfiedB = isTypeMatch && hasAnyPreferTag;

        if (satisfiedA || satisfiedB)
        {
            return TradeSatisfaction.Satisfied;
        }

        // 2. 尚可 (Okay): 不包含厭惡標籤但未達到滿意與非常滿意標準
        return TradeSatisfaction.Okay;
    }
    private float CaculatePrice(Item item)
    {
        var itemDefinition = DataManager.Instance.GetItemById(item.ItemId);
        var basePrice = itemDefinition.BasePrice;
        float RequestMultiplier;
        // 計算顧客偏好標籤與物品標籤的交集數量
        int preferMatchCount = currentmonsterGuest.monsterCustomer.PreferredTags
            .Intersect(itemDefinition.Tags)
            .Count();
        float PreferMultiplier = preferMatchCount switch
        {
            0 => 0,
            1 => currentmonsterGuest.monsterCustomer.PreferMaxPower * 0.2f,
            2 => currentmonsterGuest.monsterCustomer.PreferMaxPower * 0.5f,
            3 => currentmonsterGuest.monsterCustomer.PreferMaxPower * 1f,
            _ => currentmonsterGuest.monsterCustomer.PreferMaxPower * 1f
        };

        if (currentmonsterGuest.monsterRequest.itemType == itemDefinition.Type)
        {
            // 計算物品標籤與顧客請求標籤的相同數量
            int matchingTagCount = itemDefinition.Tags
                .Intersect(currentmonsterGuest.monsterRequest.RequestTags)
                .Count();
            switch (matchingTagCount)
            {
                case 0:
                    RequestMultiplier = 1.2f;
                    break;
                case 1:
                    RequestMultiplier = 1.3f;
                    break;
                case 2:
                    RequestMultiplier = 1.7f;
                    break;
                case 3:
                    RequestMultiplier = 3f;
                    break;
                default:
                    RequestMultiplier = 3f;
                    break;
            }
        }
        else
        {
            RequestMultiplier = 0.8f;
        }
        float BudgetMultiplier = PreferMultiplier + currentmonsterGuest.monsterCustomer.BudgetMultiplier;
        var price = basePrice * BudgetMultiplier * RequestMultiplier;
        return price;
    }
    #endregion
}
