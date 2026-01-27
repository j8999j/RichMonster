using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystem;

/// <summary>
/// 人類訂單管理模式
/// 整合事件生成器與訂單生成器，根據當日事件抽取對應的大型訂單與小型訂單
/// </summary>
[RequireComponent(typeof(HumanOrderView))]
public class HumanOrderMode : MonoBehaviour
{
    // 大型訂單數量範圍
    private const int MinLargeOrderCount = 1;
    private const int MaxLargeOrderCount = 2;

    // 當日訂單結果
    private List<HumanLargeOrder> _todayLargeOrders = new List<HumanLargeOrder>();
    private List<HumanSmallOrder> _todaySmallOrders = new List<HumanSmallOrder>();
    private List<GameEventDefinition> _todayEvents = new List<GameEventDefinition>();

    // 生成器
    private EventsGenerator _eventsGenerator;
    private HumanSmallOrderGenerator _smallOrderGenerator;
    // UI
    private HumanOrderView _humanOrderView;
    private HumanLargeOrder SelectedLargeOrder;
    private HumanSmallOrder SelectedSmallOrder;
    private List<Item> SelectedOrderItems = new List<Item>();

    public IReadOnlyList<HumanLargeOrder> TodayLargeOrders => _todayLargeOrders;
    public IReadOnlyList<HumanSmallOrder> TodaySmallOrders => _todaySmallOrders;
    public IReadOnlyList<GameEventDefinition> TodayEvents => _todayEvents;

    private void Awake()
    {
        InitializeGenerators();
        GenerateTodayOrders(GameManager.Instance.gameFlow.CurrentDay);
        _humanOrderView = GetComponent<HumanOrderView>();
    }
    private void OnEnable()
    {
        _humanOrderView.OnSelectedLargeOrder += OnSelectedOrder;
        _humanOrderView.OnSelectedSmallOrder += OnSelectedOrder;
        _humanOrderView.OnConfirmOrder += ConfirmOrder;
        _humanOrderView.AddItemToOrder += TryAddItemToOrder;
        _humanOrderView.OnOrderCancelSelected += RemoveItemFromOrderRange;
        _humanOrderView.OnOpenOrderPanel += ShowTodayOrder;
    }
    private void OnDisable()
    {
        _humanOrderView.OnSelectedLargeOrder -= OnSelectedOrder;
        _humanOrderView.OnSelectedSmallOrder -= OnSelectedOrder;
        _humanOrderView.OnConfirmOrder -= ConfirmOrder;
        _humanOrderView.AddItemToOrder -= TryAddItemToOrder;
        _humanOrderView.OnOrderCancelSelected -= RemoveItemFromOrderRange;
        _humanOrderView.OnOpenOrderPanel -= ShowTodayOrder;
    }
    #region GeneratorsEvents
    /// <summary>
    /// 初始化生成器
    /// </summary>
    public void InitializeGenerators()
    {
        var dataManager = DataManager.Instance;
        if (dataManager == null || !dataManager.IsInitialized)
        {
            Debug.LogWarning("[HumanOrderMode] DataManager 尚未初始化");
            return;
        }

        // 初始化事件生成器
        _eventsGenerator = new EventsGenerator(
            dataManager.EventDict.ToDictionary(kv => kv.Key, kv => kv.Value)
        );
        // 初始化小型訂單生成器
        _smallOrderGenerator = new HumanSmallOrderGenerator(
            dataManager.HumanSmallOrderDict.ToDictionary(kv => kv.Key, kv => kv.Value)
        );
    }
    private void LoadOrdersHistory()
    {
        var orderHistory = DataManager.Instance.CurrentPlayerData.OrderHistory;
        if (orderHistory == null || orderHistory.Count == 0) return;
        // 建立已完成訂單 ID 的快速查詢集合
        var completedOrderIds = new HashSet<string>(
            orderHistory.Where(o => o.IsCompleted).Select(o => o.OrderID)
        );
        // 更新大型訂單的完成狀態
        foreach (var order in _todayLargeOrders)
        {
            if (completedOrderIds.Contains(order.OrderId))
            {
                order.IsFinish = true;
            }
        }
        // 更新小型訂單的完成狀態
        foreach (var order in _todaySmallOrders)
        {
            if (completedOrderIds.Contains(order.OrderId))
            {
                order.IsFinish = true;
            }
        }
    }
    /// <summary>
    /// 生成當日所有訂單
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    public void GenerateTodayOrders(int dayNumber)
    {
        if (_eventsGenerator == null || _smallOrderGenerator == null)
        {
            InitializeGenerators();
        }

        // 1. 先抽取當日事件
        _todayEvents = _eventsGenerator.GenerateEventsForDay(dayNumber);

        // 2. 根據事件抽取大型訂單
        _todayLargeOrders = GenerateLargeOrdersFromEvents(dayNumber, _todayEvents);

        // 3. 抽取小型訂單
        _todaySmallOrders = _smallOrderGenerator.GenerateOrdersForDay(dayNumber);

        Debug.Log($"[HumanOrderMode] Day {dayNumber} 生成完成: " +
                  $"事件={_todayEvents.Count}, 大型訂單={_todayLargeOrders.Count}, 小型訂單={_todaySmallOrders.Count}");
    }

