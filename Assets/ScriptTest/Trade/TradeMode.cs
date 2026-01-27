using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using GameSystem;
using GameSystem;
using System;
using System.Text;
using Unity.VisualScripting;

public class TradeMode : MonoBehaviour
{
    private TradeView tradeView;
    //交易邏輯
    private TradeCheck tradeCheck;
    private RequestGenerator requestGenerator;
    private CustomerGenerator customerGenerator;
    private GuestGenerator generator;
    //交易狀態紀錄
    private TradeProgress tradeProgress;
    private List<Guest> TodayGuestList;
    private Guest currentGuest;
    private Guest TradeGuest;
    private Item currentSelectedItem;
    //價格限制
    private int MarketPrice;
    private int PriceMax;
    private int PriceMin;

    void Awake()
    {
        tradeCheck = new TradeCheck();
        requestGenerator = new RequestGenerator();
        generator = new GuestGenerator();
        tradeView = GetComponent<TradeView>();
    }
    void OnEnable()
    {
        tradeView.OnOpenShop += StartTradeMode;
        tradeView.OnItemSelected += SetMarketPrice;
        tradeView.SliderPrice += SetCurrentPrice;
        tradeView.CheckItem += SelectToRequest;
        tradeView.TradeItems += TradeThisItem;
        tradeView.TradePrice += PriceTrade;
    }
    void OnDisable()
    {
        tradeView.OnOpenShop -= StartTradeMode;
        tradeView.OnItemSelected -= SetMarketPrice;
        tradeView.SliderPrice -= SetCurrentPrice;
        tradeView.CheckItem -= SelectToRequest;
        tradeView.TradeItems -= TradeThisItem;
        tradeView.TradePrice -= PriceTrade;
    }
    /// <summary>
    /// 根據當前天數與庫存生成完整Guest列表
    /// </summary>
    public void GenerateGuestList()
    {
        if (DataManager.Instance == null || DataManager.Instance.CurrentPlayerData == null)
        {
            Debug.LogWarning("[TradeMode] DataManager or CurrentPlayerData is null, cannot generate guests.");
            return;
        }

        var playerData = DataManager.Instance.CurrentPlayerData;
        int currentDay = playerData.DaysPlayed;
        var playerInventory = playerData.InventoryItems;

        var traitDictArg = new Dictionary<string, TraitDefinition>(DataManager.Instance.TraitDict);
        var professionListArg = DataManager.Instance.ProfessionDict.Values.ToList();

        customerGenerator = new CustomerGenerator(traitDictArg, professionListArg);
        var customers = customerGenerator.GenerateCustomersForDay(currentDay, 10); // 顧客數量

        TodayGuestList = generator.BuildGuestsForToday(customers, playerInventory);

        Debug.Log($"[TradeMode] Generated {TodayGuestList.Count} guests for Day {currentDay}");

        // Log all guest details for testing
        LogAllGuestDetails();
    }

    /// <summary>
    /// 開始交易模式，開始抽選並回復資料
    /// </summary>
    public void StartTradeMode()
    {
        GenerateGuestList();
        LoadHistory();
        tradeView.UpdateTradeInfo(tradeProgress, TodayGuestList[tradeProgress.CustomerIndex], DataManager.Instance.CurrentPlayerData.InventoryItems.ToList(), tradeProgress.OnSelect);
    }

    /// <summary>
    /// 取得當前Guest
    /// </summary>
    public Guest GetCurrentGuest()
    {
        return currentGuest;
    }

    /// <summary>
    /// 取得所有Guest列表（用於測試）
    /// </summary>
    public List<Guest> GetAllGuestsForDebug()
    {
        return TodayGuestList;
    }

    /// <summary>
    /// 取得指定索引的Customer（用於測試）
    /// </summary>
    public Customer GetCustomerAt(int index)
    {
        if (TodayGuestList == null || index < 0 || index >= TodayGuestList.Count)
        {
            Debug.LogWarning($"[TradeMode] Invalid guest index: {index}");
            return null;
        }
        return TodayGuestList[index].customer;
    }

