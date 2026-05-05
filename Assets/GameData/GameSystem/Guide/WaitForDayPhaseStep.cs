using GameSystem;

public class WaitForDayPhaseStep : GuideStep
{
    private readonly int targetDay;
    private readonly DayPhase targetPhase;
    private System.Action onComplete;
    private bool isCompleted;

    public WaitForDayPhaseStep(int targetDay, DayPhase targetPhase)
    {
        this.targetDay = targetDay;
        this.targetPhase = targetPhase;
    }

    public override void Execute(System.Action onComplete)
    {
        this.onComplete = onComplete;
        isCompleted = false;

        if (IsTargetReached())
        {
            Complete();
            return;
        }

        GameFlowEvents.OnDayChanged += HandleDayChanged;
        GameFlowEvents.OnDayPhaseChanged += HandleDayPhaseChanged;
    }

    public override void Dispose()
    {
        Unsubscribe();
    }

    private void HandleDayChanged(int day)
    {
        TryComplete();
    }

    private void HandleDayPhaseChanged(DayPhase phase)
    {
        TryComplete();
    }

    private void TryComplete()
    {
        if (IsTargetReached())
            Complete();
    }

    private void Complete()
    {
        if (isCompleted)
            return;

        isCompleted = true;
        Unsubscribe();
        onComplete?.Invoke();
    }

    private bool IsTargetReached()
    {
        IReadOnlyPlayerData playerData = DataManager.Instance?.CurrentPlayerData;
        return playerData != null
            && playerData.DaysPlayed == targetDay
            && playerData.PlayingStatus == targetPhase;
    }

    private void Unsubscribe()
    {
        GameFlowEvents.OnDayChanged -= HandleDayChanged;
        GameFlowEvents.OnDayPhaseChanged -= HandleDayPhaseChanged;
    }
}
