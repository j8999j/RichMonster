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
        public string InteractionButtonText =>
            IsReturnHomeState() ? "回家休息" : "使用";

        public bool OnInteraction()
        {
            var player = DataManager.Instance.CurrentPlayerData;
            if (player.PlayingStatus == DayPhase.AfterNoon)
            {
                PlayerInfoUIEvents.InvokeCloseAll(); // 關閉隨身包以解除玩家鎖定
                GoToMonsterWorld();
                return true;
            }
            else if (IsReturnHomeState())
            {
                PlayerInfoUIEvents.InvokeCloseAll();
                ReturnHome();
                return true;
            }
            else
            {
                UnityEngine.Debug.Log("[Sou_key] 目前階段無法使用鑰匙（黃昏可前往妖界；夜晚完成交易後可回家休息）。");
            }
            return false;
        }

        /// <summary>
        /// 黃昏階段可前往妖界；夜晚且已完成交易後可回家休息。
        /// </summary>
        public bool CanShowInteractionButton()
        {
            if (DataManager.Instance == null || DataManager.Instance.CurrentPlayerData == null)
                return false;
            var player = DataManager.Instance.CurrentPlayerData;
            return player.PlayingStatus == DayPhase.AfterNoon || IsReturnHomeState();
        }

        /// <summary>夜晚且已完成交易（IsTrade==true）。</summary>
        private static bool IsReturnHomeState()
        {
            var player = DataManager.Instance?.CurrentPlayerData;
            return player != null
                && player.PlayingStatus == DayPhase.Night
                && player.IsTrade;
        }

        private static async void GoToMonsterWorld()
        {
            await GameManager.Instance.gameFlow.NextDayAsync();
            GameManager.Instance.GoToMonsterScene();
        }

        private static async void ReturnHome()
        {
            await GameManager.Instance.gameFlow.SwitchGameStageAndSaveAsync(DayPhase.HumanDay);
            GameManager.Instance.GoToHumanScene();
        }
        #endregion
    }
}
