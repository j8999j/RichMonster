using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 旅行背包：背包容量上限增加 5 格
    /// </summary>
    [SouvenirDefinition("SouAch_TravelBag")]
    public class TravelBagSouvenir : AchievementSouvenir, IBagCapacityProvider
    {
        public override string SouvenirID => "SouAch_TravelBag";

        public int GetExtraCapacity()
        {
            return 5;
        }
    }
}
