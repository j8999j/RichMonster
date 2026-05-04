using System.Collections.Generic;
using System.Linq;
using GameSystem;

public class EventsGenerator
{
    private readonly List<GameEventDefinition> _eventList;

    private const int DefaultEventCount = 4;
    private const int DAY_THRESHOLD_MID = 6;
    private const int DAY_THRESHOLD_LATE = 14;

    public EventsGenerator(Dictionary<string, GameEventDefinition> eventDict)
    {
        _eventList = eventDict != null
            ? eventDict.Values.Where(e => e != null).ToList()
            : new List<GameEventDefinition>();
    }

    public static EventTime GetEventTimeByDay(int dayNumber)
    {
        if (dayNumber < DAY_THRESHOLD_MID)
            return EventTime.Early;
        if (dayNumber < DAY_THRESHOLD_LATE)
            return EventTime.Mid;

        return EventTime.Late;
    }

    public List<GameEventDefinition> GenerateEventsForDay(int dayNumber)
    {
        EventTime currentTime = GetEventTimeByDay(dayNumber);
        return GenerateEventsForDayByTime(dayNumber, currentTime);
    }

    public List<GameEventDefinition> GenerateEventsForDay(int dayNumber, int count)
    {
        EventTime currentTime = GetEventTimeByDay(dayNumber);
        return GenerateEventsForDayByTime(dayNumber, currentTime, count);
    }

    public List<GameEventDefinition> GenerateEventsForDayByTime(int dayNumber, EventTime eventTime)
    {
        return GenerateEventsForDayByTime(dayNumber, eventTime, DefaultEventCount);
    }

    public List<GameEventDefinition> GenerateEventsForDayByTime(int dayNumber, EventTime eventTime, int count)
    {
        if (_eventList == null || _eventList.Count == 0 || count <= 0)
        {
            return new List<GameEventDefinition>();
        }

        List<GameEventDefinition> availableEvents = _eventList
            .Where(e => e.EventTimes != null && e.EventTimes.Contains(eventTime))
            .Select((eventDefinition, index) => new { Event = eventDefinition, Index = index })
            .GroupBy(x => GetEventUniqueKey(x.Event, x.Index))
            .Select(g => g.First().Event)
            .ToList();

        if (availableEvents.Count == 0)
        {
            return new List<GameEventDefinition>();
        }

        int eventCount = count < availableEvents.Count ? count : availableEvents.Count;
        var result = new List<GameEventDefinition>(eventCount);

        for (int i = 0; i < eventCount; i++)
        {
            int randomIndex = GameRng.RangeKeyed(0, availableEvents.Count, $"Event_Pick_Day{dayNumber}_{eventTime}_{i}");
            result.Add(availableEvents[randomIndex]);
            availableEvents.RemoveAt(randomIndex);
        }

        return result;
    }

    private static string GetEventUniqueKey(GameEventDefinition eventDefinition, int index)
    {
        if (!string.IsNullOrEmpty(eventDefinition.Id))
            return eventDefinition.Id;

        return $"__event_index_{index}";
    }
}
