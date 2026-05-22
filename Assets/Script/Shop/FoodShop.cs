using GameSystem;

namespace Shop
{
    public class FoodShop : ShelfShopBase
    {
        protected override string LockSource => PlayerLockSources.FoodShop;
        protected override GameCurrencyType CurrencyType => GameCurrencyType.Gold;
        protected override bool ApplyShopVisualInfo => true;
    }
}
