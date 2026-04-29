using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 特殊紀念品：妖精敬意
    /// 條件：交易使妖精客人滿意（Satisfied 或 VerySatisfied）累計達到 50 次
    /// 進度跨局儲存，使用 IAchievementSave 機制寫入 illustrated_book.json
    /// </summary>
    public class Sou_FairyTribute : SpecialSouvenir, IMonsterTradeWithRaceListener, ISpecialSouvenirSave, ISouvenirInteractive
    {
        public override string SouvenirID => "Sou_FairyTribute";

        // --- ISpecialSouvenirSave 欄位 (跨局存檔用) ---
        public override bool IsCompleted { get => IsCollected; set { } }

        // --- ISpecialSouvenirDisplayInfo 欄位 ---
        public override string DisplayName => "妖精的餽贈";
        public override string DisplayCondition => "累計讓妖精客人滿意 50 次";
        public override string DisplayDescription => "來自妖精部落的感謝，見證了你的交易手腕。";
        public override string ProgressText => $"{SatisfiedCount}/{RequiredCount}";
        public override float ProgressRatio => UnityEngine.Mathf.Clamp01((float)SatisfiedCount / RequiredCount);

        /// <summary> 累計使妖精客人滿意的次數 </summary>
        public int SatisfiedCount { get; set; }

        private const int RequiredCount = 50;
        private const string TargetRace = "妖精";

        // --- IAchievementDisplayData explicit 橋接 ---
        string IAchievementDisplayData.AchievementName => DisplayName;
        string IAchievementDisplayData.ConditionDescription => DisplayCondition;
        string IAchievementDisplayData.Description => DisplayDescription;
        AchievementLevel IAchievementDisplayData.Level => AchievementLevel.Bronze;
        bool IAchievementDisplayData.IsCompleted => IsCompleted;
        string IAchievementDisplayData.IconId => SouvenirID;
        bool IAchievementDisplayData.IsIconGrayscale =>
            !IsCollected && !(SouvenirManager.Instance != null && SouvenirManager.Instance.IsOwned(SouvenirID));
        public Sou_FairyTribute() : base()
        {
            EffectName = "花朵將會引導你";
        }

        /// <summary> 從存檔中恢復進度 </summary>
        public void Initialize()
        {
            var saved = DataManager.Instance.GetSpecialSouvenirSaveData(SouvenirID) as Sou_FairyTribute;
            if (saved != null)
            {
                SatisfiedCount = saved.SatisfiedCount;
                if (saved.IsCompleted) TryCollect();
            }
        }

        public override void Register()
        {
            Initialize();
        }

        public override void Unregister() { }

        public void OnTradeCompletedWithRace(TradeSatisfaction satisfaction, string race)
        {
            if (IsCollected) return;
            if (race != TargetRace) return;
            if (satisfaction == TradeSatisfaction.Satisfied || satisfaction == TradeSatisfaction.VerySatisfied)
            {
                SatisfiedCount++;
                DataManager.Instance.UpdateSpecialSouvenirSaveData(this);
                Debug.Log($"[Souvenir] {SouvenirID} 進度: {SatisfiedCount}/{RequiredCount}");

                if (SatisfiedCount >= RequiredCount)
                {
                    TryCollect();
                }
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
            Debug.Log($"[Souvenir] {SouvenirID} 已解鎖！");
        }
        #region ISouvenirInteractive 實作

        public bool HasInteraction => true;
        public string InteractionButtonText => "共鳴";

        public bool OnInteraction()
        {
            SystemInfoEvent.Show($"正在等候時機到來");
            return true;
        }

        public bool CanShowInteractionButton() => true;

        #endregion
    }

}
