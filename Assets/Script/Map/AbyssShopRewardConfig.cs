using UnityEngine;
using System.Collections.Generic;

namespace GameSystem
{
    public enum AbyssRewardType
    {
        MonsterGold,
        Item
    }

    [System.Serializable]
    public class AbyssRewardItem
    {
        public AbyssRewardType RewardType;
        
        [Tooltip("妖怪幣數量")]
        public int GoldAmount = 100;

        [Tooltip("物品ID")]
        [ItemIDSelect]
        public string ItemID;

        [Tooltip("獲得的物品數量")]
        public int ItemAmount = 1;

        [Tooltip("抽選權重 (越大越容易抽中)")]
        public int Weight = 1;
    }

    [System.Serializable]
    public class AbyssLayerRewardPool
    {
        [Tooltip("設定該層數 (例如 1~5)")]
        public int Layer;

        [Tooltip("該層可能出現的獎勵清單")]
        public List<AbyssRewardItem> Rewards = new List<AbyssRewardItem>();
    }

    [CreateAssetMenu(fileName = "NewAbyssShopRewardConfig", menuName = "Map/AbyssShopRewardConfig")]
    public class AbyssShopRewardConfig : ScriptableObject
    {
        [Tooltip("設定各層的獎勵掉落池")]
        public List<AbyssLayerRewardPool> LayerPools = new List<AbyssLayerRewardPool>();

        /// <summary>
        /// 根據層數取得對應的獎勵池
        /// </summary>
        public AbyssLayerRewardPool GetPoolForLayer(int layer)
        {
            return LayerPools.Find(p => p.Layer == layer);
        }
    }
}
