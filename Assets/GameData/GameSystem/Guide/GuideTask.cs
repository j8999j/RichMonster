// ============================================================
// GuideTask.cs - 抽象引導基底類別
// ============================================================
using System.Collections.Generic;
using UnityEngine;
using System;
public abstract class GuideTask
{
    public abstract string TaskName { get; }
    private List<GuideStep> steps;
    private int currentStepIndex = 0;
    private Action onTaskComplete;
    public bool IsCompleteTask { get; private set; } = false;
    /// <summary>子類在此建構並回傳步驟序列</summary>
    protected abstract List<GuideStep> BuildSteps();

    public void Start(Action onComplete)
    {
        onTaskComplete = onComplete;
        currentStepIndex = 0;
        steps = BuildSteps();
        for (int i = 0; i < steps.Count; i++)
            steps[i].StepIndex = i;
        ExecuteCurrentStep();
    }

    private void ExecuteCurrentStep()
    {
        if (currentStepIndex >= steps.Count)
        {
            Debug.Log($"[{TaskName}] 任務完成");
            onTaskComplete?.Invoke();
            return;
        }

        Debug.Log($"[{TaskName}] 執行步驟 {currentStepIndex + 1}/{steps.Count}");
        steps[currentStepIndex].Execute(OnStepComplete);
    }

    private void OnStepComplete()
    {
        steps[currentStepIndex].Dispose();
        currentStepIndex++;
        ExecuteCurrentStep();
    }

    public void Dispose()
    {
        if (steps != null && currentStepIndex < steps.Count)
            steps[currentStepIndex].Dispose();
    }
}
