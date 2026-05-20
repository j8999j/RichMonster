using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 特殊紀念品：神族敬意
    /// 條件：交易使神族客人滿意（Satisfied 或 VerySatisfied）累計達到 50 次
    /// 進度跨局儲存，使用 IAchievementSave 機制寫入 illustrated_book.json
    /// </summary>
    [SouvenirDefinition("Sou_DivineTribute")]
    public class Sou_DivineTribute : SpecialSouvenir, IMonsterTradeWithRaceListener, ISpecialSouvenirSave, ISouvenirInteractive
    {
        public override string SouvenirID => "Sou_DivineTribute";

        // --- ISpecialSouvenirSave 欄位 (跨局存檔用) ---
        public override bool IsCompleted { get => IsCollected; set { } }

        // --- ISpecialSouvenirDisplayInfo 欄位 ---
        public override string DisplayName => "神族的餽贈";
        public override string DisplayCondition => "累計讓神族客人滿意 50 次";
        public override string DisplayDescription => "來自神族部落的感謝，見證了你的交易手腕。";
        public override string ProgressText => $"{SatisfiedCount}/{RequiredCount}";
        public override float ProgressRatio => UnityEngine.Mathf.Clamp01((float)SatisfiedCount / RequiredCount);

        /// <summary> 累計使神族客人滿意的次數 </summary>
        public int SatisfiedCount { get; set; }

        private const int RequiredCount = 50;
        private const string TargetRace = "神族";

        // --- IAchievementDisplayData explicit 橋接 ---
        string IAchievementDisplayData.AchievementName => DisplayName;
        string IAchievementDisplayData.ConditionDescription => DisplayCondition;
        string IAchievementDisplayData.Description => DisplayDescription;
        AchievementLevel IAchievementDisplayData.Level => AchievementLevel.Bronze;
        bool IAchievementDisplayData.IsCompleted => IsCompleted;
        string IAchievementDisplayData.IconId => SouvenirID;
        bool IAchievementDisplayData.IsIconGrayscale =>
            !IsCollected && !(SouvenirManager.Instance != null && SouvenirManager.Instance.IsOwned(SouvenirID));

        public Sou_DivineTribute() : base()
        {
            EffectName = "神諭將會引導你";
        }

        /// <summary> 從存檔中恢復進度 </summary>
        public void Initialize()
        {
            var saved = DataManager.Instance.GetSpecialSouvenirSaveData(SouvenirID) as Sou_DivineTribute;
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
            Debug.Log($"正在等待時機到來");
            // TODO: 若未來有統一的系統提示 UI，可以在此呼叫通知玩家的 API
            return true;
        }

        public bool CanShowInteractionButton() => true;

        #endregion
    }
}
