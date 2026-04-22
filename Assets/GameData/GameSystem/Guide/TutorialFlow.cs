using System.Collections.Generic;
using UnityEngine;
public class TutorialFlow
{
    private const string SAVE_KEY = "TutorialSaveData";
    private readonly List<GuideTask> taskQueue = new List<GuideTask>();
    private int currentTaskIndex = 0;
    public void Start()
    {
        RegisterTasks();
        LoadTaskData();
        ExecuteNextTask();
    }
    //載入存檔任務進度
    private void LoadTaskData()
    {
        var data = DataManager.Instance.GetPersistentSaveData<TutorialSaveData>(SAVE_KEY);
        if (data.IsComplete)
        {
            currentTaskIndex = taskQueue.Count;
            return;
        }
        currentTaskIndex = data.CurrentTaskIndex;
        // 還原 Task1 專屬狀態
        if (currentTaskIndex < taskQueue.Count && taskQueue[currentTaskIndex] is Task1_FirstTutorial task1)
        {
            task1.IsPurchased = data.IsPurchased;
        }
    }

    private async void SaveProgress()
    {
        var data = new TutorialSaveData
        {
            CurrentTaskIndex = currentTaskIndex,
            CurrentStepIndex = currentTaskIndex < taskQueue.Count
                ? taskQueue[currentTaskIndex].CurrentStepIndex
                : 0,
            IsComplete = currentTaskIndex >= taskQueue.Count,
            LastUpdatedDay = DataManager.Instance.CurrentPlayerData.DaysPlayed
        };
        // 儲存 Task1 專屬狀態
        if (currentTaskIndex < taskQueue.Count && taskQueue[currentTaskIndex] is Task1_FirstTutorial task1)
        {
            data.IsPurchased = task1.IsPurchased;
        }
        DataManager.Instance.SetPlayerData(SAVE_KEY, data);
        try
        {
            await GameSystem.GameManager.Instance.gameFlow.SaveGameAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TutorialFlow] 教學進度存檔失敗: {ex}");
        }
    }

    private void RegisterTasks()
    {
        taskQueue.Add(new Task1_FirstTutorial());
        taskQueue.Add(new Task2_SecondTutorial());
    }

    private void ExecuteNextTask()
    {
        if (currentTaskIndex >= taskQueue.Count)
        {
            var savedData = DataManager.Instance.GetPersistentSaveData<TutorialSaveData>(SAVE_KEY);
            if (!savedData.IsComplete)
            {
                SaveProgress();
            }
            Debug.Log("[GameFlowGuide] 所有引導任務完成");
            return;
        }

        var task = taskQueue[currentTaskIndex];
        var data = DataManager.Instance.GetPersistentSaveData<TutorialSaveData>(SAVE_KEY);
        int startStep = (currentTaskIndex == data.CurrentTaskIndex) ? data.CurrentStepIndex : 0;

        Debug.Log($"[GameFlowGuide] 開始 {task.TaskName}");
        task.Start(OnTaskComplete, startStep, SaveProgress);
    }

    private void OnTaskComplete()
    {
        currentTaskIndex++;
        SaveProgress();
        ExecuteNextTask();
    }

    private void OnDestroy()
    {
        if (currentTaskIndex < taskQueue.Count)
            taskQueue[currentTaskIndex].Dispose();
    }
}