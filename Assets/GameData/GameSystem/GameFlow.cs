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
    public GameFlow(IReadOnlyPlayerData playerData, int saveSlot)
    {
        _currentPlayerData = playerData ?? new PlayerData();
        CurrentDay = _currentPlayerData.DaysPlayed;
        _saveSlot = Mathf.Max(0, saveSlot);
        //確定種子
        GameRng.InitDailySeed(_currentPlayerData.MasterSeed, CurrentDay);
    }
    public void NextDay()
    {
        CurrentDay++;
        DataManager.Instance.ModifyCurrentDay(CurrentDay);
        DataManager.Instance.ClearShopShelfData();
        GameRng.InitDailySeed(_currentPlayerData.MasterSeed, CurrentDay);
        SwitchGameStageAndSave(DayPhase.Night);
    }
    public async void SwitchGameStageAndSave(DayPhase newPhase)
    {
        DataManager.Instance.ClearOrderProgress();
        DataManager.Instance.ModifyCurrentDayPhase(newPhase);
        await SaveGameAsync();
    }
    public async Task SaveGameAsync()
    {
        await SaveManager.Instance.SaveGameAsync(_currentPlayerData as PlayerData, _saveSlot);
    }
}


