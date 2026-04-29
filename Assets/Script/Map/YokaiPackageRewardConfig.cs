using UnityEngine;
using System.Collections.Generic;

namespace GameSystem
{
    [CreateAssetMenu(fileName = "NewYokaiPackageRewardConfig", menuName = "Map/YokaiPackageRewardConfig")]
    public class YokaiPackageRewardConfig : ScriptableObject
    {
        [Tooltip("妖怪包裹可能的獎勵清單（妖怪幣或物品），含抽選權重")]
        public List<AbyssRewardItem> Rewards = new List<AbyssRewardItem>();
    }
}
