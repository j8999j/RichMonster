using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameSystem;      // 加入以存取 GameManager

namespace Souvenir
{
    /// <summary>
    /// 紀念品管理器 - 負責載入並初始化對應的紀念品腳本 (參考 AchievementManager)
    /// </summary>
    public class SouvenirManager : Singleton<SouvenirManager>
    {
        // 存放所有成就紀念品實例
        private Dictionary<string, AchievementSouvenirBase> _achievementSouvenirs
            = new Dictionary<string, AchievementSouvenirBase>();

        // 存放所有特殊紀念品實例
        private Dictionary<string, SpecialSouvenirBase> _specialSouvenirs
            = new Dictionary<string, SpecialSouvenirBase>();

        // 當前持有的紀念品 IDs
        private HashSet<string> _ownedSouvenirIds = new HashSet<string>();

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 初始化系統：透過反射找出所有紀念品腳本並實例化
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[SouvenirManager] 已經初始化過，跳過重複初始化");
                return;
            }

            // 1. 處理 AchievementSouvenirBase
            var achSouvenirTypes = FindAllSouvenirTypes<AchievementSouvenirBase>();
            Debug.Log($"[SouvenirManager] 找到 {achSouvenirTypes.Count} 個成就紀念品腳本類別");

            foreach (var type in achSouvenirTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as AchievementSouvenirBase;
                    if (instance != null && !string.IsNullOrEmpty(instance.SouvenirID))
                    {
                        if (_achievementSouvenirs.ContainsKey(instance.SouvenirID))
                        {
                            Debug.LogWarning($"[SouvenirManager] 重複的 SouvenirID '{instance.SouvenirID}'，類別: {type.Name}，將覆蓋先前的類別");
                        }
                        _achievementSouvenirs[instance.SouvenirID] = instance;
                        Debug.Log($"[SouvenirManager] 載入成就紀念品: {instance.SouvenirID}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SouvenirManager] 初始化成就紀念品 '{type.Name}' 失敗: {e.Message}");
                }
            }

            // 2. 處理 SpecialSouvenirBase
            var splSouvenirTypes = FindAllSouvenirTypes<SpecialSouvenirBase>();
            Debug.Log($"[SouvenirManager] 找到 {splSouvenirTypes.Count} 個特殊紀念品腳本類別");

            foreach (var type in splSouvenirTypes)
            {
                try
                {
                    // 注意: 衍生類別需要提供無參數建構子
                    var instance = Activator.CreateInstance(type) as SpecialSouvenirBase;
                    if (instance != null && !string.IsNullOrEmpty(instance.SouvenirID))
                    {
                        if (_specialSouvenirs.ContainsKey(instance.SouvenirID))
                        {
                            Debug.LogWarning($"[SouvenirManager] 重複的 SouvenirID '{instance.SouvenirID}'，類別: {type.Name}，將覆蓋先前的類別");
                        }
                        _specialSouvenirs[instance.SouvenirID] = instance;
                        Debug.Log($"[SouvenirManager] 載入特殊紀念品: {instance.SouvenirID}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SouvenirManager] 初始化特殊紀念品 '{type.Name}' 失敗: {e.Message}");
                }
            }

            _isInitialized = true;
            Debug.Log($"[SouvenirManager] 初始化完成，共載入 {_achievementSouvenirs.Count} 個成就紀念品與 {_specialSouvenirs.Count} 個特殊紀念品");
        }

        /// <summary>
        /// 透過反射找出所有繼承指定基類的非抽象具體類別
        /// </summary>
        private List<Type> FindAllSouvenirTypes<T>()
        {
            var baseType = typeof(T);
            
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .Where(t => t != baseType
                         && baseType.IsAssignableFrom(t)
                         && !t.IsAbstract)
                .ToList();
        }

        #region Public Query & Event API

        /// <summary>
        /// 取得指定的成就紀念品
        /// </summary>
        public AchievementSouvenirBase GetAchievementSouvenir(string souvenirId)
        {
            _achievementSouvenirs.TryGetValue(souvenirId, out var souvenir);
            return souvenir;
        }

        /// <summary>
        /// 取得所有的成就紀念品
        /// </summary>
        public List<AchievementSouvenirBase> GetAllAchievementSouvenirs()
        {
            return _achievementSouvenirs.Values.ToList();
        }

        /// <summary>
        /// 取得指定的特殊紀念品
        /// </summary>
        public SpecialSouvenirBase GetSpecialSouvenir(string souvenirId)
        {
            _specialSouvenirs.TryGetValue(souvenirId, out var souvenir);
            return souvenir;
        }

        /// <summary>
        /// 取得所有的特殊紀念品
        /// </summary>
        public List<SpecialSouvenirBase> GetAllSpecialSouvenirs()
        {
            return _specialSouvenirs.Values.ToList();
        }

