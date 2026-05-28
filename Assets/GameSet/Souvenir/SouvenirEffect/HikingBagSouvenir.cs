using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 登山背包：背包容量上限增加 15 格
    /// </summary>
    [SouvenirDefinition("SouAch_HikingBag")]
    public class HikingBagSouvenir : AchievementSouvenir, ISouvenirPipelineHandler<BagCapacityPipelineContext>
    {
        public override string SouvenirID => "SouAch_HikingBag";

        public void Apply(BagCapacityPipelineContext context)
        {
            context.AddExtraCapacity(10);
        }
    }
}
