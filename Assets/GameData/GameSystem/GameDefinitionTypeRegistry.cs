using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameSystem
{
    /// <summary>
    /// 以 Attribute 建立遊戲定義類別索引，避免 Manager 初始化時建立無效實例。
    /// </summary>
    public static class GameDefinitionTypeRegistry
    {
        private static Dictionary<string, Type> _achievementTypesById;
        private static Dictionary<string, Type> _souvenirTypesById;
        private static readonly object _lock = new object();

        public static IReadOnlyDictionary<string, Type> AchievementTypesById
        {
            get
            {
                EnsureBuilt();
                return _achievementTypesById;
            }
        }

        public static IReadOnlyDictionary<string, Type> SouvenirTypesById
        {
            get
            {
                EnsureBuilt();
                return _souvenirTypesById;
            }
        }

        public static void Invalidate()
        {
            lock (_lock)
            {
                _achievementTypesById = null;
                _souvenirTypesById = null;
            }
        }

        private static void EnsureBuilt()
        {
            if (_achievementTypesById != null && _souvenirTypesById != null) return;

            lock (_lock)
            {
                if (_achievementTypesById != null && _souvenirTypesById != null) return;

                _achievementTypesById = new Dictionary<string, Type>(StringComparer.Ordinal);
                _souvenirTypesById = new Dictionary<string, Type>(StringComparer.Ordinal);

                var types = GameTypeCache.AllConcreteGameTypes;
                for (int i = 0; i < types.Count; i++)
                {
                    var type = types[i];
                    RegisterAchievementType(type);
                    RegisterSouvenirType(type);
                }
            }
        }

        private static void RegisterAchievementType(Type type)
        {
            if (!typeof(AchievementBase).IsAssignableFrom(type)) return;

            var attribute = type.GetCustomAttribute<AchievementDefinitionAttribute>(false);
            if (attribute == null)
            {
                Debug.LogWarning($"[GameDefinitionTypeRegistry] 成就類別缺少 AchievementDefinitionAttribute: {type.FullName}");
                return;
            }

            AddType(_achievementTypesById, attribute.AchievementId, type, "成就");
        }

        private static void RegisterSouvenirType(Type type)
        {
            if (!typeof(Souvenir.SouvenirBase).IsAssignableFrom(type)) return;

            var attribute = type.GetCustomAttribute<SouvenirDefinitionAttribute>(false);
            if (attribute == null)
            {
                Debug.LogWarning($"[GameDefinitionTypeRegistry] 紀念品類別缺少 SouvenirDefinitionAttribute: {type.FullName}");
                return;
            }

            AddType(_souvenirTypesById, attribute.SouvenirId, type, "紀念品");
        }

        private static void AddType(Dictionary<string, Type> map, string id, Type type, string label)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[GameDefinitionTypeRegistry] {label}類別標記了空 ID: {type.FullName}");
                return;
            }

            if (map.TryGetValue(id, out var existingType))
            {
                Debug.LogWarning($"[GameDefinitionTypeRegistry] 重複的{label} ID '{id}': {existingType.FullName}, {type.FullName}");
                return;
            }

            map[id] = type;
        }
    }
}
