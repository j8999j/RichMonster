using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 每日刮刮樂不需花費金幣購買。
    /// ScratchCardShop 在互動前呼叫 SouvenirManager.Instance.EvaluateScratchCardFree()
    /// 回傳 true 則跳過扣費直接開始刮。
    /// </summary>
    [SouvenirDefinition("SouAch_LotteryTicket")]
    public class FreeScratchCardSouvenir : AchievementSouvenir, ISouvenirPipelineHandler<ScratchCardPipelineContext>
    {
        public override string SouvenirID => "SouAch_LotteryTicket";

        public void Apply(ScratchCardPipelineContext context)
        {
            context.MarkFree();
        }
    }
}
