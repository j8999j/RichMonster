using System;
using System.Collections.Generic;
using System.Linq;
using GameSystem;
using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 紀念品系統管理器。透過 SouvenirDefinitionAttribute 建立 ID 對 Type 的快取索引。
    /// </summary>
    public class SouvenirManager : Singleton<SouvenirManager>, ISpecialSouvenirProvider
    {
        private readonly Dictionary<string, AchievementSouvenir> _achievementSouvenirs
            = new Dictionary<string, AchievementSouvenir>();

        private readonly Dictionary<string, SpecialSouvenir> _specialSouvenirs
            = new Dictionary<string, SpecialSouvenir>();

        private readonly Dictionary<string, SouvenirBase> _souvenirById
            = new Dictionary<string, SouvenirBase>();

        private readonly HashSet<string> _ownedSouvenirIds = new HashSet<string>();

        private bool _isInitialized;
        public bool IsInitialized => _isInitialized;
        private SouvenirEffectDispatcher _effectDispatcher;
        private SouvenirEffectRegistry _effectRegistry;
        private SouvenirPipelineService _pipelineService;
        private SpecialSouvenirLifecycle _specialSouvenirLifecycle;

        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[SouvenirManager] 已初始化，略過重複初始化");
                return;
            }

            var souvenirTypesById = GameDefinitionTypeRegistry.SouvenirTypesById;
            Debug.Log($"[SouvenirManager] 已建立 {souvenirTypesById.Count} 個紀念品類別索引");

            InitializeAchievementSouvenirs(souvenirTypesById);
            InitializeSpecialSouvenirs(souvenirTypesById);

            _effectRegistry = new SouvenirEffectRegistry();
            _pipelineService = new SouvenirPipelineService(_effectRegistry);
            _effectDispatcher = new SouvenirEffectDispatcher(_effectRegistry);
            _specialSouvenirLifecycle = new SpecialSouvenirLifecycle(GetAllSpecialSouvenirsForEffects);
            _isInitialized = true;
            SnapshotOwnedSouvenirs();
            RebuildEffectRegistry();
            InitializeAllSpecialSouvenirs();
            _effectDispatcher.SubscribeGameEvents();

            Debug.Log($"[SouvenirManager] 初始化完成，成就紀念品 {_achievementSouvenirs.Count} 個，特殊紀念品 {_specialSouvenirs.Count} 個");
        }

        private void InitializeAchievementSouvenirs(IReadOnlyDictionary<string, Type> souvenirTypesById)
        {
            var dataDict = DataManager.Instance.AchievementSouvenirDict;
            if (dataDict == null) return;

            foreach (var pair in dataDict)
            {
                if (!TryCreateSouvenir(pair.Key, souvenirTypesById, out AchievementSouvenir instance))
                {
                    continue;
                }

                var data = pair.Value;
                if (data != null)
                {
                    instance.SouvenirName = data.SouvenirName;
                    instance.SouvenirDescription = data.SouvenirDescription;
                    instance.EffectName = data.SouvenirFunctionDescription;
                }

                _achievementSouvenirs[instance.SouvenirID] = instance;
                _souvenirById[instance.SouvenirID] = instance;
                Debug.Log($"[SouvenirManager] 載入成就紀念品: {instance.SouvenirID}");
            }
        }

        private void InitializeSpecialSouvenirs(IReadOnlyDictionary<string, Type> souvenirTypesById)
        {
            var dataDict = DataManager.Instance.SpecialSouvenirDict;
            if (dataDict != null)
            {
                foreach (var pair in dataDict)
                {
                    if (!TryCreateSouvenir(pair.Key, souvenirTypesById, out SpecialSouvenir instance))
                    {
                        continue;
                    }

                    var data = pair.Value;
                    if (data != null)
                    {
                        instance.SouvenirName = data.SouvenirName;
                        instance.SouvenirDescription = data.SouvenirDescription;
                    }

                    RegisterSpecialSouvenir(instance);
                    Debug.Log($"[SouvenirManager] 載入特殊紀念品: {instance.SouvenirID}");
                }
            }

            foreach (var pair in souvenirTypesById)
            {
                if (_specialSouvenirs.ContainsKey(pair.Key)) continue;
                if (!typeof(DefaultOwnedSouvenirBase).IsAssignableFrom(pair.Value)) continue;
                if (!TryCreateSouvenir(pair.Key, souvenirTypesById, out SpecialSouvenir instance)) continue;

                RegisterSpecialSouvenir(instance);
                Debug.Log($"[SouvenirManager] 載入預設持有紀念品: {instance.SouvenirID}");
            }
        }

        private static bool TryCreateSouvenir<TSouvenir>(
            string souvenirId,
            IReadOnlyDictionary<string, Type> souvenirTypesById,
            out TSouvenir souvenir)
            where TSouvenir : SouvenirBase
        {
            souvenir = null;
            if (string.IsNullOrEmpty(souvenirId)) return false;

            if (!souvenirTypesById.TryGetValue(souvenirId, out var type))
            {
                Debug.LogWarning($"[SouvenirManager] 找不到 SouvenirID '{souvenirId}' 對應的紀念品類別標記");
                return false;
            }

            if (!typeof(TSouvenir).IsAssignableFrom(type))
            {
                return false;
            }

            try
            {
                souvenir = Activator.CreateInstance(type) as TSouvenir;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SouvenirManager] 建立紀念品 '{type.Name}' 失敗: {e.Message}");
                return false;
            }

            if (souvenir == null || string.IsNullOrEmpty(souvenir.SouvenirID))
            {
                Debug.LogWarning($"[SouvenirManager] '{type.Name}' 沒有有效的 SouvenirID");
                return false;
            }

            if (souvenir.SouvenirID != souvenirId)
            {
                Debug.LogWarning($"[SouvenirManager] '{type.Name}' 的 Attribute ID '{souvenirId}' 與實例 SouvenirID '{souvenir.SouvenirID}' 不一致");
                return false;
            }

            return true;
        }

        private void RegisterSpecialSouvenir(SpecialSouvenir souvenir)
        {
            if (_specialSouvenirs.ContainsKey(souvenir.SouvenirID))
            {
                Debug.LogWarning($"[SouvenirManager] 重複的 SouvenirID '{souvenir.SouvenirID}'");
            }

            _specialSouvenirs[souvenir.SouvenirID] = souvenir;
            _souvenirById[souvenir.SouvenirID] = souvenir;
        }

        public AchievementSouvenir GetAchievementSouvenir(string souvenirId)
        {
            _achievementSouvenirs.TryGetValue(souvenirId, out var souvenir);
            return souvenir;
        }

        public List<AchievementSouvenir> GetAllAchievementSouvenirs()
        {
            return _achievementSouvenirs.Values.ToList();
        }

        public IReadOnlyList<ISpecialSouvenirSave> GetAllSpecialSouvenirSaves()
        {
            return _specialSouvenirs.Values.OfType<ISpecialSouvenirSave>().ToList();
        }

        public SpecialSouvenir GetSpecialSouvenir(string souvenirId)
        {
            _specialSouvenirs.TryGetValue(souvenirId, out var souvenir);
            return souvenir;
        }

        public List<SpecialSouvenir> GetAllSpecialSouvenirs()
        {
            return _specialSouvenirs.Values.ToList();
        }

        public bool IsOwned(string souvenirId)
        {
            return _ownedSouvenirIds.Contains(souvenirId);
        }

        public bool IsPurchased(string souvenirId)
        {
            var bookData = DataManager.Instance.GetBookData();
            return bookData?.UnLockAchievementSouvenirID != null
                && bookData.UnLockAchievementSouvenirID.Contains(souvenirId);
        }

        public void SnapshotOwnedSouvenirs()
        {
            _ownedSouvenirIds.Clear();
            var bookData = DataManager.Instance.GetBookData();
            var playerData = DataManager.Instance.CurrentPlayerData;

            if (playerData?.HoldAchievementSouvenirID != null)
            {
                foreach (var id in playerData.HoldAchievementSouvenirID)
                {
                    _ownedSouvenirIds.Add(id);
                }
            }

            if (bookData != null)
            {
                if (bookData.UnLockSpecialSouvenirID != null)
                {
                    foreach (var id in bookData.UnLockSpecialSouvenirID)
                    {
                        _ownedSouvenirIds.Add(id);
                    }
                }

                foreach (var pair in _specialSouvenirs)
                {
                    if (pair.Value is DefaultOwnedSouvenirBase && !_ownedSouvenirIds.Contains(pair.Key))
                    {
                        _ownedSouvenirIds.Add(pair.Key);
                        bookData.UnLockSpecialSouvenirID ??= new List<string>();
                        if (!bookData.UnLockSpecialSouvenirID.Contains(pair.Key))
                        {
                            bookData.UnLockSpecialSouvenirID.Add(pair.Key);
                            DataManager.Instance.SetBookDataChanged(true);
                        }
                    }
                }
            }

            Debug.Log($"[SouvenirManager] 已快照目前持有紀念品 {_ownedSouvenirIds.Count} 個");
        }

        public void ResnapshotForCurrentGame()
        {
            if (!_isInitialized) return;
            ReleaseAllSpecialSouvenirs();
            SnapshotOwnedSouvenirs();
            RebuildEffectRegistry();
            InitializeAllSpecialSouvenirs();
        }

        private void RebuildEffectRegistry()
        {
            _effectRegistry?.Rebuild(GetOwnedSouvenirs(), GetAllSpecialSouvenirsForEffects());
        }

        private IEnumerable<SouvenirBase> GetOwnedSouvenirs()
        {
            foreach (var id in _ownedSouvenirIds)
            {
                if (_souvenirById.TryGetValue(id, out var souvenir))
                {
                    yield return souvenir;
                }
            }
        }

        private IEnumerable<SpecialSouvenir> GetAllSpecialSouvenirsForEffects()
        {
            foreach (var souvenir in _specialSouvenirs.Values)
            {
                yield return souvenir;
            }
        }

        public int GetTotalAchievementPoints()
        {
            if (AchievementManager.Instance == null || !AchievementManager.Instance.IsInitialized)
            {
                return 0;
            }

            int totalPoints = 0;
            foreach (var ach in AchievementManager.Instance.GetCompletedAchievements())
            {
                totalPoints += GetPointsForLevel(ach.Level);
            }

            return totalPoints;
        }

        private int GetPointsForLevel(AchievementLevel level)
        {
            switch (level)
            {
                case AchievementLevel.Bronze: return 100;
                case AchievementLevel.Silver: return 200;
                case AchievementLevel.Gold: return 300;
                default: return 100;
            }
        }

        public int GetSpentPoints()
        {
            int spent = 0;
            var bookData = DataManager.Instance.GetBookData();
            if (bookData?.UnLockAchievementSouvenirID == null)
            {
                return spent;
            }

            foreach (var id in bookData.UnLockAchievementSouvenirID)
            {
                if (_achievementSouvenirs.TryGetValue(id, out var ach))
                {
                    spent += ach.Cost;
                }
            }

            return spent;
        }

        public int GetRemainingPoints()
        {
            return GetTotalAchievementPoints() - GetSpentPoints();
        }

        public bool TryPurchaseSouvenir(string souvenirId)
        {
            if (IsPurchased(souvenirId))
            {
                Debug.LogWarning($"[SouvenirShop] 已購買紀念品 {souvenirId}");
                return false;
            }

            if (_achievementSouvenirs.TryGetValue(souvenirId, out var ach))
            {
                int remainingPoints = GetRemainingPoints();
                if (remainingPoints >= ach.Cost)
                {
                    var bookData = DataManager.Instance.GetBookData();
                    if (bookData != null)
                    {
                        bookData.UnLockAchievementSouvenirID ??= new List<string>();
                        if (!bookData.UnLockAchievementSouvenirID.Contains(souvenirId))
                        {
                            bookData.UnLockAchievementSouvenirID.Add(souvenirId);
                            DataManager.Instance.SetBookDataChanged(true);
                        }
                    }

                    GameEventCenter.Publish(new SouvenirPurchasedEvent(souvenirId, ach.Cost, GetRemainingPoints()));
                    Debug.Log($"[SouvenirShop] 購買成功: {souvenirId}, 花費 {ach.Cost}");
                    return true;
                }

                Debug.LogWarning($"[SouvenirShop] 點數不足，紀念品 {souvenirId} 需要 {ach.Cost}，目前 {remainingPoints}");
            }
            else
            {
                Debug.LogWarning($"[SouvenirShop] 找不到可購買的成就紀念品 {souvenirId}");
            }

            return false;
        }

        public List<(AchievementSouvenir Souvenir, bool IsOwned)> GetShopCatalog()
        {
            return _achievementSouvenirs.Values
                .Select(s => (Souvenir: s, IsOwned: IsOwned(s.SouvenirID)))
                .ToList();
        }

        public void InitializeAllSpecialSouvenirs()
        {
            _specialSouvenirLifecycle?.InitializeAll();
        }

        public void ReleaseAllSpecialSouvenirs()
        {
            _specialSouvenirLifecycle?.ReleaseAll();
        }

        public void ApplyAllStartEffects()
        {
            _effectDispatcher?.ApplyAllStartEffects();
        }

        public void ApplyAllDailyEffects()
        {
            _effectDispatcher?.ApplyAllDailyEffects();
        }

        public List<ShelfSlotVisualInfo> ApplyShopShelfPipeline(
            string shopId,
            List<Shop.ShelfSlot> items,
            bool buildVisualInfos)
        {
            return _pipelineService != null
                ? _pipelineService.ApplyShopShelf(shopId, items, buildVisualInfos)
                : new List<ShelfSlotVisualInfo>();
        }

        public bool EvaluateScratchCardFree()
        {
            return _pipelineService != null && _pipelineService.EvaluateScratchCardFree();
        }

        public int CalculateExtraBagCapacity()
        {
            return _pipelineService != null ? _pipelineService.CalculateExtraBagCapacity() : 0;
        }

        public void Reset()
        {
            _effectDispatcher?.UnsubscribeGameEvents();
            ReleaseAllSpecialSouvenirs();
            _achievementSouvenirs.Clear();
            _specialSouvenirs.Clear();
            _souvenirById.Clear();
            _ownedSouvenirIds.Clear();
            _effectDispatcher = null;
            _effectRegistry = null;
            _pipelineService = null;
            _specialSouvenirLifecycle = null;
            _isInitialized = false;
            Debug.Log("[SouvenirManager] 紀念品系統已重置");
        }

        protected override void OnDestroy()
        {
            Reset();
            base.OnDestroy();
        }
    }
}