        #endregion

        #region 所有權與快照管理 (Ownership & Snapshot Management)

        /// <summary>
        /// 判斷目前是否持有給定的紀念品
        /// </summary>
        public bool IsOwned(string souvenirId)
        {
            return _ownedSouvenirIds.Contains(souvenirId);
        }

        /// <summary>
        /// 在每局遊戲開始時呼叫，從存檔載入已持有的紀念品清單
        /// </summary>
        public void SnapshotOwnedSouvenirs()
        {
            _ownedSouvenirIds.Clear();
            var saveData = DataManager.Instance.GetPersistentSaveData<SouvenirShopSaveData>("SouvenirShopSaveData");
            if (saveData != null && saveData.PurchasedSouvenirIDs != null)
            {
                foreach (var id in saveData.PurchasedSouvenirIDs)
                {
                    _ownedSouvenirIds.Add(id);
                }
            }
            Debug.Log($"[SouvenirManager] 已從存檔載入快照，目前持有 {_ownedSouvenirIds.Count} 個紀念品");
        }

        private void ForEachOwnedSouvenir<T>(Action<T> action) where T : class
        {
            foreach (var id in _ownedSouvenirIds)
            {
                if (_achievementSouvenirs.TryGetValue(id, out var ach) && ach is T targetAch)
                {
                    action(targetAch);
                }
                else if (_specialSouvenirs.TryGetValue(id, out var spl) && spl is T targetSpl)
                {
                    action(targetSpl);
                }
            }
        }

        #endregion

        #region 商店後端 API (Shop Backend API)

        /// <summary>
        /// 獲得的總成就點數 (以完成成就數量計算，每個完成的成就 1 點)
        /// </summary>
        public int GetTotalAchievementPoints()
        {
            if (AchievementManager.Instance != null && AchievementManager.Instance.IsInitialized)
            {
                return AchievementManager.Instance.GetCompletedAchievements().Count;
            }
            return 0;
        }

        /// <summary>
        /// 已花費的成就點數
        /// </summary>
        public int GetSpentPoints()
        {
            int spent = 0;
            foreach (var id in _ownedSouvenirIds)
            {
                if (_achievementSouvenirs.TryGetValue(id, out var ach))
                {
                    spent += ach.Cost;
                }
            }
            return spent;
        }

        /// <summary>
        /// 剩餘可用的成就點數
        /// </summary>
        public int GetRemainingPoints()
        {
            return GetTotalAchievementPoints() - GetSpentPoints();
        }

        /// <summary>
        /// 嘗試購買紀念品 (透過成就點數)
        /// </summary>
        public bool TryPurchaseSouvenir(string souvenirId)
        {
            if (IsOwned(souvenirId))
            {
                Debug.LogWarning($"[SouvenirShop] 購買失敗：已經擁有紀念品 {souvenirId}");
                return false;
            }

            if (_achievementSouvenirs.TryGetValue(souvenirId, out var ach))
            {
                int remainingPoints = GetRemainingPoints();
                if (remainingPoints >= ach.Cost)
                {
                    // 購買成功
                    _ownedSouvenirIds.Add(souvenirId);
                    
                    // 從存檔取出並更新
                    var saveData = DataManager.Instance.GetPersistentSaveData<SouvenirShopSaveData>("SouvenirShopSaveData");
                    if (string.IsNullOrEmpty(saveData.UniqueID))
                    {
                        saveData.UniqueID = "SouvenirShopSaveData";
                    }
                    if (GameManager.Instance != null && GameManager.Instance.gameFlow != null)
                    {
                        saveData.LastUpdatedDay = GameManager.Instance.gameFlow.CurrentDay;
                    }

                    if (!saveData.PurchasedSouvenirIDs.Contains(souvenirId))
                    {
                        saveData.PurchasedSouvenirIDs.Add(souvenirId);
                    }
                    
                    // 存入 DataManager
                    DataManager.Instance.SetPlayerData("SouvenirShopSaveData", saveData);

                    Debug.Log($"[SouvenirShop] 購買紀念品 {souvenirId} 成功！花費 {ach.Cost} 點，剩餘 {GetRemainingPoints()} 點");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[SouvenirShop] 購買失敗：紀念品 {souvenirId} 需要 {ach.Cost} 點，但只剩 {remainingPoints} 點");
                }
            }
            else
            {
                Debug.LogWarning($"[SouvenirShop] 購買失敗：找不到對應的成就紀念品 {souvenirId} (特殊紀念品無法透過商店購買)");
            }
            
            return false;
        }

        /// <summary>
        /// 取得可購買的紀念品目錄資訊
        /// </summary>
        public List<(AchievementSouvenirBase Souvenir, bool IsOwned)> GetShopCatalog()
        {
            return _achievementSouvenirs.Values
                .Select(s => (Souvenir: s, IsOwned: IsOwned(s.SouvenirID)))
                .ToList();
        }

