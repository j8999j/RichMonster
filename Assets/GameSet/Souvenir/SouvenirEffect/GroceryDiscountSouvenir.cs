using UnityEngine;

namespace Souvenir
{
    [SouvenirDefinition("SouAch_GroceryCoupon")]
    public class GroceryDiscountSouvenir : AchievementSouvenir, ISouvenirPipelineHandler<ShopShelfPipelineContext>
    {
        public override string SouvenirID => "SouAch_GroceryCoupon";

        public void Apply(ShopShelfPipelineContext context)
        {
            if (context.ShopId != ShopIDs.GroceryStore || context.Items == null || context.Items.Count == 0)
            {
                return;
            }

            var targetSlot = context.Items[0];
            if (targetSlot == null || targetSlot.Item == null) return;

            int originalPrice = targetSlot.Price;
            targetSlot.Price = Mathf.Max(0, Mathf.RoundToInt(targetSlot.Price * 0.8f));

            if (context.TryGetVisualInfo(targetSlot.SlotIndex, out var info))
            {
                info.DiscountLabel = "8折";
                info.OriginalPrice = originalPrice;
                info.IsDailySpecial = true;
                info.AddEffect(new ShelfSlotEffectVisual
                {
                    Kind = ShelfSlotEffectVisualKind.Discount,
                    SourceId = SouvenirID,
                    Label = "8折",
                    Tooltip = "雜貨店紀念品折扣",
                    OriginalPrice = originalPrice,
                    Priority = 100
                });
            }
        }
    }
}
