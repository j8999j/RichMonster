using System.Collections.Generic;
using UnityEngine;
using Shop;

namespace Souvenir
{
    /// <summary>
    /// 雜貨店每日第一件物品獲得 8 折折扣
    /// </summary>
    public class GroceryDiscountSouvenir : AchievementSouvenir, IShopDiscountProvider, IShopVisualModifier
    {
        public override string SouvenirID => "SouAch_GroceryCoupon";

        // ── 邏輯層：修改售價 ──────────────────────────────────────
        public void ApplyShopDiscount(string shopId, List<ShelfSlot> items)
        {
            if (shopId == ShopIDs.GroceryStore && items != null && items.Count > 0)
            {
                var targetSlot = items[0];
                if (targetSlot != null && targetSlot.Item != null)
                {
                    targetSlot.Price = Mathf.Max(0, Mathf.RoundToInt(targetSlot.Price * 0.8f));
                }
            }
        }

        // ── 視覺層：填入 VisualInfo 供 UI 讀取 ──────────────────────
        public void ModifyVisual(string shopId, List<ShelfSlotVisualInfo> visualInfos)
        {
            if (shopId != ShopIDs.GroceryStore || visualInfos == null || visualInfos.Count == 0) return;
            var info = visualInfos[0]; // 固定第一格
            info.DiscountLabel = "8折";
            info.IsDailySpecial = true;
            // OriginalPrice 在 ApplyShopDiscount 執行前還是原價，這裡由 UI 直接從 Item.BasePrice 取得
        }
    }
}
