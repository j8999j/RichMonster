using GameSystem;

namespace Shop
{
    public class FurnituresShop : ShelfShopBase
    {
        protected override string LockSource => PlayerLockSources.FurnituresShop;
        protected override GameCurrencyType CurrencyType => GameCurrencyType.Gold;
        protected override bool ApplyShopVisualInfo => true;
    }
}
