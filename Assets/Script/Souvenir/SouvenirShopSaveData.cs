using System;
using System.Collections.Generic;

namespace Souvenir
{
    /// <summary>
    /// 紀念品商店的持久紀錄，包含已購買的紀念品清單
    /// 跨日不會被重置，跨局只要讀取此存檔即代表永久持有
    /// </summary>
    [Serializable]
    public class SouvenirShopSaveData : ISaveData
    {
        public string UniqueID { get; set; } = SaveDataKeys.SouvenirShop;
        public int LastUpdatedDay { get; set; }
        
        /// <summary>
        /// 玩家已解鎖(購買)的紀念品 ID 清單
        /// </summary>
        public List<string> PurchasedSouvenirIDs = new List<string>();
    }
}
