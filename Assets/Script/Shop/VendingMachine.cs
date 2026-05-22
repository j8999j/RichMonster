using GameSystem;

namespace Shop
{
    public class VendingMachine : ShelfShopBase
    {
        protected override string LockSource => PlayerLockSources.VendingMachine;
        protected override GameCurrencyType CurrencyType => GameCurrencyType.Gold;
        protected override bool ApplyShopVisualInfo => true;
    }
}
