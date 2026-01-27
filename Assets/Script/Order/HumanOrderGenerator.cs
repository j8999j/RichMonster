using System.Collections.Generic;
using System.Linq;
using GameSystem;

/// <summary>
/// 人類小型訂單生成器
/// 根據日期隨機生成 3-5 個訂單（不可重複）
/// </summary>
public class HumanSmallOrderGenerator
{
    private readonly Dictionary<string, HumanSmallOrder> _orderDict;
    private List<HumanSmallOrder> _orderList;
    
    // 每日訂單數量範圍
    private const int MinOrderCount = 3;
    private const int MaxOrderCount = 5;
    
    public HumanSmallOrderGenerator(Dictionary<string, HumanSmallOrder> orderDict)
    {
        _orderDict = orderDict;
        _orderList = orderDict.Values.ToList();
    }
    
    /// <summary>
    /// 根據日期生成當日訂單列表
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    /// <returns>當日訂單列表（不可重複）</returns>
    public List<HumanSmallOrder> GenerateOrdersForDay(int dayNumber)
    {
        if (_orderList == null || _orderList.Count == 0)
        {
            return new List<HumanSmallOrder>();
        }
        
        // 隨機決定訂單數量 (3-5)
        int orderCount = GameRng.RangeKeyed(MinOrderCount, MaxOrderCount + 1, $"SmallOrder_Count_Day{dayNumber}");
        orderCount = System.Math.Min(orderCount, _orderList.Count); // 確保不超過可用訂單數量
        
        var result = new List<HumanSmallOrder>();
        var pickedIndices = new HashSet<int>();
        
        // 隨機抽取訂單（不可重複）
        for (int i = 0; i < orderCount; i++)
        {
            // 建立可用索引列表（排除已選取的）
            var availableIndices = Enumerable.Range(0, _orderList.Count)
                .Where(idx => !pickedIndices.Contains(idx))
                .ToList();
            
            if (availableIndices.Count == 0) break;
            
            string orderKey = $"SmallOrder_Pick_Day{dayNumber}_{i}";
            int pickIndex = GameRng.RangeKeyed(0, availableIndices.Count, orderKey);
            int selectedIndex = availableIndices[pickIndex];
            
            pickedIndices.Add(selectedIndex);
            result.Add(_orderList[selectedIndex]);
        }
        
        return result;
    }
    
    /// <summary>
    /// 根據日期生成當日訂單列表（指定數量）
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    /// <param name="count">訂單數量</param>
    /// <returns>當日訂單列表（不可重複）</returns>
    public List<HumanSmallOrder> GenerateOrdersForDay(int dayNumber, int count)
    {
        if (_orderList == null || _orderList.Count == 0)
        {
            return new List<HumanSmallOrder>();
        }
        
        count = System.Math.Min(count, _orderList.Count); // 確保不超過可用訂單數量
        
        var result = new List<HumanSmallOrder>();
        var pickedIndices = new HashSet<int>();
        
        // 隨機抽取訂單（不可重複）
        for (int i = 0; i < count; i++)
        {
            // 建立可用索引列表（排除已選取的）
            var availableIndices = Enumerable.Range(0, _orderList.Count)
                .Where(idx => !pickedIndices.Contains(idx))
                .ToList();
            
            if (availableIndices.Count == 0) break;
            
            string orderKey = $"SmallOrder_Pick_Day{dayNumber}_{i}";
            int pickIndex = GameRng.RangeKeyed(0, availableIndices.Count, orderKey);
            int selectedIndex = availableIndices[pickIndex];
            
            pickedIndices.Add(selectedIndex);
            result.Add(_orderList[selectedIndex]);
        }
        
        return result;
    }
}
