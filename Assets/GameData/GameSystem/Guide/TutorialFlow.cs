using System.Collections.Generic;
using UnityEngine;
public class TutorialFlow
{
    private readonly List<GuideTask> taskQueue = new List<GuideTask>();
    private int currentTaskIndex = 0;
    private BackgroundListener backgroundListener;
    public void Start()
    {
        LoadTaskData();
        RegisterTasks();
        ExecuteNextTask();
    }
    //載入存檔任務進度
    private void LoadTaskData()
    {

    }


    /// <summary>
    /// 新增任務只需在此加入一行，任何其他程式碼不需修改
    /// </summary>
    private void RegisterTasks()
    {
        taskQueue.Add(new Task1_FirstTutorial());
    }

    private void ExecuteNextTask()
    {
        if (currentTaskIndex >= taskQueue.Count)
        {
            Debug.Log("[GameFlowGuide] 所有引導任務完成");
            return;
        }

        var task = taskQueue[currentTaskIndex];
        Debug.Log($"[GameFlowGuide] 開始 {task.TaskName}");
        task.Start(OnTaskComplete);
    }

    private void OnTaskComplete()
    {
        currentTaskIndex++;
        ExecuteNextTask();
    }

    private void OnDestroy()
    {
        if (currentTaskIndex < taskQueue.Count)
            taskQueue[currentTaskIndex].Dispose();
    }
}