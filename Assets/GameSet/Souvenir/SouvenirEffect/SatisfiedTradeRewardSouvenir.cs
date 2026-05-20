using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 與妖怪交易達到「滿意」或「非常滿意」時，額外獲得一筆妖怪金幣獎勵
    /// </summary>
    [SouvenirDefinition("SouAch_ExquisitePaper")]
    public class SatisfiedTradeRewardSouvenir : AchievementSouvenir, IMonsterTradeListener
    {
        public override string SouvenirID => "SouAch_ExquisitePaper";

        /// <summary> 滿意時的額外獎勵金幣 </summary>
        private const int SatisfiedBonus = 50;
        /// <summary> 非常滿意時的額外獎勵金幣 </summary>
        private const int VerySatisfiedBonus = 150;

        public void OnTradeCompleted(TradeSatisfaction satisfaction)
        {
            int bonus = satisfaction switch
            {
                TradeSatisfaction.Satisfied     => SatisfiedBonus,
                TradeSatisfaction.VerySatisfied => VerySatisfiedBonus,
                _                               => 0
            };

            if (bonus <= 0) return;

            DataManager.Instance.ModifyMonsterGold(bonus);
            Debug.Log($"[Souvenir] 交易滿意獎勵觸發！額外獲得 {bonus} 妖怪金幣 (滿意度: {satisfaction})");
        }
    }
}