    /// <summary>
    /// 記錄所有Guest的詳細資訊（用於測試）
    /// </summary>
    private void LogAllGuestDetails()
    {
        if (TodayGuestList == null || TodayGuestList.Count == 0)
        {
            Debug.Log("[TradeMode] No guests to log.");
            return;
        }

        for (int i = 0; i < TodayGuestList.Count; i++)
        {
            var guest = TodayGuestList[i];
            var customer = guest.customer;
            var request = guest.request;

            var sb = new StringBuilder();
            sb.AppendLine($"===== Guest #{i} =====");

            // Customer基本資訊
            sb.AppendLine($"Profession: {customer.Profession}");
            sb.AppendLine($"Type: {customer.Type}");
            sb.AppendLine($"Traits: {string.Join(", ", customer.Traits)}");

            // Customer數值參數
            sb.AppendLine($"Patience: {customer.Patience}");
            sb.AppendLine($"BudgetMultiplier: {customer.BudgetMultiplier:F2}");
            sb.AppendLine($"AppraisalChance: {customer.AppraisalChance:F2}");
            sb.AppendLine($"BargainingPower: {customer.BargainingPower:F2}");
            sb.AppendLine($"IdentificationAbility: {customer.IdentificationAbility:F2}");
            sb.AppendLine($"LoseUp: {customer.LoseUp:F2}");
            sb.AppendLine($"Quality Preferences: {string.Join(", ", customer.Quality)}");

            // Request資訊
            if (request != null)
            {
                sb.AppendLine($"Request Type: {request.Type}");
                sb.AppendLine($"Target ItemType: {request.TargetItemType}");
                sb.AppendLine($"Target Tag: {request.TargetTag}");
                sb.AppendLine($"Target ItemId: {request.TargetItemId}");
                sb.AppendLine($"Dialog: {request.DialogText}");
            }
            else
            {
                sb.AppendLine("Request: NULL");
            }

            Debug.Log(sb.ToString());
        }
    }
    #region TradeLogic
    /// <summary>
    /// 檢查選擇的物品是否符合需求
    /// </summary>
    private void SelectToRequest()
    {
        var item = tradeView.SelectedItem;
        if (item == null) return;

        bool isSatisfied = tradeCheck.SelectToRequest(currentGuest, item);
        tradeView.SetPreferImage(isSatisfied);
    }

    /// <summary>
    /// 在 TradeMode 內部執行 Guest 的深拷貝
    /// </summary>
    private Guest CloneGuest(Guest original)
    {
        if (original == null) return null;

        Guest newGuest = new Guest();

        // 獲取私有的 MemberwiseClone 方法
        var memberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // 拷貝 Customer
        if (original.customer != null)
        {
            Customer customerClone = (Customer)memberwiseCloneMethod.Invoke(original.customer, null);
            // 處理 List 深拷貝
            if (original.customer.Traits != null) customerClone.Traits = new List<string>(original.customer.Traits);
            if (original.customer.Quality != null) customerClone.Quality = new List<string>(original.customer.Quality);
            if (original.customer.PreferredTags != null) customerClone.PreferredTags = new List<string>(original.customer.PreferredTags);
            newGuest.customer = customerClone;
        }

        // 拷貝 Request
        if (original.request != null)
        {
            newGuest.request = (CustomerRequest)memberwiseCloneMethod.Invoke(original.request, null);
        }

        return newGuest;
    }

    /// <summary>
    /// 載入玩家交易進度
    /// </summary>
    private void LoadHistory()
    {
        tradeProgress = DataManager.Instance.LoadTradeHistory();
        if (tradeProgress == null)
        {
            tradeProgress = new TradeProgress
            {
                CustomerIndex = 0,
                TradeTimes = 0,
                MaxPrice = 0,
                OnSelect = true,
                Patience = TodayGuestList[0].customer.Patience // 預設耐心值
            };
            currentGuest = TodayGuestList[0];
            TradeGuest = CloneGuest(currentGuest);
        }
        else if (tradeProgress.OnSelect)//有沒出價過
        {
            currentGuest = TodayGuestList[tradeProgress.CustomerIndex];
            TradeGuest = CloneGuest(currentGuest);
        }
        else//有出價過
        {
            currentGuest = TodayGuestList[tradeProgress.CustomerIndex];
            TradeGuest = CloneGuest(currentGuest);
        }
    }