        #endregion

        #region 觸發與廣播機制

        /// <summary>
        /// 註冊所有紀念品事件 (通常在每局遊戲開始時呼叫)
        /// 這邊可以依照持有狀態，僅註冊已持有的紀念品
        /// </summary>
        public void RegisterAll()
        {
            ForEachOwnedSouvenir<SpecialSouvenirBase>(souvenir => souvenir.Register());
            Debug.Log("[SouvenirManager] 已註冊所持有的紀念品事件");
        }

        /// <summary>
        /// 取消註冊所有紀念品事件 (通常在每局遊戲結束時呼叫)
        /// </summary>
        public void UnregisterAll()
        {
            foreach (var souvenir in _specialSouvenirs.Values)
            {
                souvenir.Unregister();
            }
            Debug.Log("[SouvenirManager] 已取消註冊所有紀念品事件");
        }

        /// <summary>
        /// 觸發所有實作 IApplyStartEffect 的開局效果 (僅限已持有的有作用)
        /// </summary>
        public void ApplyAllStartEffects()
        {
            ForEachOwnedSouvenir<IApplyStartEffect>(startEffect => startEffect.ApplyStartEffect());
            Debug.Log("[SouvenirManager] 已觸發所有持有的 IApplyStartEffect 開局效果");
        }

        /// <summary>
        /// 廣播商店折扣計算，讓實作 IShopDiscountProvider 的紀念品修改貨架商品售價
        /// </summary>
        public void ApplyAllShopDiscounts(string shopId, List<Shop.ShelfSlot> items)
        {
            if (items == null || items.Count == 0) return;
            ForEachOwnedSouvenir<IShopDiscountProvider>(discountProvider => discountProvider.ApplyShopDiscount(shopId, items));
        }

        /// <summary>
        /// 廣播商店購買事件，讓實作 IShopPurchaseListener 的紀念品處理如買十送一等計數
        /// </summary>
        public void NotifyItemPurchased(string shopId, string itemId, int amount)
        {
            ForEachOwnedSouvenir<IShopPurchaseListener>(purchaseListener => purchaseListener.OnItemPurchased(shopId, itemId, amount));
        }

        /// <summary>
        /// 建立商店視覺資訊列表，讓實作 IShopVisualModifier 的紀念品填入折扣標籤等視覺資料
        /// </summary>
        public System.Collections.Generic.List<ShelfSlotVisualInfo> BuildShopVisualInfos(
            string shopId,
            System.Collections.Generic.List<Shop.ShelfSlot> items)
        {
            var visualInfos = new System.Collections.Generic.List<ShelfSlotVisualInfo>();
            if (items == null) return visualInfos;
            foreach (var slot in items)
            {
                var info = new ShelfSlotVisualInfo { SlotIndex = slot.SlotIndex };
                visualInfos.Add(info);
                slot.VisualInfo = info;
            }
            
            // 讓所有符合條件且玩家已持有的紀念品填入視覺資訊
            ForEachOwnedSouvenir<IShopVisualModifier>(vm => vm.ModifyVisual(shopId, visualInfos));
            
            return visualInfos;
        }

        /// <summary>
        /// 廣播妖怪交易完成事件，讓實作 IMonsterTradeListener 的紀念品給予額外獎勵
        /// </summary>
        public void NotifyMonsterTradeCompleted(TradeSatisfaction satisfaction)
        {
            ForEachOwnedSouvenir<IMonsterTradeListener>(listener => listener.OnTradeCompleted(satisfaction));
        }

        /// <summary>
        /// 廣播每日效果，讓實作 IDailyEffect 的紀念品在換日時執行
        /// </summary>
        public void ApplyAllDailyEffects()
        {
            ForEachOwnedSouvenir<IDailyEffect>(daily => daily.ApplyDailyEffect());
            Debug.Log("[SouvenirManager] 已觸發所有每日效果");
        }

        /// <summary>
        /// 查詢玩家是否擁有讓刮刮樂免費的紀念品
        /// </summary>
        public bool IsScratchCardFree()
        {
            bool isFree = false;
            // IFreeScratchCardProvider 查詢
            ForEachOwnedSouvenir<IFreeScratchCardProvider>(provider => 
            {
                if (provider.IsScratchCardFree())
                {
                    isFree = true;
                }
            });
            return isFree;
        }

        #endregion

        protected override void OnDestroy()
        {
            UnregisterAll();
            _achievementSouvenirs.Clear();
            _specialSouvenirs.Clear();
            _ownedSouvenirIds.Clear();
            _isInitialized = false;

            base.OnDestroy();
        }
    }
}
