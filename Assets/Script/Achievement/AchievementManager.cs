using System;
using System.Collections.Generic;
using System.Linq;
using GameSystem;
using UnityEngine;

/// <summary>
/// 成就系統管理器。依照 AchievementConfig 載入並初始化有 Attribute 標記的成就類別。
/// </summary>
public class AchievementManager : Singleton<AchievementManager>
{
    private readonly Dictionary<AchievementCategory, List<AchievementBase>> _achievementsByCategory
        = new Dictionary<AchievementCategory, List<AchievementBase>>();

    private readonly Dictionary<string, AchievementBase> _achievementsById
        = new Dictionary<string, AchievementBase>();

    private bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    public void Initialize(Dictionary<string, AchievementConfig> configDict)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[AchievementManager] 已初始化，略過重複初始化");
            return;
        }

        if (configDict == null || configDict.Count == 0)
        {
            Debug.LogWarning("[AchievementManager] 沒有成就設定資料");
            _isInitialized = true;
            return;
        }

        foreach (AchievementCategory category in Enum.GetValues(typeof(AchievementCategory)))
        {
            _achievementsByCategory[category] = new List<AchievementBase>();
        }

        var achievementTypesById = GameDefinitionTypeRegistry.AchievementTypesById;
        Debug.Log($"[AchievementManager] 已建立 {achievementTypesById.Count} 個成就類別索引");

        foreach (var configPair in configDict)
        {
            var achievementId = configPair.Key;
            var config = configPair.Value;
            if (config == null || string.IsNullOrEmpty(achievementId))
            {
                continue;
            }

            if (!achievementTypesById.TryGetValue(achievementId, out var type))
            {
                Debug.LogWarning($"[AchievementManager] 找不到 AchievementID '{achievementId}' 對應的成就類別標記");
                continue;
            }

            AchievementBase instance;
            try
            {
                instance = Activator.CreateInstance(type) as AchievementBase;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementManager] 建立成就 '{type.Name}' 失敗: {e.Message}");
                continue;
            }

            if (instance == null || string.IsNullOrEmpty(instance.AchievementID))
            {
                Debug.LogWarning($"[AchievementManager] '{type.Name}' 沒有有效的 AchievementID");
                (instance as IDisposable)?.Dispose();
                continue;
            }

            if (instance.AchievementID != achievementId)
            {
                Debug.LogWarning($"[AchievementManager] '{type.Name}' 的 Attribute ID '{achievementId}' 與實例 AchievementID '{instance.AchievementID}' 不一致");
                (instance as IDisposable)?.Dispose();
                continue;
            }

            if (_achievementsById.ContainsKey(instance.AchievementID))
            {
                Debug.LogWarning($"[AchievementManager] 重複的 AchievementID '{instance.AchievementID}'，來源: {type.Name}");
            }

            try
            {
                instance.LoadConfig(config);
                instance.Initialize();
                _achievementsByCategory[config.Category].Add(instance);
                _achievementsById[instance.AchievementID] = instance;
                instance.OnUnlocked += OnAchievementUnlocked;

                Debug.Log($"[AchievementManager] 已初始化成就 {config.AchievementName} (ID: {instance.AchievementID}, 分類: {config.Category})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementManager] 初始化成就 '{instance.AchievementID}' 失敗: {e.Message}");
            }
        }

        _isInitialized = true;
        Debug.Log($"[AchievementManager] 成就系統初始化完成，共 {_achievementsById.Count} 個成就");
    }

    private void OnAchievementUnlocked(AchievementBase achievement)
    {
        Debug.Log($"[AchievementManager] 成就解鎖: {achievement.AchievementName}");
        GameEventCenter.Publish(new AchievementUnlockedEvent(
            achievement.AchievementID,
            achievement.AchievementName,
            achievement.Description,
            achievement.Level));
    }

    public List<AchievementBase> GetAchievementsByCategory(AchievementCategory category)
    {
        return _achievementsByCategory.TryGetValue(category, out var list)
            ? list
            : new List<AchievementBase>();
    }

    public AchievementConfig GetAchievementConfig(string achievementId)
    {
        if (_achievementsById.TryGetValue(achievementId, out var achievement))
        {
            return new AchievementConfig
            {
                AchievementID = achievement.AchievementID,
                AchievementName = achievement.AchievementName,
                ConditionDescription = achievement.ConditionDescription,
                Description = achievement.Description,
                Category = achievement.Category,
                Level = achievement.Level
            };
        }

        return null;
    }

    public AchievementBase GetAchievementById(string achievementId)
    {
        _achievementsById.TryGetValue(achievementId, out var achievement);
        return achievement;
    }

    public List<AchievementBase> GetCompletedAchievements()
    {
        return _achievementsById.Values
            .Where(a => a.IsCompleted)
            .ToList();
    }

    public List<AchievementBase> GetIncompleteAchievements()
    {
        return _achievementsById.Values
            .Where(a => !a.IsCompleted)
            .ToList();
    }

    public void Reset()
    {
        foreach (var achievement in _achievementsById.Values)
        {
            achievement.OnUnlocked -= OnAchievementUnlocked;
            achievement.Dispose();
        }

        _achievementsByCategory.Clear();
        _achievementsById.Clear();
        _isInitialized = false;
        Debug.Log("[AchievementManager] 成就系統已重置");
    }

    protected override void OnDestroy()
    {
        Reset();
        base.OnDestroy();
    }
}
