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
        if (newPhase != DayPhase.AfterNoon)
        {
            DataManager.Instance.ClearOrderProgress();
        }

        DataManager.Instance.ModifyCurrentDayPhase(newPhase);
        GameFlowEvents.InvokeDayPhaseChanged(newPhase);
        GuaranteeDepositGuide.Refresh();
        AuctionEntryFeeGuide.Refresh();
        AuctionDayGuide.Refresh();
        if (newPhase == DayPhase.Night)
        {
            EndingType endingType = EndingConditionDetector.EvaluateForNewMonsterDay(DataManager.Instance.CurrentPlayerData);
            if (endingType != EndingType.None)
            {
                DataManager.Instance.SetEndingReached(endingType);
                await SaveGameAsync();
                return;
            }
        }

        if (newPhase == DayPhase.HumanDay)
        {
            EndingType endingType = EndingConditionDetector.EvaluateForHumanDay(DataManager.Instance.CurrentPlayerData);
            if (endingType != EndingType.None)
            {
                DataManager.Instance.SetEndingReached(endingType);
                await SaveGameAsync();
                return;
            }

            GameFlowEvents.InvokeDayChanged(CurrentDay);
            AchievementEvents.DayEndGold(_currentPlayerData.Gold);
        }
        await SaveGameAsync();
    }
    public void StartTutorial()
    {
        var tutorialData = DataManager.Instance.GetPersistentSaveData<TutorialSaveData>(SaveDataKeys.Tutorial);
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
