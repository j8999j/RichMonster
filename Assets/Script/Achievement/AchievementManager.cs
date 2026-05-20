using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystem;

/// <summary>
/// 成就管理器 - 負責根據 AchievementConfig 載入並初始化對應的成就腳本
/// 依照 AchievementCategory 進行分類管理
/// </summary>
public class AchievementManager : Singleton<AchievementManager>
{
    // 依照分類存放所有成就實例
    private Dictionary<AchievementCategory, List<AchievementBase>> _achievementsByCategory
        = new Dictionary<AchievementCategory, List<AchievementBase>>();

    // 用 AchievementID 快速查找
    private Dictionary<string, AchievementBase> _achievementsById
        = new Dictionary<string, AchievementBase>();

    private bool _isInitialized = false;
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// 初始化成就系統：根據已載入的 AchievementConfig 資料，
    /// 透過反射找到所有繼承 AchievementBase 的腳本，配對後分類並初始化
    /// </summary>
    public void Initialize(Dictionary<string, AchievementConfig> configDict)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[AchievementManager] 已經初始化過，跳過重複初始化");
            return;
        }

        if (configDict == null || configDict.Count == 0)
        {
            Debug.LogWarning("[AchievementManager] 沒有任何成就設定資料");
            _isInitialized = true;
            return;
        }

        // 初始化分類字典
        foreach (AchievementCategory category in Enum.GetValues(typeof(AchievementCategory)))
        {
            _achievementsByCategory[category] = new List<AchievementBase>();
        }

        // 從共用反射快取取得所有 AchievementLibrary.* 內的 AchievementBase 衍生類別
        var achievementTypes = GameTypeCache.GetConcreteSubclassesOf<AchievementBase>("AchievementLibrary");
        Debug.Log($"[AchievementManager] 找到 {achievementTypes.Count} 個成就腳本類別");

        // 一次建立實例：直接從實例讀 AchievementID → 配對 config → LoadConfig + Initialize
        foreach (var type in achievementTypes)
        {
            AchievementBase instance;
            try
            {
                instance = Activator.CreateInstance(type) as AchievementBase;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementManager] 建立 '{type.Name}' 實例失敗: {e.Message}");
                continue;
            }

            if (instance == null || string.IsNullOrEmpty(instance.AchievementID))
            {
                Debug.LogWarning($"[AchievementManager] '{type.Name}' 沒有有效的 AchievementID，跳過");
                (instance as IDisposable)?.Dispose();
                continue;
            }

            if (!configDict.TryGetValue(instance.AchievementID, out var config))
            {
                Debug.LogWarning($"[AchievementManager] 找不到 AchievementID '{instance.AchievementID}' 對應的 Config，跳過");
                (instance as IDisposable)?.Dispose();
                continue;
            }

            if (_achievementsById.ContainsKey(instance.AchievementID))
            {
                Debug.LogWarning($"[AchievementManager] 重複的 AchievementID '{instance.AchievementID}'，類別: {type.Name}，將覆蓋先前的類別");
            }

            try
            {
                instance.LoadConfig(config);
                instance.Initialize();
                _achievementsByCategory[config.Category].Add(instance);
                _achievementsById[instance.AchievementID] = instance;
                instance.OnUnlocked += OnAchievementUnlocked;

                Debug.Log($"[AchievementManager] 初始化成就: {config.AchievementName} (ID: {instance.AchievementID}, 分類: {config.Category})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementManager] 初始化成就 '{instance.AchievementID}' 失敗: {e.Message}");
            }
        }

        _isInitialized = true;
        Debug.Log($"[AchievementManager] 成就系統初始化完成，共載入 {_achievementsById.Count} 個成就");
    }

    /// <summary>
    /// 成就解鎖時的回呼
    /// </summary>
    private void OnAchievementUnlocked(AchievementBase achievement)
    {
        Debug.Log($"[AchievementManager]成就解鎖: {achievement.AchievementName}");
        GameEventCenter.Publish(new AchievementUnlockedEvent(
            achievement.AchievementID,
            achievement.AchievementName,
            achievement.Description,
            achievement.Level));
    }

    #region Public Query API

    /// <summary>
    /// 取得指定分類的所有成就
    /// </summary>
    public List<AchievementBase> GetAchievementsByCategory(AchievementCategory category)
    {
        if (_achievementsByCategory.TryGetValue(category, out var list))
        {
            return list;
        }
        return new List<AchievementBase>();
    }

    /// <summary>
    /// 根據 AchievementID 取得成就設定
    /// </summary>
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

    /// <summary>
    /// 根據 AchievementID 取得成就實例
    /// </summary>
    public AchievementBase GetAchievementById(string achievementId)
    {
        _achievementsById.TryGetValue(achievementId, out var achievement);
        return achievement;
    }

    /// <summary>
    /// 取得所有已完成的成就
    /// </summary>
    public List<AchievementBase> GetCompletedAchievements()
    {
        return _achievementsById.Values
            .Where(a => a.IsCompleted)
            .ToList();
    }

    /// <summary>
    /// 取得所有未完成的成就
    /// </summary>
    public List<AchievementBase> GetIncompleteAchievements()
    {
        return _achievementsById.Values
            .Where(a => !a.IsCompleted)
            .ToList();
    }

    #endregion

    /// <summary>
    /// 重置成就系統，清除所有資料並允許重新初始化
    /// </summary>
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
