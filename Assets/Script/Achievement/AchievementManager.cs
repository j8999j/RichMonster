using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

        // 透過反射找出所有繼承 AchievementBase 的非抽象類別
        var achievementTypes = FindAllAchievementTypes();
        Debug.Log($"[AchievementManager] 找到 {achievementTypes.Count} 個成就腳本類別");

        // 建立：AchievementID → Type 的對應表
        var typeMap = BuildAchievementTypeMap(achievementTypes);

        // 對每筆 Config 建立對應的成就實例
        foreach (var kvp in configDict)
        {
            string achievementId = kvp.Key;
            AchievementConfig config = kvp.Value;

            if (!typeMap.TryGetValue(achievementId, out Type achievementType))
            {
                Debug.LogWarning($"[AchievementManager] 找不到 AchievementID '{achievementId}' 對應的腳本類別，跳過");
                continue;
            }

            try
            {
                // 建立實例並載入設定
                var instance = Activator.CreateInstance(achievementType) as AchievementBase;
                if (instance == null)
                {
                    Debug.LogError($"[AchievementManager] 無法建立 '{achievementType.Name}' 的實例");
                    continue;
                }

                instance.LoadConfig(config);
                instance.Initialize();

                // 依分類歸入
                _achievementsByCategory[config.Category].Add(instance);
                _achievementsById[achievementId] = instance;

                // 監聽解鎖事件
                instance.OnUnlocked += OnAchievementUnlocked;

                Debug.Log($"[AchievementManager] 初始化成就: {config.AchievementName} (ID: {achievementId}, 分類: {config.Category})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementManager] 初始化成就 '{achievementId}' 失敗: {e.Message}");
            }
        }

        _isInitialized = true;
        Debug.Log($"[AchievementManager] 成就系統初始化完成，共載入 {_achievementsById.Count} 個成就");
    }

    /// <summary>
    /// 透過反射找出所有繼承 AchievementBase 的非抽象具體類別
    /// </summary>
    private List<Type> FindAllAchievementTypes()
    {
        var baseType = typeof(AchievementBase);
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try { return assembly.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t => t != baseType && baseType.IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
    }

    /// <summary>
    /// 建立 AchievementID → Type 的對應表
    /// 透過暫時建立實例取得其 AchievementID
    /// </summary>
    private Dictionary<string, Type> BuildAchievementTypeMap(List<Type> types)
    {
        var map = new Dictionary<string, Type>();

        foreach (var type in types)
        {
            try
            {
                var temp = Activator.CreateInstance(type) as AchievementBase;
                if (temp != null && !string.IsNullOrEmpty(temp.AchievementID))
                {
                    if (map.ContainsKey(temp.AchievementID))
                    {
                        Debug.LogWarning($"[AchievementManager] 重複的 AchievementID '{temp.AchievementID}'，類別: {type.Name}，將覆蓋先前的類別");
                    }
                    map[temp.AchievementID] = type;
                }
                // 清理暫時實例
                (temp as IDisposable)?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AchievementManager] 無法建立 '{type.Name}' 的暫時實例: {e.Message}");
            }
        }

        return map;
    }

    /// <summary>
    /// 成就解鎖時的回呼
    /// </summary>
    private void OnAchievementUnlocked(AchievementBase achievement)
    {
        Debug.Log($"[AchievementManager]成就解鎖: {achievement.AchievementName}");
        // 可在此擴充：儲存進度、顯示 UI 通知等
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

    protected override void OnDestroy()
    {
        // 清理所有成就的事件訂閱
        foreach (var achievement in _achievementsById.Values)
        {
            achievement.OnUnlocked -= OnAchievementUnlocked;
            achievement.Dispose();
        }
        _achievementsByCategory.Clear();
        _achievementsById.Clear();
        _isInitialized = false;

        base.OnDestroy();
    }
}