    /// <summary>
    /// 根據事件生成大型訂單（每個事件各 1-2 張，不可重複）
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    /// <param name="events">當日事件列表</param>
    /// <returns>大型訂單列表</returns>
    private List<HumanLargeOrder> GenerateLargeOrdersFromEvents(int dayNumber, List<GameEventDefinition> events)
    {
        var result = new List<HumanLargeOrder>();
        var dataManager = DataManager.Instance;

        if (events == null || events.Count == 0 || dataManager == null)
        {
            return result;
        }

        // 用於追蹤已選取的訂單，確保跨事件也不重複
        var pickedOrderIds = new HashSet<string>();

        // 取得通用訂單（OrderEventId 為空或 null）
        var genericOrders = dataManager.HumanLargeOrderDict.Values
            .Where(order => order != null && string.IsNullOrEmpty(order.OrderEventId))
            .ToList();

        int eventIndex = 0;
        foreach (var gameEvent in events)
        {
            if (gameEvent == null || string.IsNullOrEmpty(gameEvent.Id))
            {
                eventIndex++;
                continue;
            }

            // 根據此事件ID篩選符合的大型訂單
            var eventOrders = dataManager.HumanLargeOrderDict.Values
                .Where(order => order != null &&
                               !string.IsNullOrEmpty(order.OrderEventId) &&
                               order.OrderEventId == gameEvent.Id)
                .ToList();

            // 合併此事件的訂單與通用訂單
            var availableOrders = eventOrders.Concat(genericOrders)
                .Where(order => !pickedOrderIds.Contains(order.OrderId)) // 排除已選取的
                .Distinct()
                .ToList();

            if (availableOrders.Count == 0)
            {
                eventIndex++;
                continue;
            }

            // 每個事件抽取 1-2 張訂單
            int orderCount = GameRng.RangeKeyed(MinLargeOrderCount, MaxLargeOrderCount + 1,
                $"LargeOrder_Count_Day{dayNumber}_Event{eventIndex}");
            orderCount = Mathf.Min(orderCount, availableOrders.Count);

            // 不可重複抽取
            var localPickedIndices = new HashSet<int>();
            for (int i = 0; i < orderCount; i++)
            {
                var availableIndices = Enumerable.Range(0, availableOrders.Count)
                    .Where(idx => !localPickedIndices.Contains(idx))
                    .ToList();

                if (availableIndices.Count == 0) break;

                int pickIndex = GameRng.RangeKeyed(0, availableIndices.Count,
                    $"LargeOrder_Pick_Day{dayNumber}_Event{eventIndex}_{i}");
                int selectedIndex = availableIndices[pickIndex];

                var selectedOrder = availableOrders[selectedIndex];
                localPickedIndices.Add(selectedIndex);
                pickedOrderIds.Add(selectedOrder.OrderId);
                result.Add(selectedOrder);
            }

            eventIndex++;
        }

        return result;
    }

