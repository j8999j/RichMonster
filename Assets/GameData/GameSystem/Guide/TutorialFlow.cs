using System.Collections.Generic;
using UnityEngine;

public class TutorialFlow
{
    private const string SAVE_KEY = "TutorialSaveData";

    private readonly List<GuideTask> taskQueue = new List<GuideTask>();
    private int currentTaskIndex;

    public void Start()
    {
        RegisterTasks();
        LoadTaskData();
        ExecuteNextTask();
    }

    private void LoadTaskData()
    {
        var data = DataManager.Instance.GetPersistentSaveData<TutorialSaveData>(SAVE_KEY);
        if (data.IsComplete)
        {
            currentTaskIndex = taskQueue.Count;
            return;
        }

        currentTaskIndex = Mathf.Clamp(data.CurrentTaskIndex, 0, taskQueue.Count);
        LoadCurrentTaskState(data);
    }

    private async void SaveProgress()
    {
        var data = new TutorialSaveData
        {
            CurrentTaskIndex = currentTaskIndex,
            CurrentStepIndex = currentTaskIndex < taskQueue.Count
                ? taskQueue[currentTaskIndex].CurrentStepIndexForSave
                : 0,
            CurrentStepId = currentTaskIndex < taskQueue.Count
                ? taskQueue[currentTaskIndex].CurrentStepIdForSave
                : null,
            IsComplete = currentTaskIndex >= taskQueue.Count,
            LastUpdatedDay = DataManager.Instance.CurrentPlayerData.DaysPlayed
        };

        WriteCurrentTaskState(data);
        DataManager.Instance.SetPlayerData(SAVE_KEY, data);

        try
        {
            await GameSystem.GameManager.Instance.gameFlow.SaveGameAsync();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TutorialFlow] Failed to save tutorial progress: {ex}");
        }
    }

    private void RegisterTasks()
    {
        taskQueue.Add(new Task1_FirstTutorial());
        taskQueue.Add(new Task2_SecondTutorial());
        taskQueue.Add(new Task3_ThirdTutorial());
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

            Debug.Log("[GameFlowGuide] Tutorial flow complete.");
            return;
        }

        var task = taskQueue[currentTaskIndex];
        var data = DataManager.Instance.GetPersistentSaveData<TutorialSaveData>(SAVE_KEY);
        bool resumesSavedTask = currentTaskIndex == data.CurrentTaskIndex;
        int fallbackStartStep = resumesSavedTask ? data.CurrentStepIndex : 0;
        string startStepId = resumesSavedTask ? ResolveCurrentTaskResumeStepId(data) : null;

        LoadCurrentTaskState(data);
        Debug.Log($"[GameFlowGuide] Start {task.TaskName}");
        task.Start(OnTaskComplete, startStepId, fallbackStartStep, SaveProgress);
    }

    private void OnTaskComplete()
    {
        currentTaskIndex++;
        SaveProgress();
        ExecuteNextTask();
    }

    private void LoadCurrentTaskState(TutorialSaveData data)
    {
        if (currentTaskIndex < taskQueue.Count && taskQueue[currentTaskIndex] is ITutorialTaskState taskState)
        {
            taskState.LoadState(data);
        }
    }

    private void WriteCurrentTaskState(TutorialSaveData data)
    {
        if (currentTaskIndex < taskQueue.Count && taskQueue[currentTaskIndex] is ITutorialTaskState taskState)
        {
            taskState.WriteState(data);
        }
    }

    private string ResolveCurrentTaskResumeStepId(TutorialSaveData data)
    {
        if (currentTaskIndex < taskQueue.Count && taskQueue[currentTaskIndex] is ITutorialTaskState taskState)
        {
            return taskState.ResolveResumeStepId(data);
        }

        return data.CurrentStepId;
    }

    private void OnDestroy()
    {
        if (currentTaskIndex < taskQueue.Count)
            taskQueue[currentTaskIndex].Dispose();
    }
}
