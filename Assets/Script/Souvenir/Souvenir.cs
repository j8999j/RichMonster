using System;
using System.Collections.Generic;
using Newtonsoft.Json;
#region ViewInterface
/// <summary>
/// 商品格子的視覺資訊，由紀念品填入，由 UI 讀取顯示
/// </summary>
public class ShelfSlotVisualInfo
{
    public int SlotIndex;
    /// <summary> 折扣標籤文字（空字串表示不顯示） </summary>
    public string DiscountLabel = "";
    /// <summary> 折扣前的原始售價（-1 表示不顯示刪除線） </summary>
    public int OriginalPrice = -1;
    /// <summary> 是否標示為「每日特價」 </summary>
    public bool IsDailySpecial = false;
}
public class ISouvenirBagView
{
    public string SouvenirName;
    public string SouvenirDescription;
    public string EffectName;
}
#endregion
namespace Souvenir
{
    // 移除舊的 SouvenirEvent，因為目前統一使用 SouvenirManager 直接廣播

    #region SouvenirEffectInterface
    /// <summary> 每局開始時立即套用的一次性效果介面（例如：增加初始金幣） </summary>
    public interface IApplyStartEffect
    {
        void ApplyStartEffect();
    }
    /// <summary> 遊戲中資料變更時觸發的效果介面（例如：增加背包空間） </summary>
    public interface IApplyPlayerDataEffect
    {
        void ApplyPlayerDataEffect();
    }

    /// <summary> 紀念品具有切換人界與妖界的能力介面 </summary>
    public interface IWorldTravelProvider
    {
        bool CanTravelWorld();
    }

    /// <summary> 商店打折提供者介面（讓紀念品修改貨架商品售價） </summary>
    public interface IShopDiscountProvider
    {
        void ApplyShopDiscount(string shopId, System.Collections.Generic.List<Shop.ShelfSlot> items);
    }

    /// <summary> 商店購買商品時的監聽介面（例如：每買10項送贈品） </summary>
    public interface IShopPurchaseListener
    {
        void OnItemPurchased(string shopId, string itemId, int amount);
    }

    /// <summary> 與妖怪交易完成時的監聽介面（例如：交易滿意時給額外獎勵） </summary>
    public interface IMonsterTradeListener
    {
        void OnTradeCompleted(TradeSatisfaction satisfaction);
    }

    /// <summary> 與妖怪交易完成時的監聽介面（包含顧客種族資訊，供各族感謝紀念品使用） </summary>
    public interface IMonsterTradeWithRaceListener
    {
        void OnTradeCompletedWithRace(TradeSatisfaction satisfaction, string race);
    }

    /// <summary> 與妖怪交易失敗（Hated）時的監聽介面（供累積失敗紀念品使用） </summary>
    public interface IMonsterTradeFailedListener
    {
        void OnTradeFailed(string race);
    }

    /// <summary> 每日重置或是每日觸發的效果介面（例如：每日免費刮刮樂、每日商品打折） </summary>
    public interface IDailyEffect
    {
        void ApplyDailyEffect();
    }

    /// <summary> 特殊紀念品的顯示資訊介面，供 UI 取得文字與進度 </summary>
    public interface ISpecialSouvenirDisplayInfo
    {
        string DisplayName { get; }
        string DisplayCondition { get; }
        string DisplayDescription { get; }
        string ProgressText { get; }
        float ProgressRatio { get; }
    }

    /// <summary> 特殊紀念品進度跨局存檔介面，同時滿足 IAchievementWithProgress 使其可直接流入成就 UI 管線 </summary>
    public interface ISpecialSouvenirSave : ISpecialSouvenirDisplayInfo, IAchievementWithProgress
    {
        string SouvenirID { get; }
        new bool IsCompleted { get; set; }
        void Initialize();
    }

