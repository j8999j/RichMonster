using GameSystem;

namespace Shop
{
    public class HumanShopEat : ShelfShopBase
    {
        protected override string LockSource => PlayerLockSources.HumanShopEat;
        protected override GameCurrencyType CurrencyType => GameCurrencyType.Gold;
    }
}
