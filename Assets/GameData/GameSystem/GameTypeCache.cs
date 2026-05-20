using System;
using System.Collections.Generic;
using System.Linq;

namespace GameSystem
{
    /// <summary>
    /// 集中管理「在玩家程式碼 assembly 中尋找衍生類別」的反射查詢。
    /// 第一次呼叫時掃 <see cref="ScannedAssemblyNames"/> 內列出的 assembly，
    /// 取出所有非抽象、非介面 Type 並快取，後續查詢只走 Where 過濾，
    /// 避免重複 AppDomain.GetAssemblies() + assembly.GetTypes() 全掃。
    /// 目前使用者：AchievementManager、SouvenirManager。
    ///
    /// 新增專案自訂 asmdef 時，把該 asmdef 的 assembly 名稱加進 <see cref="ScannedAssemblyNames"/>。
    /// </summary>
    public static class GameTypeCache
    {
        /// <summary>
        /// 白名單：只有列在這裡的 assembly 才會被掃描。
        /// 預設只掃 Unity 預設玩家程式碼 assembly "Assembly-CSharp"。
        /// 若日後把部分腳本搬進獨立 asmdef，請把該 asmdef 的 Name 加進來。
        /// </summary>
        public static readonly HashSet<string> ScannedAssemblyNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Assembly-CSharp",
        };

        private static List<Type> _cachedTypes;
        private static readonly object _initLock = new object();

        /// <summary>
        /// 取得所有玩家程式碼 assembly 內的非抽象、非介面 Type。
        /// 第一次呼叫時掃一次後快取（thread-safe）。
        /// </summary>
        public static IReadOnlyList<Type> AllConcreteGameTypes
        {
            get
            {
                if (_cachedTypes != null) return _cachedTypes;
                lock (_initLock)
                {
                    if (_cachedTypes != null) return _cachedTypes;
                    _cachedTypes = BuildCache();
                }
                return _cachedTypes;
            }
        }

        /// <summary>
        /// 取得所有繼承 TBase 的非抽象具體類別。
        /// </summary>
        public static List<Type> GetConcreteSubclassesOf<TBase>() where TBase : class
        {
            var baseType = typeof(TBase);
            var types = AllConcreteGameTypes;
            var result = new List<Type>();
            for (int i = 0; i < types.Count; i++)
            {
                var t = types[i];
                if (t != baseType && baseType.IsAssignableFrom(t))
                    result.Add(t);
            }
            return result;
        }

        /// <summary>
        /// 取得所有繼承 TBase 且 namespace 等於 targetNamespace 的非抽象具體類別。
        /// </summary>
        public static List<Type> GetConcreteSubclassesOf<TBase>(string targetNamespace) where TBase : class
        {
            var baseType = typeof(TBase);
            var types = AllConcreteGameTypes;
            var result = new List<Type>();
            for (int i = 0; i < types.Count; i++)
            {
                var t = types[i];
                if (t != baseType
                    && baseType.IsAssignableFrom(t)
                    && t.Namespace == targetNamespace)
                {
                    result.Add(t);
                }
            }
            return result;
        }

        /// <summary>
        /// 強制重建快取（單元測試或編輯器熱重載時使用）。
        /// </summary>
        public static void Invalidate()
        {
            lock (_initLock) { _cachedTypes = null; }
            GameDefinitionTypeRegistry.Invalidate();
        }

        private static List<Type> BuildCache()
        {
            var result = new List<Type>(2048);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                var asm = assemblies[a];
                if (asm.IsDynamic) continue;

                var name = asm.GetName().Name;
                if (string.IsNullOrEmpty(name)) continue;
                if (!ScannedAssemblyNames.Contains(name)) continue;

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < types.Length; i++)
                {
                    var t = types[i];
                    if (t == null) continue;
                    if (t.IsAbstract) continue;
                    if (t.IsInterface) continue;
                    result.Add(t);
                }
            }
            return result;
        }
    }
}