    /// <summary>
    /// 紀念品視覺修改介面：讓紀念品在商品生成後對 ShelfSlotVisualInfo 設定視覺資訊（例如折扣標籤）
    /// 視覺系統只需讀取 ShelfSlotVisualInfo.DiscountTag 等欄位顯示 UI，不需要知道紀念品存在
    /// </summary>
    public interface IShopVisualModifier
    {
        void ModifyVisual(string shopId, System.Collections.Generic.List<ShelfSlotVisualInfo> visualInfos);
    }

    /// <summary>
    /// 提供刮刮樂免費的效果介面
    /// </summary>
    public interface IFreeScratchCardProvider
    {
        bool IsScratchCardFree();
    }

    /// <summary>
    /// 紀念品 UI 互動介面：決定在 UI 上是否有按鈕以及按下去的功能（如：查看進度、使用）
    /// </summary>
    public interface ISouvenirInteractive
    {
        bool HasInteraction { get; }
        string InteractionButtonText { get; }
        void OnInteraction();
    }

    /// <summary>
    /// 提供額外背包容量的效果介面
    /// </summary>
    public interface IBagCapacityProvider
    {
        int GetExtraCapacity();
    }
    #endregion

    #region SouvenirClass

    /// <summary>
    /// 成就紀念品基類：有觸發效果
    /// </summary>
    public abstract class AchievementSouvenirBase : ISouvenirBagView
    {
        public abstract string SouvenirID { get; }

        /// <summary>
        /// 購買此紀念品所需的成就點數
        /// 預設從存檔資料 Achievements_Souvenir.json 獲取設定的點數
        /// </summary>
        public virtual int Cost
        {
            get
            {
                if (DataManager.Instance != null &&
                    DataManager.Instance.AchievementSouvenirDict != null &&
                    DataManager.Instance.AchievementSouvenirDict.TryGetValue(SouvenirID, out var data))
                {
                    return data.PointsFee;
                }
                return 0;
            }
        }
    }
    /// <summary>
    /// 特殊紀念品基底：遊戲中按特殊條件觸發收集，可能有額外效果
    /// 宣告實作 IAchievementWithProgress，透過 explicit 橋接讓子類別不必重複實作顯示合約
    /// </summary>
    public abstract class SpecialSouvenirBase : ISouvenirBagView
    {
        public abstract string SouvenirID { get; }
        public bool IsCollected { get; private set; }

        // 供橋接使用的抽象顯示屬性（concrete class 已實作，無需修改）
        public abstract string DisplayName { get; }
        public abstract string DisplayCondition { get; }
        public abstract string DisplayDescription { get; }
        public abstract string ProgressText { get; }
        public abstract float ProgressRatio { get; }
        public abstract bool IsCompleted { get; set; }

        /// <summary> 遊戲開始時訂閱事件，用來監聽達成條件 </summary>
        public virtual void Register() { }

        /// <summary> 遊戲結束時取消訂閱 </summary>
        public virtual void Unregister() { }
        /// <summary> 子類別在條件達成時呼叫此方法 </summary>
        protected void TryCollect()
        {
            if (IsCollected) return;
            IsCollected = true;
            OnCollected();
        }
        /// <summary> 收集完成後的回呼（例如：播放動畫、存檔） </summary>
        protected virtual void OnCollected()
        {
            // UnityEvent 通知 UI 等...
        }
    }
    /// <summary>
    /// 預設持有紀念品基底：玩家一開始就擁有，不顯示於成就頁面。
    /// 不實作 ISpecialSouvenirSave，因此 GetAllSpecialSouvenirSaves() 不會撈到。
    /// </summary>
    public abstract class DefaultOwnedSouvenirBase : SpecialSouvenirBase
    {
        public override bool IsCompleted { get => true; set { } }
        public override string DisplayName => SouvenirName;
        public override string DisplayCondition => "";
        public override string DisplayDescription => SouvenirDescription;
        public override string ProgressText => "";
        public override float ProgressRatio => 1f;

        protected DefaultOwnedSouvenirBase() : base()
        {
            TryCollect();
        }
    }

    #endregion
}