using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameSystem;



public class MonsterTradeMode : MonoBehaviour
{
    public static event System.Action OnTradeStarted;
    public static event System.Action OnTradeCompleted;

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
        GenerateGuestList();
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
        //LogAllGuestDetails();
    }

    public void InteractShopUI()
    {
        tradeView.OpenShopUI(false, _TodayMonsterGuestList.Count);
    }

    public void ExitShopUI()
    {
        tradeView.ExitShopUI();
    }

    /// <summary>
    /// 開始交易模式，開始抽選並回復資料
    /// </summary>
    public void StartTradeMode()
    {
        GenerateGuestList();
        LoadHistory();
        OnTradeStarted?.Invoke();

        if (GetHumanWorldInventory().Count <= 0 || monsterTradeProgress.CustomerIndex >= _TodayMonsterGuestList.Count)
        {
            CompleteTradeDay();
            return;
        }

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
        monsterTradeProgress = DataManager.Instance.GetMonsterTradeHistory();
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
        // 物品類型對應的多種中文描述（隨機選取以增加對話多樣性）
        var typeNameMap = new Dictionary<ItemType, List<string>>
        {
            { ItemType.Food, new List<string> { "食物", "吃的", "糧食", "能吃的東西", "填肚子的" } },
            { ItemType.Equipment, new List<string> { "裝備","能穿戴的", "器具" } },
            { ItemType.Prop, new List<string> { "道具", "物品", "東西", "好東西"} }
        };

        var fallbackNames = new List<string> { "東西", "物品", "商品", "好貨" };

        string typeName;
        if (typeNameMap.TryGetValue(request.itemType, out var names))
        {
            typeName = names[GameRng.Range(0, names.Count)];
        }
        else
        {
            typeName = fallbackNames[GameRng.Range(0, fallbackNames.Count)];
        }

        // 對話模板列表（每句都包含 {type} 以明確描述需求類型）
        var dialogTemplates = new List<string>
        {
            "我想要{type}...",
            "有沒有{type}啊？",
            "給我來點{type}吧！",
            "我在找{type}...",
            "你這有{type}嗎？",
            "聽說這裡有{type}？",
            "今天想帶點{type}回去。",
            "我就缺{type}了！",
            "能給我看看{type}嗎？",
            "有沒有好一點的{type}？",
            "我特地來買{type}的！",
            "幫我挑個{type}吧。"
        };

        // 帶標籤的對話模板（每句都包含 {type} 以明確描述需求類型）
        var tagDialogTemplates = new List<string>
        {
            "我想要{tag}的{type}...",
            "有沒有{tag}一點的{type}？",
            "給我{tag}的{type}！",
            "我在找{tag}的{type}...",
            "有{tag}的{type}嗎？",
            "聽說你這有{tag}的{type}？",
            "幫我找{tag}的{type}吧！",
            "今天就想來點{tag}的{type}。",
            "能不能給我{tag}的{type}？",
            "我特地來找{tag}的{type}的！"
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
        var PlayerInventory = GetHumanWorldInventory();
        tradeView.SetSelectTradeUI();
        monsterTradeProgress.CustomerIndex += 1;
        if (PlayerInventory.Count <= 0)
        {
            CompleteTradeDay();
            Debug.Log("商品不足本日結束");
            return;
        }
        if (monsterTradeProgress.CustomerIndex >= _TodayMonsterGuestList.Count)
        {
            CompleteTradeDay();
            return;
        }
        else//下一位
        {
            currentmonsterGuest = _TodayMonsterGuestList[monsterTradeProgress.CustomerIndex];
            tradeView.UpdateTradeInfo(_TodayMonsterGuestList[monsterTradeProgress.CustomerIndex], PlayerInventory, monsterTradeProgress.CustomerIndex, _TodayMonsterGuestList.Count, DataManager.Instance.CurrentPlayerData.MonsterGold);
            UpdateGuestDialog();
            SaveTradeProgress();
        }
    }
    private async void SaveTradeProgress()
    {
        DataManager.Instance.SetFlowSaveData(monsterTradeProgress);
        await GameManager.Instance.gameFlow.SaveGameAsync();
    }

    void ClearTradeProgress()
    {
        monsterTradeProgress.CustomerIndex = 0;
        SaveTradeProgress();
    }

    private void CompleteTradeDay()
    {
        bool wasAlreadyCompleted = DataManager.Instance.CurrentPlayerData != null
            && DataManager.Instance.CurrentPlayerData.IsTrade;

        DataManager.Instance.SetIsTrade(true);
        ClearTradeProgress();
        tradeView.EndTradeMode();

        if (!wasAlreadyCompleted)
            OnTradeCompleted?.Invoke();
    }

    private List<Item> GetHumanWorldInventory()
    {
        return DataManager.Instance.CurrentPlayerData.InventoryItems
            .Where(item =>
            {
                var definition = DataManager.Instance.GetItemById(item.ItemId);
                return definition != null && definition.World == ItemWorld.Human;
            })
            .ToList();
    }
    #endregion
    #region TradePrice
    private void PriceTrade(Item item)
    {
        var price = CaculatePrice(item);
        TradeSatisfaction satisfaction = CalculateSatisfaction(item);
        string customerId = currentmonsterGuest?.monsterCustomer?.Profession ?? string.Empty;
        string race = currentmonsterGuest?.monsterCustomer?.Race ?? string.Empty;

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

            GameEventCenter.Publish(new MonsterTradeCompletedEvent(customerId, item.ItemId, satisfaction, (int)price, race));

            tradeView.FadeOutCustomerThenCallback(() => NextGuest());
        }
        else
        {
            Debug.Log($"交易失敗: {satisfaction}");
            GameEventCenter.Publish(new MonsterTradeFailedEvent(customerId, item.ItemId, satisfaction, race));
            // 交易失敗
            GuestLeave();
        }
    }
    /// <summary>
    /// 顧客離開
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
        bool hasAnyRequestTag = request.RequestTags != null && request.RequestTags.Any(t => itemDefinition.Tags.Contains(t));
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
        // 條件 A: 符合需求類型且具有任一需求標籤
        bool satisfiedA = isTypeMatch && hasAnyRequestTag;
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