    /// <summary>
    /// 開始交易
    /// </summary>
    private void StartTrade()
    {
        GameManager.Instance.gameFlow.SwitchGameStageAndSave(DayPhase.NightTrade);
    }
    /// <summary>
    /// 下一位客人
    /// </summary>
    private void NextGuest()
    {
        var PlayerInventory = DataManager.Instance.CurrentPlayerData.InventoryItems.ToList();
        tradeView.SetSelectTradeUI();
        tradeProgress.CustomerIndex += 1;
        if (PlayerInventory.Count <= 0)
        {
            ClearTradeProgress();
            //商品不足本日結束
            //本日結束存檔
        }
        if (tradeProgress.CustomerIndex >= TodayGuestList.Count)
        {
            ClearTradeProgress();
            //本日結束存檔
        }
        else//下一位
        {
            currentGuest = TodayGuestList[tradeProgress.CustomerIndex];
            tradeProgress.TradeTimes = 0;
            tradeProgress.MaxPrice = 0;
            tradeProgress.OnSelect = true;
            tradeProgress.Patience = TodayGuestList[tradeProgress.CustomerIndex].customer.Patience;
            TradeGuest = CloneGuest(currentGuest);
            tradeView.UpdateTradeInfo(tradeProgress, TodayGuestList[tradeProgress.CustomerIndex], PlayerInventory, tradeProgress.OnSelect);
        }
    }
    void ClearTradeProgress()
    {
        tradeProgress.NowItem = null;
        tradeProgress.CustomerIndex = 0;
        tradeProgress.TradeTimes = 0;
        tradeProgress.MaxPrice = 0;
        tradeProgress.Patience = 0;
        tradeProgress.OnSelect = true;
    }
    #endregion
    #region SelectItem
    private void TradeThisItem(Item item, ItemQuality itemQuality)
    {
        bool isSatisfied = tradeCheck.SelectToRequest(currentGuest, item);
        if (!isSatisfied)
        {
            TradeGuest.customer.AppraisalChance *= 0.8f;
        }
    }
    private void SetMarketPrice(Item item, ItemQuality itemQuality)
    {
        if (item == null || itemQuality == ItemQuality.None)
            return;
        currentSelectedItem = item;
        var itemDefinition = DataManager.Instance.GetItemById(item.ItemId);
        // 在這裡進行你需要的計算
        var price = tradeCheck.MarketPriceCalculate(itemDefinition, itemQuality, currentGuest.customer);
        SetPriceRange(price / 10, price * 5);
        MarketPrice = price;
        tradeView.UpdateMarketPrice(price);
    }
    private void SetPriceRange(int priceMin, int priceMax)//設定出價範圍
    {
        PriceMax = priceMax;
        PriceMin = priceMin;
    }
    private void SetCurrentPrice(int stage)
    {
        var price = PriceMin + (PriceMax - PriceMin) * (stage) / 49;
        if (stage == 15)
        {
            price = MarketPrice;
            tradeView.SetPriceView(price);
            tradeView.SetPriceSliderPos(stage);
        }
        else
        {
            tradeView.SetPriceView(price);
        }

    }
    #endregion
    #region TradePrice
    private void PriceTrade(int CurrentPrice)
    {
        tradeProgress.OnSelect = false;
        tradeProgress.NowItem = currentSelectedItem;
        int CustomerMaxPrice = (int)(MarketPrice * TradeGuest.customer.BudgetMultiplier);
        Debug.Log("CustomerMaxPrice:"+CustomerMaxPrice);
        float successRate = currentGuest.customer.AppraisalChance - (((float)CurrentPrice - CustomerMaxPrice) / CustomerMaxPrice) * 2;
        successRate += tradeProgress.TradeTimes * TradeGuest.customer.LoseUp; 
        Debug.Log((((float)CurrentPrice - CustomerMaxPrice) / CustomerMaxPrice) * 2);
        Debug.Log("successRate:"+successRate);
        if (successRate<=0)
        {
            //交易失敗扣兩心鎖定價格
            TradeGuest.customer.Patience -= 2;
            PriceMax = CurrentPrice;
            if (TradeGuest.customer.Patience <= 0)
            {
                GuestLeave();
            }
            else
            {
                NextPrice();
            }
        }
        else
        {
            // 組合 key：當前交易次數 + 職業 + 當前日期 + 顧客索引 + 需求類型
            int currentDay = DataManager.Instance.CurrentPlayerData.DaysPlayed;
            string requestType = TradeGuest.request != null ? TradeGuest.request.Type.ToString() : "None";
            string rngKey = $"Trade_{tradeProgress.TradeTimes}_{TradeGuest.customer.Profession}_{currentDay}_{tradeProgress.CustomerIndex}_{requestType}";
            bool isSuccess = GameRng.ValueKeyed(rngKey) < successRate;
            Debug.Log(GameRng.ValueKeyed(rngKey) + " " + successRate);
            if (isSuccess)
            {
                //交易成功
                DataManager.Instance.ModifyGold(CurrentPrice);
                DataManager.Instance.RemoveItem(currentSelectedItem);
                NextGuest();
            }
            else
            {
                TradeGuest.customer.Patience -= 1;
                //交易失敗
                if (TradeGuest.customer.Patience <= 0)
                {
                    GuestLeave();
                }
                else
                {
                    PriceMax = CurrentPrice;  
                    NextPrice();
                }
            }
        }
    }
    /// <summary>
    /// 顧客離開 - 耐心耗盡時觸發
    /// </summary>
    private void GuestLeave()
    {
        Debug.Log($"[TradeMode] 顧客 {tradeProgress.CustomerIndex} 離開，耐心耗盡");
        tradeProgress.NowItem = null;
        tradeProgress.OnSelect = true; 
        tradeProgress.TradeTimes = 0;
        // 記錄最高出價（如果有的話）
        if (tradeProgress.MaxPrice > 0)
        {
            Debug.Log($"[TradeMode] 未成交最高價: {tradeProgress.MaxPrice}");
        }
        
        // 切換到下一位顧客
        NextGuest();
    }
    
    /// <summary>
    /// 下一次出價 - 繼續議價
    /// </summary>
    private void NextPrice()
    {
        Debug.Log($"[TradeMode] 進入下一次出價，剩餘耐心: {TradeGuest.customer.Patience}");
        tradeProgress.TradeTimes++;
        // 記錄當前最高出價
        tradeProgress.MaxPrice = PriceMax;
        
        // 更新交易進度中的耐心值
        tradeProgress.Patience = TradeGuest.customer.Patience;
        
        // 更新 UI，允許玩家再次出價
        var PlayerInventory = DataManager.Instance.CurrentPlayerData.InventoryItems.ToList();
        tradeView.UpdateTradeInfo(tradeProgress, TradeGuest, PlayerInventory, false);
    }
    #endregion
}
