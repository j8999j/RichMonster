using GameSystem;
namespace Souvenir
{
    /// <summary>
    /// 預設持有特殊紀念品：黃昏鑰匙
    /// 玩家一開始就擁有，不顯示於成就頁面。
    /// </summary>
    public class Sou_key : DefaultOwnedSouvenirBase, ISouvenirInteractive
    {
        public override string SouvenirID => "Sou_key";

        public Sou_key() : base()
        {
            EffectName = "爺爺留下的神祕鑰匙";
        }

        public override void Register() { }
        public override void Unregister() { }
        #region ISouvenirInteractive 實作

        public bool HasInteraction => true;
        public string InteractionButtonText => "使用";

        public void OnInteraction()
        {
            if (DataManager.Instance.CurrentPlayerData.PlayingStatus == DayPhase.AfterNoon)
            {
                GameManager.Instance.gameFlow.NextDay();
                GameManager.Instance.GoToMonsterScene();
            }
            else
            {
                UnityEngine.Debug.Log("[Sou_key] 鑰匙只能在黃昏（AfterNoon）階段使用。");
            }
        }

        /// <summary>
        /// 黃昏鑰匙僅在黃昏（AfterNoon）階段顯示互動按鈕
        /// </summary>
        public bool CanShowInteractionButton()
        {
            return DataManager.Instance != null
                && DataManager.Instance.CurrentPlayerData != null
                && DataManager.Instance.CurrentPlayerData.PlayingStatus == DayPhase.AfterNoon;
        }
        #endregion
    }
}
