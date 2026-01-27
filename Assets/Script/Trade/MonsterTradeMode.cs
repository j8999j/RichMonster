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
    private Item currentSelectedItem;
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
            new Dictionary<string, ItemTags>(DataManager.Instance.ItemTagsDict)
        );
        
    }
    void OnEnable()
    {
        tradeView.OnOpenShop += StartTradeMode;
        tradeView.TradeItems += TradeThisItem;
        tradeView.TradePrice += PriceTrade;
    }
    void OnDisable()
    {
        tradeView.OnOpenShop -= StartTradeMode;
        tradeView.TradeItems -= TradeThisItem;
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
        GameManager.Instance.gameFlow.SwitchGameStageAndSave(DayPhase.NightTrade);
        GenerateGuestList();
        LoadHistory();
        tradeView.UpdateTradeInfo(_TodayMonsterGuestList[monsterTradeProgress.CustomerIndex], DataManager.Instance.CurrentPlayerData.InventoryItems.ToList());
        UpdateGuestDialog();
    }

    /// <summary>
    /// 記錄所有Guest的詳細資訊（用於測試）
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

            Debug.Log($"    特質: {traits}");
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
        
        // 如果有請求標籤，有機率使用帶標籤的對話
        if (request.RequestTags != null && request.RequestTags.Count > 0 && GameRng.Value() > 0.3f)
        {
            // 隨機選一個標籤
            int tagIndex = GameRng.Range(0, request.RequestTags.Count);
            string tagName = GetTagDisplayName(request.RequestTags[tagIndex]);
            
            // 隨機選一個帶標籤的模板
            int templateIndex = GameRng.Range(0, tagDialogTemplates.Count);
            dialog = tagDialogTemplates[templateIndex]
                .Replace("{tag}", tagName)
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
            .Where(item => {
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
            tradeView.UpdateTradeInfo(_TodayMonsterGuestList[monsterTradeProgress.CustomerIndex], PlayerInventory);
            UpdateGuestDialog();
        }
    }
    void ClearTradeProgress()
    {
        monsterTradeProgress.CustomerIndex = 0;
    }
    #endregion
    #region SelectItem
    private void TradeThisItem(Item item)
    {
        currentSelectedItem = item;
    }
    #endregion
    #region TradePrice
    private void PriceTrade(Item item)
    {
            var price = CaculatePrice(item);
            if (TradeSuccess(item))
            {
                //交易成功
                DataManager.Instance.ModifyMonsterGold((int)price);
                DataManager.Instance.RemoveItem(item);
                NextGuest();
            }
            else
            {
                Debug.Log($"交易失敗");
                //交易失敗
                GuestLeave();
            }
    }
    /// <summary>
    /// 顧客離開 - 耐心耗盡時觸發
    /// </summary>
    private void GuestLeave()
    {
        Debug.Log($"顧客討厭該商品，顧客離開");
        // 切換到下一位顧客
        NextGuest();
    }
    /// <summary>
    /// 檢查交易是否成功（物品標籤不能包含顧客的 HateTags）
    /// </summary>
    private bool TradeSuccess(Item item)
    {
        if (item == null || currentmonsterGuest == null) return false;
        
        var itemDefinition = DataManager.Instance.GetItemById(item.ItemId);
        if (itemDefinition == null) return false;
        
        var hateTags = currentmonsterGuest.monsterCustomer.HateTags;
        if (hateTags == null || hateTags.Count == 0) return true;
        
        // 檢查物品標籤是否與 HateTags 有交集
        foreach (var tag in itemDefinition.Tags)
        {
            if (hateTags.Contains(tag))
            {
                return false; // 物品包含顧客討厭的標籤
            }
        }
        return true;
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
        
        if(currentmonsterGuest.monsterRequest.itemType == itemDefinition.Type)
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
