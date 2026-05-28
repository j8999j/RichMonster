using System.Collections.Generic;

#region ViewInterface
/// <summary>
/// 商品格子的紀念品視覺效果類型。
/// </summary>
public enum ShelfSlotEffectVisualKind
{
    Discount,
    DailySpecial,
    SouvenirEffect
}

/// <summary>
/// 單一紀念品提供給貨架 UI 的視覺提示。
/// </summary>
public class ShelfSlotEffectVisual
{
    public ShelfSlotEffectVisualKind Kind;
    public string SourceId = "";
    public string Label = "";
    public string IconId = "";
    public string Tooltip = "";
    public int OriginalPrice = -1;
    public int Priority;
}

/// <summary>
/// 商品格子的視覺資訊，由紀念品管線填入，再由商店 UI 讀取顯示。
/// </summary>
public class ShelfSlotVisualInfo
{
    public int SlotIndex;
    public List<ShelfSlotEffectVisual> Effects = new List<ShelfSlotEffectVisual>();

    /// <summary>舊欄位：保留給尚未改成 Effects 清單的 UI 或資料使用。</summary>
    public string DiscountLabel = "";
    /// <summary>舊欄位：保留給尚未改成 Effects 清單的 UI 或資料使用。</summary>
    public int OriginalPrice = -1;
    /// <summary>舊欄位：保留給尚未改成 Effects 清單的 UI 或資料使用。</summary>
    public bool IsDailySpecial = false;

    public bool HasEffects => Effects != null && Effects.Count > 0;

    public void AddEffect(ShelfSlotEffectVisual effect)
    {
        if (effect == null) return;
        Effects ??= new List<ShelfSlotEffectVisual>();
        Effects.Add(effect);
    }
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
    #region SouvenirEffectInterface
    /// <summary>每局開始時立即套用的一次性效果，例如增加初始金幣。</summary>
    public interface IApplyStartEffect
    {
        void ApplyStartEffect();
    }

    /// <summary>商店購買商品後的事件監聽，例如累積購買次數。</summary>
    public interface IShopPurchaseListener
    {
        void OnItemPurchased(string shopId, string itemId, int amount);
    }

    /// <summary>怪物交易完成後的事件監聽。</summary>
    public interface IMonsterTradeListener
    {
        void OnTradeCompleted(TradeSatisfaction satisfaction);
    }

    /// <summary>怪物交易完成後的事件監聽，包含顧客種族資訊。</summary>
    public interface IMonsterTradeWithRaceListener
    {
        void OnTradeCompletedWithRace(TradeSatisfaction satisfaction, string race);
    }

    /// <summary>怪物交易失敗後的事件監聽。</summary>
    public interface IMonsterTradeFailedListener
    {
        void OnTradeFailed(string race);
    }

    /// <summary>每日重置或每日觸發的效果。</summary>
    public interface IDailyEffect
    {
        void ApplyDailyEffect();
    }

    /// <summary>特殊紀念品的顯示資訊，供 UI 取得文字與進度。</summary>
    public interface ISpecialSouvenirDisplayInfo
    {
        string DisplayName { get; }
        string DisplayCondition { get; }
        string DisplayDescription { get; }
        string ProgressText { get; }
        float ProgressRatio { get; }
    }

    /// <summary>特殊紀念品進度跨局存檔介面，同時供成就 UI 管線讀取。</summary>
    public interface ISpecialSouvenirSave : ISpecialSouvenirDisplayInfo, IAchievementWithProgress
    {
        string SouvenirID { get; }
        new bool IsCompleted { get; set; }
        void Initialize();
    }

    /// <summary>紀念品 UI 互動介面，例如查看進度或使用紀念品。</summary>
    public interface ISouvenirInteractive
    {
        bool HasInteraction { get; }
        string InteractionButtonText { get; }
        bool OnInteraction();
        bool CanShowInteractionButton();
    }
    #endregion

    #region SouvenirClass
    /// <summary>所有紀念品的共同基類。</summary>
    public abstract class SouvenirBase : ISouvenirBagView
    {
        public abstract string SouvenirID { get; }
    }

    /// <summary>成就紀念品基類。</summary>
    public abstract class AchievementSouvenir : SouvenirBase
    {
        /// <summary>購買此紀念品所需的成就點數。</summary>
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

    /// <summary>特殊紀念品基底，通常由遊戲內事件累積進度並收集。</summary>
    public abstract class SpecialSouvenir : SouvenirBase
    {
        public bool IsCollected { get; private set; }

        public abstract string DisplayName { get; }
        public abstract string DisplayCondition { get; }
        public abstract string DisplayDescription { get; }
        public abstract string ProgressText { get; }
        public abstract float ProgressRatio { get; }
        public abstract bool IsCompleted { get; set; }

        /// <summary>遊戲開始或重新快照後初始化自身進度。</summary>
        public virtual void InitializeLifecycle() { }
        /// <summary>遊戲結束或重置時釋放自身狀態。</summary>
        public virtual void ReleaseLifecycle() { }

        /// <summary>子類別在條件達成時呼叫此方法完成收集。</summary>
        protected void TryCollect()
        {
            if (IsCollected) return;
            IsCollected = true;
            OnCollected();
        }

        /// <summary>收集完成後的回呼，例如存檔或通知 UI。</summary>
        protected virtual void OnCollected()
        {
        }
    }

    /// <summary>預設持有的特殊紀念品基底。</summary>
    public abstract class DefaultOwnedSouvenirBase : SpecialSouvenir
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
