using UnityEngine;

public abstract class MissionRequirement : ScriptableObject
{
    // 每個需求都要實作這個檢查邏輯
    public abstract bool IsMatch(ItemDefinition item);
}
