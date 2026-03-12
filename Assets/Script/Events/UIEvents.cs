using System;

public static class UIEvents
{
    // 當成就解鎖時通知UI顯示 (參數: 成就本身)
    public static event Action<AchievementBase> OnAchievementUnlocked;
    
    public static void AchievementUnlocked(AchievementBase achievement) 
    {
        OnAchievementUnlocked?.Invoke(achievement);
    }
}
