using System.Collections.Generic;
using System.Linq;
using GameSystem;

/// <summary>
/// 事件生成器
/// 根據日期隨機生成 2-4 個事件
/// 會根據天數自動判斷時期（Early/Mid/Late）並只抽取該時期的事件
/// </summary>
public class EventsGenerator
{
    private readonly Dictionary<string, GameEventDefinition> _eventDict;
    private List<GameEventDefinition> _eventList;
    
    // 每日事件數量範圍
    private const int MinEventCount = 2;
    private const int MaxEventCount = 4;
    
    // 天數階段閾值（與 GameFlow 對應）
    private const int DAY_THRESHOLD_MID = 6;   // 進入中期的天數
    private const int DAY_THRESHOLD_LATE = 14; // 進入後期的天數
    
    public EventsGenerator(Dictionary<string, GameEventDefinition> eventDict)
    {
        _eventDict = eventDict;
        _eventList = eventDict.Values.ToList();
    }
    
    /// <summary>
    /// 根據天數判斷當前遊戲時期
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    /// <returns>對應的事件時期</returns>
    public static EventTime GetEventTimeByDay(int dayNumber)
    {
        if (dayNumber < DAY_THRESHOLD_MID)
            return EventTime.Early;
        else if (dayNumber < DAY_THRESHOLD_LATE)
            return EventTime.Mid;
        else
            return EventTime.Late;
    }
    
    /// <summary>
    /// 根據日期生成當日事件列表
    /// 會自動根據天數判斷時期，只抽取該時期的事件
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    /// <returns>當日事件列表（可重複）</returns>
    public List<GameEventDefinition> GenerateEventsForDay(int dayNumber)
    {
        // 根據天數自動判斷時期
        EventTime currentTime = GetEventTimeByDay(dayNumber);
        return GenerateEventsForDayByTime(dayNumber, currentTime);
    }
    
    /// <summary>
    /// 根據日期生成當日事件列表（指定數量）
    /// 會自動根據天數判斷時期，只抽取該時期的事件
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    /// <param name="count">事件數量</param>
    /// <returns>當日事件列表</returns>
    public List<GameEventDefinition> GenerateEventsForDay(int dayNumber, int count)
    {
        // 根據天數自動判斷時期
        EventTime currentTime = GetEventTimeByDay(dayNumber);
        return GenerateEventsForDayByTime(dayNumber, currentTime, count);
    }
    
    /// <summary>
    /// 根據事件時段篩選事件後再抽取（2-4個）
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    /// <param name="eventTime">事件時段</param>
    /// <returns>該時段可用的事件列表</returns>
    public List<GameEventDefinition> GenerateEventsForDayByTime(int dayNumber, EventTime eventTime)
    {
        if (_eventList == null || _eventList.Count == 0)
        {
            return new List<GameEventDefinition>();
        }
        
        // 篩選符合時段的事件
        var filteredEvents = _eventList
            .Where(e => e.EventTimes != null && e.EventTimes.Contains(eventTime))
            .ToList();
        
        if (filteredEvents.Count == 0)
        {
            return new List<GameEventDefinition>();
        }
        
        // 隨機決定事件數量 (2-4)
        int eventCount = GameRng.RangeKeyed(MinEventCount, MaxEventCount + 1, $"Event_Count_Day{dayNumber}_{eventTime}");
        
        var result = new List<GameEventDefinition>();
        
        // 隨機抽取事件（可重複）
        for (int i = 0; i < eventCount; i++)
        {
            int randomIndex = GameRng.RangeKeyed(0, filteredEvents.Count, $"Event_Pick_Day{dayNumber}_{eventTime}_{i}");
            result.Add(filteredEvents[randomIndex]);
        }
        
        return result;
    }
    
    /// <summary>
    /// 根據事件時段篩選事件後再抽取（指定數量）
    /// </summary>
    /// <param name="dayNumber">遊戲天數</param>
    /// <param name="eventTime">事件時段</param>
    /// <param name="count">事件數量</param>
    /// <returns>該時段可用的事件列表</returns>
    public List<GameEventDefinition> GenerateEventsForDayByTime(int dayNumber, EventTime eventTime, int count)
    {
        if (_eventList == null || _eventList.Count == 0)
        {
            return new List<GameEventDefinition>();
        }
        
        // 篩選符合時段的事件
        var filteredEvents = _eventList
            .Where(e => e.EventTimes != null && e.EventTimes.Contains(eventTime))
            .ToList();
        
        if (filteredEvents.Count == 0)
        {
            return new List<GameEventDefinition>();
        }
        
        var result = new List<GameEventDefinition>();
        
        // 隨機抽取事件（可重複）
        for (int i = 0; i < count; i++)
        {
            int randomIndex = GameRng.RangeKeyed(0, filteredEvents.Count, $"Event_Pick_Day{dayNumber}_{eventTime}_{i}");
            result.Add(filteredEvents[randomIndex]);
        }
        
        return result;
    }
}
