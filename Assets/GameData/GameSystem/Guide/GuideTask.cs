using System;
using System.Collections.Generic;
using UnityEngine;

public interface ITutorialTaskState
{
    void LoadState(TutorialSaveData data);
    void WriteState(TutorialSaveData data);
    string ResolveResumeStepId(TutorialSaveData data);
}

public abstract class GuideTask
{
    private enum TaskState
    {
        Idle,
        Running,
        Completed,
        Disposed
    }

    public abstract string TaskName { get; }

    private List<GuideStep> steps;
    private int currentStepIndex;
    private Action onTaskComplete;
    private Action onStepCompleted;
    private int saveStepIndexOverride = -1;
    private string saveStepIdOverride;
    private TaskState state = TaskState.Idle;

    public int CurrentStepIndex => currentStepIndex;
    public int CurrentStepIndexForSave => saveStepIndexOverride >= 0 ? saveStepIndexOverride : currentStepIndex;
    public string CurrentStepIdForSave
        => !string.IsNullOrEmpty(saveStepIdOverride)
            ? saveStepIdOverride
            : GetStepIdAt(currentStepIndex);

    public bool IsCompleteTask { get; private set; }

    protected virtual bool SaveEveryStep => false;
    protected virtual IReadOnlyList<string> StepIds => null;

    protected abstract List<GuideStep> BuildSteps();

    public void Start(Action onComplete, int startFromStep = 0, Action onStepComplete = null)
    {
        Start(onComplete, null, startFromStep, onStepComplete);
    }

    public void Start(Action onComplete, string startFromStepId, int fallbackStartFromStep = 0, Action onStepComplete = null)
    {
        onTaskComplete = onComplete;
        onStepCompleted = onStepComplete;
        IsCompleteTask = false;
        state = TaskState.Running;

        steps = BuildSteps() ?? new List<GuideStep>();
        AssignStepMetadata();

        currentStepIndex = ResolveStartStepIndex(startFromStepId, fallbackStartFromStep);
        if (currentStepIndex > 0)
        {
            RestoreCompletedSteps(currentStepIndex);
            OnResume(currentStepIndex);
        }

        ExecuteCurrentStep();
    }

    protected virtual void OnResume(int fromStep) { }

    protected void RequestProgressSave()
    {
        onStepCompleted?.Invoke();
    }

    protected GuideStep Step(string stepId, GuideStep step)
    {
        step.StepId = stepId;
        return step;
    }

    protected GuideStep SaveAfter(GuideStep step, string saveStepId = null)
    {
        return new WithTutorialSaveStep(step, saveStepId);
    }

    protected GuideStep SaveAfter(GuideStep step, int saveStepIndex)
    {
        return new WithTutorialSaveStep(step, null, saveStepIndex);
    }

    private void AssignStepMetadata()
    {
        for (int i = 0; i < steps.Count; i++)
        {
            steps[i].StepIndex = i;
            if (string.IsNullOrEmpty(steps[i].StepId)
                && StepIds != null
                && i < StepIds.Count
                && !string.IsNullOrEmpty(StepIds[i]))
            {
                steps[i].StepId = StepIds[i];
            }

            if (string.IsNullOrEmpty(steps[i].StepId))
                steps[i].StepId = $"{TaskName}.{i}";
        }
    }

    private int ResolveStartStepIndex(string stepId, int fallbackStepIndex)
    {
        if (!string.IsNullOrEmpty(stepId))
        {
            int index = steps.FindIndex(step => step.StepId == stepId);
            if (index >= 0)
                return index;

            Debug.LogWarning($"[{TaskName}] Saved guide step id not found: {stepId}. Falling back to step index {fallbackStepIndex}.");
        }

        return Mathf.Clamp(fallbackStepIndex, 0, steps.Count);
    }

    private void RestoreCompletedSteps(int startStepIndex)
    {
        int count = Mathf.Min(startStepIndex, steps.Count);
        for (int i = 0; i < count; i++)
        {
            steps[i].Restore();
        }
    }

    private void ExecuteCurrentStep()
    {
        if (state != TaskState.Running)
            return;

        if (currentStepIndex >= steps.Count)
        {
            CompleteTask();
            return;
        }

        Debug.Log($"[{TaskName}] Execute guide step {currentStepIndex + 1}/{steps.Count}");
        steps[currentStepIndex].Execute(OnStepComplete);
    }

    private void OnStepComplete()
    {
        if (state != TaskState.Running || steps == null || currentStepIndex >= steps.Count)
            return;

        int completedStepIndex = currentStepIndex;
        GuideStep completedStep = steps[currentStepIndex];
        completedStep.Dispose();
        currentStepIndex++;

        if (SaveEveryStep || completedStep is IGuideStepSaveCheckpoint)
        {
            GuideStep nextStep = currentStepIndex < steps.Count ? steps[currentStepIndex] : null;
            saveStepIndexOverride = completedStep is IGuideStepSaveCheckpoint checkpoint
                ? checkpoint.GetSaveStepIndex(completedStepIndex, currentStepIndex)
                : currentStepIndex;
            saveStepIdOverride = completedStep is IGuideStepSaveCheckpoint idCheckpoint
                ? idCheckpoint.GetSaveStepId(completedStep, nextStep)
                : nextStep?.StepId;

            try
            {
                onStepCompleted?.Invoke();
            }
            finally
            {
                saveStepIndexOverride = -1;
                saveStepIdOverride = null;
            }
        }

        ExecuteCurrentStep();
    }

    private void CompleteTask()
    {
        IsCompleteTask = true;
        state = TaskState.Completed;
        Debug.Log($"[{TaskName}] Complete");
        onTaskComplete?.Invoke();
    }

    private string GetStepIdAt(int index)
    {
        if (steps == null || index < 0 || index >= steps.Count)
            return null;

        return steps[index].StepId;
    }

    public void Dispose()
    {
        if (state == TaskState.Disposed || state == TaskState.Completed)
            return;

        state = TaskState.Disposed;
        if (steps != null && currentStepIndex < steps.Count)
            steps[currentStepIndex].Dispose();
    }
}
