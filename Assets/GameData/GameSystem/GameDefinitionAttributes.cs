using System;

/// <summary>
/// 標記成就類別對應的資料 ID，用於建立反射快取索引。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AchievementDefinitionAttribute : Attribute
{
    public AchievementDefinitionAttribute(string achievementId)
    {
        AchievementId = achievementId;
    }

    public string AchievementId { get; }
}

/// <summary>
/// 標記紀念品類別對應的資料 ID，用於建立反射快取索引。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SouvenirDefinitionAttribute : Attribute
{
    public SouvenirDefinitionAttribute(string souvenirId)
    {
        SouvenirId = souvenirId;
    }

    public string SouvenirId { get; }
}
