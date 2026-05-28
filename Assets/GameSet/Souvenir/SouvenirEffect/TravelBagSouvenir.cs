using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 旅行背包：背包容量上限增加 5 格
    /// </summary>
    [SouvenirDefinition("SouAch_TravelBag")]
    public class TravelBagSouvenir : AchievementSouvenir, ISouvenirPipelineHandler<BagCapacityPipelineContext>
    {
        public override string SouvenirID => "SouAch_TravelBag";

        public void Apply(BagCapacityPipelineContext context)
        {
            context.AddExtraCapacity(5);
        }
    }
}
