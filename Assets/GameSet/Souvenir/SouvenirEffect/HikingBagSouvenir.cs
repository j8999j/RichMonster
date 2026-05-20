using UnityEngine;

namespace Souvenir
{
    /// <summary>
    /// 登山背包：背包容量上限增加 15 格
    /// </summary>
    [SouvenirDefinition("SouAch_HikingBag")]
    public class HikingBagSouvenir : AchievementSouvenir, IBagCapacityProvider
    {
        public override string SouvenirID => "SouAch_HikingBag";

        public int GetExtraCapacity()
        {
            return 10;
        }
    }
}
