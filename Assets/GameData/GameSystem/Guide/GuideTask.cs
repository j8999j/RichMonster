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
    private Action onStepCompleted;
    public int CurrentStepIndex => currentStepIndex;
    public bool IsCompleteTask { get; private set; } = false;
    /// <summary>子類在此建構並回傳步驟序列</summary>
    protected abstract List<GuideStep> BuildSteps();

    public void Start(Action onComplete, int startFromStep = 0, Action onStepComplete = null)
    {
        onTaskComplete = onComplete;
        onStepCompleted = onStepComplete;
        steps = BuildSteps();
        for (int i = 0; i < steps.Count; i++)
            steps[i].StepIndex = i;
        currentStepIndex = startFromStep;
        if (startFromStep > 0)
            OnResume(startFromStep);
        ExecuteCurrentStep();
    }

    /// <summary>子類可覆寫，在從中途恢復時執行特定初始化</summary>
    protected virtual void OnResume(int fromStep) { }

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
        onStepCompleted?.Invoke();
        ExecuteCurrentStep();
    }

    public void Dispose()
    {
        if (steps != null && currentStepIndex < steps.Count)
            steps[currentStepIndex].Dispose();
    }
}
