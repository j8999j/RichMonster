using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 特殊紀念品：破碎交易
    /// 條件：與妖怪交易失敗（Hated）累計達到 20 次
    /// 進度跨局儲存，使用 IAchievementSave 機制寫入 illustrated_book.json
    /// </summary>
    public class Sou_RuinedDeal : SpecialSouvenirBase, IMonsterTradeFailedListener, ISpecialSouvenirSave
    {
        public override string SouvenirID => "Sou_RuinedDeal";

        // --- ISpecialSouvenirSave 欄位 (跨局存檔用) ---
        public override bool IsCompleted { get => IsCollected; set { } }

        // --- ISpecialSouvenirDisplayInfo 欄位 ---
        public override string DisplayName => "破碎的交易";
        public override string DisplayCondition => "累計與妖怪交易失敗 20 次";
        public override string DisplayDescription => "你是不是應該檢討一下你的經營策略？";
        public override string ProgressText => $"{FailedCount}/{RequiredCount}";
        public override float ProgressRatio => UnityEngine.Mathf.Clamp01((float)FailedCount / RequiredCount);

        /// <summary> 累計與妖怪交易失敗的次數 </summary>
        public int FailedCount { get; set; }

        private const int RequiredCount = 20;

        // --- IAchievementDisplayData explicit 橋接 ---
        string IAchievementDisplayData.AchievementName => DisplayName;
        string IAchievementDisplayData.ConditionDescription => DisplayCondition;
        string IAchievementDisplayData.Description => DisplayDescription;
        AchievementLevel IAchievementDisplayData.Level => AchievementLevel.Bronze;
        bool IAchievementDisplayData.IsCompleted => IsCompleted;
        string IAchievementDisplayData.IconId => SouvenirID;
        bool IAchievementDisplayData.IsIconGrayscale =>
            !IsCollected && !(SouvenirManager.Instance != null && SouvenirManager.Instance.IsOwned(SouvenirID));

        public Sou_RuinedDeal() : base()
        {
            EffectName = "好多的客訴";
        }

        /// <summary> 從存檔中恢復進度 </summary>
        public void Initialize()
        {
            var saved = DataManager.Instance.GetSpecialSouvenirSaveData(SouvenirID) as Sou_RuinedDeal;
            if (saved != null)
            {
                FailedCount = saved.FailedCount;
                if (saved.IsCompleted) TryCollect();
            }
        }

        public override void Register()
        {
            Initialize();
        }

        public override void Unregister() { }

        public void OnTradeFailed(string race)
        {
            if (IsCollected) return;

            FailedCount++;
            DataManager.Instance.UpdateSpecialSouvenirSaveData(this);
            Debug.Log($"[Souvenir] {SouvenirID} 進度: {FailedCount}/{RequiredCount}（種族: {race}）");

            if (FailedCount >= RequiredCount)
            {
                TryCollect();
            }
        }

        protected override void OnCollected()
        {
            DataManager.Instance.UpdateSpecialSouvenirSaveData(this);

            var bookData = DataManager.Instance.GetBookData();
            if (bookData != null)
            {
                bookData.UnLockSpecialSouvenirID ??= new System.Collections.Generic.List<string>();
                if (!bookData.UnLockSpecialSouvenirID.Contains(SouvenirID))
                {
                    bookData.UnLockSpecialSouvenirID.Add(SouvenirID);
                    DataManager.Instance.SetBookDataChanged(true);
                    _ = DataManager.Instance.SaveBookAsync();
                }
            }
            Debug.Log($"[Souvenir] {SouvenirID} 已解鎖！（累計交易失敗 {FailedCount} 次）");
        }
    }
}
