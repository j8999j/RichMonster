using System;
public static class GameFlowEvents
{
    public static event Action<DayPhase> OnDayPhaseChanged;
    public static event Action<int> OnDayChanged;
    public static void InvokeDayPhaseChanged(DayPhase state)
    {
        OnDayPhaseChanged?.Invoke(state);
    }
    public static void InvokeDayChanged(int day)
    {
        OnDayChanged?.Invoke(day);
    }
}