    /// <summary>
    /// 取得所有當日訂單（大型 + 小型）
    /// </summary>
    public (List<HumanLargeOrder> largeOrders, List<HumanSmallOrder> smallOrders) GetTodayOrders()
    {
        return (_todayLargeOrders, _todaySmallOrders);
    }
    /// <summary>
    /// 清空當日訂單
    /// </summary>
    public void ClearTodayOrders()
    {
        _todayEvents.Clear();
        _todayLargeOrders.Clear();
        _todaySmallOrders.Clear();
    }
    #endregion
    #region UIMethods
    public void ShowLargeOrderBagItems(HumanLargeOrder order)
    {
        var allItems = DataManager.Instance.CurrentPlayerData.InventoryItems;
        List<Item> matchedItems = new List<Item>();
        List<Item> unmatchedTypeItems = new List<Item>();
        foreach (var item in allItems)
        {
            var definition = DataManager.Instance.GetItemById(item.ItemId);
            if (definition == null) continue;
            bool worldMatch = definition.World == ItemWorld.Monster;
            if (!worldMatch) continue;
            bool typeMatch = definition.Type == order.OrderType;
            // 符合類型且至少符合一個標籤
            bool tagMatch = definition.Tags.Any(tag => order.OrderNeedTags.Contains(tag));
            if (typeMatch && tagMatch)
            {
                matchedItems.Add(item);
            }
            else
            {
                // 類型不符合的存到另一個列表
                unmatchedTypeItems.Add(item);
            }
        }
        // 按符合標籤數量降序排序
        matchedItems = matchedItems
            .OrderByDescending(item =>
            {
                var definition = DataManager.Instance.GetItemById(item.ItemId);
                return definition?.Tags.Count(tag => order.OrderNeedTags.Contains(tag)) ?? 0;
            })
            .ToList();

        _humanOrderView.ShowBagItems(matchedItems);
        _humanOrderView.ShowUnmatchedBagItems(unmatchedTypeItems);
    }
    public void ShowSmallOrderBagItems(HumanSmallOrder order)
    {
        var allItems = DataManager.Instance.CurrentPlayerData.InventoryItems;
        List<Item> matchedItems = new List<Item>();
        List<Item> unmatchedTypeItems = new List<Item>();
        foreach (var item in allItems)
        {
            var definition = DataManager.Instance.GetItemById(item.ItemId);
            if (definition == null) continue;
            bool worldMatch = definition.World == ItemWorld.Monster;
            if (!worldMatch) continue;
            bool typeMatch = definition.Type == order.OrderType;
            if (typeMatch)
            {
                // 符合類型即可
                matchedItems.Add(item);
            }
            else
            {
                // 類型不符合的存到另一個列表
                unmatchedTypeItems.Add(item);
            }
        }
        // 按符合標籤數量降序排序
        matchedItems = matchedItems
            .OrderByDescending(item =>
            {
                var definition = DataManager.Instance.GetItemById(item.ItemId);
                return definition?.Tags.Count(tag => order.OrderNeedTags.Contains(tag)) ?? 0;
            })
            .ToList();
        _humanOrderView.ShowBagItems(matchedItems);
        _humanOrderView.ShowUnmatchedBagItems(unmatchedTypeItems);
    }
    private void ShowTodayOrder()
    {
        LoadOrdersHistory();
        var (largeOrders, smallOrders) = GetTodayOrders();
        Debug.Log($"ShowTodayOrder: largeOrders.Count = {largeOrders.Count}, smallOrders.Count = {smallOrders.Count}");
        _humanOrderView.ShowAllOrderSlots(largeOrders, smallOrders);
    }
    #endregion
    #region UI 操作
    private void OnSelectedOrder(HumanLargeOrder order)
    {
        ClearSelectedData();
        _humanOrderView.UpdateOrderSelectCount(SelectedOrderItems.Count, 3);
        SelectedLargeOrder = order;
        ShowLargeOrderBagItems(order);
        if (order.IsFinish)
        {
            _humanOrderView.ShowOrderFinish();
        }
    }
    private void OnSelectedOrder(HumanSmallOrder order)
    {
        ClearSelectedData();
        _humanOrderView.UpdateOrderSelectCount(SelectedOrderItems.Count, 1);
        SelectedSmallOrder = order;
        ShowSmallOrderBagItems(order);
        if (order.IsFinish)
        {
            _humanOrderView.ShowOrderFinish();
        }
    }
    public void TryAddItemToOrder(BagSlot Item)
    {
        if (SelectedLargeOrder == null && SelectedSmallOrder == null) return;
        OrderBagSlot orderBagSlot = Item as OrderBagSlot;
        if (orderBagSlot.OnSelected) return;
        ItemDefinition definition = DataManager.Instance.GetItemById(Item._currentData.ItemId);
        if (SelectedLargeOrder != null && SelectedLargeOrder.IsFinish == false)
        {
            if (SelectedOrderItems.Count >= 3) return;
            if (orderBagSlot.OnSelected) return;
            orderBagSlot.SetOnSelected(true);
            if (definition.Type == SelectedLargeOrder.OrderType && SelectedLargeOrder.OrderNeedTags.Any(tag => definition.Tags.Contains(tag)))
            {
                SelectedOrderItems.Add(Item._currentData);
                _humanOrderView.NewSelectItem(Item as OrderBagSlot);
                _humanOrderView.UpdateOrderSelectCount(SelectedOrderItems.Count, 3);
            }
            int TradePrice = LargeOrderTrade(SelectedOrderItems, SelectedLargeOrder);
            _humanOrderView.UpdateTradePrice(TradePrice);
        }
        else if (SelectedSmallOrder != null && SelectedSmallOrder.IsFinish == false)
        {
            if (SelectedOrderItems.Count >= 1) return;
            if (definition.Type == SelectedSmallOrder.OrderType)
            {
                if (orderBagSlot.OnSelected) return;
                orderBagSlot.SetOnSelected(true);
                if (SelectedOrderItems.Contains(Item._currentData)) return;
                SelectedOrderItems.Add(Item._currentData);
                _humanOrderView.NewSelectItem(Item as OrderBagSlot);
                _humanOrderView.UpdateOrderSelectCount(SelectedOrderItems.Count, 1);
                int TradePrice = SmallOrderTrade(SelectedOrderItems[0], SelectedSmallOrder);
                _humanOrderView.UpdateTradePrice(TradePrice);
            }
        }
    }
    public void RemoveItemFromOrderRange(OrderBagSlot Item)
    {
        if (SelectedLargeOrder == null && SelectedSmallOrder == null) return;
        if (SelectedOrderItems.Contains(Item._currentData))
        {
            SelectedOrderItems.Remove(Item._currentData);
        }
        if (SelectedLargeOrder != null)
        {
            _humanOrderView.UpdateOrderSelectCount(SelectedOrderItems.Count, 3);
            int TradePrice = LargeOrderTrade(SelectedOrderItems, SelectedLargeOrder);
            _humanOrderView.UpdateTradePrice(TradePrice);
        }
        else if (SelectedSmallOrder != null)
        {
            _humanOrderView.UpdateOrderSelectCount(SelectedOrderItems.Count, 1);
        }
    }
    private async void ConfirmOrder()
    {
        if (SelectedLargeOrder != null)
        {
            // 先標記訂單為已完成
            SelectedLargeOrder.IsFinish = true;
            DataManager.Instance.AddOrderProgress(SelectedLargeOrder.OrderId);
            int TradePrice = LargeOrderTrade(SelectedOrderItems, SelectedLargeOrder);
            DataManager.Instance.ModifyGold(TradePrice);
            foreach (var item in SelectedOrderItems)
            {
                DataManager.Instance.RemoveItem(item);
            }
            // 刷新訂單列表顯示
            ShowTodayOrder();
            ShowLargeOrderBagItems(SelectedLargeOrder);
            ClearSelectedData();
            _humanOrderView.ClearOrderView();
            await GameManager.Instance.gameFlow.SaveGameAsync();
        }
        else if (SelectedSmallOrder != null)
        {
            // 先標記訂單為已完成
            SelectedSmallOrder.IsFinish = true;
            DataManager.Instance.AddOrderProgress(SelectedSmallOrder.OrderId);
            int TradePrice = SmallOrderTrade(SelectedOrderItems[0], SelectedSmallOrder);
            DataManager.Instance.ModifyGold(TradePrice);
            DataManager.Instance.RemoveItem(SelectedOrderItems[0]);
            // 刷新訂單列表顯示
            ShowTodayOrder();
            ShowSmallOrderBagItems(SelectedSmallOrder);
            ClearSelectedData();
            _humanOrderView.ClearOrderView();
            await GameManager.Instance.gameFlow.SaveGameAsync();
        }
    }
    private void ClearSelectedData()//清空選擇的訂單與物品
    {
        SelectedLargeOrder = null;
        SelectedSmallOrder = null;
        SelectedOrderItems.Clear();
        _humanOrderView.ClearBagDetail();
        _humanOrderView.ClearOrderView();
    }
    #endregion
    #region TradeCalculate
    public int LargeOrderTrade(List<Item> ItemList, HumanLargeOrder order)
    {
        List<ItemDefinition> itemDefinitions = ItemList.Select(item => DataManager.Instance.GetItemById(item.ItemId)).ToList();
        // 每個 ItemDefinition 的 Tags 中符合 OrderNeedTags 的數量加總
        int SumTags = itemDefinitions.Sum(item => item.Tags.Count(tag => order.OrderNeedTags.Contains(tag)));
        int TradeBasePrice = itemDefinitions.Sum(item => item.BasePrice);
        float TradeMutiply = 1.024f + (0.076f * SumTags * SumTags);
        int TradePrice = (int)(TradeBasePrice * TradeMutiply);
        return TradePrice;
    }
    public int SmallOrderTrade(Item Item, HumanSmallOrder order)
    {
        ItemDefinition itemDefinition = DataManager.Instance.GetItemById(Item.ItemId);
        int SumTags = itemDefinition.Tags.Count(tag => order.OrderNeedTags.Contains(tag));
        int TradePrice = 0;
        if (SumTags == 0)
        {
            TradePrice = (int)(itemDefinition.BasePrice * 1.2f);
        }
        else
        {
            TradePrice = (int)(itemDefinition.BasePrice * 1.6f);
        }
        return TradePrice;
    }
    #endregion
}
