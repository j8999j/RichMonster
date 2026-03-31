using System.Threading.Tasks;
using GameSystem;
using UnityEngine;


public class GameFlow
{
    private const int DAY_THRESHOLD_MID = 6;   // 進入中期的天數
    private const int DAY_THRESHOLD_LATE = 14; // 進入後期的天數

    public int CurrentDay { get; private set; }
    private readonly IReadOnlyPlayerData _currentPlayerData;
    private readonly int _saveSlot;
    private readonly TutorialFlow _tutorialFlow;
    public GameFlow(IReadOnlyPlayerData playerData, int saveSlot)
    {
        _currentPlayerData = playerData ?? new PlayerData();
        CurrentDay = _currentPlayerData.DaysPlayed;
        _tutorialFlow = new TutorialFlow();
        _saveSlot = Mathf.Max(0, saveSlot);
        //確定種子
        GameRng.InitDailySeed(_currentPlayerData.MasterSeed, CurrentDay);
    }
    public void NextDay()
    {
        CurrentDay++;
        DataManager.Instance.ModifyCurrentDay(CurrentDay);
        GameRng.InitDailySeed(_currentPlayerData.MasterSeed, CurrentDay);
        SwitchGameStageAndSave(DayPhase.Night);
    }
    public async void SwitchGameStageAndSave(DayPhase newPhase)
    {
        DataManager.Instance.ClearOrderProgress();
        DataManager.Instance.ModifyCurrentDayPhase(newPhase);
        GameFlowEvents.InvokeDayPhaseChanged(newPhase);
        if (newPhase == DayPhase.Night)
        {
            DataManager.Instance.SetIsTrade(false);
        }
        else if(newPhase == DayPhase.AfterNoon)
        {
            DataManager.Instance.SetIsTrade(true);
        }
        else if(newPhase == DayPhase.HumanDay)
        {
            DataManager.Instance.SetIsTrade(false);
            GameFlowEvents.InvokeDayChanged(CurrentDay);
            AchievementEvents.DayEndGold(_currentPlayerData.Gold);
        }
        await SaveGameAsync();
    }
    public void StartTutorial()
    {
        if(_currentPlayerData.DaysPlayed == 0)
        {
            _tutorialFlow.Start();
        }
    }
    public async Task SaveGameAsync()
    {
        if (DataManager.Instance.OnPlayerDataChanged)
        {
            await SaveManager.Instance.SaveGameAsync(_currentPlayerData as PlayerData, _saveSlot);
            await DataManager.Instance.SaveAchievementAsync();
            DataManager.Instance.SetPlayerDataChanged(false);
        }
    }
}



