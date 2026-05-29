using System.Threading.Tasks;
using System.Collections.Generic;
using GameSystem;
using UnityEngine;

public class GameFlow
{
    // 前期 Day 1-5、中期 Day 6-13、後期 Day 14+
    public const int EarlyPhaseLastDay = 5;
    public const int MidPhaseLastDay = 13;

    public int CurrentDay { get; private set; }

    private readonly IReadOnlyPlayerData _currentPlayerData;
    private readonly int _saveSlot;
    private readonly TutorialFlow _tutorialFlow;
    private readonly GameFlowContext _context;
    private readonly Dictionary<DayPhase, IGamePhaseState> _states;
    private IGamePhaseState _currentState;

    public GameFlow(IReadOnlyPlayerData playerData, int saveSlot)
    {
        _currentPlayerData = playerData ?? new PlayerData();
        CurrentDay = _currentPlayerData.DaysPlayed;
        _tutorialFlow = new TutorialFlow();
        _saveSlot = Mathf.Max(0, saveSlot);

        _context = new GameFlowContext(
            DataManager.Instance,
            _currentPlayerData,
            () => CurrentDay,
            SaveGameAsync);

        _states = new Dictionary<DayPhase, IGamePhaseState>
        {
            { DayPhase.HumanDay, new HumanDayPhaseState() },
            { DayPhase.AfterNoon, new AfterNoonPhaseState() },
            { DayPhase.Night, new NightPhaseState() }
        };

        GamePhaseGuideRefreshListener.EnsureSubscribed();
        _states.TryGetValue(_currentPlayerData.PlayingStatus, out _currentState);

        GameRng.InitDailySeed(_currentPlayerData.MasterSeed, CurrentDay);
    }

    public void NextDay()
    {
        _ = NextDayAsync();
    }

    public async Task NextDayAsync()
    {
        CurrentDay++;
        DataManager.Instance.ModifyCurrentDay(CurrentDay);
        GameRng.InitDailySeed(_currentPlayerData.MasterSeed, CurrentDay);
        await SwitchGameStageAndSaveAsync(DayPhase.Night);
    }

    public void SwitchGameStageAndSave(DayPhase newPhase)
    {
        _ = SwitchGameStageAndSaveAsync(newPhase);
    }

    public async Task SwitchGameStageAndSaveAsync(DayPhase newPhase)
    {
        if (!_states.TryGetValue(newPhase, out var nextState))
        {
            Debug.LogWarning($"[GameFlow] 未知的遊戲階段: {newPhase}");
            return;
        }

        if (_currentState != null && !_currentState.CanTransitionTo(newPhase))
        {
            Debug.LogWarning($"[GameFlow] 不允許從 {_currentState.Phase} 切換到 {newPhase}");
            return;
        }

        _currentState = nextState;
        await _currentState.EnterAsync(_context);
    }

    public void StartTutorial()
    {
        var tutorialData = DataManager.Instance.GetRunSaveData<TutorialSaveData>(SaveDataKeys.Tutorial);
        if (!tutorialData.IsComplete && _currentPlayerData.DaysPlayed <= 1)
        {
            _tutorialFlow.Start();
        }
    }
    public async Task SaveGameAsync()
    {
        if (!DataManager.Instance.OnPlayerDataChanged) return;

        // 先清旗標再 await，寫檔期間若再有變更會重新把旗標標 dirty，
        // 下一次呼叫就會把那筆變更補寫進磁碟，避免被誤判為「已存」。
        DataManager.Instance.SetPlayerDataChanged(false);

        await DataManager.Instance.SaveRepository.SaveGameAsync(_currentPlayerData as PlayerData, _saveSlot);
        await DataManager.Instance.SaveAchievementAsync();
    }
}

public class GameFlowContext
{
    private readonly System.Func<int> _currentDayProvider;
    private readonly System.Func<Task> _saveGameAsync;

    public GameFlowContext(
        DataManager dataManager,
        IReadOnlyPlayerData playerData,
        System.Func<int> currentDayProvider,
        System.Func<Task> saveGameAsync)
    {
        DataManager = dataManager;
        PlayerData = playerData;
        _currentDayProvider = currentDayProvider;
        _saveGameAsync = saveGameAsync;
    }

    public DataManager DataManager { get; }
    public IReadOnlyPlayerData PlayerData { get; }
    public int CurrentDay => _currentDayProvider();

    public Task SaveGameAsync()
    {
        return _saveGameAsync();
    }

    public void ClearPhaseScopedData(DayPhase phase)
    {
        if (phase != DayPhase.AfterNoon)
        {
            DataManager.ClearOrderProgress();
        }
    }
}

public interface IGamePhaseState
{
    DayPhase Phase { get; }
    bool CanTransitionTo(DayPhase nextPhase);
    Task EnterAsync(GameFlowContext context);
}

public abstract class GamePhaseStateBase : IGamePhaseState
{
    public abstract DayPhase Phase { get; }

    public virtual bool CanTransitionTo(DayPhase nextPhase)
    {
        return true;
    }

    public async Task EnterAsync(GameFlowContext context)
    {
        context.ClearPhaseScopedData(Phase);
        context.DataManager.ModifyCurrentDayPhase(Phase);

        var endingType = EvaluateEnding(context.PlayerData);
        if (endingType != EndingType.None)
        {
            context.DataManager.SetEndingReached(endingType);
            await context.SaveGameAsync();
            return;
        }

        await OnEnterAsync(context);
        await context.SaveGameAsync();
    }

    protected virtual EndingType EvaluateEnding(IReadOnlyPlayerData playerData)
    {
        return EndingType.None;
    }

    protected virtual Task OnEnterAsync(GameFlowContext context)
    {
        return Task.CompletedTask;
    }
}

public class HumanDayPhaseState : GamePhaseStateBase
{
    public override DayPhase Phase => DayPhase.HumanDay;

    protected override EndingType EvaluateEnding(IReadOnlyPlayerData playerData)
    {
        return EndingConditionDetector.EvaluateForHumanDay(playerData);
    }

    protected override Task OnEnterAsync(GameFlowContext context)
    {
        GameEventCenter.Publish(new DayChangedEvent(context.CurrentDay));
        GameEventCenter.Publish(new DayEndedEvent(context.CurrentDay, context.PlayerData.Gold));
        return Task.CompletedTask;
    }
}

public class AfterNoonPhaseState : GamePhaseStateBase
{
    public override DayPhase Phase => DayPhase.AfterNoon;
}

public class NightPhaseState : GamePhaseStateBase
{
    public override DayPhase Phase => DayPhase.Night;

    protected override EndingType EvaluateEnding(IReadOnlyPlayerData playerData)
    {
        return EndingConditionDetector.EvaluateForNewMonsterDay(playerData);
    }
}

public static class GamePhaseGuideRefreshListener
{
    private static bool _isSubscribed;

    public static void EnsureSubscribed()
    {
        if (_isSubscribed) return;

        GameEventCenter.Subscribe<DayPhaseChangedEvent>(OnDayPhaseChanged);
        _isSubscribed = true;
    }

    private static void OnDayPhaseChanged(DayPhaseChangedEvent eventData)
    {
        GuaranteeDepositGuide.Refresh();
        AuctionEntryFeeGuide.Refresh();
        AuctionDayGuide.Refresh();
    }
}
