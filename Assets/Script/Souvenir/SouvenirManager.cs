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
    public class SouvenirManager : Singleton<SouvenirManager>, ISpecialSouvenirProvider
    {
        // 存放所有成就紀念品實例
        private Dictionary<string, AchievementSouvenir> _achievementSouvenirs
            = new Dictionary<string, AchievementSouvenir>();

        // 存放所有特殊紀念品實例
        private Dictionary<string, SpecialSouvenir> _specialSouvenirs
            = new Dictionary<string, SpecialSouvenir>();

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

            // 1. 處理 AchievementSouvenir
            var achSouvenirTypes = FindAllSouvenirTypes<AchievementSouvenir>();
            Debug.Log($"[SouvenirManager] 找到 {achSouvenirTypes.Count} 個成就紀念品腳本類別");

            foreach (var type in achSouvenirTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as AchievementSouvenir;
                    if (instance != null && !string.IsNullOrEmpty(instance.SouvenirID))
                    {
                        if (_achievementSouvenirs.ContainsKey(instance.SouvenirID))
                        {
                            Debug.LogWarning($"[SouvenirManager] 重複的 SouvenirID '{instance.SouvenirID}'，類別: {type.Name}，將覆蓋先前的類別");
                        }
                        // 從 DataManager 填充 ISouvenirBagView 顯示欄位
                        if (DataManager.Instance.AchievementSouvenirDict != null
                            && DataManager.Instance.AchievementSouvenirDict.TryGetValue(instance.SouvenirID, out var achData))
                        {
                            instance.SouvenirName = achData.SouvenirName;
                            instance.SouvenirDescription = achData.SouvenirDescription;
                            instance.EffectName = achData.SouvenirFunctionDescription;
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

            // 2. 處理 SpecialSouvenir
            var splSouvenirTypes = FindAllSouvenirTypes<SpecialSouvenir>();
            Debug.Log($"[SouvenirManager] 找到 {splSouvenirTypes.Count} 個特殊紀念品腳本類別");

            foreach (var type in splSouvenirTypes)
            {
                try
                {
                    // 注意: 衍生類別需要提供無參數建構子
                    var instance = Activator.CreateInstance(type) as SpecialSouvenir;
                    if (instance != null && !string.IsNullOrEmpty(instance.SouvenirID))
                    {
                        if (_specialSouvenirs.ContainsKey(instance.SouvenirID))
                        {
                            Debug.LogWarning($"[SouvenirManager] 重複的 SouvenirID '{instance.SouvenirID}'，類別: {type.Name}，將覆蓋先前的類別");
                        }
                        // 從 DataManager 填充 ISouvenirBagView 顯示欄位
                        if (DataManager.Instance.SpecialSouvenirDict != null
                            && DataManager.Instance.SpecialSouvenirDict.TryGetValue(instance.SouvenirID, out var splData))
                        {
                            instance.SouvenirName = splData.SouvenirName;
                            instance.SouvenirDescription = splData.SouvenirDescription;
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
            SnapshotOwnedSouvenirs();
            RegisterAll();
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
        public AchievementSouvenir GetAchievementSouvenir(string souvenirId)
        {
            _achievementSouvenirs.TryGetValue(souvenirId, out var souvenir);
            return souvenir;
        }

        /// <summary>
        /// 取得所有的成就紀念品
        /// </summary>
        public List<AchievementSouvenir> GetAllAchievementSouvenirs()
        {
            return _achievementSouvenirs.Values.ToList();
        }

        public IReadOnlyList<ISpecialSouvenirSave> GetAllSpecialSouvenirSaves()
        {
            return _specialSouvenirs.Values.OfType<ISpecialSouvenirSave>().ToList();
        }

        /// <summary>
        /// 取得指定的特殊紀念品
        /// </summary>
        public SpecialSouvenir GetSpecialSouvenir(string souvenirId)
        {
            _specialSouvenirs.TryGetValue(souvenirId, out var souvenir);
            return souvenir;
        }

        /// <summary>
        /// 取得所有的特殊紀念品
        /// </summary>
        public List<SpecialSouvenir> GetAllSpecialSouvenirs()
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
        /// 判斷指定成就紀念品是否已在圖鑑存檔（Book）中解鎖（玩家歷史購買紀錄）。
        /// 商店 UI 顯示與購買前檢查使用此方法，語意為「已用點數兌換過」。
        /// </summary>
        public bool IsPurchased(string souvenirId)
        {
            var bookData = DataManager.Instance.GetBookData();
            return bookData?.UnLockAchievementSouvenirID != null
                && bookData.UnLockAchievementSouvenirID.Contains(souvenirId);
        }

        /// <summary>
        /// 在每局遊戲開始時呼叫，從存檔載入已持有的紀念品清單。
        /// 成就紀念品：讀取 PlayerData.HoldAchievementSouvenirID（本局存檔建立當下的靜態快照）。
        /// 特殊紀念品：讀取 BookData.UnLockSpecialSouvenirID（跨局 Book 存檔）。
        /// </summary>
        public void SnapshotOwnedSouvenirs()
        {
            _ownedSouvenirIds.Clear();
            var bookData = DataManager.Instance.GetBookData();
            var playerData = DataManager.Instance.CurrentPlayerData;

            // 成就紀念品：以本局快照 HoldAchievementSouvenirID 為準
            if (playerData?.HoldAchievementSouvenirID != null)
            {
                foreach (var id in playerData.HoldAchievementSouvenirID)
                {
                    _ownedSouvenirIds.Add(id);
                }
            }

            if (bookData != null)
            {
                // 特殊紀念品：維持讀 Book 存檔
                if (bookData.UnLockSpecialSouvenirID != null)
                {
                    foreach (var id in bookData.UnLockSpecialSouvenirID)
                    {
                        _ownedSouvenirIds.Add(id);
                    }
                }

                // 檢查是否擁有預設的 Sou_key，沒有的話加入為第一項
                if (!_ownedSouvenirIds.Contains("Sou_key"))
                {
                    _ownedSouvenirIds.Add("Sou_key");
                    if (bookData.UnLockSpecialSouvenirID == null)
                        bookData.UnLockSpecialSouvenirID = new List<string>();

                    if (!bookData.UnLockSpecialSouvenirID.Contains("Sou_key"))
                    {
                        bookData.UnLockSpecialSouvenirID.Insert(0, "Sou_key");
                        DataManager.Instance.SetBookDataChanged(true);
                    }
                }

                // 自動加入所有預設持有的紀念品
                foreach (var kvp in _specialSouvenirs)
                {
                    if (kvp.Value is DefaultOwnedSouvenirBase && !_ownedSouvenirIds.Contains(kvp.Key))
                    {
                        _ownedSouvenirIds.Add(kvp.Key);
                        if (bookData.UnLockSpecialSouvenirID == null)
                            bookData.UnLockSpecialSouvenirID = new List<string>();
                        if (!bookData.UnLockSpecialSouvenirID.Contains(kvp.Key))
                        {
                            bookData.UnLockSpecialSouvenirID.Add(kvp.Key);
                            DataManager.Instance.SetBookDataChanged(true);
                        }
                    }
                }
            }
            Debug.Log($"[SouvenirManager] 已載入快照，目前持有 {_ownedSouvenirIds.Count} 個紀念品");
        }

        /// <summary>
        /// 重新以當前 PlayerData + BookData 快照並重訂閱事件。
        /// 在載入存檔 / 開新局後、進入場景前呼叫，確保成就紀念品效果符合本局 HoldAchievementSouvenirID。
        /// </summary>
        public void ResnapshotForCurrentGame()
        {
            if (!_isInitialized) return;
            UnregisterAll();
            SnapshotOwnedSouvenirs();
            RegisterAll();
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

        /// <summary>
        /// 對所有特殊紀念品（無論是否持有）執行指定動作。
        /// 用於進度計數廣播，讓未收集的紀念品也能累積條件進度。
        /// </summary>
        private void ForEachAllSpecialSouvenirs<T>(Action<T> action) where T : class
        {
            foreach (var souvenir in _specialSouvenirs.Values)
            {
                if (souvenir is T target)
                {
                    action(target);
                }
            }
        }

        #endregion

        #region 商店後端 API (Shop Backend API)

        /// <summary>
        /// 獲得的總成就點數 (以完成成就的等級計算)
        /// </summary>
        public int GetTotalAchievementPoints()
        {
            if (AchievementManager.Instance != null && AchievementManager.Instance.IsInitialized)
            {
                int totalPoints = 0;
                foreach (var ach in AchievementManager.Instance.GetCompletedAchievements())
                {
                    totalPoints += GetPointsForLevel(ach.Level);
                }
                return totalPoints;
            }
            return 0;
        }

        /// <summary>
        /// 依據成就等級獲取對應的成就點數
        /// </summary>
        private int GetPointsForLevel(AchievementLevel level)
        {
            switch (level)
            {
                case AchievementLevel.Bronze: return 100;
                case AchievementLevel.Silver: return 200; // 暫定
                case AchievementLevel.Gold: return 300;   // 暫定
                default: return 100;
            }
        }

        /// <summary>
        /// 已花費的成就點數
        /// </summary>
        public int GetSpentPoints()
        {
            int spent = 0;
            var bookData = DataManager.Instance.GetBookData();
            if (bookData?.UnLockAchievementSouvenirID == null)
                return spent;

            foreach (var id in bookData.UnLockAchievementSouvenirID)
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
            // 以 Book 存檔為準檢查重複購買（商店在單局外開啟，_ownedSouvenirIds 無法反映購買歷史）
            if (IsPurchased(souvenirId))
            {
                Debug.LogWarning($"[SouvenirShop] 購買失敗：已經擁有紀念品 {souvenirId}");
                return false;
            }

            if (_achievementSouvenirs.TryGetValue(souvenirId, out var ach))
            {
                int remainingPoints = GetRemainingPoints();
                if (remainingPoints >= ach.Cost)
                {
                    // 僅寫入 Book 存檔；_ownedSouvenirIds 由下一次開新局時的 ResnapshotForCurrentGame 接手
                    var bookData = DataManager.Instance.GetBookData();
                    if (bookData != null)
                    {
                        if (bookData.UnLockAchievementSouvenirID == null)
                            bookData.UnLockAchievementSouvenirID = new List<string>();

                        if (!bookData.UnLockAchievementSouvenirID.Contains(souvenirId))
                        {
                            bookData.UnLockAchievementSouvenirID.Add(souvenirId);
                            DataManager.Instance.SetBookDataChanged(true);
                        }
                    }

                    Debug.Log($"[SouvenirShop] 購買 {souvenirId} 成功（花費 {ach.Cost} 點），下次開新局時會被納入 HoldAchievementSouvenirID");
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
        public List<(AchievementSouvenir Souvenir, bool IsOwned)> GetShopCatalog()
        {
            return _achievementSouvenirs.Values
                .Select(s => (Souvenir: s, IsOwned: IsOwned(s.SouvenirID)))
                .ToList();
        }

        #endregion

        #region 觸發與廣播機制

        /// <summary>
        /// 註冊所有紀念品事件 (通常在每局遊戲開始時呼叫)。
        /// 已持有的紀念品呼叫 Register() 以套用功能效果；
        /// 尚未收集但需要跨局追蹤進度的特殊紀念品也會呼叫 Register() 以恢復計數。
        /// </summary>
        public void RegisterAll()
        {
            // 1. 已持有的特殊紀念品：觸發效果型事件訂閱
            ForEachOwnedSouvenir<SpecialSouvenir>(souvenir => souvenir.Register());

            // 2. 尚未收集的特殊紀念品：呼叫 Register() 以從存檔恢復進度計數
            foreach (var souvenir in _specialSouvenirs.Values)
            {
                if (!_ownedSouvenirIds.Contains(souvenir.SouvenirID))
                {
                    souvenir.Register();
                }
            }
            Debug.Log("[SouvenirManager] 已註冊所持有的紀念品事件，並初始化未收集紀念品的進度追蹤");
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
        public List<ShelfSlotVisualInfo> BuildShopVisualInfos(
            string shopId,
            List<Shop.ShelfSlot> items)
        {
            var visualInfos = new List<ShelfSlotVisualInfo>();
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
        /// 廣播妖怪交易完成事件（含種族資訊）。
        /// 使用 ForEachAllSpecialSouvenirs 以便尚未解鎖的進度型紀念品也能累積計數。
        /// </summary>
        public void NotifyMonsterTradeCompletedWithRace(TradeSatisfaction satisfaction, string race)
        {
            ForEachAllSpecialSouvenirs<IMonsterTradeWithRaceListener>(listener => listener.OnTradeCompletedWithRace(satisfaction, race));
        }

        /// <summary>
        /// 廣播妖怪交易失敗事件。
        /// 使用 ForEachAllSpecialSouvenirs 以便尚未解鎖的進度型紀念品也能累積計數。
        /// </summary>
        public void NotifyMonsterTradeFailed(string race)
        {
            ForEachAllSpecialSouvenirs<IMonsterTradeFailedListener>(listener => listener.OnTradeFailed(race));
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

        /// <summary>
        /// 查詢玩家目前擁有的紀念品提供的額外背包總容量
        /// </summary>
        public int GetExtraBagCapacity()
        {
            int extraCapacity = 0;
            ForEachOwnedSouvenir<IBagCapacityProvider>(provider =>
            {
                extraCapacity += provider.GetExtraCapacity();
            });
            return extraCapacity;
        }

        #endregion

        /// <summary>
        /// 重置紀念品系統，清除所有資料並允許重新初始化
        /// </summary>
        public void Reset()
        {
            UnregisterAll();
            _achievementSouvenirs.Clear();
            _specialSouvenirs.Clear();
            _ownedSouvenirIds.Clear();
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
