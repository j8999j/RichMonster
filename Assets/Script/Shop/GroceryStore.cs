using GameSystem;

namespace Shop
{
    public class GroceryStore : ShelfShopBase
    {
        protected override string LockSource => PlayerLockSources.GroceryStore;
        protected override GameCurrencyType CurrencyType => GameCurrencyType.Gold;
        protected override bool ApplyShopVisualInfo => true;
    }
}
